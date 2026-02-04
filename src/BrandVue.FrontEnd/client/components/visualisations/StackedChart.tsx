import React from "react";
import { DataSubsetManager } from "../../DataSubsetManager";
import { EntityInstance } from "../../entity/EntityInstance";
import * as BrandVueApi from "../../BrandVueApi";
import { CuratedFilters} from "../../filter/CuratedFilters";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { StringHelper } from "../../helpers/StringHelper";
import { Metric } from "../../metrics/metric";
import { ChartFooterInformation } from "./ChartFooterInformation";
import { ViewHelper } from "./ViewHelper";
import DataSortOrder = BrandVueApi.DataSortOrder;
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import AugmentedReactHighCharts from "./AugmentedReactHighCharts";
import CompressClientData = BrandVueApi.Factory;
import BrandVueOnlyLowSampleHelper from "./BrandVueOnlyLowSampleHelper";
import { ICommonDataPoint } from "./ICommonDataPoint";
import {StackedChartRenderHelper} from "../../helpers/StackedChartRenderHelper";
import { EntitySet } from "../../entity/EntitySet";
import {
    AverageTotalRequestModel, ComparisonPeriodSelection,
    IEntityWeightedDailyResults,
    StackedAverageResults,
    StackedMeasureResult
} from "../../BrandVueApi";
import { IGoogleTagManager } from "../../googleTagManager";
import { EntitySetAverage } from "../../entity/EntitySetAverage";
import styles from "./StackedChart.module.less";
import { getFormattedLabel } from "../../helpers/HighchartHelper";
import { useAppSelector } from '../../state/store';
import { selectSubsetId } from '../../state/subsetSlice';

import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

export interface IStackedChartProps{
    googleTagManager: IGoogleTagManager,
    title: string,
    height: number,
    curatedFilters: CuratedFilters,
    entitySet: EntitySet,
    availableEntitySets: EntitySet[],
    metrics: Metric[],
    activeBrand: EntityInstance,
    ordering: string[],
    orderingDirection: DataSortOrder,
    colours: string[]
    updateAverageRequests: (averageRequests: AverageTotalRequestModel[]) => void;
}

function ensureCategoriesShowing(xAxis: { type: string; categories: any[]; labels }) {
    if (xAxis.categories.length > 50) {
        xAxis.labels.step = 1;
        xAxis.labels.rotation = -45;
    }
}

