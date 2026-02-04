import {
    AverageType,
    DisplaySignificanceDifferences,
    EntityWeightedDailyResults,
    GroupedVariableDefinition,
    InstanceListVariableComponent,
    ReportOrder,
    Significance,
} from '../../../../../BrandVueApi';
import { Metric } from '../../../../../metrics/metric';
import { Options, SeriesPieOptions } from 'highcharts';
import { getFormattedValueText } from '../../../../helpers/SurveyVueUtils';
import BrandVueOnlyLowSampleHelper from '../../../BrandVueOnlyLowSampleHelper';
import { getLabelTextColor } from '../../../../helpers/ChromaHelper';
import { CustomPointOptionsObject, getPointForWeightedDailyResult } from './PointOptions';
import { getAverageDisplayText } from '../../../AverageHelper';
import { getSignificance } from './HighchartsOptionsHelper';
import { NumberFormattingHelper } from '../../../../../helpers/NumberFormattingHelper';
import { IEntityConfiguration } from '../../../../../entity/EntityConfiguration';
import { OverlapError } from './CustomErrors';

const counterSeriesLabel = "counter-series";
const defaultBackgroundColor = '#DFDFDF';
const pieChartType = 'pie';
const unsupportedNetOverlapWarning = "More than one overlap found between net results. This is not supported.";
const unsupportedNetAndNonNetOverlapWarning = "More than one overlap found between net and non-net results. This is not supported.";
const otherDataGroupName = 'smallvals';
const net = "net";
const nonNet = "nonNet";

export function getDoughnutChartOptions(
    results: EntityWeightedDailyResults[],
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    highlightLowSample: boolean,
    showTop: number | undefined,
    allMetrics: Metric[],
    entityConfiguration: IEntityConfiguration,
    partDefinedSplitBy: string,
    selectSignificanceComparator: (a: string, b: string) => void,
    reportOrder: ReportOrder,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    variableDefinition?: GroupedVariableDefinition,): Options {

    let series: SeriesPieOptions[] = getDoughnutSeries(metric,
        allMetrics,
        entityConfiguration,
        partDefinedSplitBy,
        results,
        instanceToColourMap,
        decimalPlaces,
        showWeightedCounts,
        showTop,
        reportOrder,
        displaySignificanceDifferences,
        variableDefinition);

    const allSampleSizes = results.map(r => r.weightedDailyResults[0].unweightedSampleSize);
    const hasDifferentSampleSizes = !allSampleSizes.every(size => size == allSampleSizes[0]);

    return {
        ...getChartOptions(series, highlightLowSample, (a, b) => selectSignificanceComparator(a, b)),
        tooltip: {
            enabled: true,
            useHTML: true,
            outside: true,
            className: "custom-tooltip-container",
            followPointer: true,
            formatter: function (this: any): string {
                const getColourSquare = (entityInstanceName: string, showColour: boolean): string => {
                    if (!showColour) {
                        return "";
                    }
                    const colour = instanceToColourMap.get(entityInstanceName) ?? instanceToColourMap[entityInstanceName];
                    return colour ? `<div class="custom-tooltip-square" style="background: ${colour};">&nbsp;</div>` : "";
                };

                const formatToolTip = (point: any, showColour: boolean): string => {
                    const name = point.name;
                    const roundedValue = getFormattedValueText(point.y!, metric, decimalPlaces);
                    const sampleInfo = hasDifferentSampleSizes ? ` of ${NumberFormattingHelper.formatCount(point.sampleSize)}` : '';
                    const sampleDiv = point.name == getAverageDisplayText(AverageType.Mean) || point.name == getAverageDisplayText(AverageType.Median) ?
                        ''
                        : `<div class="custom-tooltip-point">n = ${NumberFormattingHelper.formatCount(point.count)}${sampleInfo}</div>`;

                    return `<div class="custom-tooltip-title">
                            ${getColourSquare(name, showColour)}
                            <span>${name}</span>
                        </div>
                        <div class="custom-tooltip-point">Value: ${roundedValue}</div>
                        ${sampleDiv}`;
                };
                const custom = this.point.custom;
                if (custom?.constituentData) {
                    return custom.constituentData.map(instance => formatToolTip(instance, true)).join("");
                }
                return formatToolTip(this.point.options, false);
            },
        },
    };
}

