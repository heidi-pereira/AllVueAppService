import {
    IAverageDescriptor,
    CrossbreakCompetitionResults,
    CrosstabAverageResults,
    CrosstabBreakAverageResults,
    DisplaySignificanceDifferences,
    EntityInstance,
    EntityWeightedDailyResults,
    GroupedCrossbreakCompetitionResults,
    WeightedDailyResult
} from '../../../../../BrandVueApi';
import {Metric} from '../../../../../metrics/metric';
import {Options, PointLabelObject, SeriesBarOptions} from 'highcharts';
import {getOverTimeChartCategories, getFormattedValueText} from '../../../../helpers/SurveyVueUtils';
import {BarColour, BarPointWidth} from '../../Cards/ReportsPageCardChartContent';
import {
    CustomPointOptionsObject,
    getInversePointForWeightedDailyResult,
    getNegativeInversePointForWeightedDailyResult,
    getNumberOfItems,
    getPointForWeightedDailyResult
} from './PointOptions';
import BrandVueOnlyLowSampleHelper, {getLowSampleThreshold} from '../../../BrandVueOnlyLowSampleHelper';
import {Chalk, ChalkLight, getLabelTextColor, Slate, SlateDark} from '../../../../helpers/ChromaHelper';
import {ICommonDataPoint} from '../../../ICommonDataPoint';
import {getAverageDisplayText} from '../../../AverageHelper';
import { NumberFormattingHelper } from '../../../../../helpers/NumberFormattingHelper';
import { getSignificance } from './HighchartsOptionsHelper';
import { DateFormattingHelper } from '../../../../../helpers/DateFormattingHelper';

const barSize = 56;

