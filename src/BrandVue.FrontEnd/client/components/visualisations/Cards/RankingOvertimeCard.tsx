import React from "react";
import { useEffect, useLayoutEffect, useState } from "react";
import { EntityInstance } from "../../../entity/EntityInstance";
import { Metric } from "../../../metrics/metric";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import * as BrandVueApi from "../../../BrandVueApi";
import Highcharts from 'highcharts';
import { Options } from 'highcharts';
import { ViewHelper } from "../ViewHelper";
import TileTemplate from "../shared/TileTemplate";
import TileTemplateChart from "./TileTemplateChart";
import { EntitySet } from "../../../entity/EntitySet";
import { NumberFormattingHelper } from "../../../helpers/NumberFormattingHelper";
import { DateFormattingHelper } from "../../../helpers/DateFormattingHelper";
import { CategoryComparisonPlaceholder } from "../../throbber/CategoryComparisonPlaceholder";
import { getNameOfPeriodBetween } from "../../helpers/DateHelper";
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from '../../../state/subsetSlice';

import {selectTimeSelection} from "../../../state/timeSelectionStateSelectors";

interface IRankingOvertimeCardProps {
    linkText: string;
    entityInstance: EntityInstance | undefined;
    entitySet: EntitySet | undefined;
    metric: Metric;
    curatedFilters: CuratedFilters;
    nextPageUrl: string;
}

interface IRankLimit {
    bestRank: number,
    worstRank: number,
}

const colorBlue = "#5daed4";
const colorLightBlue = "#cee7f2";
const colorLightGreen = "#bcdac2";
const colorLightRed = "#f3b3b3";
const colorGreen = "#218234";
const colorRed = "#d70000";

const RankingOvertimeCard: React.FunctionComponent<IRankingOvertimeCardProps> = (props: IRankingOvertimeCardProps) => {

    const [isLoading, setIsLoading] = useState(true);
    const [layoutReady, setLayoutReady] = useState(false);
    const [overtimeResults, setOvertimeResults] = useState<BrandVueApi.RankingOvertimeResultsByDate[]>([]);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    useLayoutEffect(() => {
        setLayoutReady(true);
    },[]);

    if (!props.entityInstance || props.metric.entityCombination.length !== 1) {
        throw new Error("Invalid combination of focus instance and entity combination");
    }

    const entityInstance = props.entityInstance!;

    useEffect(() => {
        setIsLoading(true);
        const model = ViewHelper.createCuratedRequestModel(
            getEntityInstanceIds(props.metric, props.entitySet),
            [props.metric],
            props.curatedFilters,
            entityInstance.id,
            { continuousPeriod: true },
            subsetId,
            timeSelection
        );

        BrandVueApi.Factory.DataClient(throwError => throwError()).getRankingOvertime(model)
            .then(rankingOvertimeResults => {
                setOvertimeResults(rankingOvertimeResults.results);
                setIsLoading(false);
            }
        );
    }, [
        props.entityInstance,
        props.metric,
        props.curatedFilters.endDate,
        props.curatedFilters.average.averageId,
        props.curatedFilters.demographicFilter.genders,
        props.curatedFilters.demographicFilter.ageGroups,
        props.curatedFilters.demographicFilter.regions,
        props.curatedFilters.demographicFilter.socioEconomicGroups,
        props.curatedFilters.measureFilters.length,
        timeSelection
    ]);

    if (isLoading) {
        return <TileTemplate
            nextPageUrl={props.nextPageUrl}
            linkText={getLinkText(props.linkText, props.metric.varCode)}
            descriptionNode={generateDescription(isLoading, props.metric, props.entityInstance, props.curatedFilters)}
            className="ranking-overtime-tile">
            <CategoryComparisonPlaceholder />
        </TileTemplate>;
    }

    return <TileTemplateChart
        handleWidth
        handleHeight
        nextPageUrl={props.nextPageUrl}
        linkText={getLinkText(props.linkText, props.metric.varCode)}
        descriptionNode={generateDescription(isLoading, props.metric, props.entityInstance, props.curatedFilters)}
        className="ranking-overtime-tile"
        getChartOptions={(width, height) => buildChartOptions(isLoading, height, width, entityInstance.id, overtimeResults, props.metric.name, props.curatedFilters.average)}
        refreshMode={layoutReady ? 'debounce' : undefined}
    />;
}

function getEntityInstanceIds(metric: Metric, entitySet: EntitySet | undefined): number[] {
    if (metric.entityCombination.length === 0 || !entitySet) {
        return [];
    }

    return entitySet.getInstances().getAll().map(entityInstance => entityInstance.id);
}