function getDoughnutSeries(
    metric: Metric,
    allMetrics: Metric[],
    entityConfiguration: IEntityConfiguration,
    partDefinedSplitBy: string,
    results: EntityWeightedDailyResults[],
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    showTop: number | undefined,
    reportOrder: ReportOrder,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    variableDefinition?: GroupedVariableDefinition
): SeriesPieOptions[] {
    const metricIsNet = metric.originalMetricName;
    const singleDoughnutSeriesName = "counter";
    const singleOrInnerDoughnutSeriesIndex = 1;

    if (!metricIsNet) {
        const groupedData = groupTopN(results,
            metric,
            instanceToColourMap,
            decimalPlaces,
            showWeightedCounts,
            singleDoughnutSeriesName,
            displaySignificanceDifferences,
            singleOrInnerDoughnutSeriesIndex,
            showTop);
        return getSingleDoughnutSeries(metric, singleDoughnutSeriesName, groupedData);
    }

    const originalMetric = getOriginalMetric(metric, allMetrics);
    const originalInstanceIds = getOriginalInstanceIds(originalMetric, entityConfiguration, partDefinedSplitBy, metric);

    let nonNetResults = getFilteredResults(results, originalInstanceIds, true);
    const netResults = getFilteredResults(results, originalInstanceIds, false);

    const nonNetInstanceIds = getInstanceIds(nonNetResults, variableDefinition);
    const netInstanceIds = getInstanceIds(netResults, variableDefinition);

    const overlapCount = getNetAndNonNetOverlapCount(nonNetInstanceIds, netInstanceIds);
    if (overlapCount?.some(item => Object.values(item)[0] > 1)) {
        throw new OverlapError(unsupportedNetAndNonNetOverlapWarning);
    }

    const netOverlapCount = getNetOverlapCount(netInstanceIds);
    if (netOverlapCount?.some(item => Object.values(item)[0] > 1)) {
        throw new OverlapError(unsupportedNetOverlapWarning);
    }

    if (netResults.length > 0 && overlapCount?.some(item => Object.values(item)[0] > 0)) {
        const { filteredNetResults, excludedNetResults } = filterNetResults(netResults, nonNetInstanceIds, variableDefinition);
        nonNetResults = [...nonNetResults, ...excludedNetResults];
        sortData(nonNetResults, reportOrder);

        const netData = filterGroupedData(filteredNetResults, metric, instanceToColourMap, decimalPlaces,
            showWeightedCounts, singleDoughnutSeriesName, singleOrInnerDoughnutSeriesIndex, undefined, displaySignificanceDifferences);
        const nonNetData = filterGroupedData(nonNetResults, metric, instanceToColourMap, decimalPlaces,
            showWeightedCounts, singleDoughnutSeriesName, singleOrInnerDoughnutSeriesIndex, showTop, displaySignificanceDifferences);

        return [
            ...getInnerSeries(metric, nonNetData),
            ...getOuterSeries(metric, netData)
        ];
    } else {
        const groupedData = groupTopN(results,
            metric,
            instanceToColourMap,
            decimalPlaces,
            showWeightedCounts,
            singleDoughnutSeriesName,
            displaySignificanceDifferences,
            singleOrInnerDoughnutSeriesIndex,
            showTop);
        return getSingleDoughnutSeries(metric, singleDoughnutSeriesName, groupedData);
    }
}

function filterNetResults(netResults: EntityWeightedDailyResults[], nonNetInstanceIds: number[] | undefined, variableDefinition?: GroupedVariableDefinition) {
    const filteredNetResults = netResults.filter(result => {
        let ids = variableDefinition?.groups?.filter(g => result.entityInstance.id === g.toEntityInstanceId)
            .map(g => g.component as InstanceListVariableComponent).flatMap(i => i.instanceIds);
        return nonNetInstanceIds?.some(id => ids?.includes(id));
    });

    const excludedNetResults = netResults.filter(result =>
        !filteredNetResults.some(filteredResult => filteredResult.entityInstance.id === result.entityInstance.id)
    );

    return { filteredNetResults, excludedNetResults };
}

