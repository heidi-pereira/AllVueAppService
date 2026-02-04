import { Metric } from "../../metrics/metric";
import React from "react";
import * as BrandVueApi from "../../BrandVueApi";
import { CuratedFilters} from "../../filter/CuratedFilters";
import { EntityInstance } from "../../entity/EntityInstance";
import { ViewHelper } from "./ViewHelper";
import { ChartFooterInformation } from "./ChartFooterInformation";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { StringHelper } from "../../helpers/StringHelper";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import AugmentedReactHighCharts from "./AugmentedReactHighCharts";
import {ICommonDataPoint} from "./ICommonDataPoint";
import BrandVueOnlyLowSampleHelper from "./BrandVueOnlyLowSampleHelper";
import {StackedChartRenderHelper} from "../../helpers/StackedChartRenderHelper";
import { IGoogleTagManager } from "../../googleTagManager";
import {ComparisonPeriodSelection} from "../../BrandVueApi";
import { useAppSelector } from '../../state/store';
import { selectSubsetId } from '../../state/subsetSlice';

import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

type Props = { googleTagManager: IGoogleTagManager, activeBrand: EntityInstance, height: number, metrics: Metric[], curatedFilters: CuratedFilters, colours: string[] };

function getLastOrUndefined<T>(arr: T[]) {
    return arr[arr.length - 1] || undefined;
}

function getLastDataPointOrPlaceholder(categoryResults: BrandVueApi.CategoryResults[], metric: Metric) {
    return categoryResults.map(groupResults =>
        new SingleBarChartDataPoint(metric, getLastOrUndefined(groupResults.weightedDailyResults), groupResults.category));
}

function getDataPoints(brandResults: BrandVueApi.BrokenDownResults, metric: Metric) {
    const separatorPoint = new SingleBarChartDataPoint(undefined, undefined, '-');
    const total = [
        new SingleBarChartDataPoint(metric, getLastOrUndefined(brandResults.total), "Total")
    ];

    return total
        .concat(getPointsForBreakdown(brandResults.byAgeGroup, metric, separatorPoint))
        .concat(getPointsForBreakdown(brandResults.byGender, metric, separatorPoint))
        .concat(getPointsForBreakdown(brandResults.byRegion, metric, separatorPoint))
        .concat(getPointsForBreakdown(brandResults.bySocioEconomicGroup, metric, separatorPoint));
}

function getPointsForBreakdown(breakDown: BrandVueApi.CategoryResults[], metric: Metric, separatorPoint: SingleBarChartDataPoint) {
    if (breakDown.length < 2) return [];
    return [
        separatorPoint,
        ...getLastDataPointOrPlaceholder(breakDown, metric)
    ];
}

