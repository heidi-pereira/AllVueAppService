import { PointOptionsObject } from 'highcharts';
import {DisplaySignificanceDifferences, Significance, WeightedDailyResult} from '../../../../../BrandVueApi';
import { Metric } from '../../../../../metrics/metric';
import { getFormattedValueText } from '../../../../helpers/SurveyVueUtils';
import { getShouldShowSignificance } from '../ReportsChartHelper';

export interface CustomPointOptionsObject extends PointOptionsObject {
    count: number;
    sampleSize: number;
    formattedText: string;
    subtitle?: string;
    isAveragePoint: boolean;
    significance: Significance;
    significanceHelpText: string | undefined;
}

export function getPointForWeightedDailyResult(
    result: WeightedDailyResult,
    metric: Metric,
    decimalPlaces: number,
    pointId: string,
    pointName: string,
    labelColor: string | undefined,
    showWeightedCounts: boolean,
    isAveragePoint: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    xValue?: number,
    subtitle?: string): CustomPointOptionsObject
{
    const sampleSize = getSampleForPoint(result, showWeightedCounts);
    const yValue = sampleSize > 0 ? result.weightedResult : undefined;
    return {
        id: pointId,
        name: pointName,
        x: xValue,
        y: yValue,
        count: getNumberOfItems(result, metric, showWeightedCounts),
        sampleSize: sampleSize,
        formattedText: getFormattedValueText(result.weightedResult, metric, decimalPlaces),
        dataLabels: {
            color: labelColor,
        },
        isAveragePoint: isAveragePoint,
        significance: getShouldShowSignificance(result.significance, displaySignificanceDifferences),
        significanceHelpText: result.sigificanceHelpText,
        subtitle: subtitle
    };
}

export function getPointForFunnelWeightedDailyResult(
    result: WeightedDailyResult,
    metric: Metric,
    decimalPlaces: number,
    pointId: string,
    pointName: string,
    labelColor: string | undefined,
    showWeightedCounts: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences): CustomPointOptionsObject {
    const point = getPointForWeightedDailyResult(result, metric, decimalPlaces, pointId, pointName, labelColor, showWeightedCounts, false, displaySignificanceDifferences)
    let lowValue;
    let highValue;
    if (point.y != undefined) {
        lowValue = -(point.y / 2);
        highValue = (point.y / 2);
    }
    return {
        ...point,
        low: lowValue,
        high: highValue
    };
}

export function getInversePointForWeightedDailyResult(
    result: WeightedDailyResult,
    maxResult: number,
    metric: Metric,
    decimalPlaces: number,
    pointId: string,
    pointName: string,
    showWeightedCounts: boolean,
    isAveragePoint: boolean): CustomPointOptionsObject
{
    const sampleSize = getSampleForPoint(result, showWeightedCounts);
    const numberOfItems = getNumberOfItems(result, metric, showWeightedCounts);
    const yValue = maxResult - Math.max(0, result.weightedResult);
    return {
        id: pointId,
        name: pointName,
        y: yValue,
        count: numberOfItems,
        sampleSize: sampleSize,
        formattedText: getFormattedValueText(result.weightedResult, metric, decimalPlaces),
        isAveragePoint: isAveragePoint,
        significance: result.significance ?? Significance.None,
        significanceHelpText: result.sigificanceHelpText,
    };
}

export function getNegativeInversePointForWeightedDailyResult(
    result: WeightedDailyResult,
    minResult: number,
    metric: Metric,
    decimalPlaces: number,
    pointId: string,
    pointName: string,
    showWeightedCounts: boolean,
    isAveragePoint: boolean): CustomPointOptionsObject
{
    const sampleSize = getSampleForPoint(result, showWeightedCounts);
    const numberOfItems = getNumberOfItems(result, metric, showWeightedCounts);
    const yValue = minResult - Math.min(0, result.weightedResult);
    return {
        id: pointId,
        name: pointName,
        y: yValue,
        count: numberOfItems,
        sampleSize: sampleSize,
        formattedText: getFormattedValueText(result.weightedResult, metric, decimalPlaces),
        isAveragePoint: isAveragePoint,
        significance: result.significance ?? Significance.None,
        significanceHelpText: result.sigificanceHelpText,
    };
}

export function getNumberOfItems(result: WeightedDailyResult, metric: Metric, showWeightedCounts: boolean) {
    if (metric.isPercentage()) {
        return showWeightedCounts ? result.weightedValueTotal : Math.round(result.unweightedValueTotal);
    } else {
        return showWeightedCounts ? result.weightedSampleSize : Math.round(result.unweightedSampleSize);
    }
}

export function getSampleForPoint(result: WeightedDailyResult, showWeightedCounts: boolean) {
    const sampleSize = showWeightedCounts ? result.weightedSampleSize : result.unweightedSampleSize;
    return sampleSize;
}