function filterGroupedData(results: EntityWeightedDailyResults[],
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    singleDoughnutSeriesName: string,
    singleOrInnerDoughnutSeriesIndex: number,
    showTop: number | undefined,
    displaySignificanceDifferences: DisplaySignificanceDifferences) {
    const names = results.map(n => n.entityInstance.name);
    return groupTopN(
        results,
        metric,
        instanceToColourMap,
        decimalPlaces,
        showWeightedCounts,
        singleDoughnutSeriesName,
        displaySignificanceDifferences,
        singleOrInnerDoughnutSeriesIndex,
        showTop
    ).filter(g => names.some(name => name == g.name) || g.name == otherDataGroupName);
}

function sortData(results: EntityWeightedDailyResults[], order: ReportOrder) {
    //result ordering
    switch(order){
        case ReportOrder.ResultOrderDesc:
        case ReportOrder.ResultOrderAsc:
            sortByResultOrder(results);
            break;
    }

    //ascending vs descending
    switch (order) {
        case ReportOrder.ResultOrderAsc:
        case ReportOrder.ScriptOrderAsc:
            results.reverse();
            break;
    }
}

function sortByResultOrder(results: EntityWeightedDailyResults[]) {
    results.sort((a, b) => {
        const sumA = a.weightedDailyResults.reduce((sum, current) => sum + current.weightedResult, 0);
        const sumB = b.weightedDailyResults.reduce((sum, current) => sum + current.weightedResult, 0);
        return sumB - sumA;
    });
}

export function getSingleDoughnutSeries(
    metric: Metric,
    seriesName: string,
    data: CustomPointOptionsObject[]): SeriesPieOptions[] {
    return [
        getBackgroundSeries(counterSeriesLabel, defaultBackgroundColor, DoughnutType.Single),
        getSeriesPieOptions(metric,
            seriesName,
            DoughnutType.Single,
            data
        )
    ];
}

function getInnerSeries(
    metric: Metric,
    data: CustomPointOptionsObject[]
): SeriesPieOptions[] {
    return [
        getBackgroundSeries(counterSeriesLabel, defaultBackgroundColor, DoughnutType.Inner),
        getSeriesPieOptions(metric,
            nonNet,
            DoughnutType.Inner,
            data)
    ];
}

function getOuterSeries(
    metric: Metric,
    data: CustomPointOptionsObject[]
): SeriesPieOptions[] {
    return [
        getBackgroundSeries(counterSeriesLabel, defaultBackgroundColor, DoughnutType.Outer),
        getSeriesPieOptions(metric,
            net,
            DoughnutType.Outer,
            data)
    ];
}

function getOriginalMetric(metric: Metric, allMetrics: Metric[]): Metric {
    return allMetrics.find(m => m.name == metric?.originalMetricName)!;
}

function getOriginalInstanceIds(
    originalMetric: Metric,
    entityConfiguration: IEntityConfiguration,
    partDefinedSplitBy: string,
    metric: Metric
): number[] {
    if (originalMetric.entityCombination.length == 1) {
        return entityConfiguration.getDefaultEntitySetFor(originalMetric.entityCombination[0])
            .getInstances()
            .getAll()
            .map(e => e.id);
    } else {
        const entityType = metric.entityCombination.find(e => e.identifier == partDefinedSplitBy);
        return entityConfiguration.getDefaultEntitySetFor(entityType!)
            .getInstances()
            .getAll()
            .map(e => e.id);
    }
}

function getFilteredResults(
    results: EntityWeightedDailyResults[],
    instanceIds: number[],
    include: boolean
): EntityWeightedDailyResults[] {
    return results?.filter(r => include ? instanceIds.includes(r.entityInstance.id) : !instanceIds.includes(r.entityInstance.id));
}