function makeChartFromSingleMetricBreakdown(results: BrandVueApi.StackedProfileResults, metrics: Metric[], curatedFilters: CuratedFilters, colours: string[], height: number) {
    const formatter = metrics[0].fmt;
    const sharedMetricPrefix = StringHelper.sharedPrefixIncludingColon(metrics.map(m => m.name));

    const chartData: any = results.data.map((brandResults, i) => ({
        name: brandResults.measure.name.substring(sharedMetricPrefix.length),
        color: colours.length ? colours[i] : StackedChartRenderHelper.hslColourFromPercentAsDecimal(i / (results.data.length - 1)),
        data: getDataPoints(brandResults, metrics[i]),
        type: "column",
    }));

    const categories = chartData.length ? chartData[0].data.map(c => c.name) : [];
    const formattedDate = results.data[0].total.length ? DateFormattingHelper.formatDateRange(results.data[0].total[0].date, curatedFilters.average) : "";
    const yaxisTitle = metrics[0].graphAxisTitle;

    BrandVueOnlyLowSampleHelper.addLowSampleIndicators(chartData);
    const config = {
        chart: {
            height: height,
            type: 'column',
            backgroundColor: 'rgba(255,255,255,0)'
        },
        tooltip: {
            formatter: function (this: any): string {
                const name = this.series.name;
                const seriesName = this.point.name;
                const formattedValue = this.point.formattedValue;
                const sampleSize = this.point.formatn;

                return `<div class="custom-tooltip-title">
                            <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div> 
                            <span>${name}</span>
                        </div>
                        <div class="custom-tooltip-point">${seriesName}</div>
                        <div class="custom-tooltip-point">Value: ${formattedValue}</div>
                        <div class="custom-tooltip-point">n = ${sampleSize}</div>`;
            },
        },
        xAxis: {
            type: 'category',
            categories: categories
        },
        yAxis: {
            max: metrics.every(m => m.isPercentage()) && sharedMetricPrefix.length > 0 ? 1 : null,
            title: {
                text: yaxisTitle
            },
            stackLabels: {
                enabled: false
            },
            labels: {
                formatter: function (this: { value: any }) {
                    return formatter(this.value);
                }
            }
        },
        legend: {
            enabled: true,
            align: 'center',
            verticalAlign: 'bottom',
            layout: 'horizontal',
            itemMarginTop: 5,
            itemMarginBottom: 5,
            itemStyle: { fontWeight: "normal" },
            labelFormatter: function (this: { name: any, data: any }) {
                if (!this.data.length) {
                    return `<span style='font-weight: bold'>${this.name}</span>`;
                }
                return this.name;
            }
        },
        plotOptions: {
            column: {
                stacking: 'normal',
                animation: false,
                dataLabels: {
                    enabled: true,
                    color: '#000',
                    format: '{point.displayValue}',
                    style: {
                        fontWeight: 'normal',
                        textOutline: false,
                        fontSize: '12px'
                    }
                }
            },
            series: {
                states: {
                    inactive: {
                        opacity: 1
                    }
                }
            }
        },
        series: chartData.concat([
            { name: ' ', color: '#fff', lineWidth: 0, data: [] },
            { name: formattedDate, color: '#fff', lineWidth: 0, data: [] }
        ])
    };

    return config;
}

const StackedProfileChart = (props: Props) => {
    const [results, setResults] = React.useState<BrandVueApi.StackedProfileResults | null>(null);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    React.useEffect(() => {
        const fetchData = async () => {
            const request = ViewHelper.createCuratedRequestModel([props.activeBrand.id],
                props.metrics,
                props.curatedFilters,
                props.activeBrand.id,
                { comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly },
                subsetId,
                timeSelection);
            const result = await BrandVueApi.Factory.DataClient(err => err()).getStackedProfileResults(request);
            setResults(result);
        };

        fetchData();
    }, [props.metrics, timeSelection]); //We only need to depend on metrics and timeSelection here because every render is a new array

    if (!results) {
        return null;
    }

    const config = makeChartFromSingleMetricBreakdown(results, props.metrics, props.curatedFilters, props.colours, props.height);

    const afterRender = (chart) => {
        if (!results.hasData) {
            chart.showLoading();
        }
    };

    return (
        <>
            <AugmentedReactHighCharts config={config} afterRender={afterRender} googleTagManager={props.googleTagManager} />
            <ChartFooterInformation sampleSizeMeta={results.sampleSizeMetadata} activeBrand={props.activeBrand} metrics={props.metrics} average={props.curatedFilters.average} />
        </>
    );
}

export default StackedProfileChart;

export class SingleBarChartDataPoint implements ICommonDataPoint {
    constructor(metric?: Metric, resultOrNull?: BrandVueApi.WeightedDailyResult, name?: string) {
        if (resultOrNull && resultOrNull.weightedResult !== null && resultOrNull.weightedResult !== undefined && metric) {
            this.y = resultOrNull.weightedResult;
            this.sampleSize = resultOrNull.unweightedSampleSize;
            this.formattedValue = metric.longFmt(resultOrNull.weightedResult);

            if (resultOrNull.weightedResult < 0.03) {
                this.displayValue = "";
            } else {
                // Rule is that for bar charts, all % metric values should be 0dp and with out % sign
                if (metric.isPercentage()) {
                    this.displayValue = metric.fmt(resultOrNull.weightedResult).replace('%', '');
                } else {
                    this.displayValue = this.formattedValue;
                }
            }

            this.formatn = NumberFormattingHelper.format0Dp(this.sampleSize);
        } else {
            this.y = 0;
            this.sampleSize = 0;
            this.formattedValue = "";
            this.formatn = "no data";
        }

        this.name = name;
    }

    public y?: number;
    public displayValue?: string;
    public formattedValue?: string;
    public sampleSize: number;
    public formatn?: string;
    public name?: string;
}
