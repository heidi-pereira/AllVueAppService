import React from "react";
import { useEffect, useState } from "react";
import { AverageType, 
    BaseExpressionDefinition, 
    CrossMeasure, 
    CrosstabAverageResults, 
    CrosstabResults,
    CrosstabSignificanceType, 
    DisplaySignificanceDifferences, 
    MultiEntityOverTimeAverageResultsWithBreaksModel, 
    ReportOrder,
    SigConfidenceLevel } from "../../../BrandVueApi";
import Crosstab from "./Crosstab";
import * as BrandVueApi from "../../../BrandVueApi";
import { Metric } from "../../../metrics/metric";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import { EntitySet } from "../../../entity/EntitySet";
import Throbber from "../../throbber/Throbber";
import { EntityInstance } from "../../../entity/EntityInstance";
import Freshchat from "../../../freshchat";
import {PaginationData} from "../PaginationData";
import { getCrosstabRequestModel } from "./CrosstabHelper";
import { hasSingleEntityInstance } from "../../helpers/SurveyVueUtils";
import { useEntityConfigurationStateContext } from "../../../entity/EntityConfigurationStateContext";
import { getVerifiedAverageType } from "../AverageHelper";
import { useMetricStateContext } from "../../../metrics/MetricStateContext";
import * as actionTypes from '../../../metrics/metricsActionTypeConstants';
import { CuratedCrosstabResult, CurateResults } from "./ResultsCuration";
import { useAppSelector } from "../../../state/store";
import { selectHydratedVariableConfiguration } from '../../../state/variableConfigurationSelectors';
import { selectSubsetId } from "../../../state/subsetSlice";
import { selectTimeSelection } from "../../../state/timeSelectionStateSelectors";

interface IProps {
    metric: Metric | undefined;
    activeEntitySet: EntitySet | undefined;
    secondaryEntitySets: EntitySet[];
    curatedFilters: CuratedFilters;
    categories: CrossMeasure[];
    includeCounts: boolean;
    highlightLowSample: boolean;
    highlightSignificance: boolean;
    displaySignificanceDifferences: DisplaySignificanceDifferences;
    significanceType: CrosstabSignificanceType;
    resultSortingOrder: ReportOrder;
    decimalPlaces: number;
    hideEmptyRows: boolean;
    hideEmptyColumns: boolean;
    showTop: number | undefined;
    baseExpressionOverride?: BaseExpressionDefinition;
    setCanDownload(canDownload: boolean): void;
    isUserAdmin  : boolean;
    setIsLowSample(isLowSample: boolean): void;
    allMetrics: Metric[];
    isSurveyVue: boolean;
    setCanIncludeCounts(canIncludeCounts: boolean): void;
    isDataWeighted: boolean;
    currentPaginationData: PaginationData;
    focusInstance?: EntityInstance;
    averageTypes: AverageType[];
    displayMeanValues: boolean;
    splitBy?: BrandVueApi.IEntityType;
    sigConfidenceLevel: SigConfidenceLevel;
    hideTotalColumn: boolean;
    showMultipleTablesAsSingle: boolean;
    calculateIndexScores: boolean;
    lowSampleThreshold: number;
    displayStandardDeviation: boolean;
}