function getDateString(date: Date, average: BrandVueApi.IAverageDescriptor): string {
    return DateFormattingHelper.formatDatePoint(date, average);
}

function getLinkText(providedLinkText: string, metricName: string) : string {
    if (providedLinkText && providedLinkText.length > 0) {
        return providedLinkText;
    }
    return `Explore ${metricName} ranking`;
}

function generateDescription(isLoading: boolean, metric: Metric, entityInstance: EntityInstance | undefined, curatedFilters: CuratedFilters): React.ReactNode {
    if (isLoading) {
        return (
            <div className="card-description-placeholder">
                <div className="first-sentence"></div>
                <div className="second-sentence"></div>
            </div>
        );
    }

    const timePeriod = getNameOfPeriodBetween(curatedFilters.startDate, curatedFilters.endDate).toLowerCase();

    if (!entityInstance) {
        return (
            <p className="description">
                <strong>{metric.varCode}</strong> rank changes - last {timePeriod}
            </p>
        );
    }

    return (
        <p className="description">
            <strong>{metric.varCode}</strong> rank changes for {entityInstance.name} - last {timePeriod}
        </p>
    );
}

function generateTooltipFromHighchartsDatapoint(dataPoint: Highcharts.Point,
    focusEntityInstanceId: number,
    allRankings: BrandVueApi.RankingOvertimeResultsByDate[],
    competitorCount: number,
    metricName: string,
    average: BrandVueApi.IAverageDescriptor) : string
{
    const rank = dataPoint.y;
    const dateString = dataPoint.category;
    const rankingsForDate = allRankings.find(r => dateString === getDateString(r.date, average));
    if (!rankingsForDate || !rank) {
        throw new Error("Failed to match data back to competitors");
    }

    const rankIndex = rankingsForDate.results.findIndex(r => r.entityInstance.id === focusEntityInstanceId);
    const displayedEntities: BrandVueApi.RankingOvertimeResult[] = [];
    const indices: number[] = [ 0, rankIndex, rankingsForDate.results.length - 1 ]
        .filter((value, index, array) => array.indexOf(value) === index);

    indices.forEach(index => displayedEntities.push(rankingsForDate.results[index]));

    return `
    <div class="brandvue-tooltip">
        <div class="tooltip-header">${metricName}</div>
        <div class="tooltip-header light margin-bottom-half">${dateString}</div>
        <div class="tooltip-header light margin-bottom">You ranked ${NumberFormattingHelper.getOrdinalName(rank)} out of ${competitorCount}</div>
        ${displayedEntities.map(rankingResult => {
                const additionalClass = rankingResult.entityInstance.id === focusEntityInstanceId ? " bold" : "";
                const entityName = rankingResult.entityInstance.id === focusEntityInstanceId ? "You" : rankingResult.entityInstance.name;
                return `<div class="tooltip-label${additionalClass}">${entityName}</div>
                        <div class="tooltip-value${additionalClass}">${NumberFormattingHelper.getOrdinalName(rankingResult.rank)}</div>`
            }
        ).join("\r\n")}
    </div>`;
}

function getMinimumAndMaximumChartRanks(rankingsForFocusEntity: BrandVueApi.RankingOvertimeResult[], competitorCount: number) : IRankLimit {
    const orderedRanks = rankingsForFocusEntity.map(r => r.rank).sort((a: number, b: number) => a - b);
    const bestRank = orderedRanks[0];
    const worstRank = orderedRanks[orderedRanks.length - 1];
    return getAdjustedRankRange(bestRank, worstRank, 10, competitorCount);
}

function getAdjustedRankRange(initialBestRank: number, initialWorstRank: number, targetSize: number, maximumRank: number): IRankLimit {
    let bestRank = initialBestRank;
    let worstRank = initialWorstRank;
    let currentSize = worstRank - bestRank;

    const completedAdjusting = () => currentSize >= targetSize - 1 || (bestRank == 1 && worstRank == maximumRank);

    const bumpBest = () => {
        if (bestRank > 1) {
            bestRank--;
            currentSize = worstRank - bestRank;
        }
    }

    const bumpWorst = () => {
        if (worstRank < maximumRank && !completedAdjusting()) {
            worstRank++;
            currentSize = worstRank - bestRank;
        }
    }

    while (!completedAdjusting()) {
        bumpBest();
        bumpWorst();
    }

    return { bestRank, worstRank };
}

