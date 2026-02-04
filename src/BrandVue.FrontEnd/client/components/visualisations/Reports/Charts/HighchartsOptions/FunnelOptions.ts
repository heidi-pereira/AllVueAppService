import {
    IAverageDescriptor,
    CalculationType,
    DisplaySignificanceDifferences,
    EntityWeightedDailyResults, GroupedCrossbreakCompetitionResults, GroupedVariableDefinition, VariableConfigurationModel, WaveComparisonResults,
} from '../../../../../BrandVueApi';
import { Metric } from '../../../../../metrics/metric';
import { Options, PointLabelObject, SeriesColumnrangeOptions } from 'highcharts';
import { formatOverTimeDate, getFormattedValueText } from '../../../../helpers/SurveyVueUtils';
import { BarColour } from '../../Cards/ReportsPageCardChartContent';
import BrandVueOnlyLowSampleHelper from '../../../BrandVueOnlyLowSampleHelper';
import { getLabelTextColor } from '../../../../helpers/ChromaHelper';
import { CustomPointOptionsObject, getPointForFunnelWeightedDailyResult } from './PointOptions';
import { NumberFormattingHelper } from '../../../../../helpers/NumberFormattingHelper';
import { getMeanCalculationValue } from '../../../../../helpers/HighchartHelper';
import { UnsupportedVariableError } from './CustomErrors';

const MaxFunnelBars = 6;
const MaxFunnelsSupportedPerChart = 5;

export const validateMetricForFunnelChart = (metric: Metric, variable: VariableConfigurationModel | undefined) => {
    const errorMessages: string[] = [];
    if (metric.entityCombination.length > 1) {
        errorMessages.push("Multi entity");
    }
    if (metric.calcType != CalculationType.YesNo) {
        errorMessages.push("Metric/variable calculation type not supported");
    }
    if (variable?.definition instanceof GroupedVariableDefinition) {
        if (variable.definition.groups && variable.definition.groups.length > MaxFunnelBars) {
            errorMessages.push(`Over funnel bar limit (${MaxFunnelBars})`);
        }
    }
    else {
        errorMessages.push("Variable type not supported");
    }
    return errorMessages;
}

const emptySeriesPlaceholder = {
    type: 'columnrange',
    data: [],
    showInLegend: false
};

function chunkArray(seriesArray: SeriesColumnrangeOptions[], chunkSize: number): SeriesColumnrangeOptions[][] {
    if (seriesArray.length <= chunkSize) {
        return [seriesArray];
    }
    let result: SeriesColumnrangeOptions[][] = [];
    for (let i = 0; i < seriesArray.length; i += chunkSize) {
        let chunk = seriesArray.slice(i, i + chunkSize);
        if (chunk.length < chunkSize) {
            chunk = [...chunk, ...new Array(chunkSize - chunk.length).fill(emptySeriesPlaceholder as SeriesColumnrangeOptions)];
        }
        result.push(chunk);
    }
    return result;
}