function getInstanceIds(
    results: EntityWeightedDailyResults[],
    variableDefinition?: GroupedVariableDefinition
): number[] | undefined {
    const components = variableDefinition?.groups?.filter(g => results.some(r => r.entityInstance.id === g.toEntityInstanceId)).map(g => g.component as InstanceListVariableComponent);
    return components?.flatMap(component => component.instanceIds);
}

function getNetAndNonNetOverlapCount(
    nonNetInstanceIds: number[] | undefined,
    netInstanceIds: number[] | undefined,
): { [id: number]: number }[] | undefined {
    const filteredNetInstanceIds = netInstanceIds?.filter(id => nonNetInstanceIds?.includes(id));

    const countMap = new Map<number, number>();

    filteredNetInstanceIds?.forEach(id => {
        countMap.set(id, (countMap.get(id) || 0) + 1);
    });

    const result = Array.from(countMap.entries()).map(([id, count]) => ({ [id]: count }));

    return result;
}

function getNetOverlapCount(netInstanceIds: number[] | undefined) : { [id: number]: number }[] | undefined 
{
    const countMap = new Map<number, number>();

    netInstanceIds?.forEach(id => {
        countMap.set(id, (countMap.get(id) || 0) + 1);
    });

    const result = Array.from(countMap.entries()).map(([id, count]) => ({ [id]: count }));

    return result;
}

export function getLegendMapForIncludedInstances(results: EntityWeightedDailyResults[],
    instanceToColourMap: Map<string, string>) {
    let legendMap: Map<string, string> = new Map();
    instanceToColourMap.forEach((value, key, map) => {
        if (results.some(result => result.entityInstance.name == key)) {
            legendMap.set(key, value);
        }
    });
    return legendMap;
}

function getSizeAndInnerSize(doughnutType: DoughnutType): [size: string, innerSize: string] {
    switch (doughnutType) {
        case DoughnutType.Single: {
            return ['100%', '60%']
        }
        case DoughnutType.Inner: {
            return ['70%', '60%']
        }
        case DoughnutType.Outer: {
            return ['100%', '75%']
        }
    }
}

function getBackgroundSeries(seriesClassName: string, colour: string, doughnutType: DoughnutType): SeriesPieOptions {
    const data = [{
        id: 'pointId',
        name: 'pointName',
        x: 0,
        y: 1,
        count: 1,
        sampleSize: 99999,
        formattedText: '',
        isAveragePoint: false,
        significance: Significance.None,
        significanceHelpText: '',
        subtitle: '',
        color: colour,
        borderColor: colour
    }];

    const sizeAndInnerSize = getSizeAndInnerSize(doughnutType);
    return {
        id: 'background' + doughnutType,
        name: 'background' + doughnutType,
        type: pieChartType,
        size: sizeAndInnerSize[0],
        innerSize: sizeAndInnerSize[1],
        data: data,
        className: seriesClassName + doughnutType,
        borderWidth: 1,
        dataLabels: {
            enabled: false,
        },
        showInLegend: false,
        animation: false,
        enableMouseTracking: false
    };
}

