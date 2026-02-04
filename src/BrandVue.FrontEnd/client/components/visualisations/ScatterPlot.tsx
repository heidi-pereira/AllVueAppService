import { Metric } from "../../metrics/metric";
import * as BrandVueApi from "../../BrandVueApi";
import React from "react";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { EntityInstance } from "../../entity/EntityInstance";
import { ViewHelper } from "./ViewHelper";
import { ChartFooterInformation } from "./ChartFooterInformation";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import { PageHandler } from "../PageHandler";
import AugmentedReactHighCharts from "./AugmentedReactHighCharts";
import BrandVueOnlyLowSampleHelper from "./BrandVueOnlyLowSampleHelper";
import { EntitySet } from "../../entity/EntitySet";
import { IGoogleTagManager } from "../../googleTagManager";
import {ComparisonPeriodSelection, IAxisRange} from "../../BrandVueApi";
import CurrentPeriodOnly = ComparisonPeriodSelection.CurrentPeriodOnly;
import { useAppSelector } from '../../state/store';
import { selectSubsetId } from '../../state/subsetSlice';

import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";


interface IScatterPlotProps {
    googleTagManager: IGoogleTagManager,
    activeBrand: EntityInstance,
    entitySet: EntitySet,
    height: number,
    metrics: Metric[],
    curatedFilters: CuratedFilters;
    pageHandler: PageHandler;
    xAxisRange: IAxisRange,
    yAxisRange: IAxisRange,
    sections: string[][]
}

const currentPeriodSeriesId = "cp";
const previousPeriodSeriesId = "pp";

export function tickPositioner(min: number, max: number, steps: number) {
    const ticks: number[] = [];
    let tick = min;
    const step = (max - min) / steps;
    while (tick < max - step / 2) {
        ticks.push(tick);
        tick += step;
    }
    ticks.push(max);

    return ticks;
}

function renderSections(chart: any, sections: string[][]) {

    if (sections.length === 0) {
        return;
    }

    const xAxis = chart.xAxis[0];
    const yAxis = chart.yAxis[0];
    const rows = sections.length;
    const cols = rows ? sections[0].length : 0;

    const renderer = chart.renderer;

    xAxis.update({
        tickPositioner: (min, max) => tickPositioner(min, max, cols)
    });
    yAxis.update({
        tickPositioner: (min, max) => tickPositioner(min, max, rows)
    });

    const xInc = (xAxis.max - xAxis.min) / cols;
    const yInc = (yAxis.max - yAxis.min) / rows;

    for (let r = 0; r < rows; r++) {
        for (let c = 0; c < cols; c++) {
            const sectionName = sections[rows - r - 1][c];
            const xPixels = xAxis.toPixels(xAxis.min + c * xInc + xInc / 2);
            const yPixels = yAxis.toPixels(yAxis.min + r * yInc + yInc / 2) - 8; //8px to offset the height of the text
            renderer.label(sectionName, xPixels, yPixels)
                .css({ color: '#ccc' })
                .attr({
                    zIndex: 1,
                    'text-anchor': 'middle'
                }).add();
        }
    }
}

