import {EntityInstance} from "../../entity/EntityInstance";
import {Metric} from "../../metrics/metric";
import {CuratedFilters} from "../../filter/CuratedFilters";
import {ViewHelper} from "./ViewHelper";
import React from "react";
import * as BrandVueApi from "../../BrandVueApi";
import {
    AverageMultiEntityChartModel,
    AverageTotalRequestModel,
    AverageType,
    CrossbreakCompetitionResults,
    CrossMeasure,
    CuratedResultsModel,
    CuratedResultsModelWithCrossbreaks,
    EntityWeightedDailyResults,
    Factory,
    MultiEntityRequestModel,
    MultiEntityRequestModelWithCrossbreaks,
    OverTimeAverageResults,
    OverTimeResults,
    SampleSizeMetadata
} from "../../BrandVueApi";
import moment from "moment";
import {ChartFooterInformation} from "./ChartFooterInformation";
import AugmentedReactHighCharts from "./AugmentedReactHighCharts";
import LowSample from "./BrandVueOnlyLowSampleHelper";
import {AreaDataPoint} from "./AreaDataPoint";
import {XAxisFormatting} from "../../helpers/XAxisFormatting";
import {defaultFocusColour, EntitySet} from "../../entity/EntitySet";
import {FilterInstance} from "../../entity/FilterInstance";
import {EntitySetAverage} from "../../entity/EntitySetAverage";
import {LegendOptions, Options, SeriesLineOptions, SeriesOptionsType} from "highcharts";
import {Chalk, getColourMap} from "../helpers/ChromaHelper";
import {useMetricStateContext} from "../../metrics/MetricStateContext";
import {
    getMetricResultsSummaryFromEntityWeightedDailyResults,
    MetricResultsSummary
} from "../helpers/MetricInsightsHelper";
import {OverTimeDataPoint} from "./OverTimeDataPoint";
import {DateFormattingHelper} from "../../helpers/DateFormattingHelper";
import {NoDataError} from "../../NoDataError";
import { IGoogleTagManager } from "../../googleTagManager";
import {useAppDispatch, useAppSelector} from "../../state/store";
import {Dispatch} from "redux";
import WeightedDailyResult = BrandVueApi.WeightedDailyResult;
import {setOverTimeResultsAndAverages} from "../../state/resultsSlice";
import { selectSubsetId } from '../../state/subsetSlice';
import { ITimeSelectionOptions } from "../../state/ITimeSelectionOptions";
import { selectTimeSelection } from "../../state/timeSelectionStateSelectors";

export enum legendPosition {
    None = -1,
    Bottom = 0,
    RightHandSide = 1,
}

interface IDashBoxProps {
    googleTagManager: IGoogleTagManager;
    activeBrand: EntityInstance;
    height: number;
    metrics: Metric[];
    curatedFilters: CuratedFilters;
    legendPosition: legendPosition;
    entitySet: EntitySet;
    availableEntitySets: EntitySet[];
    filterInstance?: FilterInstance;
    breaks?: CrossMeasure;
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void;
    updateAverageRequests: (averageRequests: AverageTotalRequestModel[]) => void;
    hideFooter?: boolean;
    showArea?: boolean;
    showFocusInstanceOnly?: boolean;
    onSuccess?: () => void;
    onNoData?: () => void;
    onFailure?: () => void;
    partId: number | null;
}

const _defaultLineWidth = 1.5;
const _focusInstanceLineWidth = _defaultLineWidth * 3;

