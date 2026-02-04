import React from "react";
import {
    IAverageDescriptor,
    Significance,
    ScorecardPerformanceCompetitorsMetricResult,
    Factory,
    WeightedDailyResult,
    ScorecardPerformanceResults,
    ScorecardPerformanceCompetitorDataResult,
    ScorecardPerformanceMetricResult,
    CalculationPeriodSpan,
    EntityInstance as ApiEntityInstance,
} from "../../BrandVueApi";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import moment from "moment";
import {Link, useLocation} from 'react-router-dom';
import * as PageHandler from "../PageHandler";
import ScorecardNextSteps, { willRenderNextStep } from "./ScorecardNextSteps";
import ScorecardCompetitorSet from "./ScorecardCompetitorSet";
import ScorecardKey, { scorecardAverageStyle } from "./ScorecardKey";
import ScorecardSignificance from "./ScorecardSignificance";
import { ChartFooterInformation } from "../visualisations/ChartFooterInformation";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { ViewHelper } from "../visualisations/ViewHelper";
import Tooltip from "../Tooltip";
import { getUrlForMetricOrPageDisplayName } from "../helpers/PagesHelper";
import { UnitOfTime } from "./ScorecardDatePicker";
import { getMakeUpToUnitOfTime } from './../helpers/PeriodHelper';
import { getSignificanceMeaning, SignificanceMeaning } from "../../metrics/metricHelper";
import { EntitySet } from "../../entity/EntitySet";
import { EntitySetAverage } from "../../entity/EntitySetAverage";
import { CuratedResultsModel, ScorecardPerformanceCompetitorResults } from "../../BrandVueApi";
import Throbber from "../throbber/Throbber";
import {useAppDispatch, useAppSelector} from "../../state/store";
import {setGenericAverageResults, setGenericResults} from "../../state/resultsSlice";
import { useReadVueQueryParams } from "../helpers/UrlHelper";
import { selectSubsetId } from "client/state/subsetSlice";
import { EntityInstance } from "client/entity/EntityInstance";
import { selectTimeSelection } from "../../state/timeSelectionStateSelectors";

interface IScoreCardPerformanceProps {
    title: string,
    height: number,
    curatedFilters: CuratedFilters,
    metrics: Metric[],
    mainInstance: EntityInstance,
    nextSteps: string,
    pageHandler: PageHandler.PageHandler,
    partId: number;
    entitySet: EntitySet;
    availableEntitySets: EntitySet[];
}



type CellData = { classNames: string, content: string, tooltip: NonNullable<React.ReactNode> };
type PeriodCellData = { deltaCell: CellData | undefined, valueCell: CellData };