const CrosstabsContainer: React.FunctionComponent<IProps> = (props: IProps) => {
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { questionTypeLookup, metricsDispatch } = useMetricStateContext();
    const { variables, loading: isVariablesLoading, error: variablesLoadingError } = useAppSelector(selectHydratedVariableConfiguration);
    const timeSelection = useAppSelector(selectTimeSelection);

    // Per metric local state
    const [ isLoading, setIsLoading ] = useState<boolean>(false);
    const [ isError, setIsError ] = useState<boolean>(false);
    const [ errorMessage, setErrorMessage ] = useState<string>("");
    const [ crosstabResults, setCrosstabResults ] = useState<CuratedCrosstabResult[]>([]);
    const [ averageResults, setAverageResults ] = useState<CrosstabAverageResults[][]>([]);
    const [eligibleToShow, setEligibleToShow] = useState<boolean>(props.metric?.hasData ?? false);
    const [rawCrosstabResults, setRawCrosstabResults] = useState<CrosstabResults[]>([]);
    const subsetId = useAppSelector(selectSubsetId);

    // Local state retained as metric is changed
    const [ showResultsOfHiddenMetrics, setShowResultsOfHiddenMetrics ] = useState<boolean>(false);
    
    useEffect(() => {
        setEligibleToShow(props.metric?.hasData ?? false)
    }, [props.metric])

    useEffect(() => {
        if(crosstabResults){
            const uncuratedResults = crosstabResults.map(c => c.crosstabResult);
            const hasLowSample = hasLowSampleSize(uncuratedResults)
            props.setIsLowSample(hasLowSample);
        }
    }, [crosstabResults, props.lowSampleThreshold])

    const enabledAverages = hasSingleEntityInstance(props.metric, props.activeEntitySet?.getInstances().getAll().map(i => i.id)) ?
        [] : props.averageTypes;

    useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);
        setIsError(false);
        props.setCanDownload(false);

        if (props.metric && !isVariablesLoading && !variablesLoadingError) {
            const metricOfDesire = props.metric;
            metricOfDesire.defaultSplitByEntityTypeName = props.splitBy?.identifier ?? "";
            if (props.metric.disableMeasure && !showResultsOfHiddenMetrics) {
                setCrosstabResults([]);
                setIsLoading(false);
                return;
            }
            getCrosstabResults(metricOfDesire).then(data => {
                if (!isCancelled) {
                    setRawCrosstabResults(data.crosstabResult);

                    setAverageResults(data.average)
                    setIsLoading(false);
                    props.setCanDownload(true);
                    curateResults(data.crosstabResult);
                }
            }).catch(error => {
                if (!isCancelled) {
                    setCrosstabResults([]);
                    setIsLoading(false);
                    setIsError(true);
                    let errorMsg = error.message;

                    if (error.response) {
                        const json = JSON.parse(error.response);
                        if (json.error && json.error.message) {
                            const resultMessage: string = json.error.message;
                            if (!resultMessage.startsWith('Exception of type')) {
                                errorMsg = resultMessage;
                            }
                        }
                    }
                    setErrorMessage(errorMsg);
                }
            });
        }

        return () => {
            isCancelled = true;
        };

    }, [
        props.metric?.name,
        props.metric?.calcType,
        JSON.stringify(props.splitBy),
        JSON.stringify(props.activeEntitySet),
        JSON.stringify(props.secondaryEntitySets),
        props.curatedFilters.endDate.toString(),
        props.curatedFilters.average.averageId,
        JSON.stringify(props.curatedFilters.demographicFilter),
        JSON.stringify(props.curatedFilters.measureFilters),
        JSON.stringify(props.curatedFilters.compositeFilters),
        props.curatedFilters.comparisonPeriodSelection,
        props.categories.length,
        JSON.stringify(props.categories),
        props.significanceType,
        props.baseExpressionOverride?.baseType,
        props.baseExpressionOverride?.baseMeasureName,
        props.baseExpressionOverride?.baseVariableId,
        props.decimalPlaces,
        props.focusInstance,
        props.currentPaginationData.currentPageNo,
        JSON.stringify(enabledAverages),
        showResultsOfHiddenMetrics,
        props.isDataWeighted,
        props.displayMeanValues,
        eligibleToShow,
        isVariablesLoading,
        variables,
        props.sigConfidenceLevel,
        props.showMultipleTablesAsSingle,
        props.calculateIndexScores,
        props.highlightSignificance
    ]);

    useEffect(() => {
        curateResults(rawCrosstabResults);
    }, [
        rawCrosstabResults,
        props.resultSortingOrder,
        props.showTop,
        props.hideEmptyRows,
        JSON.stringify(props.splitBy),
        props.metric
    ]);

    function curateResults(rawResults: CrosstabResults[]) {
        if (props.metric) {
            let results = [...rawResults];
            const metricOfDesire = props.metric;
            metricOfDesire.defaultSplitByEntityTypeName = props.splitBy?.identifier ?? "";
            const crosstabResults = results.map(c => ({
                crosstabResult: c,
                ...CurateResults(c, metricOfDesire, props.showTop, props.hideEmptyRows, props.resultSortingOrder),
            }));
            setCrosstabResults(crosstabResults);
            if (crosstabResults.length > 0) {
                props.setCanIncludeCounts(crosstabResults.every(r => r.canIncludeCounts));
            }
        }
    }

    function hasLowSampleSize(data: CrosstabResults[]) {
        return data.some(d => d.instanceResults.some(
            r => Object.values(r.values).some(v => v.sampleSizeMetaData.sampleSize.unweighted <= props.lowSampleThreshold)));
    }

    async function getCrosstabResults(metric: Metric) : Promise<{crosstabResult: CrosstabResults[]; average: CrosstabAverageResults[][]}> {
        const dataClient = BrandVueApi.Factory.DataClient(throwError => throwError());
        const requestModel = getCrosstabRequestModel(metric,
            props.allMetrics,
            props.categories,
            props.activeEntitySet,
            props.secondaryEntitySets,
            props.curatedFilters,
            entityConfiguration,
            props.currentPaginationData,
            props.isSurveyVue,
            props.highlightSignificance,
            props.displaySignificanceDifferences,
            props.significanceType,
            props.isDataWeighted,
            props.hideEmptyColumns,
            props.focusInstance,
            props.baseExpressionOverride,
            subsetId,
            props.sigConfidenceLevel,
            props.showMultipleTablesAsSingle,
            timeSelection,
            props.calculateIndexScores
        );

        var crossTabResults =  await dataClient.crosstabResults(requestModel);
        if (enabledAverages.length > 0) {
            const allAverages = await Promise.all(
                enabledAverages.map(async average => {
                    const averageModel = new MultiEntityOverTimeAverageResultsWithBreaksModel(
                        {
                            averageType: getVerifiedAverageType(average,
                                metric,
                                questionTypeLookup,
                                props.isSurveyVue,
                                props.allMetrics,
                                variables),
                            model: requestModel});
                    return await dataClient.getAverageResultsWithBreaksMultiEntity(averageModel);
                })
            );

            return {crosstabResult: crossTabResults, average: allAverages.filter(av => av && av.length > 0)};
        }

        return {crosstabResult: crossTabResults, average: []};
    }

    async function updateShowMetricWithNoData(): Promise<void> {
        setEligibleToShow(true);
        return metricsDispatch({type: actionTypes.SET_ELIGIBLE_FOR_CROSSTAB_OR_ALLVUE, data: {metric: props.metric!, isEligible: true}});        
    }

    const showFreshChat = (e: any) => {
        e.preventDefault();
        Freshchat.GetOrCreateWidget().show({name: 'Report an error'});
    }

    const getWarningMessage = () => {
        if (isError) {
            let errorMessageToUser = `Error occurred loading crosstab data:
                ${errorMessage}
                If you are using saved breaks, one of your breaks may have had its name changed`;
            if (props.isDataWeighted) {
                errorMessageToUser +=`
                Or weighting may not be valid`
                }
            return (<>{errorMessageToUser}</>);
        } else if (props.metric === undefined){
            return (<>
                {`Data can't be displayed because no metrics have been enabled.
                If this problem persists, please `}<a href="#" onClick={showFreshChat} className="link">contact us</a>
            </>);
        }
    }

    if (props.metric === undefined || isError) {
        return (
            <div className="center-div">
                <div className="center-text">
                    {getWarningMessage()}
                </div>
            </div>);
    } else if (isLoading) {
        return (<div className="throbber-container"><Throbber /></div>);
    } else if (props.metric.disableMeasure && !showResultsOfHiddenMetrics) {
        return (

            <div className="center-div">
                <div className="center-text">
                    <p>By default results are not shown for disabled questions</p>
                    <button id="showHiddenData" className={`hollow-button`} onClick={() => setShowResultsOfHiddenMetrics(true)}>
                        <span>Temporarily show data</span>
                    </button>
                </div>
            </div>);
    } else if (!eligibleToShow && !showResultsOfHiddenMetrics && props.metric.generationType == BrandVueApi.AutoGenerationType.CreatedFromField) {
        return (
            <div className="center-div">
                <div className="center-text">
                    <p>By default, results are not shown for questions without response data</p>
                    <p>If responses are detected, the question will be enabled automatically</p>
                </div>
                <div className="flex">
                    <button id="showHiddenData" className={`hollow-button`} onClick={() => setShowResultsOfHiddenMetrics(true)}>
                        <span>Temporarily show data</span>
                    </button>
                    <button id="showHiddenData" className={`hollow-button`} onClick={() => updateShowMetricWithNoData()}>
                        <span>Always show data for this question</span>
                    </button>
                </div>
            </div>);
    } else {
        return (
            <>
                {crosstabResults.map((result, i) => {
                    const averagesForTable = averageResults.map(resultsForType => resultsForType[i]);
                    return (<Crosstab
                        key={`crosstab-${i}-${result.crosstabResult.categories.map(c => c.name).toString()}`}
                        metric={props.metric!}
                        results={result}
                        includeCounts={props.includeCounts}
                        highlightLowSample={props.highlightLowSample}
                        highlightSignificance={props.highlightSignificance}
                        displaySignificanceDifferences={props.displaySignificanceDifferences}
                        significanceType={props.significanceType}
                        resultSortingOrder={props.resultSortingOrder}
                        decimalPlaces={props.decimalPlaces}
                        hideEmptyRows={props.hideEmptyRows}
                        hideEmptyColumns={props.hideEmptyColumns}
                        showTop={props.showTop}
                        isUserAdmin={props.isUserAdmin}
                        allMetrics={props.allMetrics}
                        baseExpressionOverride={props.baseExpressionOverride}
                        isSurveyVue={props.isSurveyVue}
                        averageResults={averagesForTable}
                        displayMeanValues={props.displayMeanValues}
                        isDataWeighted={props.isDataWeighted}
                        hideTotalColumn={props.hideTotalColumn}
                        hasBreaksApplied={props.categories.length > 0}
                        lowSampleThreshold={props.lowSampleThreshold}
                        displayStandardDeviation={props.displayStandardDeviation}
                    />);
                })}
            </>
        );
    }
}
export default CrosstabsContainer;