function groupTopN(
    results: EntityWeightedDailyResults[],
    metric: Metric,
    instanceToColourMap: Map<string, string>,
    decimalPlaces: number,
    showWeightedCounts: boolean,
    seriesName: string,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    seriesIndex?: number,
    topNCutoff?: number
): CustomPointOptionsObject[] {
    const seriesId = `${seriesName}${seriesIndex ?? ''}`
    const groupedSegmentColour = '#6C757D';
    const groupedSegmentLabelColour = getLabelTextColor(groupedSegmentColour);

    const topResults = topNCutoff ? results.slice(0, topNCutoff) : results;
    const groupedResults = results.filter(r => !topResults.includes(r));
    const data = topResults.map(r => getPoint(r, metric, decimalPlaces, seriesId, instanceToColourMap, showWeightedCounts, displaySignificanceDifferences));
    const groupedValues = groupedResults.map(r => getPoint(r, metric, decimalPlaces, seriesId, instanceToColourMap, showWeightedCounts, displaySignificanceDifferences));

    if (groupedValues.length > 0) {
        const groupedValuesSum = groupedValues.map(value => value.y).reduce((acc, val) => (acc ?? 0) + (val ?? 0), 0) as number;
        const groupedValuesCount = groupedValues.map(value => value.count).reduce((acc, val) => (acc ?? 0) + (val ?? 0), 0);
        data.push({
            id: seriesId + otherDataGroupName,
            name: otherDataGroupName,
            x: undefined,
            y: groupedValuesSum,
            count: groupedValuesCount,
            sampleSize: groupedValues[0].sampleSize,
            formattedText: getFormattedValueText(groupedValuesSum, metric, decimalPlaces),
            dataLabels: {
                color: groupedSegmentLabelColour
            },
            isAveragePoint: false,
            significance: Significance.None,
            significanceHelpText: "",
            subtitle: "",
            color: groupedSegmentColour,
            borderColor: groupedSegmentColour,
            custom: { constituentData: groupedValues }
        });
    }
    return data;
}

enum DoughnutType {
    Single,
    Inner,
    Outer
}

function getSeriesPieOptions(
    metric: Metric,
    seriesName: string,
    doughnutType: DoughnutType,
    data: CustomPointOptionsObject[]
): SeriesPieOptions {
    const totalPercentage = data.map(d => d.y).reduce((acc, val) => (acc ?? 0) + (val ?? 0), 0) ?? 1;
    const endAngle = 360.0 * totalPercentage;
    const sizeAndInnerSize = getSizeAndInnerSize(doughnutType);

    return {
        id: seriesName,
        name: seriesName,
        type: pieChartType,
        size: sizeAndInnerSize[0],
        innerSize: sizeAndInnerSize[1],
        data: data,
        className: seriesName + doughnutType,
        borderWidth: 1,
        endAngle: endAngle,
        dataLabels: {
            enabled: true,
            defer: false,
            useHTML: true,
            distance: doughnutType == DoughnutType.Outer ? '-13%' : '-20%',
            connectorWidth: 0,
            formatter: function (this: Highcharts.PointLabelObject) {
                const point = this.point.options as CustomPointOptionsObject;

                if (point.formattedText && point.y) {
                    return `${point.formattedText} ${getSignificance(point.significance, metric.downIsGood)}`;
                }
            }
        },
        showInLegend: true,
    };
}

function getPoint(r: EntityWeightedDailyResults, metric: Metric, decimalPlaces: number, seriesId: string, instanceToColourMap: Map<string, string>,
    showWeightedCounts: boolean, displaySignificanceDifferences: DisplaySignificanceDifferences): CustomPointOptionsObject {
    const result = r.weightedDailyResults[0];
    const instanceName = r.entityInstance?.name ?? metric.displayName;
    const color = instanceToColourMap[instanceName] ?? instanceToColourMap.get(instanceName);
    const labelColor = getLabelTextColor(color);
    return {
        ...getPointForWeightedDailyResult(result, metric, decimalPlaces, seriesId + instanceName, instanceName, labelColor, showWeightedCounts,
            false, displaySignificanceDifferences),
        color: color,
        borderColor: color
    };
}

function getChartOptions(
    series: SeriesPieOptions[],
    highlightLowSample: boolean,
    selectSignificanceComparator: (splitColumnData: string, multiBreakComparandData: string) => void): Options {

    if (highlightLowSample) {
        BrandVueOnlyLowSampleHelper.addLowSampleIndicators(series);
    }

    return {
        chart: {
            type: pieChartType,
            animation: false,
            backgroundColor: 'transparent'
        },
        series: series,
        legend: {
            enabled: false,
        },
        plotOptions: {
            pie: {
                showInLegend: true
            },
            series: {
                stacking: 'normal',
                states: {
                    hover: {
                        enabled: true,
                        halo: {
                            opacity: 0,
                        },
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
                            selectSignificanceComparator(this.series.name, this.category.toLocaleString());
                        }
                    }
                }
            }
        }
    };
}