const ScatterPlot = (props: IScatterPlotProps) => {
    const [results, setResults] = React.useState<BrandVueApi.ImpactMapResults | null>(null);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    React.useEffect(() => {
        const fetchData = async () => {
            const brandGroup = props.entitySet.getInstances();

            const model = ViewHelper.createCuratedRequestModel(
                brandGroup.getAll().map(b => b.id),
                props.metrics,
                props.curatedFilters,
                props.activeBrand.id,
                {},
                subsetId,
                timeSelection);
            const result = await BrandVueApi.Factory.DataClient(err => err()).getImpactMapResults(model)
            setResults(result);
        };

        fetchData();
    }, [props.metrics, timeSelection]); //We only need to depend on metrics and scoreCardAverage here because every render is a new array

    if (!results || props.metrics.length < 2) {
        return null;
    }

    const chartData: any = results.data.map(d => {

        const r: any = {
            id: d.entityInstance.id,
            name: d.entityInstance.name,
            color: props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(d.entityInstance)),
            marker: {
                enabled: false
            },
            showInLegend: false,
            type: 'scatter',
            data: [
                {
                    formattedSampleSize: NumberFormattingHelper.format0Dp(d.current.metric1.unweightedSampleSize),
                    formattedSampleSize2: NumberFormattingHelper.format0Dp(d.current.metric2.unweightedSampleSize),
                    sampleSize: Math.min(d.current.metric1.unweightedSampleSize, d.current.metric2.unweightedSampleSize),
                    brand: d.entityInstance,
                    formattedDate: DateFormattingHelper.formatDateRange(d.current.metric1.date,
                        props.curatedFilters.average),
                    x: d.current.metric1.weightedResult,
                    y: d.current.metric2.weightedResult,
                    formatx: props.metrics[0].name +
                        '=' +
                        props.metrics[0].longFmt(d.current.metric1.weightedResult),
                    formaty: props.metrics[1].name +
                        '=' +
                        props.metrics[1].longFmt(d.current.metric2.weightedResult),
                    marker: { symbol: 'circle', enabled: true, radius: 10 },
                    dataLabels: {
                        enabled: true,
                        color: '#000',
                        format: '{point.brand.name}',
                        padding: 12,
                        style: { fontWeight: "normal" }
                    }
                }
            ]
        };

        if (d.previous.metric1 && d.previous.metric2) {
            r.data.push({
                formattedSampleSize: NumberFormattingHelper.format0Dp(d.previous.metric1.unweightedSampleSize),
                formattedSampleSize2: NumberFormattingHelper.format0Dp(d.previous.metric2.unweightedSampleSize),
                sampleSize: Math.min(d.previous.metric1.unweightedSampleSize, d.previous.metric2.unweightedSampleSize),
                brand: d.entityInstance,
                formattedDate: DateFormattingHelper.formatDateRange(d.previous.metric1.date,
                    props.curatedFilters.average),
                x: d.previous.metric1.weightedResult,
                y: d.previous.metric2.weightedResult,
                formatx: props.metrics[0].name +
                    '=' +
                    props.metrics[0].longFmt(d.previous.metric1.weightedResult),
                formaty: props.metrics[1].name +
                    '=' +
                    props.metrics[1].longFmt(d.previous.metric2.weightedResult),
                marker: { symbol: 'circle', enabled: true, radius: 3 }
            });
        }

        return r;
    });

    const formatterX = props.metrics[0].getNumberFormatForAxis(ViewHelper.calcMaxMinusMinValue(chartData));
    const formatterY = props.metrics[1].getNumberFormatForAxis(ViewHelper.calcMaxMinusMinValue(chartData));

    let currentPeriodDate = 'Current period';
    let previousPeriodDate = 'Previous period';
    if (results.data.length) {
        currentPeriodDate = DateFormattingHelper.formatDateRange(results.data[0].current.metric1.date, props.curatedFilters.average);
        previousPeriodDate = DateFormattingHelper.formatDateRange(results.data[0].previous.metric1.date, props.curatedFilters.average);
    }

    const series = chartData.concat([
        { name: " ", color: '#000', lineWidth: 0, marker: { enabled: false }, data: [] },
        { id: currentPeriodSeriesId, canToggle: false, name: currentPeriodDate, color: '#000', lineWidth: 0, marker: { symbol: 'circle', radius: 10 }, data: [] }

    ]);

    if (props.curatedFilters.comparisonPeriodSelection !== CurrentPeriodOnly) {
        series.push({
            id: previousPeriodSeriesId,
            canToggle: false,
            name: previousPeriodDate,
            color: '#000',
            lineWidth: 0,
            marker: { symbol: 'circle', radius: 3 },
            data: []
        });
    }

    BrandVueOnlyLowSampleHelper.addLowSampleIndicators(series);

    const config = {
        chart: {
            height: props.height,
            backgroundColor: 'rgba(255,255,255,0)'
        },
        tooltip: {
            formatter: function (this: any): string {
                const name = this.point.brand.name;
                const seriesName = this.point.formattedDate;
                const metricX = this.point.formatx;
                const metricY = this.point.formaty;
                const sampleSizeX = this.point.formattedSampleSize;
                const sampleSizeY = this.point.formattedSampleSize2;

                return `<div class="custom-tooltip-title">
                            <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div> 
                            <span>${name}</span>
                        </div>
                        <div class="custom-tooltip-point">Date: ${seriesName}</div>
                        <div class="custom-tooltip-point">${metricX} (n = ${sampleSizeX})</div>
                        <div class="custom-tooltip-point">${metricY} (n = ${sampleSizeY})</div>`;
            },
        },
        xAxis: {
            title: {
                text: props.metrics[0].name
            },
            gridLineColor: '#eee',
            gridLineWidth: 1.0,
            labels: {
                formatter: function (this: { value: any }) {
                    return formatterX(this.value);
                }
            },
            min: props.xAxisRange.min,
            max: props.xAxisRange.max,
        },
        yAxis: {
            title: {
                text: props.metrics[1].name
            },
            gridLineColor: '#eee',
            gridLineWidth: 1.0,
            labels: {
                formatter: function (this: { value: any }) {
                    return formatterY(this.value);
                }
            },
            min: props.yAxisRange.min,
            max: props.yAxisRange.max,
        },
        legend: {
            enabled: true,
            align: 'center',
            verticalAlign: 'bottom',
            layout: 'horizontal',
            itemMarginTop: 5,
            itemMarginBottom: 5
        },
        plotOptions: {
            scatter: {
                animation: false,
                lineWidth: 1,
                dashStyle: 'dot',
                dataLabels: {
                    allowOverlap: true
                }
            }
        },
        series: series
    };

    return (
        <>
            <AugmentedReactHighCharts config={config} afterRender={chart => renderSections(chart, props.sections)} googleTagManager={props.googleTagManager} />
            <ChartFooterInformation sampleSizeMeta={results.sampleSizeMetadata} activeBrand={props.activeBrand} metrics={props.metrics} average={props.curatedFilters.average} />
        </>
    );
}

export default ScatterPlot;