function defaultColour(optionIndex: number, totalNumberOfOptions: number) {
    if (optionIndex > totalNumberOfOptions - 1) {
        return StackedChartRenderHelper.hslColourFromPercentAsDecimal(optionIndex / (totalNumberOfOptions - 1));
    }

    const red = "#FF6158";
    const pinkRed = "#FF9797";
    const orange = "#F6C277";
    const lightGreen = "#D7E1AC";
    const green = "#90A53A";
    const darkGreen = "#65A53A";

    const defaultColoursTwo: string[] = [red, green];
    const defaultColoursThree: string[] = [red, orange, green];
    const defaultColoursFour: string[] = [red, orange, lightGreen, green];
    const defaultColoursFive: string[] = [red, pinkRed, orange, lightGreen, green];
    const defaultColoursSix: string[] = [red, pinkRed, orange, lightGreen, green, darkGreen];

    switch (totalNumberOfOptions) {
        case 2:
            return defaultColoursTwo[optionIndex];
        case 3:
            return defaultColoursThree[optionIndex];
        case 4:
            return defaultColoursFour[optionIndex];
        case 5:
            return defaultColoursFive[optionIndex];
        case 6:
            return defaultColoursSix[optionIndex];
        default:
            return StackedChartRenderHelper.hslColourFromPercentAsDecimal(optionIndex / (totalNumberOfOptions - 1));
    }
}
const StackedChart = (props: IStackedChartProps) => {
    const [results, setResults] = React.useState<BrandVueApi.StackedResults | null>(null);
    const [averageResults, setAverageResults] = React.useState([] as {name: string, results: StackedAverageResults}[]);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    React.useEffect(() => {
        const fetchData = async () => {
            const brandGroup = props.entitySet.getInstances();
            const enabledMetrics = DataSubsetManager.filterMetricByCurrentSubset(props.metrics);
            const request = ViewHelper.createCuratedRequestModel(brandGroup.getAll().map(b => b.id),
                enabledMetrics,
                props.curatedFilters,
                props.activeBrand.id,
                {
                    ordering: props.ordering,
                    orderingDirection: props.orderingDirection,
                    comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly
                },
                subsetId,
                timeSelection);
            const averageRequests:any[] = []
            var promises = props.entitySet.getAverages().getAll().map(async average => {
                const entitySet = average.getEntitySet(props.entitySet, props.availableEntitySets);
                const requestModel = ViewHelper.createAverageRequestModelOrNull(
                    entitySet.getInstances().getAll().map(b=>b.id),
                    enabledMetrics,
                    props.curatedFilters,
                    props.activeBrand.id,
                    {
                        ordering: props.ordering,
                        orderingDirection: props.orderingDirection,
                        comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly
                    },
                    subsetId,
                    timeSelection);
                averageRequests.push(new AverageTotalRequestModel({
                    averageName: entitySet.name,
                    requestModel: requestModel!
                }));
                return {name: entitySet.name, results: await CompressClientData.DataClient(err => err()).getStackedAverageResults(requestModel!)};
            });
            Promise.all(promises).then((r) => {
                setAverageResults(r);
            });
            props.updateAverageRequests(averageRequests);
            const result = await BrandVueApi.Factory.DataClient(err => err()).getStackedResults(request);
            setResults(result);
        };

        fetchData();

    }, [props.metrics]); //We only need to depend on metrics here because every render is a new array

    if (!results) {
        return null;
    }

    const compress = (result: StackedMeasureResult) => {
        const data = result.data.filter(y => hasPlottableValue(y));
        return new StackedMeasureResult({
            typeName: result.typeName,
            name: result.name,
            data: data,
            sampleSizeMetadata: result.sampleSizeMetadata,
            hasData: data.length > 0,
            lowSampleSummary: result.lowSampleSummary,
            trialRestrictedData: result.trialRestrictedData
        });
    }

    const formatter = props.metrics[0].fmt;
    const sharedMetricPrefix = StringHelper.sharedPrefixIncludingColon(results.measures.map(m => m.name));
    const hasPlottableValue = (dailyResult: IEntityWeightedDailyResults) => dailyResult.weightedDailyResults[0].weightedResult > 0;
    const compressToSingleStack = results.measures.every(x => x.data.filter(y => hasPlottableValue(y)).length <= 1);

    const measures: StackedMeasureResult[] = compressToSingleStack
        ? results.measures.map((x) => compress(x))
        : results.measures;
    const averageStyle = styles.average;
    const series = measures.map((s, i) => ({
        type: 'column',
        name: s.name.substring(sharedMetricPrefix.length),
        data: [...s.data.map(d => new SingleBarChartDataPoint(props.metrics[i],
            d.weightedDailyResults[0],
            s.name)),
            ...averageResults.flatMap(ar=>ar.results.measures.find(m=>m.name == s.name)?.data.map(d=> new SingleBarChartDataPoint(props.metrics[i], d, ar.name, averageStyle )))
        ],
        color: props.colours.length
            ? props.colours[i]
            : defaultColour(i, results.measures.length)
    }));

    const brandNames = results.measures[0].data.map(b => ({
        name: b.entityInstance.name,
        color: props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(b.entityInstance)),
        id: b.entityInstance.id,
    }));
    const averageNames = averageResults.map(r => ({name: EntitySetAverage.getChartDisplayName(r.name), color: "000"}));

    const dataForDate = results.measures[0].data[0];
    const formattedDate = dataForDate.weightedDailyResults.length ? DateFormattingHelper.formatDateRange(dataForDate.weightedDailyResults[0].date, props.curatedFilters.average) : "";

    BrandVueOnlyLowSampleHelper.addLowSampleIndicators(series);

    const afterRender = (chart) => {
        if (!results.hasData && (props.entitySet.getInstances().getAll().length > 0)) {
            chart.showLoading();
        }
    };

    const config = {
        chart: {
            height: props.height,
            backgroundColor: 'rgba(255,255,255,0)'
        },
        tooltip: {
            formatter: function (this: any): string {
                const seriesName = this.series.name;
                const entityInstance = this.x as EntityInstance;
                const formattedValue = this.point.formattedValue;
                const sampleSize = NumberFormattingHelper.format0Dp(this.point.sampleSize);
                const brandColor = props.entitySet.getInstanceColor(entityInstance);
                const bullet = brandColor
                    ? `<div class="custom-tooltip-dot" style="background:${brandColor}">&nbsp;</div>`
                    : "";

                return `<div class="custom-tooltip-title">
                                ${bullet}<span>${entityInstance.name}</span>
                            </div>
                            <div class="custom-tooltip-title">
                                <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div>
                                <span>${seriesName}</span>
                            </div>
                            <div class="custom-tooltip-point">Value: ${formattedValue}</div>
                            <div class="custom-tooltip-point">n = ${sampleSize}</div>`;
            },
        },
        xAxis: {
            type: 'category',
            categories: [...brandNames, ...averageNames],
            labels: {
                formatter: (e: any): string => {
                    const bullet = e.value.color
                        ? `<span class="brandBullet material-symbols-outlined" style="color:${e.value.color}">\ue061</span>`
                        : "";
                    const fontWeight = e.value.id === props.activeBrand.id || compressToSingleStack ? 'bold' : 'normal';
                    const labelText = compressToSingleStack ? props.entitySet.name : e.value.toString();
                    return bullet + getFormattedLabel(e.axis, labelText, fontWeight);
                }
            }
        },
        yAxis: {
            max: props.metrics.every(m => m.isPercentage()) && sharedMetricPrefix.length > 0 ? 1 : null,
            title: {
                text: props.metrics[0].graphAxisTitle
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
            symbolRadius: 0,
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
        series: [
            ...series,
            { name: ' ', color: '#fff', lineWidth: 0, data: [] },
            { name: formattedDate, color: '#fff', lineWidth: 0, data: [] }
        ],
    };

    ensureCategoriesShowing(config.xAxis);

    return (
        <>
            <AugmentedReactHighCharts config={config} afterRender={afterRender} googleTagManager={props.googleTagManager} />
            <ChartFooterInformation sampleSizeMeta={results.sampleSizeMetadata} activeBrand={props.activeBrand} metrics={props.metrics} average={props.curatedFilters.average} />
        </>
    );
}

export default StackedChart;

export class SingleBarChartDataPoint implements ICommonDataPoint {
    constructor(metric: Metric, result: BrandVueApi.WeightedDailyResult, name?: string, className?: string | undefined) {
        this.y = result.weightedResult;
        this.n = result.unweightedSampleSize;
        this.formattedValue = metric.longFmt(result.weightedResult);

        if (result.weightedResult < 0.03) {
            this.displayValue = "";
        } else {
            // Rule is that for bar charts, all % metric values should be 0dp and with out % sign
            if (metric.isPercentage()) {
                this.displayValue = metric.fmt(result.weightedResult).replace('%', '');
            } else {
                this.displayValue = this.formattedValue;
            }
        }

        this.formatn = NumberFormattingHelper.format0Dp(this.n);
        this.sampleSize = result.unweightedSampleSize;
        this.name = name;
        this.className = className;
    }

    public y?: number;
    public displayValue?: string;
    public formattedValue?: string;
    public n?: number;
    public formatn?: string;
    public name?: string;
    public sampleSize: number;
    public className: string | undefined;
}