export function getBarChartOptions(results: EntityWeightedDailyResults[],
    metric: Metric,
    decimalPlaces: number,
    isWeighted: boolean,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    averageResults: CrosstabAverageResults[],
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options
{
    const {minResult, maxResult} = getMinMaxResult(metric, () =>
        results
            .map(r => r.weightedDailyResults[0]?.weightedResult ?? 0)
            .concat(averageResults.flatMap(r => r.dailyResultPerBreak.map(br => br.weightedDailyResult[0]?.weightedResult ?? 0)))
    );

    const labelOffsetY = -35;
    const series = getSeries(results, minResult, maxResult, metric, decimalPlaces, isWeighted, labelOffsetY, highlightLowSample, showWeightedCounts,
        averageResults, displaySignificanceDifferences);
    let categories = results.map(r => r.entityInstance?.name ?? metric.displayName);
    averageResults.forEach(ar => {
        categories.push(getAverageDisplayText(ar.averageType))
    });
    const chartHeight = (results.length + averageResults.length) * (barSize + BarPointWidth);
    const options = getChartOptions(series, categories, labelOffsetY, chartHeight, BarPointWidth, 0);
    return options;
}

export function getSplitBarChartOptions(results: CrossbreakCompetitionResults,
    metric: Metric,
    breakToColourMap: Map<string, string>,
    decimalPlaces: number,
    isWeighted: boolean,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    averageResults: CrosstabAverageResults[],
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options
{
    const {minResult, maxResult} = getMinMaxResult(metric, () =>
        results.instanceResults
            .flatMap(splitResult => splitResult.entityResults.map(r => r.weightedDailyResults[0]?.weightedResult ?? 0))
            .concat(averageResults.flatMap(r => r.dailyResultPerBreak.map(br => br.weightedDailyResult[0]?.weightedResult ?? 0)))
    );

    let series = results.instanceResults.flatMap((splitResult, index) => {
        return getSplitSeries(splitResult.entityResults,
            minResult,
            maxResult, 
            metric,
            breakToColourMap, 
            decimalPlaces,
            index,
            splitResult.breakName,
            highlightLowSample,
            showWeightedCounts,
            false,
            displaySignificanceDifferences);
    });

    averageResults.forEach(ar => {
        const instanceName = getAverageDisplayText(ar.averageType);
        ar.dailyResultPerBreak.forEach((drpb, index) => {
            const seriesId = index;
            const seriesColour = breakToColourMap.get(drpb.breakName);
            const labelColor = getLabelTextColor(seriesColour);

            const [inverseSeries, negativeInverseSeries, dataSeries] = getSeriesForDailyResult(
                drpb.weightedDailyResult,
                minResult,
                maxResult,
                metric,
                decimalPlaces,
                isWeighted,
                index,
                seriesId + instanceName,
                instanceName,
                true,
                metric.downIsGood,
                labelColor,
                displaySignificanceDifferences
            );

            const stackedSeries = series.filter(s => s.stack == index);
            let matchingInverseSeries = stackedSeries[0];
            let matchingNegativeInverseSeries = stackedSeries.length == 3 ? stackedSeries[1] : undefined;
            let matchingDataSeries = stackedSeries.length == 3 ? stackedSeries[2] : stackedSeries[1];
            if (matchingDataSeries){
                matchingDataSeries.data = matchingDataSeries.data?.concat(dataSeries.data!);
            }
            if (matchingInverseSeries) {
                matchingInverseSeries.data = matchingInverseSeries.data?.concat(inverseSeries.data!);
            }
            if (matchingNegativeInverseSeries) {
                matchingNegativeInverseSeries.data = matchingNegativeInverseSeries.data?.concat(negativeInverseSeries.data!)
            }
        })
    })

    let categories = results.instanceResults[0].entityResults.map(i => i.entityInstance?.name ?? metric.displayName);
    averageResults.forEach(ar => {
        categories.push(getAverageDisplayText(ar.averageType))
    });

    const numberOfSplitBars = results.instanceResults.length + averageResults.length;
    const numberOfInstances = results.instanceResults[0].entityResults.length + averageResults.length;
    const numberOfLabels = numberOfInstances;
    const totalNumberOfBars = numberOfSplitBars * numberOfInstances;
    const pointWidth = 25;
    const barSpacing = pointWidth + 5;
    const chartHeight = 90 + (barSpacing * totalNumberOfBars) + (pointWidth * numberOfLabels) + (numberOfInstances * 5);
    const labelOffsetY = ((numberOfSplitBars / 2.0) * -barSpacing) + 2;
    const groupPadding = (50 + (numberOfInstances * 15)) / chartHeight;
    const pointPlacement = -(numberOfInstances * 14) / chartHeight

    let instanceToTooltipMap = getInstanceToTooltipMap(results, metric, breakToColourMap, decimalPlaces, showWeightedCounts, averageResults, isWeighted);

    return {
        ...getChartOptions(series, categories, labelOffsetY, chartHeight, pointWidth, groupPadding, pointPlacement),
        tooltip: {
            enabled: true,
            useHTML: true,
            outside: true,
            className: "custom-tooltip-container",
            formatter: function (this: any): string {
                const instanceName = this.point.name;
                return instanceToTooltipMap.get(instanceName) ?? "";
            }
        },
    };
}

export function getOvertimeBarChartOptions(results: EntityWeightedDailyResults[],
    metric: Metric,
    average: IAverageDescriptor,
    entityInstanceColourMap: Map<string, string>,
    decimalPlaces: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options
{
    const {minResult, maxResult} = getMinMaxResult(metric, () =>
        results.flatMap(entityResult => entityResult.weightedDailyResults.map(r => r?.weightedResult ?? 0))
    );

    let series = results.flatMap((entityResult, index) => {
        const seriesName = entityResult.entityInstance?.name ?? metric.displayName;
        const weightedDailyResults = [...entityResult.weightedDailyResults].reverse();
        return getOvertimeSeries(weightedDailyResults,
            minResult,
            maxResult,
            metric,
            average,
            decimalPlaces,
            index,
            seriesName,
            entityInstanceColourMap.get(seriesName),
            highlightLowSample,
            showWeightedCounts,
            displaySignificanceDifferences);
    });

    const categories = getOverTimeChartCategories(average, results[0].weightedDailyResults).reverse();

    const numberOfSplitBars = results.length;
    const numberOfInstances = results[0].weightedDailyResults.length;
    const numberOfLabels = numberOfInstances;
    const totalNumberOfBars = numberOfSplitBars * numberOfInstances;
    const pointWidth = 25;
    const barSpacing = pointWidth + 5;
    const chartHeight = 90 + (barSpacing * totalNumberOfBars) + (pointWidth * numberOfLabels) + (numberOfInstances * 5);
    const labelOffsetY = ((numberOfSplitBars / 2.0) * -barSpacing) + 2;
    const groupPadding = (50 + (numberOfInstances * 15)) / chartHeight;
    const pointPlacement = -(numberOfInstances * 14) / chartHeight

    return {
        ...getChartOptions(series, categories, labelOffsetY, chartHeight, pointWidth, groupPadding, pointPlacement),
    };
}

export function getMultiBreakBarChartOptions(
    results: GroupedCrossbreakCompetitionResults,
    metric: Metric,
    decimalPlaces: number,
    isWeighted: boolean,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): Options
{
    const {minResult, maxResult} = getMinMaxResult(metric, () =>
        results.groupedBreakResults.flatMap(group =>
            group.breakResults.instanceResults.flatMap(breakResults => breakResults.entityResults.map(r => r.weightedDailyResults[0]?.weightedResult ?? 0))
        )
    );
    
    const categories = results.groupedBreakResults.map(group => {
        return {
            name: group.groupName,
            categories: group.breakResults.instanceResults.map(r => r.breakName),
            data: group.breakResults.instanceResults.map(i => {
                const entityResult = new EntityWeightedDailyResults({...i.entityResults[0]});
                entityResult.entityInstance = new EntityInstance({...entityResult.entityInstance})
                entityResult.entityInstance.name = i.breakName;
                return entityResult;
            }) 
        }
    });
    for (var categoryIndex  = 1; categoryIndex  < categories.length; categoryIndex  += 2) {
        const emptyEntityWeightedResults = new EntityWeightedDailyResults({
            entityInstance: new EntityInstance(),
            weightedDailyResults: [new WeightedDailyResult()] ,
            unweightedResponseCount: 0,
            weightedResponseCount: 0,
        })
        const emptyCategory = {
            name: '',
            categories: [''],
            data: [emptyEntityWeightedResults] 
        };
        categories.splice(categoryIndex , 0, emptyCategory);
    }

    const labelOffsetY = -35;
    
    const flattenedData = categories.flatMap((c, index) => c.data);
    const series = getSeries(flattenedData, minResult, maxResult, metric, decimalPlaces, isWeighted, labelOffsetY, highlightLowSample, showWeightedCounts,
        [], displaySignificanceDifferences);

    const chartHeight = (flattenedData.length) * (barSize + BarPointWidth);
    return {
        ...getChartOptions(series, categories.flatMap(c => c.categories), labelOffsetY, chartHeight, BarPointWidth, 0),
        tooltip: {
            enabled: true,
            useHTML: true,
            outside: true,
            className: "custom-tooltip-container",
            formatter: function (this: any): string {
                return getMultiBreakTooltip(this, results);
            }
        },
    };
}


const getMultiBreakTooltip = (pointer: any, results: GroupedCrossbreakCompetitionResults) => {
    const pointerInstanceName = pointer.point.name;
    const breakName = results.groupedBreakResults.find(g => g.breakResults.instanceResults.some(i => i.breakName === pointerInstanceName))?.groupName ?? "";
    const selectedEntityInstanceName = results.groupedBreakResults[0].breakResults.instanceResults[0].entityResults[0].entityInstance.name ?? ""
    const allPointOptions = pointer.point.series.points.map(p => p.options)

    return (
        `
            <div class="custom-tooltip-title"> 
                <span>${selectedEntityInstanceName}</span>
            </div>
            <div>
                <span>${breakName}</span>
            </div>
            ${results.groupedBreakResults.find(g => g.groupName === breakName)?.breakResults.instanceResults.map(i => {
                const data = allPointOptions.find(o => o.name === i.breakName);
                return `
                <div class="custom-tooltip-row">
                    <span>
                        ${data.name}
                    </span>
                    <span class="custom-tooltip-offset-container">
                        ${data.formattedText}
                        <span class="custom-tooltip-light">
                            (${(NumberFormattingHelper.formatCount(data.count))} of ${(NumberFormattingHelper.formatCount(data.sampleSize)) })
                        </span>
                    </span>
                </div>
                `}).join('')}
        `
    )
}

function getMinMaxResult(metric: Metric, getResults: () => number[]) {
    let minResult = 0;
    let maxResult = 1;
    if (!metric.isPercentage()) {
        const allResultValues = getResults();
        minResult = Math.min(...allResultValues);
        maxResult = Math.max(...allResultValues);

        if (minResult < 0 && maxResult > 0) {
            //has both negative and positive, make the chart centered around 0
            maxResult = Math.max(maxResult, Math.abs(minResult));
            minResult = -1 * maxResult;
        }
    }
    return {minResult: minResult, maxResult: maxResult};
}

function getSeriesForDailyResult(result: WeightedDailyResult,
    minResult: number,
    maxResult: number,
    metric: Metric,
    decimalPlaces: number,
    isWeighted: boolean,
    labelOffsetY: number,
    pointId: string,
    pointName: string,
    isAveragePoint: boolean,
    downIsGood: boolean,
    labelColor: string | undefined,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesBarOptions[]
{
    const showInverse = maxResult > 0;
    const showNegativeInverse = minResult < 0;

    const data = [getPointForWeightedDailyResult(result, metric, decimalPlaces, pointId, pointName, labelColor, false, isAveragePoint, displaySignificanceDifferences)];
    const inverseData = showInverse ?
        [getInversePointForWeightedDailyResult(result, maxResult, metric, decimalPlaces, pointId, pointName, false, isAveragePoint)] : [];
    const negativeInverseData = showNegativeInverse ?
        [getNegativeInversePointForWeightedDailyResult(result, minResult, metric, decimalPlaces, pointId, pointName, false, isAveragePoint)] : [];
    return getSeriesBarOptions(data,
        inverseData,
        negativeInverseData,
        labelOffsetY,
        false,
        false,
        showInverse,
        decimalPlaces,
        isWeighted,
        downIsGood);
}

function getSeries(results: EntityWeightedDailyResults[],
    minResult: number,
    maxResult: number,
    metric: Metric,
    decimalPlaces: number,
    isWeighted: boolean,
    labelOffsetY: number,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    averageResults: CrosstabAverageResults[],
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesBarOptions[]
{
    const showInverse = maxResult > 0;
    const showNegativeInverse = minResult < 0;

    let data = results.map(r => getPoint(r, metric, decimalPlaces, SlateDark, showWeightedCounts, false, displaySignificanceDifferences));
    let inverseData = showInverse ?
        results.map(r => getInversePoint(r, maxResult, metric, decimalPlaces, showWeightedCounts, false)) : [];
    let negativeInverseData = showNegativeInverse ?
        results.map(r => getNegativeInversePoint(r, minResult, metric, decimalPlaces, showWeightedCounts, false)) : [];
    if(averageResults.length > 0) {
        ({ data, inverseData, negativeInverseData } = AddAverageData(
            minResult,
            maxResult,
            averageResults,
            data,
            inverseData,
            negativeInverseData,
            metric,
            decimalPlaces,
            showInverse,
            showNegativeInverse,
            displaySignificanceDifferences));
    }
    const allSampleSizes = results.map(r => r.weightedDailyResults[0].unweightedSampleSize);
    const hasDifferentSampleSizes = metric.isPercentage() && !allSampleSizes.every(size => size == allSampleSizes[0]);
    return getSeriesBarOptions(data,
        inverseData,
        negativeInverseData,
        labelOffsetY,
        hasDifferentSampleSizes,
        highlightLowSample,
        showInverse,
        decimalPlaces,
        isWeighted,
        metric.downIsGood);
}

function getSeriesBarOptions(data: CustomPointOptionsObject[],
    inverseData: CustomPointOptionsObject[],
    negativeInverseData: CustomPointOptionsObject[],
    labelOffsetY: number,
    hasDifferentSampleSizes: boolean,
    highlightLowSample: boolean,
    showInverse: boolean,
    decimalPlaces: number,
    isWeighted: boolean,
    downIsGood: boolean)
{
    const dataLabelConfig = {
        allowOverlap: true,
        enabled: true,
        useHTML: true,
        align: 'right',
        x: 0 as any,
        y: labelOffsetY - 10 as any,
        style: {
            fontSize: "14px",
            whiteSpace: "nowrap",
            fontWeight: "normal",
            color: SlateDark,
        },
        formatter: function (this: PointLabelObject): string {
            const point = this.point.options as CustomPointOptionsObject;
            const sampleInfo = hasDifferentSampleSizes ? ` of ${NumberFormattingHelper.formatCount(point.sampleSize)}` : '';
            const sampleData = point.isAveragePoint ?
                ``
                :
                `<span style="color:${Slate};">(${NumberFormattingHelper.formatCount(point.count)}${sampleInfo})</span>`;

            return `<span style="color:${SlateDark};">
                        ${point.formattedText}
                        ${getSignificance(point.significance, downIsGood)}
                        ${sampleData}
                    </span>`
        }
    };

    let counterSeries: SeriesBarOptions = {
        id: 'counter',
        name: 'counter',
        type: 'bar',
        data: data,
        color: BarColour,
        className: 'counter-series',
        clip: false,
    };

    let inverseSeries: SeriesBarOptions = {
        id: 'inverse',
        name: 'inverse',
        type: 'bar',
        color: Chalk,
        data: inverseData,
        linkedTo: 'counter',
    };

    const negativeInverseSeries: SeriesBarOptions = {
        id: 'negative-inverse',
        name: 'negative-inverse',
        type: 'bar',
        color: Chalk,
        data: negativeInverseData,
        linkedTo: 'counter',
    }

    if (highlightLowSample) {
        BrandVueOnlyLowSampleHelper.addLowSampleIndicators([counterSeries]);
        lightenInverseSeriesIfLowSample(inverseSeries);
        lightenInverseSeriesIfLowSample(negativeInverseSeries);
    }

    if (showInverse) {
        inverseSeries = {
            ...inverseSeries,
            dataLabels: dataLabelConfig
        };
    } else {
        counterSeries = {
            ...counterSeries,
            dataLabels: dataLabelConfig
        };
    }

    const series: SeriesBarOptions[] = [];
    series.push(inverseSeries);
    series.push(negativeInverseSeries);
    series.push(counterSeries);

    return series;
}

function AddAverageData(minResult: number,
    maxResult: number,
    averageResults: CrosstabAverageResults[],
    data: CustomPointOptionsObject[],
    inverseData: CustomPointOptionsObject[],
    negativeInverseData: CustomPointOptionsObject[],
    metric: Metric,
    decimalPlaces: number,
    showInverse: boolean,
    showNegativeInverse: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences)
{
    if (averageResults[0].dailyResultPerBreak.length > 0) {
        averageResults.forEach(ar => {
            data = data.concat(ar.dailyResultPerBreak.map(r => getAveragePoint(r, metric, decimalPlaces, 
                getAverageDisplayText(ar.averageType), true, SlateDark, displaySignificanceDifferences)));
        });
        if (showInverse) {
            averageResults.forEach(ar => {
                inverseData = inverseData.concat(ar.dailyResultPerBreak.map(r => getInverseAveragePoint(r, maxResult, metric, decimalPlaces, getAverageDisplayText(ar.averageType), true)));
            });
        }
        if (showNegativeInverse) {
            averageResults.forEach(ar => {
                negativeInverseData = negativeInverseData.concat(ar.dailyResultPerBreak.map(r => getNegativeInverseAveragePoint(r, minResult, metric, decimalPlaces, getAverageDisplayText(ar.averageType), true)));
            });
        }
    } else {
        averageResults.forEach(ar => {
            data = data.concat(getAveragePoint(ar.overallDailyResult, metric, decimalPlaces, getAverageDisplayText(ar.averageType),
                true, SlateDark, displaySignificanceDifferences));
        });
        if (showInverse) {
            averageResults.forEach(ar => {
                inverseData = inverseData.concat(getInverseAveragePoint(ar.overallDailyResult, maxResult, metric, decimalPlaces, getAverageDisplayText(ar.averageType), true));
            });
        }
        if (showNegativeInverse) {
            averageResults.forEach(ar => {
                negativeInverseData = negativeInverseData.concat(getNegativeInverseAveragePoint(ar.overallDailyResult, minResult, metric, decimalPlaces, getAverageDisplayText(ar.averageType), true));
            });
        }
    }
    return { data, inverseData, negativeInverseData };
}

function getSplitSeries(results: EntityWeightedDailyResults[],
    minResult: number,
    maxResult: number,
    metric: Metric,
    breakToColourMap: Map<string, string>,
    decimalPlaces: number,
    index: number,
    seriesName: string,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    isAveragePoint: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesBarOptions[]
{
    const showInverse = maxResult > 0;
    const showNegativeInverse = minResult < 0;
    const seriesColour = breakToColourMap.get(seriesName);
    const labelColor = getLabelTextColor(seriesColour);

    const data = results.map(r => getPoint(r, metric, decimalPlaces, labelColor, showWeightedCounts, isAveragePoint, displaySignificanceDifferences));
    const inverseData = showInverse ?
        results.map(r => getInversePoint(r, maxResult, metric, decimalPlaces, showWeightedCounts, isAveragePoint)) : [];
    const negativeInverseData = showNegativeInverse ?
    results.map(r => getNegativeInversePoint(r, minResult, metric, decimalPlaces, showWeightedCounts, isAveragePoint)) : [];

    return dataToSplitSeries(metric, index, data, inverseData, negativeInverseData, seriesName, seriesColour, highlightLowSample, showInverse, showNegativeInverse);
}

function getOvertimeSeries(weightedDailyResults: WeightedDailyResult[],
    minResult: number,
    maxResult: number,
    metric: Metric,
    average: IAverageDescriptor,
    decimalPlaces: number,
    index: number,
    seriesName: string,
    seriesColour: string | undefined,
    highlightLowSample: boolean,
    showWeightedCounts: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): SeriesBarOptions[]
{
    const showInverse = maxResult > 0;
    const showNegativeInverse = minResult < 0;
    const labelColor = getLabelTextColor(seriesColour);

    const data = weightedDailyResults.map(r => getOvertimePoint(r, metric, decimalPlaces, labelColor, showWeightedCounts, average, displaySignificanceDifferences));
    const inverseData = showInverse ?
        weightedDailyResults.map(r => getInverseOvertimePoint(r, maxResult, metric, decimalPlaces, showWeightedCounts, average)) : [];
    const negativeInverseData = showNegativeInverse ?
        weightedDailyResults.map(r => getNegativeInverseOvertimePoint(r, minResult, metric, decimalPlaces, showWeightedCounts, average)) : [];
    return dataToSplitSeries(metric, index, data, inverseData, negativeInverseData, seriesName, seriesColour, highlightLowSample, showInverse, showNegativeInverse);
}

function dataToSplitSeries(
    metric: Metric,
    index: number,
    data: CustomPointOptionsObject[],
    inverseData: CustomPointOptionsObject[],
    negativeInverseData: CustomPointOptionsObject[],
    seriesName: string,
    seriesColour: string | undefined,
    highlightLowSample: boolean,
    showInverse: boolean,
    showNegativeInverse: boolean
): SeriesBarOptions[] {
    const dataName = `dataseries-${metric.urlSafeName}-${index}`;
    const inverseName = `inverseseries-${metric.urlSafeName}-${index}`;
    const negativeInverseName = `negative-${inverseName}`;

    const inverseSeries: SeriesBarOptions = {
        id: inverseName,
        name: inverseName,
        type: 'bar',
        color: Chalk,
        data: inverseData,
        stack: index,
        linkedTo: dataName
    }

    const negativeInverseSeries: SeriesBarOptions = {
        id: negativeInverseName,
        name: negativeInverseName,
        type: 'bar',
        color: Chalk,
        data: negativeInverseData,
        stack: index,
        linkedTo: dataName
    }

    const dataSeries: SeriesBarOptions = {
        id: dataName,
        name: seriesName,
        type: 'bar',
        color: seriesColour,
        data: data,
        stack: index,
        clip: false,
        dataLabels: {
            allowOverlap: false,
            enabled: true,
            verticalAlign: "middle",
            overflow: "allow",
            crop: false,
            style: {
                fontSize: "14px",
                whiteSpace: "nowrap",
                fontWeight: "normal",
                textOutline: "none"
            },
            useHTML: true,
            formatter: function(this: PointLabelObject): string | undefined {
                const shapeArgs = (this.point as any).shapeArgs;
                const point = this.point.options as CustomPointOptionsObject;
                const roundedValue = point.formattedText;
                if (shapeArgs.height > roundedValue.length * 10) {
                    return `<div>
                                ${roundedValue.toString()}
                                ${getSignificance(point.significance, metric.downIsGood)}
                            </div>`

                }
            }
        },
    };

    if (highlightLowSample) {
        lightenInverseSeriesIfLowSample(inverseSeries);
        lightenInverseSeriesIfLowSample(negativeInverseSeries);
        BrandVueOnlyLowSampleHelper.addLowSampleIndicators([dataSeries]);
    }

    const series: SeriesBarOptions[] = [];
    if (showInverse) series.push(inverseSeries);
    if (showNegativeInverse) series.push(negativeInverseSeries);
    series.push(dataSeries);

    return series;
}

function getPoint(r: EntityWeightedDailyResults, metric: Metric, decimalPlaces: number, labelColor: string | undefined, showWeightedCounts: boolean,
    isAveragePoint: boolean, displaySignificanceDifferences: DisplaySignificanceDifferences): CustomPointOptionsObject {
    const result = r.weightedDailyResults[0];
    const instanceName = r.entityInstance?.name ?? metric.displayName;
    return getPointForWeightedDailyResult(result, metric, decimalPlaces, "data" + instanceName, instanceName, labelColor, showWeightedCounts, isAveragePoint, displaySignificanceDifferences);
}

function getOvertimePoint(result: WeightedDailyResult, metric: Metric, decimalPlaces: number, labelColor: string | undefined, showWeightedCounts: boolean,
    average: IAverageDescriptor, displaySignificanceDifferences: DisplaySignificanceDifferences) {
    const date = DateFormattingHelper.formatDatePoint(result.date, average);
    return getPointForWeightedDailyResult(result, metric, decimalPlaces, "data" + date, date, labelColor, showWeightedCounts, false, displaySignificanceDifferences);
}

function getAveragePoint(r: CrosstabBreakAverageResults, metric: Metric, decimalPlaces: number, instanceName: string, isAveragePoint: boolean,
    labelColor: string | undefined, displaySignificanceDifferences: DisplaySignificanceDifferences): CustomPointOptionsObject {
    const result = r.weightedDailyResult;
    return getPointForWeightedDailyResult(result, metric, decimalPlaces, "data" + instanceName, instanceName, labelColor, false, isAveragePoint, displaySignificanceDifferences);
}

function getInversePoint(r: EntityWeightedDailyResults, maxResult: number, metric: Metric, decimalPlaces: number, showWeightedCounts: boolean, isAveragePoint: boolean): CustomPointOptionsObject {
    const result = r.weightedDailyResults[0];
    const instanceName = r.entityInstance?.name ?? metric.displayName;
    return getInversePointForWeightedDailyResult(result, maxResult, metric, decimalPlaces, "inverse" + instanceName, instanceName, showWeightedCounts, isAveragePoint);
}

function getInverseOvertimePoint(result: WeightedDailyResult, maxResult: number, metric: Metric, decimalPlaces: number, showWeightedCounts: boolean, average: IAverageDescriptor): CustomPointOptionsObject {
    const date = DateFormattingHelper.formatDatePoint(result.date, average);
    return getInversePointForWeightedDailyResult(result, maxResult, metric, decimalPlaces, "inverse" + date, date, showWeightedCounts, false);
}

function getInverseAveragePoint(r: CrosstabBreakAverageResults, maxResult: number, metric: Metric, decimalPlaces: number, instanceName: string, isAveragePoint: boolean): CustomPointOptionsObject {
    const result = r.weightedDailyResult;
    return getInversePointForWeightedDailyResult(result, maxResult, metric, decimalPlaces, "inverse" + instanceName, instanceName, false, isAveragePoint);
}

function getNegativeInversePoint(r: EntityWeightedDailyResults, minResult: number, metric: Metric, decimalPlaces: number, showWeightedCounts: boolean, isAveragePoint: boolean): CustomPointOptionsObject {
    const result = r.weightedDailyResults[0];
    const instanceName = r.entityInstance?.name ?? metric.displayName;
    return getNegativeInversePointForWeightedDailyResult(result, minResult, metric, decimalPlaces, "negative-inverse" + instanceName, instanceName, showWeightedCounts, isAveragePoint);
}

function getNegativeInverseOvertimePoint(result: WeightedDailyResult, minResult: number, metric: Metric, decimalPlaces: number, showWeightedCounts: boolean, average: IAverageDescriptor): CustomPointOptionsObject {
    const date = DateFormattingHelper.formatDatePoint(result.date, average);
    return getNegativeInversePointForWeightedDailyResult(result, minResult, metric, decimalPlaces, "negative-inverse" + date, date, showWeightedCounts, false);
}

function getNegativeInverseAveragePoint(r: CrosstabBreakAverageResults, minResult: number, metric: Metric, decimalPlaces: number, instanceName: string, isAveragePoint: boolean): CustomPointOptionsObject {
    const result = r.weightedDailyResult;
    return getNegativeInversePointForWeightedDailyResult(result, minResult, metric, decimalPlaces, "negative-inverse" + instanceName, instanceName, false, isAveragePoint);
}

function getToolTip(instanceName: string, breakToColourMap: Map<string, string>,
    resultsPerBreak: { breakName: string, roundedValue: string, isAverage: boolean, count?: number }[]): string {
    return (
        `<div class="custom-tooltip-title">
                            <span>${instanceName}</span>
                        </div>
                        ${resultsPerBreak.map(r => {
                            const includeCount = r.isAverage ?
                            '' :
                                `<span class="custom-tooltip-light">(${(NumberFormattingHelper.formatCount(r.count))})</span>`

                            return `<div class="custom-tooltip-point custom-tooltip-row">
                                        <div class="custom-tooltip-row">
                                            <div class="custom-tooltip-dot" style="background: ${breakToColourMap.get(r.breakName)}; margin-right: 5px">&nbsp;</div>
                                            ${r.breakName}
                                        </div>
                                        <div class="custom-tooltip-row custom-tooltip-offset-container">
                                            ${r.roundedValue}&nbsp;
                                            ${includeCount}
                                        </div>
                                    </div>`;
                        }).join('\n')}`
    );
}

function getInstanceToTooltipMap(results: CrossbreakCompetitionResults, metric: Metric, breakToColourMap: Map<string, string>,
        decimalPlaces: number, showWeightedCounts: boolean, averageResults: CrosstabAverageResults[], isWeighted:boolean): Map<string,string> {
    const mappedResults = results.instanceResults[0].entityResults.map((instanceResults, index) => {
        const instanceName = instanceResults.entityInstance?.name ?? metric.displayName;
        const resultsPerBreak = results.instanceResults.map(breakResults => {
            const breakName = breakResults.breakName;
            const result = breakResults.entityResults[index].weightedDailyResults[0];
            const value = result.weightedResult;
            const numberOfItems = getNumberOfItems(result, metric, showWeightedCounts);
            return {
                breakName: breakName,
                roundedValue: getFormattedValueText(value, metric, decimalPlaces),
                isAverage: false,
                count: numberOfItems,
            };
        });
        const tooltip = getToolTip(instanceName, breakToColourMap, resultsPerBreak)
        return {
            instanceName: instanceName,
            tooltip: tooltip,
        };
    });

    const mappedAverages = averageResults.map(ar => {
        const instanceName = getAverageDisplayText(ar.averageType);
        const resultsPerBreak = ar.dailyResultPerBreak.map((result) => {
            const breakName = result.breakName;
            const value = result.weightedDailyResult.weightedResult;
            return {
                breakName: breakName,
                roundedValue: getFormattedValueText(value, metric, decimalPlaces),
                isAverage: true,
            };
        });
        const tooltip = getToolTip(instanceName, breakToColourMap, resultsPerBreak);
        return {
            instanceName: instanceName,
            tooltip: tooltip,
        };
    })

    const allMapped = mappedResults.concat(mappedAverages);
    return new Map(allMapped.map(d => [d.instanceName, d.tooltip]));
}

function getChartOptions(
    series: SeriesBarOptions[],
    categories: string[],
    categoriesLabelOffsetY: number,
    chartHeight: number,
    pointWidth: number,
    groupPadding: number,
    pointPlacement?: number | string): Options
{
    return {
        chart: {type: "bar", animation: true, height: chartHeight, backgroundColor: 'transparent'},
        series: series,
        tooltip: {enabled: false},
        xAxis: {
            categories: categories,
            type: 'category',
            labels: {
                align: 'left',
                x: 0,
                y: categoriesLabelOffsetY,
                style: {
                    color: SlateDark,
                    fontSize: "14px",
                    whiteSpace: "nowrap",
                    fontWeight: "normal",
                    width: 450,
                    textOverflow: "ellipsis",
                },
            },
            lineWidth: 0,
            gridLineWidth: 0,
            height: chartHeight,
        },
        yAxis: {
            title: {text: undefined},
            labels: {enabled: false},
            lineWidth: 0,
            gridLineWidth: 0,
            startOnTick: false,
            endOnTick: false,
        },
        legend: {
            enabled: false,
        },
        plotOptions: {
            bar: {
                pointWidth: pointWidth,
                groupPadding: groupPadding,
                pointPlacement: pointPlacement,
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
                stacking: 'percent'
            }
        },
        responsive: {
            rules: [
            {
                condition: {
                    maxWidth: 400
                },
                chartOptions: {
                    xAxis: {
                        labels: {
                            style: {
                                width: 150,
                            },
                        },
                    },
                }
            },
            {
                condition: {
                    minWidth: 400,
                    maxWidth: 600
                },
                chartOptions: {
                    xAxis: {
                        labels: {
                            style: {
                                width: 250,
                            },
                        },
                    },
                }
            }]
        }
    };
}

function lightenInverseSeriesIfLowSample(inverseSeries: SeriesBarOptions) {
    for (var point of inverseSeries.data!) {
        var pointWithSample = point as ICommonDataPoint;

        if (pointWithSample && pointWithSample.sampleSize <= getLowSampleThreshold()) {
            pointWithSample.color = ChalkLight;
        }
    }
}