export function getFunnelChartOptions(
    results: EntityWeightedDailyResults[] | WaveComparisonResults | GroupedCrossbreakCompetitionResults,
    metric: Metric,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    highlightLowSample: boolean,
    displayEntityInstanceIds: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    variable?: VariableConfigurationModel): Options[] {

    const validationErrors = validateMetricForFunnelChart(metric, variable);
    if (validationErrors.length > 0) {
        throw new UnsupportedVariableError(validationErrors.join(", "));
    }

    let hasDifferentSampleSizes = false;
    let series: SeriesColumnrangeOptions[] = [];
    let categories: string[] = [];
    let allSampleSizes: number[] = [];

    if (results instanceof WaveComparisonResults) {
        const waveResults = results.comparisonResults[0].waveResults;

        series = waveResults.flatMap((waveResult, resultIndex) => {
            return getSeries(waveResult.entityResults, metric, decimalPlaces, showWeightedCounts, waveResult.waveName, waveResults.length, displaySignificanceDifferences, "counter-series", resultIndex, BarColour);
        });
        categories = waveResults[0].entityResults.map(r => getCategoryLabels(r, metric, displayEntityInstanceIds));
        allSampleSizes = waveResults[0].entityResults.map(r => r.weightedDailyResults[0].unweightedSampleSize);

    } else if (results instanceof GroupedCrossbreakCompetitionResults) {
        const breakResults = results.groupedBreakResults[0].breakResults.instanceResults;

        series = breakResults.flatMap((breakResult, resultIndex) => {
            return getSeries(breakResult.entityResults, metric, decimalPlaces, showWeightedCounts, breakResult.breakName, breakResults.length, displaySignificanceDifferences, "counter-series", resultIndex, BarColour);
        });
        categories = breakResults[0].entityResults.map(r => getCategoryLabels(r, metric, displayEntityInstanceIds));
        allSampleSizes = breakResults[0].entityResults.map(r => r.weightedDailyResults[0].unweightedSampleSize);
    } else if (Array.isArray(results) && results.every(item => item instanceof EntityWeightedDailyResults)) {
        series = [getSeries(results, metric, decimalPlaces, showWeightedCounts, "counter", 1, displaySignificanceDifferences, "counter-series", 1, BarColour)];
        categories = results.map(r => getCategoryLabels(r, metric, displayEntityInstanceIds));
        allSampleSizes = results.map(r => r.weightedDailyResults[0].unweightedSampleSize);
    }

    hasDifferentSampleSizes = !allSampleSizes.every(size => size == allSampleSizes[0]);


    const seriesChunks = chunkArray(series, MaxFunnelsSupportedPerChart);
    const chartOptions: Options[] = seriesChunks.map(seriesChunk => ({
        ...getChartOptions(seriesChunk, categories, highlightLowSample),
        tooltip: {
            enabled: true,
            useHTML: true,
            outside: true,
            distance: 45,
            className: "custom-tooltip-container",
            formatter: function (this: any): string {
                const point = this.point.options;
                const name = this.point.name;
                const roundedValue = getFormattedValueText(point.y!, metric, decimalPlaces);
                const sampleInfo = hasDifferentSampleSizes ? ` of ${NumberFormattingHelper.formatCount(point.sampleSize)}` : '';
                const sampleDiv = `<div class="custom-tooltip-point">n = ${NumberFormattingHelper.formatCount(point.count)}${sampleInfo}</div>`;

                return `<div class="custom-tooltip-title">
                            <span>${name}</span>
                        </div>
                        <div class="custom-tooltip-point">Value: ${roundedValue}</div>
                        ${sampleDiv}`;
            },
        }
    }));

    return chartOptions;
}

export function getOverTimeFunnelChartOptions(
    results: EntityWeightedDailyResults[],
    metric: Metric,
    average: IAverageDescriptor,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    highlightLowSample: boolean,
    displayEntityInstanceIds: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    variable?: VariableConfigurationModel): Options[] {

    const validationErrors = validateMetricForFunnelChart(metric, variable);
    if (validationErrors.length > 0) {
        throw new UnsupportedVariableError(validationErrors.join(", "));
    }

    const series = results[0].weightedDailyResults.map((dailyResult, index) => {
        const seriesName = formatOverTimeDate(average, dailyResult.date);
        return getOverTimeSeries(
            results,
            index,
            metric,
            decimalPlaces,
            showWeightedCounts,
            seriesName,
            results[0].weightedDailyResults.length,
            displaySignificanceDifferences,
            "counter-series",
            index,
            BarColour);
    });
    const categories = results.map(r => getCategoryLabels(r, metric, displayEntityInstanceIds));

    const seriesChunks = chunkArray(series, MaxFunnelsSupportedPerChart);
    const chartOptions: Options[] = seriesChunks.map(seriesChunk => ({
        ...getChartOptions(seriesChunk, categories, highlightLowSample),
        tooltip: {
            enabled: true,
            useHTML: true,
            outside: true,
            distance: 45,
            className: "custom-tooltip-container",
            formatter: function (this: any): string {
                const point = this.point.options;
                const name = this.point.name;
                const roundedValue = getFormattedValueText(point.y!, metric, decimalPlaces);
                const sampleDiv = `<div class="custom-tooltip-point">n = ${NumberFormattingHelper.formatCount(point.count)} of ${NumberFormattingHelper.formatCount(point.sampleSize)}</div>`;

                return `<div class="custom-tooltip-title">
                            <span>${name}</span>
                        </div>
                        <div class="custom-tooltip-point">Value: ${roundedValue}</div>
                        ${sampleDiv}`;
            },
        }
    }));

    return chartOptions;
}

function getCategoryLabels(entityInstance: EntityWeightedDailyResults, metric: Metric, includeId: boolean): string {
    if (entityInstance) {
        return (includeId && (entityInstance.entityInstance.id || entityInstance.entityInstance.id === 0))
            ? `${entityInstance.entityInstance.name} (${getMeanCalculationValue(entityInstance.entityInstance, metric)})`
            : entityInstance.entityInstance.name ? entityInstance.entityInstance.name : metric.displayName;
    }
    return metric.displayName;
}

function getSeries(
    results: EntityWeightedDailyResults[],
    metric: Metric,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    seriesName: string,
    totalSeriesCount: number,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    seriesClassName?: string,
    seriesIndex?: number,
    seriesColour?: string): SeriesColumnrangeOptions {
    const seriesId = `${seriesName}${seriesIndex ?? ''}`
    const labelColor = getLabelTextColor(seriesColour);
    let data = results.map(r => getPoint(r, metric, decimalPlaces, seriesId, labelColor, showWeightedCounts, displaySignificanceDifferences));

    return dataToFunnelOptions(metric, decimalPlaces, totalSeriesCount, data, seriesId, seriesName, seriesColour, seriesClassName);
}

