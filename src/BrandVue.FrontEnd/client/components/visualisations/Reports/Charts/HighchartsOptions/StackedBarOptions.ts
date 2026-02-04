import { Metric } from "../../../../../metrics/metric";
import { Point, Options, SeriesBarOptions } from 'highcharts';
import { BarPointWidth } from "../../Cards/ReportsPageCardChartContent";
import { CustomPointOptionsObject, getPointForWeightedDailyResult } from "./PointOptions";
import { IAverageDescriptor, AverageType, CrossbreakCompetitionResults, DisplaySignificanceDifferences, EntityWeightedDailyResults, OverTimeResults, ReportOrder, StackedMultiEntityResults, WeightedDailyResult } from "../../../../../BrandVueApi";
import { getLabelTextColor, Slate, SlateDark } from "../../../../helpers/ChromaHelper";
import BrandVueOnlyLowSampleHelper from '../../../BrandVueOnlyLowSampleHelper';
import { getAverageDisplayText } from "../../../AverageHelper";
import { sortData } from "./SortOrderOptions";
import { getSignificance } from "./HighchartsOptionsHelper";
import { NumberFormattingHelper } from "../../../../../helpers/NumberFormattingHelper";
import { getOverTimeChartCategories } from "client/components/helpers/SurveyVueUtils";

export function getStackedBarChartOptions(
    results: StackedMultiEntityResults,
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    order: ReportOrder,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    const titleCategories = results.resultsPerInstance.map(r => r.filterInstance.name);
    const baseSizeCategories = results.resultsPerInstance.map(r => getBaseSizeCategory(r.data));
    const series = getSeriesStackedMultiEntity(results, metric, instanceToColourMap, decimalPlaces, showWeightedCounts, displaySignificanceDifferences);

    sortData({ results: series, order })

    return getChartOptions(series, titleCategories, baseSizeCategories, highlightLowSample);
}

export function getStackedSingleEntityBarChartOptions(
    results: EntityWeightedDailyResults[],
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    order: ReportOrder,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    const titleCategories = [metric.displayName];
    const baseSizeCategories = [getBaseSizeCategory(results)];
    const series = getSeriesStackedSingleEntity(results, metric, instanceToColourMap, decimalPlaces, showWeightedCounts, displaySignificanceDifferences);

    sortData({ results: series, order })

    return getChartOptions(series, titleCategories, baseSizeCategories, highlightLowSample);
}