const DashBox = (props: IDashBoxProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options>(getChartOptions(props, []));
    const [sampleSizeMeta, setSampleSizeMeta] = React.useState<SampleSizeMetadata>(new SampleSizeMetadata());
    const [isLoading, setIsLoading] = React.useState<boolean>(false);
    const [, handleError] = React.useState();
    
    const metric = props.metrics[0];
    const { enabledMetricSet } = useMetricStateContext();
    const crossMeasureMetric = props.breaks && enabledMetricSet.getMetric(props.breaks.measureName);
    const [metricResultsSummary, setMetricResultsSummary] = React.useState<MetricResultsSummary>();
    const dispatch = useAppDispatch();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    
    React.useEffect(() => {
        props.updateMetricResultsSummary(metricResultsSummary!);
    }, [JSON.stringify(metricResultsSummary)]);

    const hasEndDate = props.curatedFilters.endDate ? true : false;

    React.useEffect(() => {
        if (hasEndDate) {
            setIsLoading(true);
            let isCancelled = false;

            const getChartAndOptions = props.breaks ?
                () => getCrossbreakChartOptionsAndSample(props, metric, crossMeasureMetric, subsetId, timeSelection) :
                () => getOvertimeChartOptionsAndSample(props, metric, dispatch, subsetId, timeSelection);

            getChartAndOptions()
                .then(optionsAndSample => {
                    if (!isCancelled) {
                        setChartOptions(optionsAndSample.options);
                        setSampleSizeMeta(optionsAndSample.sampleSizeMetadata);
                        setMetricResultsSummary(getMetricResultsSummaryFromEntityWeightedDailyResults(optionsAndSample.data, optionsAndSample.sampleSizeMetadata));
                        if (props.onSuccess) { props.onSuccess() };
                    }
                }).catch((e: any) => {
                    if (!isCancelled) {
                        if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                            if (props.onNoData) { props.onNoData() };
                        } else {
                            if (props.onFailure) { props.onFailure() };
                        }
                        handleError(() => { throw e })
                    }
                }).finally(() => {
                    if (!isCancelled) {
                        setIsLoading(false);
                    }
                });

            return () => {
                isCancelled = true;
            };
        }
    }, [
        JSON.stringify(props.entitySet),
        props.filterInstance,
        props.breaks,
        props.activeBrand,
        props.height,
        JSON.stringify(props.curatedFilters)
    ]);

    const afterRender = (c) => {
        if (isLoading) {
            c.showLoading();
        } else {
            c.hideLoading();
        }
    }

    const footer = () => {
        if (!props.hideFooter) {
            return <ChartFooterInformation
                sampleSizeMeta={sampleSizeMeta}
                activeBrand={props.activeBrand}
                metrics={props.metrics}
                average={props.curatedFilters.average}
                doesHaveBrandMetric={props.entitySet.type.isBrand} />
        }
    }

    return (
        <>
            <AugmentedReactHighCharts config={chartOptions} afterRender={afterRender} googleTagManager={props.googleTagManager} />
            {footer()}
        </>
    );
}

