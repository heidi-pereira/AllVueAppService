import { Metric } from "../../../../../metrics/metric";
import { Options, SeriesColumnOptions, PointLabelObject, PointOptionsObject } from 'highcharts';
import { CustomPointOptionsObject, getPointForWeightedDailyResult } from "./PointOptions";
import { IAverageDescriptor, AverageType, CrossbreakCompetitionResults, CrosstabAverageResults, DisplaySignificanceDifferences, EntityWeightedDailyResults, Features, OverTimeAverageResults, OverTimeResults, ReportOrder, StackedMultiEntityResults } from "../../../../../BrandVueApi";
import { getLabelTextColor } from "../../../../helpers/ChromaHelper";
import BrandVueOnlyLowSampleHelper from '../../../BrandVueOnlyLowSampleHelper';
import { getAverageDisplayText } from "../../../AverageHelper";
import { sortData } from "./SortOrderOptions";
import { getSignificance } from "./HighchartsOptionsHelper";
import { NumberFormattingHelper } from "../../../../../helpers/NumberFormattingHelper";
import { getMeanCalculationValue } from "../../../../../helpers/HighchartHelper";
import { getOverTimeChartCategories } from "../../../../helpers/SurveyVueUtils";

export function getStackedColumnChartOptions(results: StackedMultiEntityResults,
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    averages: OverTimeAverageResults[][],
    order: ReportOrder,
    selectSignificanceComparator: (entityName: string) => void,
    displayEntityInstanceIds: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    let series = getStackedSeries(results, metric, instanceToColourMap, decimalPlaces, showWeightedCounts, displayEntityInstanceIds, displaySignificanceDifferences);
    sortData({ results: series, order })
    const categories = results.resultsPerInstance.map(r => r.filterInstance.name)

    return getChartOptions(series, categories, metric, highlightLowSample, (e) => selectSignificanceComparator(e));
}

export function getStackedSingleEntityColumnChartOptions(results: EntityWeightedDailyResults[],
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    order: ReportOrder,
    selectSignificanceComparator: (entityName: string) => void,
    includeEntityIdInLabel: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    let series = getStackedSingleEntitySeries(results, metric, instanceToColourMap, decimalPlaces, showWeightedCounts, includeEntityIdInLabel, displaySignificanceDifferences);
    sortData({ results: series, order })
    const categories = [metric.displayName];

    return getChartOptions(series, categories, metric, highlightLowSample, (e) => selectSignificanceComparator(e));
}

export function getStackedOvertimeColumnChartOptions(results: OverTimeResults,
    metric: Metric,
    average: IAverageDescriptor,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    order: ReportOrder,
    selectSignificanceComparator: (entityName: string) => void,
    includeEntityIdInLabel: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    let series = getStackedOvertimeSeries(results.entityWeightedDailyResults, metric, instanceToColourMap, decimalPlaces,
        showWeightedCounts, includeEntityIdInLabel, displaySignificanceDifferences);
    sortData({ results: series, order });

    const categories = getOverTimeChartCategories(average, results.entityWeightedDailyResults[0].weightedDailyResults);

    return getChartOptions(series, categories, metric, highlightLowSample, (e) => selectSignificanceComparator(e));
}

export function getSplitStackedColumnChartOptions(results: CrossbreakCompetitionResults,
    metric: Metric,
    decimalPlaces: number,
    instanceToColourMap: Map<string, string>,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    averages: CrosstabAverageResults[],
    selectSignificanceComparator: (entityName: string) => void,
    displayEntityInstanceIds: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    const series = getSplitStackedSeries(results, metric, instanceToColourMap, decimalPlaces, showWeightedCounts, displayEntityInstanceIds, displaySignificanceDifferences);
    const categories = results.instanceResults.map(r => r.breakName);

    return getChartOptions(series, categories, metric, highlightLowSample, (e) => selectSignificanceComparator(e));
}

