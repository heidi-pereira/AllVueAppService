import React from "react";
import * as BrandVueApi from "../../BrandVueApi";
import { ComparisonPeriodSelection, IAverageDescriptor, RankingTableResult, RankingTableResults } from "../../BrandVueApi";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { EntityInstance } from "../../entity/EntityInstance";
import { Metric } from "../../metrics/metric";
import { ViewHelper } from "./ViewHelper";
import { ChartFooterInformation } from "./ChartFooterInformation";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { EntitySet } from "../../entity/EntitySet";
import { FilterInstance } from "../../entity/FilterInstance";
import Throbber from "../throbber/Throbber";
import Significance = BrandVueApi.Significance;
import { getMetricResultsSummaryFromRankingTableResult, MetricResultsSummary } from "../helpers/MetricInsightsHelper";
import {useAppDispatch, useAppSelector} from "../../state/store";
import {Dispatch} from "redux";
import {setGenericResults} from "../../state/resultsSlice";
import { selectSubsetId } from "../../state/subsetSlice";
import { ITimeSelectionOptions } from "../../state/ITimeSelectionOptions";
import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

interface IRankingTableProps {
    activeBrand: EntityInstance;
    curatedFilters: CuratedFilters;
    metric: Metric;
    entitySet: EntitySet;
    filterInstance?: FilterInstance;
    partId: number;
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void;
}

function getWeightedDailyResultSampleSizeTitle(rankIsValid: boolean, sampleSize: number): string {
    return rankIsValid ? NumberFormattingHelper.format0Dp(sampleSize) : "-";
}

function getWeightedDailyResultValue(rankIsValid: boolean, result: number, metric: Metric): string {
    return rankIsValid ? metric.longFmt(result) : "-";
}

function getIconChangeClass(result: RankingTableResult): string {
    const currentRankIsValid = result.currentWeightedDailyResult.unweightedSampleSize > 0;
    const previousRankIsValid = result.previousWeightedDailyResult.unweightedSampleSize > 0;

    if (!currentRankIsValid || !previousRankIsValid) {
        return "none";
    }
    if (result.currentRank === result.previousRank!) {
        return "same";
    }
    return result.currentRank < result.previousRank! ? "up" : "down";
}


function getEntityInstanceIds(props: IRankingTableProps): number[] {
    return props.entitySet.getInstances().getAll().map(entityInstance => entityInstance.id);
}

async function load(props: IRankingTableProps, dispatch: Dispatch, subsetId: string, timeSelectionOptions: ITimeSelectionOptions) {
    if (props.metric.entityCombination.length > 1) {
        return loadForMultipleEntities(props, dispatch, subsetId, timeSelectionOptions);
    } else {
        return loadForSingleEntity(props, dispatch, subsetId, timeSelectionOptions);
    }
}

async function loadForSingleEntity(props: IRankingTableProps, dispatch: Dispatch, subsetId: string, timeSelection: ITimeSelectionOptions) {
    const request = ViewHelper.createCuratedRequestModel(getEntityInstanceIds(props),
        [props.metric],
        props.curatedFilters,
        props.activeBrand.id,
        {},
        subsetId,
        timeSelection);
    const requestAsMultiEntity = ViewHelper.createMultiEntityRequestModel({
        curatedFilters: props.curatedFilters,
        metric: props.metric,
        splitBySet: props.entitySet,
        filterInstances: props.filterInstance ? [props.filterInstance] : [],
        continuousPeriod: false,
        subsetId: subsetId
    }, timeSelection);
    const results = await BrandVueApi.Factory.DataClient(err => err()).getRankedBrands(request);
    dispatch(setGenericResults({ results: results, request: requestAsMultiEntity, partId: props.partId, averages: [], averagesSelected: 0 }));
    props.updateMetricResultsSummary(getMetricResultsSummaryFromRankingTableResult(results.results, results.sampleSizeMetadata));
    return results;
}

async function loadForMultipleEntities(props: IRankingTableProps, dispatch: Dispatch, subsetId: string, timeSelection: ITimeSelectionOptions) {
    const request = ViewHelper.createMultiEntityRequestModel({
        curatedFilters: props.curatedFilters,
        metric: props.metric,
        splitBySet: props.entitySet,
        filterInstances: props.filterInstance ? [props.filterInstance] : [],
        continuousPeriod: false,
        subsetId: subsetId
    }, timeSelection);
    const results = await BrandVueApi.Factory.DataClient(err => err()).getRankingTableResults(request);
    dispatch(setGenericResults({ results: results, request: request, partId: props.partId, averages: [], averagesSelected: 0 }));
    return results;
}

