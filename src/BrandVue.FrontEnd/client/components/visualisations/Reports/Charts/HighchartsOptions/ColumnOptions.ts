import {
    IAverageDescriptor,
    AverageType,
    CrossbreakCompetitionResults,
    DisplaySignificanceDifferences,
    EntityWeightedDailyResults,
    GroupedCrossbreakCompetitionResults,
    WeightedDailyResult,
} from '../../../../../BrandVueApi';
import { Metric } from '../../../../../metrics/metric';
import { Options, PointLabelObject, SeriesColumnOptions } from 'highcharts';
import { getOverTimeChartCategories, getFormattedValueText } from '../../../../helpers/SurveyVueUtils';
import { BarColour } from '../../Cards/ReportsPageCardChartContent';
import BrandVueOnlyLowSampleHelper from '../../../BrandVueOnlyLowSampleHelper';
import { getLabelTextColor, SlateDark } from '../../../../helpers/ChromaHelper';
import { CustomPointOptionsObject, getPointForWeightedDailyResult } from './PointOptions';
import { getAverageDisplayText } from '../../../AverageHelper';
import { getSignificance } from './HighchartsOptionsHelper';
import { NumberFormattingHelper } from '../../../../../helpers/NumberFormattingHelper';
import { getMeanCalculationValue } from '../../../../../helpers/HighchartHelper';
import { DateFormattingHelper } from '../../../../../helpers/DateFormattingHelper';

export function getColumnChartOptions(
    results: EntityWeightedDailyResults[],
    metric: Metric,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    highlightLowSample: boolean,
    displayEntityInstanceIds: boolean,
    selectSignificanceComparator: (a: string, b: string) => void,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    let series: SeriesColumnOptions[]
    let categories: string[];
    let hasDifferentSampleSizes = false;

    series = [getSeries(results, metric, decimalPlaces, showWeightedCounts, "counter", displaySignificanceDifferences, undefined, "counter-series", 1, BarColour)];
    categories = results.map(r => getCategoryLabels(r, metric, displayEntityInstanceIds));
    const allSampleSizes = results.map(r => r.weightedDailyResults[0].unweightedSampleSize);
    hasDifferentSampleSizes = !allSampleSizes.every(size => size == allSampleSizes[0]);

    return {
        ...getChartOptions(series, categories, metric, highlightLowSample, (a, b) => selectSignificanceComparator(a, b)),
        tooltip: {
            enabled: true,
            useHTML: true,
            outside: true,
            className: "custom-tooltip-container",
            formatter: function (this: any): string {
                const point = this.point.options;
                const name = this.point.name;
                const roundedValue = getFormattedValueText(point.y!, metric, decimalPlaces);
                const sampleInfo = hasDifferentSampleSizes ? ` of ${NumberFormattingHelper.formatCount(point.sampleSize)}` : '';
                const sampleDiv = this.point.name == getAverageDisplayText(AverageType.Mean) || this.point.name == getAverageDisplayText(AverageType.Median) ?
                    ''
                    : `<div class="custom-tooltip-point">n = ${NumberFormattingHelper.formatCount(point.count)}${sampleInfo}</div>`;

                return `<div class="custom-tooltip-title">
                            <span>${name}</span>
                        </div>
                        <div class="custom-tooltip-point">Value: ${roundedValue}</div>
                        ${sampleDiv}`;
            },
        },
    };
}

function getCategoryLabels(entityInstance: EntityWeightedDailyResults, metric: Metric, includeId: boolean): string {
    if (entityInstance) {
        return (includeId && (entityInstance.entityInstance.id || entityInstance.entityInstance.id === 0))
            ? `${entityInstance.entityInstance.name} (${getMeanCalculationValue(entityInstance.entityInstance, metric)})`
            : entityInstance.entityInstance.name ? entityInstance.entityInstance.name: metric.displayName;
    }
    return metric.displayName;
}

export function getSplitColumnChartOptions(
    results: CrossbreakCompetitionResults,
    metric: Metric,
    entityInstanceColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    highlightLowSample: boolean,
    selectSignificanceComparator: (a: string, b: string) => void,
    displayEntityInstanceIds: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    let series = results.instanceResults.map((splitResult, index) => {
        const seriesName = splitResult.breakName;
        const className = undefined;
        const colour = entityInstanceColourMap.get(seriesName);
        //we do not calculate averages here because breaks
        return getSeries(splitResult.entityResults,
            metric,
            decimalPlaces,
            showWeightedCounts,
            seriesName,
            displaySignificanceDifferences,
            displayEntityInstanceIds ? splitResult.breakEntityInstanceId ?? undefined : undefined,
            className,
            index,
            colour);
    });

    let categories = results.instanceResults[0].entityResults.map(r => getCategoryLabels(r, metric, displayEntityInstanceIds));

    return dataToSplitColumnChartOptions(series, categories, metric, decimalPlaces, highlightLowSample, selectSignificanceComparator);
}