export function getStackedOvertimeBarChartOptions(
    results: OverTimeResults,
    metric: Metric,
    average: IAverageDescriptor,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    order: ReportOrder,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options
{
    const series = getSeriesStackedOvertime(results.entityWeightedDailyResults, metric, instanceToColourMap, decimalPlaces,
        showWeightedCounts, displaySignificanceDifferences);

    const titleCategories = getOverTimeChartCategories(average, results.entityWeightedDailyResults[0].weightedDailyResults).reverse();
    const baseSizeCategories = getOvertimeBaseSizeCategories(results.entityWeightedDailyResults).reverse();

    return getChartOptions(series, titleCategories, baseSizeCategories, highlightLowSample);
}

export function getSplitStackedBarChartOptions(
    results: CrossbreakCompetitionResults,
    metric: Metric,
    decimalPlaces: number,
    instanceToColourMap: Map<string, string>,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    const titleCategories = results.instanceResults.map(r => r.breakName); //todo - map sig by title categories on stacked cards
    const baseSizeCategories = results.instanceResults.map(r => getBaseSizeCategory(r.entityResults));
    const series = getSeriesCrossbreakCompetition(results, metric, instanceToColourMap, decimalPlaces, showWeightedCounts, displaySignificanceDifferences);
    return getChartOptions(series, titleCategories, baseSizeCategories, highlightLowSample);
}

function getSeriesStackedMultiEntity(results: StackedMultiEntityResults,
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesBarOptions[] {
        return results.resultsPerInstance[0].data.map((d, index) => {
            const name = d.entityInstance?.name ?? metric.displayName;
            const color = instanceToColourMap.get(name);
            const pointLabelColor = getLabelTextColor(color);
            const points = results.resultsPerInstance.map(r => {
                const result = r.data[index].weightedDailyResults[0];
                return getPointForWeightedDailyResult(result, metric, decimalPlaces,
                    "data" + r.filterInstance.name, r.filterInstance.name, pointLabelColor, showWeightedCounts, false, displaySignificanceDifferences);
            });
            return getBar(name, points, color, metric.downIsGood);
        });
}

function getSeriesStackedSingleEntity(results: EntityWeightedDailyResults[],
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesBarOptions[] {
        return results.map((d, index) => {
            const name = d.entityInstance?.name ?? metric.displayName;
            const color = instanceToColourMap.get(name);
            const pointLabelColor = getLabelTextColor(color);
            const points = [getPointForWeightedDailyResult(d.weightedDailyResults[0], metric, decimalPlaces,
                `data_${index}_${name}`, name, pointLabelColor, showWeightedCounts, false, displaySignificanceDifferences)];
            return getBar(name, points, color, metric.downIsGood);
        });
}

function getSeriesStackedOvertime(results: EntityWeightedDailyResults[],
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesBarOptions[] {
        return results.map((d, index) => {
            const name = d.entityInstance?.name ?? metric.displayName;
            const color = instanceToColourMap.get(name);
            const pointLabelColor = getLabelTextColor(color);
            const points = d.weightedDailyResults.map((result, resultIndex) =>
                    getPointForWeightedDailyResult(result, metric, decimalPlaces, `data_${index}_${name}_${resultIndex}`, name, pointLabelColor,
                        showWeightedCounts, false, displaySignificanceDifferences)
                ).reverse();
            return getBar(name, points, color, metric.downIsGood);
        });
}

function getSeriesCrossbreakCompetition(results: CrossbreakCompetitionResults,
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesBarOptions[]
{
        return results.instanceResults[0].entityResults.map((d, index) => {
            const name = d.entityInstance?.name ?? metric.displayName;
            const color = instanceToColourMap.get(name);
            const pointLabelColor = getLabelTextColor(color);
            const points = results.instanceResults.map(r => {
                const result = r.entityResults[index].weightedDailyResults[0]
                return getPointForWeightedDailyResult(result, metric, decimalPlaces, "data" + r.breakName, r.breakName, pointLabelColor,
                    showWeightedCounts, false, displaySignificanceDifferences);
            });
            return getBar(name, points, color, metric.downIsGood);
        });    
}

function getBar(instanceName: string, points: CustomPointOptionsObject[], color: string | undefined, downIsGood: boolean): SeriesBarOptions {
    return {
        id: instanceName,
        name: instanceName,
        type: 'bar',
        color: color,
        data: points,
        dataLabels: {
            enabled: true,
            defer: false,
            useHTML: true,
            formatter: function (this: Highcharts.PointLabelObject) {
                //shapeArgs is missing from Highcharts.PointLabelObject type definition
                const shapeArgs = (this.point as any).shapeArgs;
                const point = this.point.options as CustomPointOptionsObject;

                if (point.formattedText && shapeArgs.height >= point.formattedText.length * 10) {
                    return `${point.formattedText} ${getSignificance(point.significance, downIsGood)}`;
                }
            }
        }
    };
}

function getBaseSizeCategory(results: EntityWeightedDailyResults[]): string {
    return getSampleSizeDescription(results.map(d => d.weightedDailyResults[0]));
}

function getOvertimeBaseSizeCategories(results: EntityWeightedDailyResults[]): string[] {
    return results[0].weightedDailyResults.map((_, index) => {
        return getSampleSizeDescription(results.map(d => d.weightedDailyResults[index]));
    });
}

function getSampleSizeDescription(results: WeightedDailyResult[]) {
    const allSampleSizes = results.map(d => d.weightedSampleSize);
    if (allSampleSizes.every(size => size == allSampleSizes[0])) {
        return `(${Math.round(allSampleSizes[0]).toLocaleString()})`;
    }
    return "";
}

function getChartOptions(series: SeriesBarOptions[], titleCategories: string[], baseSizeCategories: string[], highlightLowSample: boolean): Options {
    const chartHeight = titleCategories.length * (56 + BarPointWidth);
    const labelOffsetY = -31;
    
    if (highlightLowSample) {
        BrandVueOnlyLowSampleHelper.addLowSampleIndicators(series);
    }

    return {
        chart: {
            type: "bar",
            animation: true,
            height: chartHeight,
            backgroundColor: 'transparent'
        },
        series: series,
        tooltip: {
            enabled: true,
            animation: false,
            pointFormatter: function (this: Point): string {
                const point = this.options as CustomPointOptionsObject;
                const significanceHelpText = point.significanceHelpText ?? "";
                const sampleDiv = this.series.name == getAverageDisplayText(AverageType.Mean) || this.series.name == getAverageDisplayText(AverageType.Median) ?
                ''
                : `<div class="custom-tooltip-point">n = ${NumberFormattingHelper.formatCount(point.count)}</div>`;

                return `<span style="color:${this.color}">\u25CF</span>
                        ${this.series.name}: ${point.formattedText}
                        ${sampleDiv}
                        ${significanceHelpText}`;
            }
        },
        xAxis: [{
            id: 'title',
            categories: titleCategories,
            type: 'category',
            labels: {
                align: 'left',
                x: 0,
                y: labelOffsetY,
                style: {
                    color: SlateDark,
                    fontSize: "14px",
                    whiteSpace: "nowrap",
                    fontWeight: "normal",
                    width: 700,
                    textOverflow: "ellipsis",
                },
            },
            lineWidth: 0,
            gridLineWidth: 0,
            height: chartHeight,
        }, {
            id: 'baseSizes',
            categories: baseSizeCategories,
            opposite: true,
            linkedTo: 0,
            type: 'category',
            labels: {
                align: 'right',
                x: 0,
                y: labelOffsetY,
                style: {
                    color: Slate,
                    fontSize: "14px",
                    whiteSpace: "nowrap",
                    fontWeight: "normal",
                },
            },
            lineWidth: 0,
            gridLineWidth: 0,
            height: chartHeight,
        }],
        yAxis: {
            title: { text: undefined },
            labels: { enabled: false },
            lineWidth: 0,
            gridLineWidth: 0,
            maxPadding: 0,
            startOnTick: false,
            endOnTick: false,
        },
        legend: {
            enabled: false,
        },
        plotOptions: {
            bar: {
                groupPadding: 0,
                pointWidth: BarPointWidth,
            },
            series: {
                animation: {
                    duration: 500,
                },
                states: {
                    hover: {
                        enabled: false,
                    },
                },
                stacking: 'normal',
                dataLabels: {
                    style: {
                        textOutline: 'none',
                        fontSize: '14px',
                        fontWeight: 'normal'
                    }
                }
            }
        },
    };
}