function renderDesktopRow(result: RankingTableResult, includePreviousPeriod: boolean, props: IRankingTableProps, results: RankingTableResults) {
    const currentRankIsValid = result.currentWeightedDailyResult.unweightedSampleSize > 0;
    const previousRankIsValid = result.previousWeightedDailyResult.unweightedSampleSize > 0;
    const significance = result.currentWeightedDailyResult.significance!;
    const isActiveBrand = props.entitySet.type.isBrand && props.activeBrand.id === result.entityInstance.id;

    let significanceIconClass = "same";
    if (significance === Significance.Up) {
        significanceIconClass = `up ${props.metric.downIsGood ? "negative" : "positive"}`;
    } else if (significance === Significance.Down) {
        significanceIconClass = `down ${props.metric.downIsGood ? "positive" : "negative"}`;
    }

    return (
        <tr key={result.entityInstance.id} className={isActiveBrand ? "active" : ""}>
            <td className="brand-name">
                <div className="brand-color-circle" style={{ backgroundColor: props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(result.entityInstance)) }} />
                <span>{result.entityInstance.name}</span>
                {results.lowSampleSummary.map(x => x.entityInstanceId).indexOf(result.entityInstance.id) >= 0 ? <i className="lowSampleBrandWarning" /> : null}
            </td>
            <td />
            {includePreviousPeriod &&
                <td>{result.previousRank ? result.previousRank : "-"}</td>
            }
            <td>
                <div className="flex-container">
                    <span>{currentRankIsValid && result.multipleWithCurrentRank ? "=" : ""}{currentRankIsValid ? result.currentRank : "-"}</span>
                    {includePreviousPeriod &&
                        <i className={`material-symbols-outlined rank-change ${getIconChangeClass(result)}`} />
                    }
                </div>
            </td>
            <td />
            {includePreviousPeriod &&
                <td title={`n=${getWeightedDailyResultSampleSizeTitle(previousRankIsValid, result.previousWeightedDailyResult.unweightedSampleSize)}`}>
                    {getWeightedDailyResultValue(previousRankIsValid, result.previousWeightedDailyResult.weightedResult, props.metric)}
                </td>
            }
            <td title={`n=${getWeightedDailyResultSampleSizeTitle(currentRankIsValid, result.currentWeightedDailyResult.unweightedSampleSize)}`}>
                {getWeightedDailyResultValue(currentRankIsValid, result.currentWeightedDailyResult.weightedResult, props.metric)}
            </td>
            {includePreviousPeriod &&
                <td>
                    {significance ?
                        (
                            <i className={`material-symbols-outlined significance ${significanceIconClass}`} />
                        ) : "N/A"
                    }
                </td>
            }
            <td />
        </tr>
    );
}

function renderMobileRow(result: RankingTableResult, includePreviousPeriod: boolean, props: IRankingTableProps, results: RankingTableResults) {
    let currentRankIsValid = result.currentWeightedDailyResult.unweightedSampleSize > 0;
    let previousRankIsValid = result.previousWeightedDailyResult.unweightedSampleSize > 0;

    return (
        <tr key={result.entityInstance.id} className={props.activeBrand.id === result.entityInstance.id ? "active" : ""}>
            {includePreviousPeriod ?
                <>
                    <td className="mobile-arrow">
                        <i className={`material-symbols-outlined rank-change ${getIconChangeClass(result)}`} />
                    </td>
                    <td className="brand-name">
                        <div className="brand-color-circle" style={{ backgroundColor: props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(result.entityInstance)) }} />
                        <div className="brand-name-label">{result.entityInstance.name}</div>
                        {results.lowSampleSummary.map(x => x.entityInstanceId).indexOf(result.entityInstance.id) >= 0 ? <i className="lowSampleBrandWarning" /> : null}
                    </td>
                    <td title={`n=${getWeightedDailyResultSampleSizeTitle(previousRankIsValid, result.previousWeightedDailyResult.unweightedSampleSize)}`}>
                        {getWeightedDailyResultValue(previousRankIsValid, result.previousWeightedDailyResult.weightedResult, props.metric)}
                    </td>
                    <td title={`n=${getWeightedDailyResultSampleSizeTitle(currentRankIsValid, result.currentWeightedDailyResult.unweightedSampleSize)}`}>
                        {getWeightedDailyResultValue(currentRankIsValid, result.currentWeightedDailyResult.weightedResult, props.metric)}
                    </td>
                </>
                :
                <>
                    <td className="brand-name">
                        <div className="brand-color-circle" style={{ backgroundColor: props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(result.entityInstance)) }} />
                        <div className="brand-name-label">{result.entityInstance.name}</div>
                        {results.lowSampleSummary.map(x => x.entityInstanceId).indexOf(result.entityInstance.id) >= 0 ? <i className="lowSampleBrandWarning" /> : null}
                    </td>
                    <td>{currentRankIsValid && result.multipleWithCurrentRank ? "=" : ""}{currentRankIsValid ? result.currentRank : "-"}</td>
                    <td title={`n=${getWeightedDailyResultSampleSizeTitle(currentRankIsValid, result.currentWeightedDailyResult.unweightedSampleSize)}`}>
                        {getWeightedDailyResultValue(currentRankIsValid, result.currentWeightedDailyResult.weightedResult, props.metric)}
                    </td>
                </>
            }
        </tr>
    );
}

