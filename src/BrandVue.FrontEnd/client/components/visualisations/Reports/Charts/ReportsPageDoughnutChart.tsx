import { CuratedFilters } from '../../../../filter/CuratedFilters';
import React from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import Highcharts, { Options } from 'highcharts';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { PageCardState } from '../../shared/SharedEnums';
import { getCompetitionResults } from './ChartData/CompetitionResultsDataHandler';
import { getDoughnutChartOptions, getLegendMapForIncludedInstances } from './HighchartsOptions/DoughnutOptions';
import { AverageType, BaseExpressionDefinition, CrosstabAverageResults, DisplaySignificanceDifferences, EntityWeightedDailyResults, GroupedVariableDefinition, MainQuestionType, SampleSizeMetadata } from '../../../../BrandVueApi';
import ReportsPageColumnChartTemplate from './ReportsPageColumnChartTemplate';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { getReverseLegendMap } from './ReportsChartHelper';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { NoDataError } from '../../../../NoDataError';
import { selectHydratedVariableConfiguration } from '../../../../state/variableConfigurationSelectors';
import { useAppSelector } from "../../../../state/store";
import { selectCurrentReport } from 'client/state/reportSelectors';
import { OverlapError } from './HighchartsOptions/CustomErrors';
import { useAppDispatch } from '../../../../state/store';
import { setReportErrorState, setIsSettingsChange } from '../../../../state/reportSlice';
import { ReportErrorState } from '../../shared/ReportErrorState';
import { getMeanCalculationValue } from '../../../../helpers/HighchartHelper';
import { selectSubsetId } from '../../../../state/subsetSlice';
import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageCardChartProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    order: BrandVueApi.ReportOrder;
    showTop: number | undefined;
    filterInstances: FilterInstance[];
    splitByType: BrandVueApi.IEntityType | undefined;
    baseExpressionOverride?: BaseExpressionDefinition;
    setDataState(state: PageCardState): void;
    setIsLowSample?(isLowSample: boolean): void;
    averageTypes: AverageType[];
    setAverageMentions(result: CrosstabAverageResults): void;
    updatePart(colours: string[]): void;
}

const ReportsPageDoughnutChart = (props: IReportsPageCardChartProps) => {
    const dispatch = useAppDispatch();

    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [sampleSizeMeta, setSampleSizeMeta] = React.useState<SampleSizeMetadata>();
    const [footerAverages, setFooterAverages] = React.useState<CrosstabAverageResults[] | undefined>();
    const [instanceToColourMap, setInstanceToColourMap] = React.useState<Map<string, string>>(new Map<string, string>());
    const [instanceNameToId, setInstanceNameToId] = React.useState<Map<string, number>>(new Map<string, number>());
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const getNameFromResult = (result: EntityWeightedDailyResults): string => {
        return result.entityInstance?.name ?? props.reportPart.metric!.displayName;
    }

    function getVariableDefinition(metricName: string): BrandVueApi.GroupedVariableDefinition | undefined {
        var variableDefinition = variables.find(v => v.displayName === metricName);
        return variableDefinition?.definition as BrandVueApi.GroupedVariableDefinition;
    }

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);

        const fetchData = async () => {
            try {
                const variableDefinition = await getVariableDefinition(props.reportPart.metric!.name);

                const d = await getCompetitionResults(
                    props.reportPart,
                    props.curatedFilters,
                    props.order,
                    undefined,
                    props.filterInstances,
                    props.averageTypes,
                    questionTypeLookup,
                    metrics,
                    variables,
                    subsetId,
                    timeSelection,
                    props.splitByType,
                    props.baseExpressionOverride
                );

                if (!isCancelled) {
                    if (props.setIsLowSample) {
                        props.setIsLowSample(d.results.lowSampleSummary.length > 0);
                    }

                    setFooterAverages(d.averages);

                    const { legendMap, storeColours } = getReverseLegendMap(
                        props.reportPart.part.colours,
                        d.results.periodResults[0].resultsPerEntity.map(r => r.entityInstance.name)
                    );

                    if (storeColours) {
                        const coloursArray = Array.from(legendMap).map(([key, value]) => `${key}:${value}`);
                        props.updatePart(coloursArray);
                    }

                    const results = d.results.periodResults[0].resultsPerEntity;
                    const options = getDoughnutChartOptions(
                        results,
                        props.reportPart.metric!,
                        legendMap,
                        report.decimalPlaces,
                        props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell,
                        report.highlightLowSample,
                        props.showTop,
                        metrics,
                        entityConfiguration,
                        props.reportPart.part.defaultSplitBy,
                        (a, b) => { },
                        props.order,
                        report.displaySignificanceDifferences,
                        variableDefinition
                    ) as Options;
                    const instanceNameToIdMap = props.reportPart.part.displayMeanValues
                        ? new Map(d.results.periodResults[0].resultsPerEntity.map(r => [getNameFromResult(r), getMeanCalculationValue(r.entityInstance, props.reportPart.metric!)]))
                        : new Map();

                    setInstanceNameToId(instanceNameToIdMap);
                    setInstanceToColourMap(getLegendMapForIncludedInstances(results, legendMap));
                    setChartOptions(options);
                    setSampleSizeMeta(d.results.sampleSizeMetadata);
                }
            } catch (e: any) {
                if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                    props.setDataState(PageCardState.NoData);
                } else {
                    if (e instanceof OverlapError) {
                        dispatch(setIsSettingsChange(false));
                        dispatch(setReportErrorState({ isError: true, errorMessage: e.message } as ReportErrorState));
                        props.setDataState(PageCardState.NotSupportedOverlap);
                    } else {
                        props.setDataState(PageCardState.Error);
                        throw e;
                    }
                }
            } finally {
                if (!isCancelled) {
                    setIsLoading(false);
                }
            }
        };

        fetchData();

        // Cleanup function to cancel any state updates if the component unmounts
        return () => {
            isCancelled = true;
        };
    }, [
        report.decimalPlaces,
        props.curatedFilters,
        props.order,
        props.showTop,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        report.highlightLowSample,
        props.averageTypes,
    ]);

    const chart = React.useRef<Highcharts.Chart>();

    return (
        <ReportsPageColumnChartTemplate
            isLoading={isLoading}
            chartOptions={chartOptions}
            chart={chart}
            sampleSizeMeta={sampleSizeMeta}
            metric={props.reportPart.metric!}
            questionTypeLookup={props.questionTypeLookup}
            filterInstanceNames={props.filterInstances.map(i => i.instance.name)}
            baseExpressionOverride={props.baseExpressionOverride}
            footerAverages={footerAverages}
            decimalPlaces={report.decimalPlaces}
            legendMap={instanceToColourMap}
            instanceNameToId={instanceNameToId}
        />
    );
};

export default ReportsPageDoughnutChart;