function getSingleEntityRequestModel(
    props: IDashBoxProps,
    entityInstancesToPlot: number[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions
): CuratedResultsModel | undefined {
    return ViewHelper.createCuratedRequestModel(
        entityInstancesToPlot,
        props.metrics,
        props.curatedFilters,
        props.activeBrand.id,
        { continuousPeriod: true },
        subsetId,
        timeSelection
    );
}

function getMultipleEntityRequestModel(
    metric: Metric,
    props: IDashBoxProps,
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    entityInstanceIds?: number[]
): MultiEntityRequestModel {
    const filterInstances: FilterInstance[] = [];
    if (metric.entityCombination.length > 1 && props.filterInstance) {
        filterInstances.push(props.filterInstance);
    }
    if (entityInstanceIds) {
        return ViewHelper.createMultiEntityRequestModelForInstances({
            curatedFilters: props.curatedFilters,
            metric: props.metrics[0],
            splitBySet: props.entitySet,
            splitByEntityInstanceIds: entityInstanceIds,
            filterInstances: filterInstances,
            continuousPeriod: true,
            subsetId: subsetId
        }, timeSelection);
    }
    return ViewHelper.createMultiEntityRequestModel({
        curatedFilters: props.curatedFilters,
        metric: props.metrics[0],
        splitBySet: props.entitySet,
        filterInstances: filterInstances,
        continuousPeriod: true,
        subsetId: subsetId
    }, timeSelection);
}

function getSingleEntityAverageRequestModel(
    props: IDashBoxProps,
    average: EntitySetAverage,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
): CuratedResultsModel | undefined {
    const averageSet = average.getEntitySet(props.entitySet, props.availableEntitySets);
    if (averageSet) {
        const instancesForAverage = averageSet.getInstances();
        return ViewHelper.createAverageRequestModelOrNull(
            instancesForAverage.getAll().map(b => b.id),
            props.metrics,
            props.curatedFilters,
            props.activeBrand.id,
            { continuousPeriod: true },
            subsetId,
            timeSelection
        ) ?? undefined;
    }
    return undefined;
}

function getMultipleEntityAverageRequestModel(
    props: IDashBoxProps,
    average: EntitySetAverage,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
) {
    const averageSet = average.getEntitySet(props.entitySet, props.availableEntitySets);
    if (!averageSet) {
        return;
    }

    const requestModel = ViewHelper.createAverageRequestModelForMultipleEntities(
        props.curatedFilters,
        props.metrics[0],
        averageSet,
        props.filterInstance ? [props.filterInstance] : [],
        true,
        subsetId,
        timeSelection
    );
    return new AverageMultiEntityChartModel ({
        averageType: AverageType.Mean,
        requestModel: requestModel
    });
}

async function getOvertimeChartOptionsAndSample(
    props: IDashBoxProps,
    metric: Metric,
    dispatch: Dispatch,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
) {
    let {overTimeData, request} = await (props.showFocusInstanceOnly
        ? getOverTimeData(props, metric, subsetId, timeSelection, true)
        : getOverTimeData(props, metric, subsetId, timeSelection));
    const mainInstanceId = props.entitySet.mainInstance?.id ?? props.entitySet.getInstances().getAll()[0]?.id;
    const averageData = await getOvertimeAverageData(props, metric, subsetId, timeSelection);
    sortOverTimeResult(overTimeData, props.activeBrand);
    const series = getDataSeries(props, 
        overTimeData.entityWeightedDailyResults, 
        averageData);
    dispatch(setOverTimeResultsAndAverages({
        results: overTimeData,
        request: request,
        partId: props.partId ?? 0,
        averages: averageData,
        focusedInstanceId: mainInstanceId,
    }));
    if (props.showArea) {
        const areaData = await getAreaData(props, metric, subsetId, timeSelection);
        const areaName = props.entitySet.name.replace("(average)", "") + " (range)";
        const areaSeries: SeriesOptionsType = {
            id: areaName,
            type: "arearange",
            name: areaName,
            lineWidth: 0,
            color: Chalk,
            zIndex: -1,
            marker: {
                enabled: false
            },
            data: areaData
        };

        series.push(areaSeries);
    }

    return { options: getChartOptions(props, series), sampleSizeMetadata: overTimeData.sampleSizeMetadata, data: overTimeData.entityWeightedDailyResults };
}

async function getAreaData(
    props: IDashBoxProps,
    metric: Metric,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
) {
    const { overTimeData } = await getOverTimeData(props, metric, subsetId, timeSelection);
    const weightedResults: WeightedDailyResult[] = [];

    overTimeData.entityWeightedDailyResults.forEach((entityWeightedDailyResult) => {
            entityWeightedDailyResult.weightedDailyResults.forEach((weightedDailyResult) => {
                weightedResults.push(weightedDailyResult);
            });
        }
    );

    const areaData: AreaDataPoint[] = [];
    weightedResults.forEach((weightedDailyResult) => {
        if (weightedDailyResult.weightedResult !== 0) {
            const index = areaData.map(p => p.formattedDate)
                .indexOf(DateFormattingHelper.formatDatePoint(weightedDailyResult.date, props.curatedFilters.average));
            if (index === -1) {
                areaData.push(new AreaDataPoint(weightedDailyResult,
                    weightedDailyResult,
                    metric,
                    props.curatedFilters.average));
            } else {
                let point: AreaDataPoint = areaData[index];
                if (weightedDailyResult.weightedResult < point.low) {
                    point.low = weightedDailyResult.weightedResult;
                }
                if (weightedDailyResult.weightedResult > point.high) {
                    point.high = weightedDailyResult.weightedResult;
                }
            }
        }
    });
    areaData.sort((p1, p2)=> p1.x - p2.x );

    return areaData;
}

async function getCrossbreakChartOptionsAndSample(
    props: IDashBoxProps,
    metric: Metric,
    crossMeasureMetric: Metric | undefined,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
) {
    const data = await getCrossbreakData(props, metric, subsetId, timeSelection);
    const { overTimeData } = await getOverTimeData(props, metric, subsetId, timeSelection, true);
    const series = getDataSeriesForBreaks(props, data, overTimeData, crossMeasureMetric);
    return { options: getChartOptions(props, series), sampleSizeMetadata: overTimeData.sampleSizeMetadata, data: [] };
}

async function getOverTimeData(
    props: IDashBoxProps,
    metric: Metric,
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    mainInstanceOnly?: boolean
): Promise<{
    request: MultiEntityRequestModel;
    overTimeData: OverTimeResults
}> {
    const mainInstanceId = props.entitySet.mainInstance?.id ?? props.entitySet.getInstances().getAll()[0]?.id;
    const requestModel = mainInstanceOnly
        ? getMultipleEntityRequestModel(metric, props, subsetId, timeSelection, [mainInstanceId])
        : getMultipleEntityRequestModel(metric, props, subsetId, timeSelection);
    const overTimeData = await Factory.DataClient(err => err()).getOverTimeResultsForMultipleEntities(requestModel);
    return {overTimeData: overTimeData, request: requestModel};
}

async function getCrossbreakData(
    props: IDashBoxProps,
    metric: Metric,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
): Promise<CrossbreakCompetitionResults> {
    if (!props.breaks) {
        throw new Error("No breaks provided");
    }
    const mainInstanceId = props.entitySet.mainInstance?.id ?? props.entitySet.getInstances().getAll()[0]?.id;
    if (metric.entityCombination.length > 1) {
        const requestModel = getMultipleEntityRequestModel(metric, props, subsetId, timeSelection, [mainInstanceId]);
        const requestModelWithBreaks = new MultiEntityRequestModelWithCrossbreaks({
            multiEntityRequestModel: requestModel,
            breaks: [props.breaks],
        });
        const results = await Factory.DataClient(err => err()).getGroupedCrossbreakCompetitionResultsMultiEntity(requestModelWithBreaks);
        return results.groupedBreakResults[0].breakResults;
    } else {
        const requestModel = getSingleEntityRequestModel(props, [mainInstanceId], subsetId, timeSelection);
        if (!requestModel) {
            throw new Error("Need to request data for some entity instances");
        }
        const requestModelWithBreaks = new CuratedResultsModelWithCrossbreaks({
            curatedResultsModel: requestModel,
            breaks: [props.breaks]
        });
        const results = await Factory.DataClient(err => err()).getGroupedCrossbreakCompetitionResults(requestModelWithBreaks);
        return results.groupedBreakResults[0].breakResults;
    }
}

interface IOverTimeAverageData {
    name: string;
    results: OverTimeAverageResults;
    request: MultiEntityRequestModel;
}

async function getOvertimeAverageData(
    props: IDashBoxProps,
    metric: Metric,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
): Promise<IOverTimeAverageData[]> {
    const averages = props.entitySet.getAverages().getAll();
    let averageRequests = [] as AverageTotalRequestModel[];
    if (metric.entityCombination.length > 0) {
        let promises = averages
            .map(average => {
                const entitySet = average.getEntitySet(props.entitySet, props.availableEntitySets);
                const curatedRequestModel = getSingleEntityAverageRequestModel(props, average, subsetId, timeSelection);
                const requestModel = getMultipleEntityAverageRequestModel(props, average, subsetId, timeSelection);
                if(curatedRequestModel != null && requestModel != null) {
                    averageRequests.push(new AverageTotalRequestModel({
                        averageName: entitySet.name,
                        requestModel: curatedRequestModel
                    }));
                    return {
                        requestModel: requestModel,
                        entitySet: entitySet
                    };
                }
                return null;
            })
            .filter(x => x != null)
            .map(async x => {
                const averageData = await Factory.DataClient(err => err()).getOverTimeAverageResults(x!.requestModel);
                return { name: x!.entitySet.name, results: averageData , request: x!.requestModel.requestModel }
        });
        props.updateAverageRequests(averageRequests);
        return (await Promise.all(promises)).filter(a => a.results.hasData);
    }
    return [];
}

function sortOverTimeResult(result: BrandVueApi.OverTimeResults, activeBrand: EntityInstance) {
    result.entityWeightedDailyResults.sort((d1, d2) => {
        return d1.entityInstance.id == activeBrand.id ? -1
            : d2.entityInstance.id === activeBrand.id ? 1
                : d1.entityInstance.name < d2.entityInstance.name ? -1 : 1;
    });
}

function getDataSeries(props: IDashBoxProps, results: EntityWeightedDailyResults[], averageResults: {name:string, results:OverTimeAverageResults}[]): SeriesOptionsType[] {
    const metric = props.metrics[0];
    const dataSeries: SeriesOptionsType[] = results.map((result, i) => {
        const isProfileMetric = result.entityInstance.name ? false : props.metrics[i].isProfileMetric();
        const lineWidth = result.entityInstance.id === props.entitySet.mainInstance?.id ? _focusInstanceLineWidth : _defaultLineWidth;
        return {
            id: result.entityInstance.id?.toString(),
            type: 'line',
            name: isProfileMetric ? props.metrics[i].name : result.entityInstance.name,
            data: result.weightedDailyResults.map(w => new OverTimeDataPoint(w, metric, props.curatedFilters.average)),
            color: props.showFocusInstanceOnly
                ? defaultFocusColour
                : props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(result.entityInstance)),
            lineWidth: lineWidth
        }
    });

    if (dataSeries.length > 0) {
        averageResults.forEach(averageResult => {
            dataSeries.push({
                type: 'line',
                color: '#000',
                name: EntitySetAverage.getChartDisplayName(averageResult.name),
                dashStyle: 'LongDash',
                data: averageResult.results.weightedDailyResults.map(r => new OverTimeDataPoint(r, metric, props.curatedFilters.average))
            });
        });
    }

    LowSample.addLowSampleIndicators(dataSeries);
    return dataSeries;
}