const RankingTable = (props: IRankingTableProps) => {
    const [isLoading, setIsLoading] = React.useState(true);
    const [isMobile, setIsMobile] = React.useState(false);
    const [results, setResults] = React.useState<RankingTableResults | null>(null);
    const dispatch = useAppDispatch();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const checkIsMobile = () => {
        setIsMobile(window.innerWidth < 700);
    };

    React.useEffect(() => {
        window.addEventListener("resize", checkIsMobile);

        return () => {
            window.removeEventListener("resize", checkIsMobile);
        }
    }, []);

    React.useEffect(() => {
        const fetchData = async () => {
            setIsLoading(true);
            const result = await load(props, dispatch, subsetId, timeSelection);
            setResults(result);
            setIsLoading(false);
        };

        fetchData();
    }, [JSON.stringify(props.entitySet), props.filterInstance, props.activeBrand, JSON.stringify(props.curatedFilters), timeSelection]);

    if (isLoading || !results) {
        return (
            <div className="throbber-container-fixed">
                <Throbber />
            </div>
        );
    }

    const includePreviousPeriod: boolean = props.curatedFilters.comparisonPeriodSelection !== ComparisonPeriodSelection.CurrentPeriodOnly;
    let currentDate = "";
    let previousDate = "";
    if (results.results.length) {
        const firstCurrentResult = results.results[0];
        currentDate = DateFormattingHelper.formatDateRange(firstCurrentResult.currentWeightedDailyResult.date, props.curatedFilters.average);
        if (includePreviousPeriod) {
            const resultWithDataInPreviousPeriod = results.results.find(currentResult => currentResult.previousWeightedDailyResult.unweightedSampleSize > 0);
            previousDate = resultWithDataInPreviousPeriod ? DateFormattingHelper.formatDateRange(resultWithDataInPreviousPeriod.previousWeightedDailyResult.date,
                props.curatedFilters.average) : "No previous data";
        }
    }
    const getDesktopContent = () => {
        return (
            <>
                <div>
                    <table className="rankingTable">
                        <thead>
                            <tr>
                                <th />
                                <th />
                                <th className="rankGroup" colSpan={includePreviousPeriod ? 2 : 1}>Ranking</th>
                                <th />
                                <th className="rankGroup" colSpan={includePreviousPeriod ? 3 : 1}>{props.metric.varCode} score</th>
                                <th />
                            </tr>
                            <tr>
                                <th className="display-name">{props.entitySet.type.displayNameSingular}</th>
                                <th />
                                {includePreviousPeriod &&
                                    <th>{previousDate}</th>
                                }
                                <th>{currentDate}</th>
                                <th />
                                {includePreviousPeriod &&
                                    <th>{previousDate}</th>
                                }
                                <th>{currentDate}</th>
                                {includePreviousPeriod &&
                                    <th><span className="significanceText">Significance</span></th>
                                }
                                <th />
                            </tr>
                        </thead>
                        <tbody>
                            {results.results.map(result => renderDesktopRow(result, includePreviousPeriod, props, results))}
                        </tbody>
                    </table>
                </div>
                <ChartFooterInformation sampleSizeMeta={results.sampleSizeMetadata} activeBrand={props.activeBrand} metrics={[props.metric]} average={props.curatedFilters.average} />
            </>
        );
    }

    const getMobileContent = () => {
        return (
            <div>
                <table className="rankingTable">
                    <thead>
                        <tr>
                            {includePreviousPeriod ?
                                <>
                                    <th />
                                    <th />
                                    <th className="rankGroup" colSpan={2}>Score</th>
                                </> :
                                <>
                                    <th />
                                    <th className="rankGroup" colSpan={2}>{currentDate}</th>
                                </>
                            }
                        </tr>
                        <tr>

                            {includePreviousPeriod ?
                                <>
                                    <th />
                                    <th className="display-name">{props.entitySet.type.displayNameSingular}</th>
                                    <th>{previousDate}</th>
                                    <th>{currentDate}</th>
                                </> :
                                <>
                                    <th className="display-name">{props.entitySet.type.displayNameSingular}</th>
                                    <th>Rank</th>
                                    <th>Score</th>
                                </>
                            }
                        </tr>
                    </thead>
                    <tbody>
                        {results.results.map(result => renderMobileRow(result, includePreviousPeriod, props, results))}
                    </tbody>
                </table>
                <ChartFooterInformation sampleSizeMeta={results.sampleSizeMetadata} activeBrand={props.activeBrand} metrics={[props.metric]} average={props.curatedFilters.average} />
            </div>
        );
    }

    if (isMobile) {
        return getMobileContent();
    } else {
        return getDesktopContent();
    }
}

export default RankingTable;