function buildChartOptions(isLoading: boolean,
    height: number,
    width: number,
    focusEntityInstanceId: number,
    rankings: BrandVueApi.RankingOvertimeResultsByDate[],
    metricName: string,
    average: BrandVueApi.IAverageDescriptor) : Options
{
    const filteredRankings: BrandVueApi.RankingOvertimeResult[] = [];
    for (var i = 0; i < rankings.length; i++) {
        const rankingsForPeriod = rankings[i].results.filter(r => r.entityInstance.id === focusEntityInstanceId);
        if (!rankingsForPeriod || rankingsForPeriod.length !== 1) {
            throw new Error("Incorrect information in results");
        }
        filteredRankings.push(rankingsForPeriod[0]);
    }

    const competitorCount = isLoading ? 1 : rankings[0].results.length;
    const xCategories = rankings.map(r => getDateString(r.date, average));
    const yCategories = Array.from({length: competitorCount + 1}, (_, i) => NumberFormattingHelper.getOrdinalName(i));
    const rankLimits = getMinimumAndMaximumChartRanks(filteredRankings, competitorCount);
    const data = isLoading ? [] : mapRankingOvertimeResultsToData(filteredRankings);

    return {
        chart: {
            type: "line",
            animation: false,
            height: height
        },
        tooltip: {
            enabled: true,
            formatter: function(this: Highcharts.TooltipFormatterContextObject): string {
                return generateTooltipFromHighchartsDatapoint(this.point, focusEntityInstanceId, rankings, competitorCount, metricName, average)
            },
        },
        xAxis: {
            categories: xCategories,
            opposite: true,
            lineWidth: 0,
            labels: {
                step: filteredRankings.length - 1
            }
        },
        yAxis: {
            reversed: true,
            categories: yCategories,
            min: rankLimits.bestRank,
            max: rankLimits.worstRank,
            tickInterval: 1,
            tickmarkPlacement: "on",
            type: "category",
            labels: {
                align: "center",
                step: rankLimits.worstRank - rankLimits.bestRank,
                x: width / 2,
                y: 4
            },
            title: {
                text: null,
            },
            gridLineDashStyle: "Dot",
            gridLineColor: "#a7a9ac",
        },
        plotOptions: {
            line: {
                marker: {radius: 5},
            }
        },
        series: [{
            type: "line",
            data: data,
            color: "#000000",
            lineWidth: 1,
            animation: !isLoading,
            showInLegend: false,
        }]
    };
}

function mapRankingOvertimeResultsToData(rankingsForFocusEntity: BrandVueApi.RankingOvertimeResult[]) {
    return rankingsForFocusEntity.map((r, i) => {
        if (i === 0) {
            return {
                name: NumberFormattingHelper.getOrdinalName(r.rank),
                y: r.rank,
                color: colorBlue,
                marker: {
                    radius: 16,
                    fillColor: colorLightBlue,
                    lineColor: colorBlue,
                    lineWidth: 2,
                    enabled: true,
                },
                dataLabels: {
                    enabled: true,
                    format: "{point.name}",
                    verticalAlign: "middle",
                    x: -1,
                    y: -1,
                }
            }
        }

        const prevRank = rankingsForFocusEntity[i - 1].rank;

        if (i === rankingsForFocusEntity.length - 1) {
            return {
                name: NumberFormattingHelper.getOrdinalName(r.rank),
                y: r.rank,
                color: r.rank === prevRank ? colorBlue : (r.rank < prevRank ? colorGreen : colorRed),
                marker: {
                    radius: 16,
                    fillColor: r.rank === prevRank ? colorLightBlue : (r.rank < prevRank ? colorLightGreen : colorLightRed),
                    lineColor: r.rank === prevRank ? colorBlue : (r.rank < prevRank ? colorGreen : colorRed),
                    lineWidth: 2,
                    enabled: true,
                },
                dataLabels: {
                    enabled: true,
                    format: "{point.name}",
                    verticalAlign: "middle",
                    x: -1,
                    y: -1,
                }
            }
        }

        return {
            name: NumberFormattingHelper.getOrdinalName(r.rank),
            y: r.rank,
            color: r.rank === prevRank ? colorBlue : (r.rank < prevRank ? colorGreen: colorRed),
            marker: {
                radius: 8,
                fillColor: r.rank === prevRank ? colorLightBlue : (r.rank < prevRank ? colorLightGreen : colorLightRed),
                lineColor: r.rank === prevRank ? colorBlue : (r.rank < prevRank ? colorGreen : colorRed),
                lineWidth: 2,
                enabled: true,
            },
        }
    });
}

export default RankingOvertimeCard;