function getDataSeriesForBreaks(props: IDashBoxProps, results: CrossbreakCompetitionResults, totalResults: OverTimeResults, crossMeasureMetric: Metric | undefined): SeriesOptionsType[] {
    const metric = props.metrics[0];
    const audienceSuffix = crossMeasureMetric ? ` - ${crossMeasureMetric.varCode}` : '';
    const colours = getColourMap(results.instanceResults.map(r => r.breakName));
    let dataSeries: SeriesLineOptions[] = results.instanceResults.map(r => {
        return {
            id: r.breakName,
            type: 'line',
            name: `${r.breakName}${audienceSuffix} (${r.entityResults[0].entityInstance.name})`,
            data: r.entityResults[0].weightedDailyResults.map(w => new OverTimeDataPoint(w, metric, props.curatedFilters.average)),
            color: colours.get(r.breakName)!,
            lineWidth: _defaultLineWidth
        };
    });

    if (dataSeries.length > 0) {
        const totalSeries: SeriesLineOptions[] = totalResults.entityWeightedDailyResults.map(result => {
            return {
                type: 'line',
                color: '#000',
                name: `Total (${result.entityInstance.name})`,
                dashStyle: 'LongDash',
                data: result.weightedDailyResults.map(r => new OverTimeDataPoint(r, metric, props.curatedFilters.average))
            };
        });
        dataSeries = totalSeries.concat(dataSeries);
    }

    LowSample.addLowSampleIndicators(dataSeries);
    return dataSeries;
}

