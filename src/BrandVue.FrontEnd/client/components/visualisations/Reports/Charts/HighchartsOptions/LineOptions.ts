import {
    IAverageDescriptor,
    DisplaySignificanceDifferences,
    EntityWeightedDailyResults,
    OverTimeResults,
    WaveComparisonResults, WeightedDailyResult
} from '../../../../../BrandVueApi';
import { Metric } from '../../../../../metrics/metric';
import { Options, SeriesLineOptions, XAxisOptions } from 'highcharts';
import { CustomPointOptionsObject, getPointForWeightedDailyResult } from './PointOptions';
import BrandVueOnlyLowSampleHelper from '../../../BrandVueOnlyLowSampleHelper';
import { formatOverTimeDate, getOverTimeChartCategories, getFormattedValueText } from '../../../../helpers/SurveyVueUtils';
import { ViewHelper } from '../../../ViewHelper';
import { ICommonDataPoint } from '../../../ICommonDataPoint';
import { getSignificance } from './HighchartsOptionsHelper';
import { getMeanCalculationValue } from '../../../../../helpers/HighchartHelper';
import { NumberFormattingHelper } from '../../../../../helpers/NumberFormattingHelper';

type ResultForWave = {
    waveName: string;
    results: WeightedDailyResult[];
}

interface CustomPointObjectWithWave extends CustomPointOptionsObject {
    waveName: string;
}