function getStackedSeries(results: StackedMultiEntityResults,
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    includeEntityIdInLabel: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesColumnOptions[] {
    const resultsData = results.resultsPerInstance[0].data.map((d, i) => {
        const name = d.entityInstance.name ?? metric.displayName;
        const entityInstanceLabel = includeEntityIdInLabel ? `${name} (${d.entityInstance.id})` : `${name}`;
        const color = instanceToColourMap.get(d.entityInstance.name ?? metric.displayName);
        const pointLabelColor = getLabelTextColor(color);
        const points = results.resultsPerInstance.map((r) => {
            const result = r.data[i].weightedDailyResults[0];
            return getPointForWeightedDailyResult(result, metric, decimalPlaces, "data" + r.filterInstance.name, r.filterInstance.name, pointLabelColor,
                showWeightedCounts, false, displaySignificanceDifferences);
        });
        return getColumn(name, points, color, metric.downIsGood, entityInstanceLabel);
    });

    return resultsData;
}

function getStackedSingleEntitySeries(results: EntityWeightedDailyResults[],
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    includeEntityIdInLabel: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesColumnOptions[] {
    const resultsData = results.map((d, i) => {
        const name = d.entityInstance.name ?? metric.displayName;
        const meanCalculationValue = getMeanCalculationValue(d.entityInstance, metric);
        const entityInstanceLabel = includeEntityIdInLabel ? `${name} (${meanCalculationValue})` : `${name}`;
        const color = instanceToColourMap.get(d.entityInstance.name ?? metric.displayName);
        const pointLabelColor = getLabelTextColor(color);
        const points = [getPointForWeightedDailyResult(d.weightedDailyResults[0], metric, decimalPlaces, `data_${i}_${name}`, name, pointLabelColor,
            showWeightedCounts, false, displaySignificanceDifferences)];
        return getColumn(name, points, color, metric.downIsGood, entityInstanceLabel);
    });

    return resultsData;
}

function getStackedOvertimeSeries(results: EntityWeightedDailyResults[],
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    includeEntityIdInLabel: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesColumnOptions[] {
    const resultsData = results.map((d, i) => {
        const name = d.entityInstance.name ?? metric.displayName;
        const meanCalculationValue = getMeanCalculationValue(d.entityInstance, metric);
        const entityInstanceLabel = includeEntityIdInLabel ? `${name} (${meanCalculationValue})` : `${name}`;
        const color = instanceToColourMap.get(d.entityInstance.name ?? metric.displayName);
        const pointLabelColor = getLabelTextColor(color);
        const points = d.weightedDailyResults.map((result, resultIndex) =>
            getPointForWeightedDailyResult(result, metric, decimalPlaces, `data_${i}_${name}_${resultIndex}`, name, pointLabelColor,
                showWeightedCounts, false, displaySignificanceDifferences));
        return getColumn(name, points, color, metric.downIsGood, entityInstanceLabel);
    });

    return resultsData;
}

function getSplitStackedSeries(results: CrossbreakCompetitionResults,
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    includeValueInLabel: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesColumnOptions[] {
    return results.instanceResults[0].entityResults.map((d, index) => {
        const name = d.entityInstance.name ?? metric.displayName;
        const valueLabel = includeValueInLabel ? `${name} (${getMeanCalculationValue(d.entityInstance, metric)})` : `${name}`;
        const color = instanceToColourMap.get(d.entityInstance.name ?? metric.displayName);
        const pointLabelColor = getLabelTextColor(color);
        const points = results.instanceResults.map(r => {
            const result = r.entityResults[index].weightedDailyResults[0];
            return getPointForWeightedDailyResult(result, metric, decimalPlaces, "data" + r.breakName, r.breakName, pointLabelColor,
                showWeightedCounts, false, displaySignificanceDifferences);
        });
        return getColumn(name, points, color, metric.downIsGood, valueLabel);
    });
}

function getColumn(instanceName: string,
    points: CustomPointOptionsObject[],
    color: string | undefined,
    downIsGood: boolean,
    entityInstanceLabel: string): SeriesColumnOptions {
    return {
        id: instanceName,
        name: `${entityInstanceLabel}`,
        type: 'column',
        color: color,
        data: points,
        dataLabels: {
            allowOverlap: true,
            enabled: true,
            verticalAlign: "middle",
            useHTML: true,
            style: {
                fontSize: "14px",
                whiteSpace: "nowrap",
                fontWeight: "normal",
            },
            formatter: function (this: PointLabelObject) {
                //shapeArgs is missing from Highcharts.PointLabelObject type definition
                const shapeArgs = (this.point as any).shapeArgs;
                const point = this.point.options as CustomPointOptionsObject;
                if (point.formattedText && shapeArgs.height >= 15) {

                    return (
                        `
                        <span>
                            ${point.formattedText}
                            ${getSignificance(point.significance, downIsGood)}
                        </span>
                        `
                    )
                }
            }
        },
    };
}

function getChartOptions(series: SeriesColumnOptions[],
    categories: string[],
    metric: Metric,
    highlightLowSample: boolean,
    selectSignificanceComparator: (entityName: string) => void): Options {

    if (highlightLowSample) {
        BrandVueOnlyLowSampleHelper.addLowSampleIndicators(series);
    }

    let fixPercentageYaxis = false;
    if (metric.isPercentage() && series.every(s => s.data)) {
        const summedResults = series[0].data!.map((_, index) => {
            const points = series.map(s => s.data![index]) as PointOptionsObject[];
            return points.reduce((total, point) => total + point.y!, 0);
        });
        //handle float/rounding issues, do they all add to ~100%
        if (summedResults.every(r => r <= 1.005)) {
            fixPercentageYaxis = true;
        }
    }

    return {
        chart: { type: "column", height: 1200, backgroundColor: 'transparent' },
        series: series,
        tooltip: {
            useHTML: true,
            className: "custom-tooltip-container",
            outside: true,
            formatter: function (this: any): string {
                const seriesName = this.series.name;
                const point = this.point.options as CustomPointOptionsObject;
                const significance = this.point.significanceHelpText ?? "";
                const sampleDiv = this.point.name == getAverageDisplayText(AverageType.Mean) || this.point.name == getAverageDisplayText(AverageType.Median) ?
                    ''
                    : `<div class="custom-tooltip-point">n = ${NumberFormattingHelper.formatCount(point.count)}</div>`;

                return `<div class="custom-tooltip-title">
                            <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div>
                            <span>${seriesName}</span>
                        </div>
                        <div class="custom-tooltip-point">Value: ${point.formattedText}</div>
                        ${sampleDiv}
                        ${significance}`;
            },
        },
        xAxis: {
            type: 'category',
            categories: categories,
            labels: {
                style: {
                    textOverflow: 'ellipsis',
                    fontSize: "14px",
                    color: '#666',
                    whiteSpace: 'wrap',
                    width: 150
                },
            },
        },
        yAxis: {
            max: fixPercentageYaxis ? 1 : null,
            title: {
                text: metric.yAxisTitle()
            },
            stackLabels: {
                enabled: false
            },
            labels: {
                formatter: function (this: { value: any }) {
                    return metric.fmt(this.value);
                }
            }
        },
        legend: {
            enabled: true,
            align: 'center',
            verticalAlign: 'bottom',
            layout: 'horizontal',
            symbolRadius: 0,
            itemStyle: {
                fontWeight: 'normal',
            }
        },
        plotOptions: {
            column: {
                groupPadding: 0.15,
            },
            series: {
                stacking: 'normal',
                states: {
                    hover: {
                        enabled: false,
                    },
                },
                dataLabels: {
                    style: {
                        textOutline: 'none',
                        whiteSpace: "nowrap",
                        fontWeight: "normal",
                        fontSize: '14px',
                    }
                },
                point: {
                    events: {
                        click: function () {
                            selectSignificanceComparator(this.category.toLocaleString());
                        }
                    }
                }
            }
        },
    };
}