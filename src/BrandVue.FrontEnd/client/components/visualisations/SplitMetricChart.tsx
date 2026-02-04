import React from "react";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { EntityInstance } from "../../entity/EntityInstance";
import { Metric } from "../../metrics/metric";
import { ViewHelper } from "./ViewHelper";
import { DataSubsetManager } from "../../DataSubsetManager";
import { ChartFooterInformation } from "./ChartFooterInformation";
import { StringHelper } from "../../helpers/StringHelper";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { Factory, MeasureFilterRequestModel, SplitMetricResults, WeightedDailyResult } from "../../BrandVueApi";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import AugmentedReactHighCharts from "./AugmentedReactHighCharts";
import { ICommonDataPoint } from "./ICommonDataPoint";
import BrandVueOnlyLowSampleHelper from "./BrandVueOnlyLowSampleHelper";
import { FilterInstance } from "../../entity/FilterInstance";
import { EntitySet } from "../../entity/EntitySet";
import { IGoogleTagManager } from "../../googleTagManager";
import {useMetricStateContext} from "../../metrics/MetricStateContext";
import { useAppSelector } from '../../state/store';
import { MetricSet } from "client/metrics/metricSet";
import { selectSubsetId } from '../../state/subsetSlice';
import { ITimeSelectionOptions } from "../../state/ITimeSelectionOptions";
import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

interface ISplitMetricChartProps {
    googleTagManager: IGoogleTagManager,
    title: string,
    height: number,
    curatedFilters: CuratedFilters,
    metrics: Metric[],
    activeBrand: EntityInstance,
    entitySet: EntitySet,
    filterInstance?: FilterInstance,
    filters: string[],
    colours: string[],
}

function parseFilters(filters: string[], metricConfig: MetricSet): MeasureFilterRequestModel[] {
    // Assume filter takes form: {label}:{measure}=[!]{value,value,value}
    return filters
        .map(f => f.split(':')[1])
        .map(f => f.split('='))
        .map(f => {

            const invert = f[1].startsWith('!');
            if (invert) {
                f[1] = f[1].substring(1);
            }
            var filterMetric = metricConfig.getMetric(f[0]);
            var entityInstances = {};
            for (var entityType of filterMetric!.entityCombination ?? []) {
                entityInstances[entityType.identifier] = [-1];
            }
            return new MeasureFilterRequestModel({
                entityInstances: entityInstances,
                measureName: f[0],
                values: f[1].split(',').map(v => +v),
                invert: invert,
                treatPrimaryValuesAsRange: false
            });
        });
}

async function getResults(props: ISplitMetricChartProps, 
    allMetrics: MetricSet, 
    subsetId: string, 
    timeSelection: ITimeSelectionOptions): Promise<SplitMetricResults> 
{
    const validRequestedMetrics = DataSubsetManager.filterMetricByCurrentSubset(props.metrics);
    const metric = validRequestedMetrics[0];

    if (metric.entityCombination.length > 1) {
        return await getSplitMetricResults(props, metric, allMetrics, subsetId, timeSelection);
    }
    return await getSplitMetricResultsSingleEntity(props, validRequestedMetrics, allMetrics, subsetId, timeSelection);
}

async function getSplitMetricResultsSingleEntity(props: ISplitMetricChartProps,
    validRequestedMetrics: Metric[],
    allMetrics: MetricSet,
    subsetId: string,
    timeSelection: ITimeSelectionOptions): Promise<SplitMetricResults> {
    // Currently there's no way to select a single instances for single entity metrics except for the active brand
    if (!props.entitySet.type.isBrand) throw new Error("Can only support brand metrics when single entity");

    const model = ViewHelper.createCuratedRequestModel([],
        validRequestedMetrics,
        props.curatedFilters,
        props.activeBrand.id,
        {},
        subsetId,
        timeSelection);

    model.additionalMeasureFilters = parseFilters(props.filters, allMetrics);

    return await Factory.DataClient(err => err()).getSplitMetricResultsSingleEntity(model);
}

async function getSplitMetricResults(props: ISplitMetricChartProps, metric: Metric, metricConfig: MetricSet, subsetId: string, timeSelection:ITimeSelectionOptions) {
    const model = ViewHelper.createMultiEntityRequestModel({
        curatedFilters: props.curatedFilters,
        metric: metric,
        splitBySet: props.entitySet,
        filterInstances: props.filterInstance ? [props.filterInstance] : [],
        continuousPeriod: false,
        focusEntityId: props.activeBrand.id,
        subsetId: subsetId
    }, timeSelection);

    model.additionalMeasureFilters = parseFilters(props.filters, metricConfig);

    return await Factory.DataClient(err => err()).getSplitMetricResults(model);
}