function getOverTimeSeries(results: EntityWeightedDailyResults[],
    dateIndex: number,
    metric: Metric,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    seriesName: string,
    totalSeriesCount: number,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    seriesClassName?: string,
    seriesIndex?: number,
    seriesColour?: string): SeriesColumnrangeOptions
{
    const seriesId = `${seriesName}${seriesIndex ?? ''}`;
    const labelColor = getLabelTextColor(seriesColour);
    const data = results.map(r => {
        const result = r.weightedDailyResults[dateIndex];
        const instanceName = r.entityInstance?.name ?? metric.displayName;
        return getPointForFunnelWeightedDailyResult(result,
            metric,
            decimalPlaces,
            seriesId + instanceName,
            instanceName,
            labelColor,
            showWeightedCounts,
            displaySignificanceDifferences);
    });

    return dataToFunnelOptions(metric, decimalPlaces, totalSeriesCount, data, seriesId, seriesName, seriesColour, seriesClassName);
}

function dataToFunnelOptions(
    metric: Metric,
    decimalPlaces: number,
    totalSeriesCount: number,
    points: CustomPointOptionsObject[],
    seriesId: string,
    seriesName: string,
    seriesColour?: string,
    seriesClassName?: string
): SeriesColumnrangeOptions {
    const conversionData = getConversionPoint(points);

    const biggestWidthBarValue = Math.max(0, ...points.map(d => d.y || 0));
    const slateDarker = '#26292C';

    return {
        id: seriesId,
        name: seriesName,
        type: 'columnrange',
        color: seriesColour,
        data: conversionData,
        className: seriesClassName,
        showInLegend: false,
        dataLabels: [{
            enabled: true,
            allowOverlap: true,
            overflow: 'allow',
            useHTML: true,
            align: "center",
            inside: true,
            style: {
                fontSize: "14px",
                whiteSpace: "nowrap",
                fontWeight: "normal",
            },
            formatter: function (this: PointLabelObject): string {
                const pointOptions = this.point.options as CustomPointOptionsObject;
                if (pointOptions.y != null && pointOptions.y > 0) {
                    let lowXOffset = 0;
                    let colour = '';

                    const currentBarWidthRelativeToBiggestBar = (pointOptions.y / biggestWidthBarValue) * 100;
                    if (currentBarWidthRelativeToBiggestBar < (4 + decimalPlaces)) {
                        lowXOffset = 50;
                        colour = `color:${ slateDarker }`;
                    }
                    return `<span style="${colour};
                    right: ${lowXOffset}px;
                    position: relative;">
                            ${pointOptions.formattedText}
                        </span>`;
                }
                else {
                    return "";
                }
            }
        },
        {
            enabled: true,
            useHTML: true,
            allowOverlap: true,
            crop: false,
            overflow: 'allow',
            align: "right",
            verticalAlign: "middle",
            inside: false,
            formatter: function (this: { point: any }): string {
                if (this.point.index > 0) {
                    const pointOptions = this.point.options as CustomPointOptionsObject;
                    // Calculate dynamic x offset based on the data point value and number of series being displayed on plot area
                    const seriesCountOffsetMultiplier = 3;
                    const seriesCountOffset = Math.min(totalSeriesCount, MaxFunnelsSupportedPerChart) * seriesCountOffsetMultiplier;
                    const dynamicXOffset = (this.point.plotLow - this.point.plotHigh) + 75 - seriesCountOffset;
                    const formattedDifference = getFormattedValueText(this.point.difference, metric, decimalPlaces)
                    const noOfLevels = conversionData.length;
                    const baseLabelSize = 50; //the best px size for 5 level charts
                    const offset = (noOfLevels - 5) * 5; // adjust offset based on levels
                    const calculatedLabelSize = baseLabelSize - offset;
                    const fontSize = 17-noOfLevels;
                    // Return the label with the dynamic x offset
                    return `<span style="
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    color:${slateDarker};
                    position: relative;
                    border: 1px solid ${slateDarker};
                    border-radius: 50%;
                    font-size: ${fontSize}px;
                    font-weight: 600;
                    height: ${calculatedLabelSize}px;
                    width: ${calculatedLabelSize}px;
                    left: ${dynamicXOffset}px;">
                         ${formattedDifference}
                    </span>`;
                }
                else { return ''; }

            }
        }],
    };
}