const ScorecardPerformance = (props: IScoreCardPerformanceProps) => {
    let mainInstanceModelBeingLoaded: CuratedResultsModel | null = null;
    let competitorsModelBeingLoaded: CuratedResultsModel | null = null;
    const timeSelection = useAppSelector(selectTimeSelection);
    const [isMobile, setIsMobile] = React.useState<boolean>(false);
    const [isAverageLoaded, setIsAverageLoaded] = React.useState<boolean>(!!timeSelection.scorecardAverage);
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const location = useLocation();
    const dispatch = useAppDispatch();
    const readVueQueryParams = useReadVueQueryParams();
    const [mainInstanceResults, setMainInstanceResults] = React.useState<ScorecardPerformanceResults>(getEmptyScorecardPerformanceResults());
    const [competitorResults, setCompetitorResults] = React.useState<ScorecardPerformanceCompetitorResults>(getEmptyCompetitorBrandResult());
    const [averageResults, setAverageResults] = React.useState<({ average: EntitySetAverage, result: ScorecardPerformanceCompetitorResults })[]>(getEmptyAverageResults());
    const subsetId = useAppSelector(selectSubsetId);
    function getEmptyAverageResults(): ({ average: EntitySetAverage, result: ScorecardPerformanceCompetitorResults })[] {
        return [];
    }

    function getEmptyCompetitorBrandResult(): ScorecardPerformanceCompetitorResults {
        const competitorData = props.entitySet.getInstances().getAll().map(b => {
            const weightedDailyResult = new WeightedDailyResult();
            const entityInstance = new ApiEntityInstance({
                id: b.id,
                name: b.name,
                color: props.entitySet.getInstanceColor(b),
                enabledBySubset: {},
                startDateBySubset: {},
                fields: {},
                imageUrl: b.imageUrl,
            });
            const scorecardPerformance = new ScorecardPerformanceCompetitorDataResult({ entityInstance: entityInstance, result: weightedDailyResult });
            return scorecardPerformance;
        });
        const competitorDefault = props.metrics.map(m => new ScorecardPerformanceCompetitorsMetricResult({ metricName: m.name, competitorData: competitorData, competitorAverage: 0 }));
        const competitorBrandResult = new ScorecardPerformanceCompetitorResults();
        competitorBrandResult.metricResults = competitorDefault;
        return competitorBrandResult;
    }


    function calculatePeriods(scorecardAverage: IAverageDescriptor): Date[] {
        const pl: UnitOfTime = getUnitOfTimeBasedOnAverages(scorecardAverage);
        const np: Date[] = [];
        const startDate = props.curatedFilters.calcScorecardStartDate(scorecardAverage);

        if (startDate < props.curatedFilters.endDate) {
            let p = 0;
            while (true) {
                const dt = moment.utc(startDate).startOf(pl).add(p, pl).endOf(pl)
                    .startOf('D').toDate();
                if (dt > props.curatedFilters.endDate) {
                    break;
                }
                np.push(dt);
                p += 1;
            }
        }

        return np;
    }

    function getEmptyScorecardPerformanceResults(scorecardAverage?: IAverageDescriptor): ScorecardPerformanceResults {
        const periods =  isAverageLoaded && scorecardAverage ? calculatePeriods(scorecardAverage) : [];
        const periodResults = periods.map(p => {
            const weightedDailyResult = new WeightedDailyResult();
            weightedDailyResult.date = p;
            return weightedDailyResult;
        });
        const activeDefault = props.metrics.map(m => new ScorecardPerformanceMetricResult({ metricName: m.name, periodResults: periodResults }));
        const mainInstanceResults = new ScorecardPerformanceResults();
        mainInstanceResults.metricResults = activeDefault;
        return mainInstanceResults
    }

    React.useEffect(() => {
        checkIsMobile();
        window.addEventListener("resize", checkIsMobile);
        return () => {
            window.removeEventListener("resize", checkIsMobile);
        }
    }, [])

    React.useEffect(() => {
        load();
    }, [
        props.curatedFilters,
        props.metrics,
        props.mainInstance,
        JSON.stringify(props.entitySet),
        props.pageHandler,
        timeSelection.scorecardAverage
    ])

    function getUnitOfTimeBasedOnAverages(scorecardAverage: IAverageDescriptor): UnitOfTime {
        return getMakeUpToUnitOfTime(scorecardAverage.makeUpTo);
    }

    const checkIsMobile = () => {
        const screenWidth = window.innerWidth;
        setIsMobile(screenWidth < 700);
    };

    const isMainInstanceInGroup = (): boolean => {
        return props.entitySet.getInstances().getAll().some(value => value.id === props.mainInstance.id);
    }

    const requestAverageModelFromProps = (average: EntitySetAverage, scorecardAverage: IAverageDescriptor): CuratedResultsModel | null => {
        const averageModel = ViewHelper.createAverageRequestModelOrNull(
            average.getEntitySet(props.entitySet, props.availableEntitySets).getInstances().getAll().map(x => x.id),
            props.metrics,
            props.curatedFilters,
            props.mainInstance.id,
            { useScorecardDates: true },
            subsetId,
            timeSelection);
        const endOfLastPeriod = props.curatedFilters.endDate;
        const pl: UnitOfTime = getUnitOfTimeBasedOnAverages(scorecardAverage);
        const startOfLastPeriod = moment.utc(props.curatedFilters.endDate).startOf(pl).toDate();
        if (averageModel != null)
            averageModel.period.comparisonDates =
                [new CalculationPeriodSpan({ startDate: startOfLastPeriod, endDate: endOfLastPeriod })]; //Triples performance for monthly by picking single period rather than 3
        return averageModel;
    }


    const getAverages = () => {
        if (props.entitySet.getAverages().getAll().length) {
            var promises = props.entitySet.getAverages().getAll().map(async average => {
                var request = requestAverageModelFromProps(average, timeSelection.scorecardAverage)!;
                return await Factory.DataClient(err => err()).getScorecardPerformanceResultsAverage(request).then(r => {
                    return {
                        average: average,
                        request: request,
                        data: r
                    };
                });
            });
            Promise.all(promises).then((r) => {
                dispatch(setGenericAverageResults({ 
                    partId: props.partId, 
                    payload: r.map(response => ({ 
                        request: response.request, 
                        results: response.data, 
                        name: props.availableEntitySets.find(x=>response.average.entitySetId == x.id)!.name,
                        requested: new Date(Date.now())
                    }))
                }));
                setAverageResults(
                    r.map(a => ({ average: a.average, result: a.data })
                    ));
            });
        } else {
            setAverageResults(getEmptyAverageResults());
            dispatch(setGenericAverageResults({
                partId: props.partId,
                payload: []
            }));
        }
    }

    const hasEndDate = props.curatedFilters.endDate ? true : false;

    const load = () => {
        if (hasEndDate) {
            setIsLoading(true);
            setIsAverageLoaded(false);
            if (!timeSelection.scorecardAverage) {
                setIsLoading(false);
                return;
            }
            const requestModel = ViewHelper.createCuratedRequestModel([props.mainInstance.id],
                props.metrics,
                props.curatedFilters,
                props.mainInstance.id,
                { useScorecardDates: true },
                subsetId,
                timeSelection
            );

            mainInstanceModelBeingLoaded = requestModel;
            Factory.DataClient(throwErr => throwErr())
                .getScorecardPerformanceResults(
                    requestModel
                ).then(r => {
                    if (mainInstanceModelBeingLoaded === requestModel) {
                        mainInstanceModelBeingLoaded = null;
                        setMainInstanceResults(r);
                        setIsAverageLoaded(true);
                    }
                    dispatch(setGenericResults({ results: r, request: requestModel, partId: props.partId, averagesSelected: props.entitySet.getAverages().getAll().length, averages: [] }));
                });
            loadCompetitorInstances();
            getAverages();
            setIsLoading(false);
        }
    }

    const competitorInstances = (): EntityInstance[] => props.entitySet.getInstances().getAll();

    const loadCompetitorInstances = () => {

        const competitorsModel = ViewHelper.createAverageRequestModelOrNull(
            competitorInstances().map(b => b.id),
            props.metrics,
            props.curatedFilters,
            props.mainInstance.id,
            { useScorecardDates: true },
            subsetId,
            timeSelection);

        if (competitorsModel) {
            const endOfLastPeriod = props.curatedFilters.endDate;
            const pl: UnitOfTime = getUnitOfTimeBasedOnAverages(timeSelection.scorecardAverage);
            const startOfLastPeriod = moment.utc(props.curatedFilters.endDate).startOf(pl).toDate();
            competitorsModel.period.comparisonDates =
                [new CalculationPeriodSpan({ startDate: startOfLastPeriod, endDate: endOfLastPeriod })]; //Triples performance for monthly by picking single period rather than 3

            competitorsModelBeingLoaded = competitorsModel;
            Factory.DataClient(throwErr => throwErr())
                .getScorecardPerformanceResultsAverage(competitorsModel).then(r => {
                    if (competitorsModelBeingLoaded === competitorsModel) {
                        competitorsModelBeingLoaded = null;
                        setCompetitorResults(r);
                    }
                });
        } else {
            setCompetitorResults(getEmptyCompetitorBrandResult());
        }
    }


    const keyName = (metricResult: ScorecardPerformanceCompetitorsMetricResult, metric: Metric): string => {
        const minPeer = metricResult.competitorData.reduce((p, c) => c.result.weightedResult < p.result.weightedResult ? c : p, metricResult.competitorData[0]);
        let keyName = metric.numFormat;
        if (metric.isPercentage() && (minPeer.result.weightedResult < 0)) {
            keyName = keyName + "negative";
        }
        return keyName;
    }

    const calculateMinMax = (min: number, max: number) => {
        const aVerySmallNumber = 0.00000001;
        const order = Math.floor(Math.log10(max - aVerySmallNumber)) + 1;
        //if number between 0-10 then round to the nearest whole number,
        //otherwise round to nearest quarter eg 25, 50, 75 or 250, 500, 750 etc....
        const factor = order <= 1 ? 10 : 4;
        const multiples = Math.floor(Math.pow(10, order) / factor);
        if ((max % multiples) !== 0) {
            max = (Math.floor(max / multiples) + 1) * multiples;
        }

        min = min >= 0 ? 0 : (Math.floor(min / multiples)) * multiples;
        return { min: min || 0, max: max || 0 }
    }

    const getCSSClassForSignificance = (significance: Significance, metricDownIsGood: boolean): string => {
        const sigMeaning = getSignificanceMeaning(significance, metricDownIsGood);
        switch (sigMeaning) {
            case SignificanceMeaning.Neutral: return "sigNone";
            case SignificanceMeaning.Good: return "sigPositive";
            case SignificanceMeaning.Bad: return "sigNegative";
            default: throw Error("Unsupported significance meaning!");
        }
    }

    if (isLoading) {
        return (
            <div className="throbber-container-fixed">
                <Throbber />
            </div>
        );
    }
    if (!timeSelection.scorecardAverage) {
        return <div>No valid score card average</div>
    }
    const getMetricByName = (name: string): Metric => props.metrics.find(m => m.name === name) || props.metrics[0];
    const getFormattedResultForDate = (date: Date, results: WeightedDailyResult[]): WeightedDailyResult | undefined => {
        return results.find(r => moment.utc(r.date).startOf('D').toString() === moment.utc(date).startOf('D').toString());
    }
    const periods = calculatePeriods(timeSelection.scorecardAverage);
    const formatDeltaValue = (metric: string, value: number) => {
        var result = getMetricByName(metric).deltaFmt(value);
        return result;
    }

    const flattenedArray = ([] as ScorecardPerformanceCompetitorDataResult[]).concat(...competitorResults.metricResults.map(m => m.competitorData));
    var dateTitleForVsCompetitors = "(Loading...)";
    var dateForTooltip = "";

    if (flattenedArray.length > 0 && !competitorsModelBeingLoaded) {

        let initialValue = flattenedArray[0].result.date;
        for (let item of flattenedArray) {
            if (item.result.date > initialValue) {
                initialValue = item.result.date;
            };
        }       dateForTooltip = DateFormattingHelper.formatDatePoint(initialValue, timeSelection.scorecardAverage);
 
        dateTitleForVsCompetitors = "(" + dateForTooltip + ")";
        if (initialValue == null) {
            dateTitleForVsCompetitors = "(No data available)";
        }
    }
    const resultTypeKeyToLimits = {};
    competitorResults.metricResults.forEach(m => {
        let maxPeerValue = m.competitorData.reduce((p, c) => Math.max(p, c.result.weightedResult), Number.MIN_SAFE_INTEGER);
        let minPeerValue = m.competitorData.reduce((p, c) => Math.min(p, c.result.weightedResult), Number.MAX_VALUE);
        const metric = getMetricByName(m.metricName);

        if (metric.isPercentage()) {
            const result = calculateMinMax(minPeerValue * 100, maxPeerValue * 100);
            minPeerValue = result.min / 100;
            maxPeerValue = result.max / 100;
        } else {
            const result = calculateMinMax(minPeerValue, maxPeerValue);
            minPeerValue = result.min;
            maxPeerValue = result.max;
        }

        const key = keyName(m, metric);
        if (resultTypeKeyToLimits[key] === undefined) {
            resultTypeKeyToLimits[key] = { min: minPeerValue, max: maxPeerValue };
        } else {
            resultTypeKeyToLimits[key].min = Math.min(minPeerValue, resultTypeKeyToLimits[key].min);
            resultTypeKeyToLimits[key].max = Math.max(maxPeerValue, resultTypeKeyToLimits[key].max);
        }
    });

    const getNameColumn = (metricName: string, metricDownIsGood: boolean, significances: Significance[]) => {
        const minNumberOfColsToHaveSigAll = 2;
        const countSignificanceUp = significances.filter(x => x === Significance.Up).length;
        const countSignificanceDown = significances.filter(x => x === Significance.Down).length;
        const allPeriodsAreIncreasing = periods.length > minNumberOfColsToHaveSigAll && countSignificanceUp === periods.length - 1;
        const allPeriodsAreDecreasing = periods.length > minNumberOfColsToHaveSigAll && countSignificanceDown === periods.length - 1;
        const increaseClass = metricDownIsGood ? "negative" : "positive";
        const decreaseClass = metricDownIsGood ? "positive" : "negative";

        const significanceClass = allPeriodsAreIncreasing ? `sigUpAll ${increaseClass}` :
            allPeriodsAreDecreasing ? `sigDownAll ${decreaseClass}` :
                '';

        return (
            <div className="name-cell no-gutters">
                <div className="col-auto">
                    <Link to={{
                        pathname: getUrlForMetricOrPageDisplayName(metricName, location, readVueQueryParams, { ignoreQuery: true }),
                        search: location.search
                    }}>{metricName}</Link>
                </div>
                <div className={significanceClass + " col-auto"} />
            </div>
        );
    };

    const generateCellData = (mainInstanceMetricResult: ScorecardPerformanceMetricResult): { periodCellData: PeriodCellData[], significances: Significance[] } => {
        let oldValue: WeightedDailyResult | undefined = undefined;

        const periodCellData: PeriodCellData[] = [];

        const significances: Significance[] = [];
        const metricName = mainInstanceMetricResult.metricName;
        for (let currentPeriod = 0; currentPeriod < periods.length; currentPeriod++) {

            const currentValue = getFormattedResultForDate(periods[currentPeriod], mainInstanceMetricResult.periodResults);
            const metric = getMetricByName(metricName);

            let delta = Number.NaN;
            let significance = Significance.None;

            if (oldValue != undefined && currentValue != undefined) {

                delta = currentValue.weightedResult - oldValue.weightedResult;
                significance = currentValue.significance!;
                significances.push(significance);
            }

            let deltaCell: CellData | undefined = undefined;

            if (currentPeriod > 0) {

                const fromDate = moment.utc(periods[currentPeriod]).add(-1, getUnitOfTimeBasedOnAverages(timeSelection.scorecardAverage)).toDate();
                const deltaToolTipHtml: React.ReactNode = (
                    <div className="brandvue-tooltip">
                        <div className="tooltip-header">Change in {metric.varCode}</div>
                        <div className="tooltip-label">From</div><div className="tooltip-value">{DateFormattingHelper.formatDatePoint(fromDate, timeSelection.scorecardAverage)}</div>
                        <div className="tooltip-label">To</div><div className="tooltip-value">{DateFormattingHelper.formatDatePoint(periods[currentPeriod], timeSelection.scorecardAverage)}</div>
                        <div className="tooltip-label">Change</div><div className="tooltip-value">{metric.deltaFmt(delta)}</div>
                    </div>
                );

                deltaCell = {
                    classNames: 'deltaColumn text-end ' + getCSSClassForSignificance(significance, metric.downIsGood),
                    content: formatDeltaValue(metricName, delta),
                    tooltip: isAverageLoaded || delta > 0 ? deltaToolTipHtml : "",
                };
            }

            const sampleSize = currentValue !== undefined ? currentValue.unweightedSampleSize : 0;
            const valueToolTipHtml: React.ReactNode = (
                <div className="brandvue-tooltip">
                    <div className="tooltip-header">{metric.varCode} - {DateFormattingHelper.formatDatePoint(periods[currentPeriod], timeSelection.scorecardAverage)}</div>
                    <div className="tooltip-label">{props.mainInstance.name}</div><div className="tooltip-value">{(sampleSize === 0 || currentValue === undefined) ? "-" : metric.fmt(currentValue.weightedResult)}</div>
                    <div className="tooltip-label">N</div><div className="tooltip-value">{sampleSize === 0 ? "No data" : sampleSize}</div>
                </div>
            );

            if (currentValue !== undefined) {
                oldValue = currentValue;
            }

            periodCellData.push({
                deltaCell: deltaCell,
                valueCell: {
                    classNames: "valueColumn text-left",
                    content: getMetricByName(metricName).fmt(currentValue !== undefined ? currentValue.weightedResult : undefined),
                    tooltip: isAverageLoaded || (currentValue && currentValue.weightedResult > 0) ? valueToolTipHtml : "",
                }
            });
        }

        return { periodCellData, significances };
    };

    const getScorecardFooter = () => {
        return <>
            <div className="scorecardFooter not-exported mt-4">
                {willRenderNextStep(props.nextSteps) &&
                    <ScorecardNextSteps nextSteps={props.nextSteps} />
                }
                <ScorecardCompetitorSet mainInstance={props.mainInstance}
                    instanceGroup={props.entitySet.getInstances()} title={"Competitors"} />
                <div className="scorecardSeparator" />
                <ScorecardKey mainInstance={props.mainInstance} averages={props.entitySet.getAverages().getAll().map(x => x.getEntitySet(props.entitySet, props.availableEntitySets))} />
                <div className="scorecardSeparator" />
                <ScorecardSignificance metricResults={props.metrics} />
            </div>
            <ChartFooterInformation sampleSizeMeta={mainInstanceResults.sampleSizeMetadata}
                activeBrand={props.mainInstance} metrics={props.metrics}
                average={timeSelection.scorecardAverage} />
        </>;
    };

    const getDesktopContent = () => {
        return (
            <div className="scorecardControl scorecardControl--performance">
                <div className="table-overflow">
                    <table className="scorecard-table">
                        <thead>
                            <tr>
                                <th />
                                {periods.map((p, i) => {
                                    const periodDataIsLoaded = getFormattedResultForDate(p, mainInstanceResults.metricResults[0].periodResults) || isAverageLoaded;
                                    return (
                                        <th colSpan={i < periods.length - 1 ? 2 : 1} key={p.getTime()}>
                                            <span> {periodDataIsLoaded ? DateFormattingHelper.formatDatePoint(p, timeSelection.scorecardAverage) : "Loading..."}</span>
                                        </th>
                                    );
                                })}
                                <th className="text-center">
                                    <span>vs Competitors </span><span>{dateTitleForVsCompetitors}</span>
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            {mainInstanceResults.metricResults.map((mainInstanceMetricResults, i) => {
                                const metricName = mainInstanceMetricResults.metricName;
                                const metricCompetitors = competitorResults.metricResults[i];
                                const metricAverages = averageResults.map(x => ({
                                    average: x.average.getEntitySet(props.entitySet, props.availableEntitySets),
                                    metric: x.result.metricResults[i]
                                }));
                                const cellData = generateCellData(mainInstanceMetricResults);
                                const metric = getMetricByName(metricName);

                                return (
                                    <tr key={metricName}>
                                        <td className={"name-cell-parent"}>
                                            {getNameColumn(metricName, metric.downIsGood, cellData.significances)}
                                        </td>
                                        {cellData.periodCellData.map((p, i) => {
                                            const deltaCell = p.deltaCell;
                                            const valueCell = p.valueCell;

                                            return (
                                                <React.Fragment key={i}>
                                                    {deltaCell &&
                                                        <Tooltip placement="top" title={deltaCell.tooltip} >
                                                            <td className={deltaCell.classNames}>{deltaCell.content}</td>
                                                        </Tooltip>}
                                                    <Tooltip placement="top" title={valueCell.tooltip}>
                                                        <td className={valueCell.classNames}>{valueCell.content}</td>
                                                    </Tooltip>
                                                </React.Fragment>
                                            );
                                        })}
                                        <td>
                                            {metricCompetitors && !competitorsModelBeingLoaded && <VsCompetitorsTableBarChart mainInstance={props.mainInstance} metricName={metricName}
                                                includeMainInstance={isMainInstanceInGroup()}
                                                forPeriod={dateForTooltip}
                                                current={mainInstanceMetricResults.periodResults.length ? mainInstanceMetricResults.periodResults[mainInstanceMetricResults.periodResults.length - 1].weightedResult : 1}
                                                averages={metricAverages}
                                                competitorData={metricCompetitors.competitorData}
                                                upperBoundary={resultTypeKeyToLimits[keyName(metricCompetitors, metric)].max}
                                                lowerBoundary={resultTypeKeyToLimits[keyName(metricCompetitors, metric)].min}
                                                metric={metric} />}
                                        </td>
                                    </tr>
                                );
                            })
                            }
                        </tbody>
                    </table>
                </div>
                {getScorecardFooter()}
            </div>
        );
    };

    const getMobileContent = () => {
        return (
            <div className="scorecardControl scorecardControl--performance">
                <div className="table-overflow">
                    {
                        mainInstanceResults.metricResults.map((mainInstanceMetricResults, i) => {
                            const metricAverages = averageResults.map(x => ({
                                average: x.average.getEntitySet(props.entitySet, props.availableEntitySets),
                                metric: x.result.metricResults[i]
                            }));
                            const metricCompetitors = competitorResults.metricResults[i];
                            const cellData = generateCellData(mainInstanceMetricResults);
                            const metricName = mainInstanceMetricResults.metricName;
                            const metric = getMetricByName(metricName);
                            return (
                                <table className="scorecard-table" key={metricName}>
                                    <thead>
                                        <tr>
                                            <th colSpan={2}>
                                                {getNameColumn(metricName, metric.downIsGood, cellData.significances)}
                                            </th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {periods.map((p, i) => {

                                            const dataForPeriod = cellData.periodCellData[i];
                                            const deltaCell = dataForPeriod.deltaCell;
                                            const valueCell = dataForPeriod.valueCell;

                                            const periodDataIsLoaded = getFormattedResultForDate(p, mainInstanceResults.metricResults[0].periodResults) || isAverageLoaded;

                                            return (
                                                <tr key={i}>
                                                    <td className="date-mobile">
                                                        <span> {periodDataIsLoaded ? DateFormattingHelper.formatDatePoint(p, timeSelection.scorecardAverage) : "Loading..."} </span>
                                                    </td>
                                                    <td className='text-end'>
                                                        {deltaCell &&
                                                            <Tooltip title={deltaCell.tooltip} >
                                                                <div className={deltaCell.classNames + ' mobile-cells'}>
                                                                    {deltaCell.content}
                                                                </div>
                                                            </Tooltip>}
                                                        <Tooltip title={valueCell.tooltip}>
                                                            <div className={valueCell.classNames + ' mobile-cells'}>
                                                                {valueCell.content}
                                                            </div>
                                                        </Tooltip>
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                        <tr>
                                            <td colSpan={2}>
                                                <div className='vs-competitors-mobile'>vs
                                                    Competitors {dateTitleForVsCompetitors}</div>
                                                {metricCompetitors && !competitorsModelBeingLoaded &&
                                                    <VsCompetitorsTableBarChart mainInstance={props.mainInstance} metricName={metricName}
                                                        includeMainInstance={isMainInstanceInGroup()}
                                                        forPeriod={dateForTooltip}
                                                        current={mainInstanceMetricResults.periodResults.length ? mainInstanceMetricResults.periodResults[mainInstanceMetricResults.periodResults.length - 1].weightedResult : 1}
                                                        averages={metricAverages}
                                                        competitorData={metricCompetitors.competitorData}
                                                        upperBoundary={resultTypeKeyToLimits[keyName(metricCompetitors, metric)].max}
                                                        lowerBoundary={resultTypeKeyToLimits[keyName(metricCompetitors, metric)].min}
                                                        metric={metric} />}
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            )
                        })
                    }
                </div>
                {getScorecardFooter()}
            </div>
        );
    };

    if (isMobile) {
        return getMobileContent();
    } else {
        return getDesktopContent();
    }
}

export default ScorecardPerformance;

interface IVsCompetitorsTableBarChartProps {
    mainInstance: EntityInstance;
    includeMainInstance: boolean;
    metricName: string;
    forPeriod: string;
    current: number;
    averages: { average: EntitySet, metric: ScorecardPerformanceCompetitorsMetricResult }[];
    competitorData: ScorecardPerformanceCompetitorDataResult[];
    upperBoundary: number;
    lowerBoundary: number;
    metric: Metric;
}

const VsCompetitorsTableBarChart: React.FunctionComponent<IVsCompetitorsTableBarChartProps> = (props: IVsCompetitorsTableBarChartProps) => {
    const competitorData = props.competitorData.filter(d => props.includeMainInstance || d.entityInstance.id !== props.mainInstance.id);

    const maxPeer = competitorData.reduce((p, c) => c.result.weightedResult > p.result.weightedResult ? c : p, competitorData[0]);
    const minPeer = competitorData.reduce((p, c) => c.result.weightedResult < p.result.weightedResult ? c : p, competitorData[0]);

    const lowerBoundary = minPeer.result.weightedResult < props.lowerBoundary ? minPeer.result.weightedResult : props.lowerBoundary;
    const upperBoundary = maxPeer.result.weightedResult > props.upperBoundary ? maxPeer.result.weightedResult : props.upperBoundary;

    let minText = props.metric.fmt(lowerBoundary);
    let maxText = props.metric.fmt(upperBoundary);

    if (minText === maxText) {
        minText = props.metric.longFmt(lowerBoundary);
        maxText = props.metric.longFmt(upperBoundary);
    }

    const calcPercentage = (v: number) => {
        const val = ((v - lowerBoundary) / (upperBoundary - lowerBoundary)) * 100;
        if (isNaN(val) || !isFinite(val)) {
            return 1;
        }

        return val;
    };

    const barStyle = {
        marginLeft: calcPercentage(minPeer.result.weightedResult) + "%",
        marginRight: (100 - calcPercentage(maxPeer.result.weightedResult)) + "%",
    };

    const averageStyle = (average: number, i: number, averageCount: number): React.CSSProperties => {
        const styles = scorecardAverageStyle(average, i, averageCount);
        return {
            marginLeft: calcPercentage(average) - 1 + "%",
            ...styles,
        }
    };

    const currentValueStyle = {
        marginLeft: Math.max(0, calcPercentage(props.current) - 1) + "%"
    };

    const getCompetitorAverageTooltip = (metricResult: ScorecardPerformanceCompetitorsMetricResult, average: EntitySet) => {
        return <><div className="tooltip-label">{average.name}</div><div className="tooltip-value">{props.metric.fmt(metricResult.competitorAverage)}</div></>
    }

    const toolTipHtml = (
        <div className="brandvue-tooltip">
            <div className="tooltip-header">{props.metricName} - {props.forPeriod}</div>
            <div className="tooltip-label">{props.mainInstance.name}</div><div className="tooltip-value">{props.metric.fmt(props.current)}</div>
            {props.averages?.map(x => getCompetitorAverageTooltip(x.metric, x.average))}
            <div className="tooltip-label">Top - {maxPeer.entityInstance.name}</div><div className="tooltip-value">{props.metric.fmt(maxPeer.result.weightedResult)}</div>
            <div className="tooltip-label">Bottom - {minPeer.entityInstance.name}</div><div className="tooltip-value">{props.metric.fmt(minPeer.result.weightedResult)}</div>
        </div>);
    var averageCount = props.averages?.length ?? 0;
    return (
        <div className='minMaxCell'>
            <div className="col-auto text-center scorecard-bar-number">{minText}</div>
            <div className="align-middle mt-1 minMaxBar" >
                <Tooltip title={toolTipHtml} placement="top">
                    <div className="scorecard-bar">
                        <div className="rangebar" style={barStyle} />
                        {
                            props.averages?.map((x, i) => <div className="averagebar" style={averageStyle(x.metric.competitorAverage, i, averageCount)} />)
                        }
                        <div className="currentvaluebar" style={currentValueStyle} />
                    </div>
                </Tooltip>
            </div>
            <div className="col-auto text-center scorecard-bar-number">{maxText}</div>
        </div>
    );
}