export function getLineChartOptions(
    results: WaveComparisonResults,
    metric: Metric,
    seriesColourMap: Map<string, string>,
    decimalPlaces: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    showLegend: boolean,
    isSmallCard: boolean,
    includeSignificance: boolean,
    selectSignificanceComparator: (selectedWaveName: string) => void,
    displayEntityInstanceIds: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options {
    const categories = results.comparisonResults[0].waveResults.map(r => r.waveName);
    const series = results.comparisonResults.flatMap((resultsPerWave, resultIndex) => {
        const breakName = resultsPerWave.breakName != null ? ` - ${resultsPerWave.breakName}` : '';
        return resultsPerWave.waveResults[0].entityResults.map((result, entityIndex) => {
            const entityResults = resultsPerWave.waveResults.map(r => {
                return {
                    waveName: r.waveName,
                    results: r.entityResults[entityIndex].weightedDailyResults
                };
            });
            const entityIdLabel = displayEntityInstanceIds && result.entityInstance ?
                `(${getMeanCalculationValue(result.entityInstance, metric)})` : '';
            const seriesName = `${result.entityInstance?.name ?? metric.displayName}${entityIdLabel}${breakName}`;
            const seriesId = `${seriesName}${resultIndex ?? ''}`
            const points = entityResults.map(r => getPoint(r.results[0], metric, decimalPlaces, seriesName, seriesId, r.waveName,
                undefined, showWeightedCounts, false, displaySignificanceDifferences));
            return getSeries(points,
                metric,
                decimalPlaces,
                seriesId,
                seriesName,
                false,
                includeSignificance,
                selectSignificanceComparator,
                undefined, seriesColourMap.get(getSeriesName(result, metric, breakName)));
        });
    });

    const allData = results.comparisonResults.flatMap(r => r.waveResults.flatMap(d => d.entityResults.map(e => e.weightedDailyResults[0].weightedResult)));
    const min = Math.floor(Math.min(...allData) * 10) / 10;
    const max = Math.ceil(Math.max(...allData) * 10) / 10;
    return getChartOptions(series, min, max, categories, metric, decimalPlaces, highlightLowSample, showLegend, isSmallCard);
}

export function getOvertimeLineChartOptions(
    results: OverTimeResults,
    metric: Metric,
    average: IAverageDescriptor,
    seriesColourMap: Map<string, string>,
    decimalPlaces: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    showLegend: boolean,
    isSmallCard: boolean,
    includeSignificance: boolean,
    selectSignificanceComparator: (selectedWaveName: string) => void,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options
{
    const categories = getOverTimeChartCategories(average, results.entityWeightedDailyResults[0].weightedDailyResults);

    const series = results.entityWeightedDailyResults.map((entityResult, resultIndex) => {
        const seriesName = entityResult.entityInstance?.name ?? metric.displayName;
        const seriesId = `${seriesName}${resultIndex ?? ''}`
        const points = entityResult.weightedDailyResults.map(r => {
            const name = formatOverTimeDate(average, r.date);
            return getPoint(r, metric, decimalPlaces, seriesName, seriesId, name, undefined, showWeightedCounts, false, displaySignificanceDifferences)
        });
        return getSeries(points,
            metric,
            decimalPlaces,
            seriesId,
            seriesName,
            false,
            includeSignificance,
            selectSignificanceComparator,
            undefined, seriesColourMap.get(seriesName));
    });

    const allData = results.entityWeightedDailyResults.flatMap(r => r.weightedDailyResults.map(d => d.weightedResult));
    const min = Math.floor(Math.min(...allData) * 10) / 10;
    const max = Math.ceil(Math.max(...allData) * 10) / 10;
    const options = getChartOptions(series, min, max, categories, metric, decimalPlaces, highlightLowSample, showLegend, isSmallCard);

    function applyXAxisLabelStyle(xAxisOption: XAxisOptions) {
        if (xAxisOption.labels) {
            xAxisOption.labels.style = {
                ...xAxisOption.labels.style,
                whiteSpace: 'nowrap',
                textOverflow: 'none',
            };
        }
    }
    if (Array.isArray(options.xAxis)) {
        options.xAxis.forEach(applyXAxisLabelStyle);
    } else if (options.xAxis) {
        applyXAxisLabelStyle(options.xAxis);
    }
    return options;
}

export function getWaveComparisonSeriesNames(results: WaveComparisonResults, metric: Metric) {
    return results.comparisonResults.flatMap(resultsPerWave => {
        const breakName = resultsPerWave.breakName != null ? ` - ${resultsPerWave.breakName}` : '';
        return resultsPerWave.waveResults[0].entityResults.map(r => getSeriesName(r, metric, breakName));
    });
}

function getSeriesName(result: EntityWeightedDailyResults, metric: Metric, breakName: string) {
    return `${result.entityInstance?.name ?? metric.displayName}${breakName}`;
}

function getSeries(
    points: CustomPointObjectWithWave[],
    metric: Metric,
    decimalPlaces: number,
    seriesId: string,
    seriesName: string,
    isAverageSeries: boolean,
    includeSignificance: boolean,
    selectSignificanceComparator: (selectedWaveName: string) => void,
    seriesClassName?: string,
    seriesColour?: string): SeriesLineOptions {

    const dashStyle = isAverageSeries ? 'Dash' : 'Solid';

    return {
        id: seriesId,
        name: seriesName,
        type: 'line',
        color: seriesColour,
        data: points,
        dashStyle: dashStyle,
        point: {
            events: {
                click: function (this: any) {
                    selectSignificanceComparator(this.category.name);
                }
            }
        },
        className: seriesClassName,
        events: {
            mouseOver: function (this: any) {
                this.update({
                    dataLabels: {
                        enabled: includeSignificance
                    }
                }, true)
            },
            mouseOut: function (this: any) {
                this.update({
                    dataLabels: {
                        enabled: false
                    }
                }, true)
            }
        },
        dataLabels: {
            useHTML: true,
            style: {
                fontSize: "14px",
                fontWeight: '400',
                color: '#666',
            },
            formatter: function (this: any): string {
                const point = this.point.options as CustomPointObjectWithWave;
                const roundedValue = getFormattedValueText(point.y!, metric, decimalPlaces);
                return `${getSignificance(point.significance, metric.downIsGood)} ${roundedValue}`;
            },
        },
    };
}

function getPoint(
    result: WeightedDailyResult,
    metric: Metric,
    decimalPlaces: number,
    seriesName: string,
    seriesId: string,
    waveName: string,
    labelColor: string | undefined,
    showWeightedCounts: boolean,
    isAveragePoint: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): CustomPointObjectWithWave {
    const pointName = `${seriesName} - ${waveName}`;
    return {
        ...getPointForWeightedDailyResult(result, metric, decimalPlaces, seriesId + waveName, pointName, labelColor, showWeightedCounts,
            isAveragePoint, displaySignificanceDifferences),
        waveName: waveName,
    };
}

function getChartOptions(series: SeriesLineOptions[], minValue: number, maxValue: number, categories: string[], metric: Metric, decimalPlaces: number,
    highlightLowSample: boolean, showLegend: boolean, isSmallCard: boolean): Options {
    const numberOfWaves = series[0]?.data != null ? series[0].data.length : 1;
    const hasMoreThanOneWave = numberOfWaves > 1;
    const isPercentageMetric = metric.isPercentage();

    if (highlightLowSample) {
        series.forEach(s => addLowSampleForLine(s));
    }

    return {
        chart: {
            type: "line",
            animation: true,
            backgroundColor: 'transparent',
            scrollablePlotArea: { minHeight: 200 },
        },
        series: series,
        tooltip: {
            enabled: true,
            useHTML: true,
            outside: true,
            className: "custom-tooltip-container",
            formatter: function (this: any): string {
                const seriesName = this.series.name;
                const point = this.point.options as CustomPointObjectWithWave;
                const roundedValue = getFormattedValueText(point.y!, metric, decimalPlaces);
                const sampleDiv = point.isAveragePoint
                    ? ''
                    : `<div class="custom-tooltip-point">n = ${NumberFormattingHelper.formatCount(point.count)}</div>`;
                const significance = point.significanceHelpText ?? '';

                return `<div class="custom-tooltip-title">
                            <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div>
                            <span>${seriesName}</span>
                        </div>
                        <div class="custom-tooltip-point">${point.waveName}</div>
                        <div class="custom-tooltip-point">Value: ${roundedValue}</div>
                        ${sampleDiv}
                        ${significance}`;
            },
        },
        xAxis: {
            type: 'category',
            categories: categories,
            tickWidth: 1,
            tickmarkPlacement: 'on',
            min: hasMoreThanOneWave ? 0.4 : undefined,
            max: hasMoreThanOneWave ? categories.length - 1.4 : undefined,
            labels: {
                style: {
                    textOverflow: 'ellipsis',
                    fontSize: "14px",
                    color: '#666',
                }
            },
        },
        yAxis: {
            title: {
                text: undefined,
            },
            labels: {
                formatter: function (this: { value: any }) {
                    return metric.fmt(this.value);
                }
            },
            minRange: isPercentageMetric ? 0.3 : 10,
            min: minValue,
            max: maxValue,
            floor: isPercentageMetric ? 0 : undefined,
            ceiling: isPercentageMetric ? 100 : undefined,
        },
        legend: {
            enabled: showLegend,
            align: 'center',
            verticalAlign: 'bottom',
            layout: 'horizontal',
            symbolRadius: 0,
            itemStyle: {
                fontWeight: 'normal',
            }
        },
        plotOptions: {
            line: {
                marker: {
                    radius: 4,
                    symbol: 'circle',
                    enabled: numberOfWaves <= (isSmallCard ? 15 : ViewHelper.enabledThreshold)
                },
            },
            series: {
                lineWidth: 2,
                states: {
                    hover: {
                        lineWidth: 3
                    }
                },
                animation: {
                    duration: 500,
                },
            }
        }
    };
}

//custom method for this as its not quite the same as the one in LowSampleHelper and don't want to break BV low sample styling
function addLowSampleForLine(series: SeriesLineOptions) {
    const zones: Highcharts.SeriesZonesOptionsObject[] = [];

    let inLowSample: boolean = false;

    for (let i = 0; i < series.data!.length; i++) {
        const pointWithSample = series.data![i] as (Highcharts.PointOptionsObject & ICommonDataPoint);

        if (!pointWithSample) {
            continue;
        }

        // Start a "low sample" zone if it's at or below the low sample threshold
        if (pointWithSample.sampleSize <= BrandVueOnlyLowSampleHelper.lowSampleForEntity) {
            pointWithSample.marker = {
                fillColor: '#FFFFFF',
                lineColor: series.color
            }
            if (!inLowSample) {
                inLowSample = true;
                zones.push({ value: Math.max(i - 1, 0) });
            }
        } else {
            if (inLowSample) {
                inLowSample = false;
                zones.push({ value: i, dashStyle: 'Dot' });
            }
        }
    }

    // make sure zones reach end of chart
    zones.push({ value: series.data!.length, dashStyle: inLowSample ? 'Dot' : undefined });

    if (zones.length) {
        const seriesChart = series;
        seriesChart.zoneAxis = 'x';
        seriesChart.zones = zones;
    }
}