function calculateMinMaxOverride(series: any[]): { min: number; max: number; overrideMinMax: boolean; } {
    const componentMin = 0;
    const componentMax = 1;

    let seriesMin = 10000;
    let seriesMax = -10000;
    series.map(s => s.data.map(p => {
        seriesMin = Math.min(seriesMin, p.y);
        seriesMax = Math.max(seriesMax, p.y);
    }));

    let overrideMinMax = true;
    let min = seriesMin;
    let max = seriesMax;
    const trustMetricMinMax = componentMin <= seriesMin && componentMax >= seriesMax;

    if (trustMetricMinMax && componentMax * 0.9 < seriesMax) {
        // e.g. Try not to show over 100% on a percentage graph
        max = Math.min(seriesMax, componentMax);
    }
    else {
        overrideMinMax = false;
    }

    return {
        min: min,
        max: max,
        overrideMinMax: overrideMinMax,
    };
}

function getChartOptions(props: IDashBoxProps, dataSeries: SeriesOptionsType[]): Options {
    const metric = props.metrics[0];
    const minMaxOverride = calculateMinMaxOverride(dataSeries);

    const { tickPositioner, labelXformatter } = XAxisFormatting.formatAll(props.curatedFilters.average.makeUpTo);

    var formatter = metric.getNumberFormatForAxis(ViewHelper.calcMaxMinusMinValueBySeries(dataSeries));
    var highlightBrand = props.activeBrand.name;
    var legendConfig: LegendOptions = {
        enabled: true,
        align: 'right',
        verticalAlign: 'middle',
        layout: 'vertical',
        itemMarginTop: 2,
        itemMarginBottom: 2,
        maxHeight: 0,
        itemStyle: {
            fontWeight: "normal",
        },
        labelFormatter: function (this: { name: any, options: any }) {
            var text = this.name;
            if (this.options.visible && this.name === highlightBrand) {
                text = "<b>" + this.name + "</b>";
            }
            return text;
        }
    }

    switch (props.legendPosition) {
        case legendPosition.Bottom:
            legendConfig.align = 'center';
            legendConfig.verticalAlign = 'bottom';
            legendConfig.layout = 'horizontal';
            legendConfig.maxHeight = 100;
            break;
        case legendPosition.None:
            legendConfig.enabled = false;
            break;
        default:
            break;
    }

    const shouldDisplayYAxisTitle = props.height > 200;
    let chartOptions: Options = {
        chart: {
            backgroundColor: 'rgba(255,255,255,0)'
        },
        tooltip: {
            formatter: function (this: any): string {
                const name = this.series.name;
                const seriesName = this.point.formattedDate;
                const formattedValue = this.point.formatx;
                const sampleSize = this.point.formatn;

                return `<div class="custom-tooltip-title">
                        <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div>
                        <span>${name}</span>
                    </div>
                    <div class="custom-tooltip-point">Date: ${seriesName}</div>
                    <div class="custom-tooltip-point">Value: ${formattedValue}</div>
                    <div class="custom-tooltip-point">n = ${sampleSize}</div>`;
            },
        },
        xAxis: {
            type: 'datetime',
            tickPositioner: (tickPositioner as any),
            labels: {
                formatter: function (this: { value: any }) {
                    const dateToFormat = moment.utc(this.value);
                    return dateToFormat.format(labelXformatter).replace("$$HY$$", dateToFormat.month() < 6 ? "1st" : "2nd");
                }
            }
        },
        yAxis: {
            title: {
                text: shouldDisplayYAxisTitle ? metric.yAxisTitle(props.filterInstance?.instance.name) : "",
            },
            labels: {
                formatter: function (this: { value: any }) {
                    return formatter(this.value);
                }
            },
            maxPadding: 0,
            min: minMaxOverride.overrideMinMax ? minMaxOverride.min : undefined,
            max: minMaxOverride.overrideMinMax ? minMaxOverride.max : undefined
        },
        legend: legendConfig,
        plotOptions: {
            line: {
                lineWidth: _defaultLineWidth,
                animation: false,
                marker: {
                    radius: 3,
                    symbol: 'circle',
                    enabledThreshold: ViewHelper.enabledThreshold,
                },
                states: {
                    hover: {
                        lineWidthPlus: _defaultLineWidth
                    }
                }
            },
        },
        series: dataSeries
    };

    if (props.showArea) {
        // Apply square legend symbols
        if (chartOptions.chart) {
            chartOptions.chart.events = {
                load: function () {
                    $(".highcharts-legend-item path").attr('stroke-width', 10);
                },
                redraw: function () {
                    $(".highcharts-legend-item path").attr('stroke-width', 10);
                }
            }
        }
        if (chartOptions.legend) {
            chartOptions.legend.symbolHeight = 11;
            chartOptions.legend.symbolWidth = 11;
            chartOptions.legend.symbolRadius = 0;
        }
    }

    return chartOptions;
}


export default DashBox;