const SplitMetricChart = (props: ISplitMetricChartProps) => {
    const [results, setResults] = React.useState<SplitMetricResults | null>(null);
    const [isLoading, setIsLoading] = React.useState<boolean>(false);
    const [, handleError] = React.useState();
    const xLabelMinSplitLength = 10;
    const { enabledMetricSet } = useMetricStateContext();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    
    React.useEffect(() => {
        const loading = async () => {
            setIsLoading(true);
            try {
                const result = await getResults(props, enabledMetricSet, subsetId, timeSelection);
                setResults(result);
            } catch (e) {
                handleError(() => { throw e });
            } finally {
                setIsLoading(false);
            }
        };
        loading();
    }, [
        JSON.stringify(props.entitySet),
        JSON.stringify(props.metrics.map(m => m.name)), 
        JSON.stringify(props.curatedFilters),
        JSON.stringify(props.filterInstance),
        timeSelection
    ]);

    if (!results) {
        return null;
    }

    const formatter = props.metrics[0].fmt;
    const prefix = StringHelper.sharedPrefixIncludingColon(results.orderedMeasures);
    const orderedMeasures = results.orderedMeasures.map(m => m.substr(prefix.length));

    const series: any = results.orderedResults.map((resultSet, seriesIndex) =>
    ({
        name: props.filters[seriesIndex].split(':')[0],
        data: resultSet.map((d, measureIndex) => new SingleBarChartDataPoint(props.metrics[0], d, orderedMeasures[measureIndex], props.colours[seriesIndex])),
        type: 'line',
        color: props.colours[seriesIndex]
    })
    );
    BrandVueOnlyLowSampleHelper.addLowSampleIndicators(series);

    const formattedDate = results != null && results.orderedResults.length ? DateFormattingHelper.formatDateRange(results.orderedResults[0][0].date, props.curatedFilters.average) : "";


    const splitUpStringBreakingByNextSpaceCharacter = (stringToSplitUp : String, minLengthOfStringSplit: number) :string [] => {
        const lengthOfString = stringToSplitUp.length;
        const maximumNumberOfSplits = Math.ceil(lengthOfString / minLengthOfStringSplit)
        const stringSplit = Array<string>(0)
        let offset = 0

        for (let i = 0; i < maximumNumberOfSplits && offset < lengthOfString; i++) {
            var lengthOfThisSplit = minLengthOfStringSplit;

            while (stringToSplitUp[offset + lengthOfThisSplit] && stringToSplitUp[offset + lengthOfThisSplit] != ' ') {
                lengthOfThisSplit++;
            }
            stringSplit.push(stringToSplitUp.substring(offset, offset + lengthOfThisSplit));
            offset += lengthOfThisSplit + 1;
        }
        return stringSplit
    }

    const sortNote = props.filters.length === 2
        ? 'Note: sorted on difference'
        : 'Note: sorted on summed values';
    
    const afterRender = (chart) => {
        if (isLoading) {
            chart.showLoading();
        } else {
            chart.hideLoading();
        }
    };
    const yAxisTitle = () => {
        if (props.metrics.length > 1)
            return "% of respondents";
        if (props.filterInstance) {
            return `${props.metrics[0].name} (${props.filterInstance?.instance.name})`
        }
        return `${props.metrics[0].name}`
    }

    const config = {
        chart: {
            height: props.height,
            backgroundColor: 'rgba(255,255,255,0)'
        },
        title: {
            text: props.title
        },
        tooltip: {
            formatter: function (this: any): string {
                const name = this.series.name;
                const pointName = this.point.name;
                const formattedValue = this.point.formattedLongValue;
                const sampleSize = this.point.formatn;

                return `<div class="custom-tooltip-title">
                            <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div>
                            <span>${name}</span>
                        </div>
                        <div class="custom-tooltip-point">${pointName}</div>
                        <div class="custom-tooltip-point">Value: ${formattedValue}</div>
                        <div class="custom-tooltip-point">n = ${sampleSize}</div>`;
            },
        },
        xAxis: {
            type: 'category',
            categories: orderedMeasures,
            labels: {
                padding:25
            }
        },
        yAxis: {
            ceiling: props.metrics.every(m => m.isPercentage()) ? 1 : undefined,
            title: {
                text: yAxisTitle()
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
            itemStyle: {
                fontWeight: "normal",
            },
            labelFormatter: function (this: { chart: any, name: any, index: number }) {
                if (this.chart.legend && this.index === this.chart.legend.allItems.length - 1) {
                    return `<span style='font-weight: bold'>${this.name}</span>`;
                }
                return this.name;
            }
        },
        plotOptions: {
            line: {
                animation: false,
                marker: {
                    radius: 4,
                    symbol: 'circle'
                },
                dashStyle: 'solid',
                lineWidth: 2,
                states: {
                    hover: {
                        lineWidth: 3
                    }
                }
            },
            series: {
                dataLabels: {
                    enabled: false,
                    color: '#ccc',
                    format: '{point.formattedValue}',
                    y: 20
                }
            }
        },
        series: []
    };

    if (series && series.length > 0) {
        config.series = series.concat([
            { name: sortNote, color: '#fff', lineWidth: 0, data: [] },
            { name: ' ', color: '#fff', lineWidth: 0, data: [] },
            { name: formattedDate, color: '#fff', lineWidth: 0, data: [] }
        ]);
    }

    return (
        <>
            <AugmentedReactHighCharts config={config} afterRender={afterRender} googleTagManager={props.googleTagManager} />
            <ChartFooterInformation sampleSizeMeta={results.sampleSizeMetadata} activeBrand={props.activeBrand} metrics={props.metrics} average={props.curatedFilters.average} />
        </>
    );
}

export default SplitMetricChart;

export class SingleBarChartDataPoint implements ICommonDataPoint  {
    constructor(metric?: Metric, resultOrNull?: WeightedDailyResult, name?: string, brandColor?: string) {
        if (resultOrNull
            && resultOrNull.weightedResult !== null
            && resultOrNull.weightedResult !== undefined
            && metric) {
            this.y = resultOrNull.weightedResult;
            this.sampleSize = resultOrNull.unweightedSampleSize;
            this.formattedValue = metric.fmt(resultOrNull.weightedResult);
            this.formattedLongValue = metric.longFmt(resultOrNull.weightedResult);
            this.formatn = NumberFormattingHelper.format0Dp(this.sampleSize);
        }

        this.name = name || "";
        this.color = brandColor || "";
    }

    public y: number;
    public formattedValue: string;
    public formattedLongValue: string;
    public sampleSize: number;
    public formatn: string;
    public name: string;
    public color: string;
}