function getPoint(r: EntityWeightedDailyResults,
    metric: Metric,
    decimalPlaces: number,
    seriesId: string,
    labelColor: string | undefined,
    showWeightedCounts: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences) {
    const result = r.weightedDailyResults[0];
    const instanceName = r.entityInstance?.name ?? metric.displayName;
    return getPointForFunnelWeightedDailyResult(result, metric, decimalPlaces, seriesId + instanceName, instanceName, labelColor,
        showWeightedCounts, displaySignificanceDifferences);
}

function getConversionPoint(data) {
    return data.map((point, index, array) => {
        if (index === 0) {
            return { ...point, difference: null }; // No previous element for the first item
        }
        else {
            const previousY = array[index - 1].y;
            const difference = (point.y / previousY);
            return { ...point, difference };
        }
    });
};

function setChartWidth(chart) {
    const chartWidth = chart.chartWidth;
    const barValues = chart.series[0].data;
    const largeChartSizeArea = 1000;
    const chartAreaMax = [1.4, 1.2];
    const minDifference = 0.1;
    let increaseChartArea = false;
    if ((barValues[0].high - barValues[1].high < minDifference)) {
        increaseChartArea = true;
    }
    if (increaseChartArea) {
        if (chartWidth < largeChartSizeArea) {
            chart.yAxis[0].update({
                max: barValues[0].high*chartAreaMax[0]
            });
        }
        else {
            chart.yAxis[0].update({
                max: barValues[0].high*chartAreaMax[1]
            });
        }
    }
};
function getSeriesTitle(title: string | undefined) {
    if (title) {
        return {
            useHTML: true,
            style: {
                fontSize: "16px",
                padding: '0 20px'
            },
            text: `<div class="threeLineClamp" title="${title}">${title}</div>`
                }
    }
}

function getyAxis(seriesNames: string[]): Highcharts.YAxisOptions | Highcharts.YAxisOptions[] {
    const yAxis = {
        title: {
            text: "",
        },
        labels: {
            enabled: false
        },
        gridLineWidth: 0,
        tickInterval: 0.01
    } as Highcharts.YAxisOptions;

    const seriesCount = seriesNames.length;

    if (seriesCount > 1) {
        const yAxes: Highcharts.YAxisOptions[] = [];
        const seriesSpacer = 2;
        const plotAreaRightSpacer = 2;
        const spacers = (plotAreaRightSpacer + (seriesCount * seriesSpacer));
        const axisWidth = (100 - spacers) / seriesCount;

        for (let i = 0; i < Math.min(seriesCount, MaxFunnelsSupportedPerChart); i++) {
            const yAxisPart = {
                ...yAxis,
                title: getSeriesTitle(seriesNames[i]),
                offset: 0,
                opposite: true,
                width: `${axisWidth}%`,
                left: `${(axisWidth + seriesSpacer) * i}%`
            }

            yAxes.push(yAxisPart);
        }

        return yAxes;
    }

    return yAxis;
}

function assignEachSeriesToYAxis(series: SeriesColumnrangeOptions[]) {
    return series.map((s, i) => ({
        ...s,
        yAxis: i
    }));
}

function getChartOptions(series: SeriesColumnrangeOptions[],
    categories: string[],
    highlightLowSample: boolean): Options {

    if (highlightLowSample) {
        BrandVueOnlyLowSampleHelper.addLowSampleIndicators(series);
    }
    return {
        chart: {
            type: "columnrange",
            animation: true,
            backgroundColor: 'transparent',
            scrollablePlotArea: { minHeight: 200 },
            spacingRight: series.length > 1 ? 25 : 10,
            inverted: true,
            events: {
                load: function (this: any) {
                    const chart = this;
                    if (chart.series?.length === 1) {
                        setChartWidth(chart);
                        chart.redraw();
                    }
                },
            },
        },
        series: series.length > 1 ? assignEachSeriesToYAxis(series) : series,
        xAxis: {
            type: 'category',
            categories: categories,
            labels: {
                enabled: true,
                style: {
                    fontSize: "14px",
                    color: '#666',
                },
                formatter: function (this: any): string {
                    const chartWidth = this.chart.chartWidth;
                    const largeChartSizeArea = 1000;
                    let funnelLevelLabelWidth = '200px';

                    if (chartWidth < largeChartSizeArea) {
                        funnelLevelLabelWidth = '80px';
                    }

                    return `<span style="display: inline-block;
                    max-width:${funnelLevelLabelWidth};
                    text-overflow: ellipsis;
                    white-space: nowrap;
                    overflow: hidden;">${this.value}</span>`
                },
            },
        },
        yAxis: getyAxis(series.map(s => s.name) as string[]),
        legend: {
            enabled: false
        },
        plotOptions: {
            columnrange: {
                groupPadding: 0,
            },
            series: {
                animation: {
                    duration: 500,
                },
                states: {
                    hover: {
                        enabled: true,
                    },
                }
            }
        }
    };
}