export function getOvertimeColumnChartOptions(
    results: EntityWeightedDailyResults[],
    metric: Metric,
    average: IAverageDescriptor,
    breakToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    highlightLowSample: boolean,
    selectSignificanceComparator: (a: string, b: string) => void,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    let series = results.map((entityResult, index) => {
        const seriesName = entityResult.entityInstance?.name ?? metric.displayName;
        const className = undefined;
        const colour = breakToColourMap.get(seriesName);
        return getOvertimeSeries(entityResult,
            metric,
            average,
            decimalPlaces,
            showWeightedCounts,
            seriesName,
            displaySignificanceDifferences,
            className,
            index,
            colour);
    });

    const categories = getOverTimeChartCategories(average, results[0].weightedDailyResults);

    return dataToSplitColumnChartOptions(series, categories, metric, decimalPlaces, highlightLowSample, selectSignificanceComparator);
}

function dataToSplitColumnChartOptions(
    series: SeriesColumnOptions[],
    categories: string[],
    metric: Metric,
    decimalPlaces: number,
    highlightLowSample: boolean,
    selectSignificanceComparator: (a: string, b: string) => void): Options {
    return {
        ...getChartOptions(series, categories, metric, highlightLowSample, (a, b) => selectSignificanceComparator(a, b)),
        tooltip: {
            useHTML: true,
            outside: true,
            className: "custom-tooltip-container",
            formatter: function (this: any): string {
                const seriesName = this.series.name;
                const point = this.point.options as CustomPointOptionsObject;
                const roundedValue = getFormattedValueText(point.y!, metric, decimalPlaces);
                const sampleDiv = point.isAveragePoint ?
                    ''
                    : `<div class="custom-tooltip-point">n = ${NumberFormattingHelper.formatCount(point.count)}</div>`;
                const sigDif = point.significanceHelpText ?? ""

                return `<div class="custom-tooltip-title">
                            <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div>
                            <span>${seriesName}</span>
                        </div>
                        <div class="custom-tooltip-point">Value: ${roundedValue}</div>
                        ${sampleDiv}
                        ${sigDif}`;
            },
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
    };
}

export function getMultiBreakColumnChartOptions(
    results: GroupedCrossbreakCompetitionResults,
    metric: Metric,
    decimalPlaces: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    selectSignificanceComparator: (a: string, b: string) => void,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    const categories = results.groupedBreakResults.map(group => {
        return {
            name: group.groupName,
            categories: group.breakResults.instanceResults.map(r => r.breakName)
        }
    });
    for (var i = 1; i < categories.length; i += 2) {
        const emptyCategory = {
            name: '',
            categories: ['']
        };
        categories.splice(i, 0, emptyCategory);
    }

    const primaryEntityInstance = results.groupedBreakResults[0]?.breakResults.instanceResults[0]?.entityResults[0]?.entityInstance ?? "";
    const seriesId = metric.varCode;
    const seriesName = `${metric.displayName}: ${primaryEntityInstance.name}`
    const points: CustomPointOptionsObject[] = [];
    const labelColor = getLabelTextColor(BarColour);
    let xValue = 0;
    results.groupedBreakResults.forEach(group => {
        group.breakResults.instanceResults.forEach(result => {
            const entityResult = result.entityResults[0];
            const pointName = entityResult.entityInstance.name;
            const pointId = `${group.groupName}_${result.breakName}_${pointName}`;
            const subtitle = `${group.groupName}: ${result.breakName}`;
            const point = getPointForWeightedDailyResult(entityResult.weightedDailyResults[0],
                metric,
                decimalPlaces,
                pointId,
                pointName,
                labelColor,
                showWeightedCounts,
                false,
                displaySignificanceDifferences,
                xValue,
                subtitle);
            points.push(point);
            xValue++;
        });
        xValue++;
    });

    let series: SeriesColumnOptions = {
        id: seriesId,
        name: seriesName,
        type: 'column',
        color: BarColour,
        data: points,
        dataLabels: {
            enabled: true,
            useHTML: true,
            verticalAlign: "top",
            y: -40 as any,
            style: {
                fontSize: "14px",
                whiteSpace: "nowrap",
                fontWeight: "normal",
            },
            formatter: function (this: PointLabelObject): string {
                const point = this.point.options as CustomPointOptionsObject;
                return `<span style="color:${SlateDark};">
                    ${point.formattedText}
                    ${getSignificance(point.significance, metric.downIsGood)}
                </span>`;
            }
        },
    };

    const chartOptions = getChartOptions([series], [], metric, highlightLowSample, (a, b) => selectSignificanceComparator(a, b));

    return {
        ...chartOptions,
        xAxis: {
            ...chartOptions.xAxis,
            tickWidth: 0,
            categories: categories as any,
            labels: {
                style: {
                    textOverflow: 'ellipsis',
                    fontSize: "14px",
                    color: '#666',
                    whiteSpace: 'nowrap',
                    width: 150
                }
            }
        },
        tooltip: {
            useHTML: true,
            outside: true,
            className: "custom-tooltip-container",
            formatter: function (this: any): string {
                const seriesName = this.series.name;
                const point = this.point.options as CustomPointOptionsObject;
                const sigDif = point.significanceHelpText ?? "";
                const sampleDiv = point.isAveragePoint ?
                    ''
                    : `<div class="custom-tooltip-point">n = ${NumberFormattingHelper.formatCount(point.count)}</div>`;

                return `<div class="custom-tooltip-title">
                            <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div>
                            <span>${seriesName}</span>
                        </div>
                        <div class="custom-tooltip-point">${point.subtitle}</div>
                        <div class="custom-tooltip-point">Value: ${point.formattedText}</div>
                        ${sampleDiv}
                        ${sigDif}`;
            }
        }
    };
}

function getSeries(
    results: EntityWeightedDailyResults[],
    metric: Metric,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    seriesName: string,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    entityInstanceId?: number,
    seriesClassName?: string,
    seriesIndex?: number,
    seriesColour?: string): SeriesColumnOptions
{
    const seriesId = `${seriesName}${seriesIndex ?? ''}`
    const labelColor = getLabelTextColor(seriesColour);
    let data = results.map(r => getPoint(r, metric, decimalPlaces, seriesId, labelColor, showWeightedCounts, displaySignificanceDifferences));
    return seriesFromPoints(data, metric, seriesId, seriesName, seriesClassName, seriesColour);
}

function getOvertimeSeries(
    results: EntityWeightedDailyResults,
    metric: Metric,
    average: IAverageDescriptor,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    seriesName: string,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    seriesClassName?: string,
    seriesIndex?: number,
    seriesColour?: string): SeriesColumnOptions
{
    const seriesId = `${seriesName}${seriesIndex ?? ''}`
    const labelColor = getLabelTextColor(seriesColour);
    let data = results.weightedDailyResults.map(r => getOvertimePoint(r, metric, decimalPlaces, seriesId, labelColor, showWeightedCounts, average, displaySignificanceDifferences));
    return seriesFromPoints(data, metric, seriesId, seriesName, seriesClassName, seriesColour);
}

function seriesFromPoints(
    data: CustomPointOptionsObject[],
    metric: Metric,
    seriesId: string,
    seriesName: string,
    seriesClassName?: string,
    seriesColour?: string): SeriesColumnOptions
{
    return {
        id: seriesId,
        name: seriesName,
        type: 'column',
        color: seriesColour,
        data: data,
        className: seriesClassName,
        dataLabels: {
            enabled: true,
            useHTML: true,
            verticalAlign: "top",
            y: -40 as any,
            style: {
                fontSize: "14px",
                whiteSpace: "nowrap",
                fontWeight: "normal",
            },
            formatter: function (this: PointLabelObject): string {
                const point = this.point.options as CustomPointOptionsObject;
                return `<span style="color:${SlateDark};">
                            ${point.formattedText}
                            ${getSignificance(point.significance, metric.downIsGood)}
                        </span>`;
            }
        },
    };
}

function getPoint(r: EntityWeightedDailyResults, metric: Metric, decimalPlaces: number, seriesId: string, labelColor: string | undefined,
    showWeightedCounts: boolean, displaySignificanceDifferences: DisplaySignificanceDifferences) {
    const result = r.weightedDailyResults[0];
    const instanceName = r.entityInstance?.name ?? metric.displayName;
    return getPointForWeightedDailyResult(result, metric, decimalPlaces, seriesId + instanceName, instanceName, labelColor, showWeightedCounts,
        false, displaySignificanceDifferences);
}

function getOvertimePoint(result: WeightedDailyResult, metric: Metric, decimalPlaces: number, seriesId: string, labelColor: string | undefined,
    showWeightedCounts: boolean, average: IAverageDescriptor, displaySignificanceDifferences: DisplaySignificanceDifferences): CustomPointOptionsObject {
    const date = DateFormattingHelper.formatDatePoint(result.date, average);
    return getPointForWeightedDailyResult(result, metric, decimalPlaces, seriesId + date, date, labelColor, showWeightedCounts, false, displaySignificanceDifferences);
}

function getChartOptions(series: SeriesColumnOptions[],
    categories: string[],
    metric: Metric,
    highlightLowSample: boolean,
    selectSignificanceComparator: (splitColumnData: string, multiBreakComparandData: string) => void): Options {

    if (highlightLowSample) {
        BrandVueOnlyLowSampleHelper.addLowSampleIndicators(series);
    }

    return {
        chart: { type: "column", animation: true, backgroundColor: 'transparent', scrollablePlotArea: { minHeight: 200 } },
        series: series,
        xAxis: {
            type: 'category',
            categories: categories,
            labels: {
                style: {
                    textOverflow: 'ellipsis',
                    fontSize: "14px",
                    color: '#666',
                },
            },
        },
        yAxis: {
            title: {
                text: metric.yAxisTitle(),
            },
            labels: {
                formatter: function (this: { value: any }) {
                    return metric.fmt(this.value);
                }
            }
        },
        legend: {
            enabled: false,
        },
        plotOptions: {
            column: {
                groupPadding: 0.15,
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
                point: {
                    events: {
                        click: function () {
                            selectSignificanceComparator(this.series.name, this.category.toLocaleString());
                        }
                    }
                }
            }
        }
    };
}