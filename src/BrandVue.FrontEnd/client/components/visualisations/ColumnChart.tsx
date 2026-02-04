import { EntityInstance } from "../../entity/EntityInstance";
import { Metric } from "../../metrics/metric";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { EntitySet } from "../../entity/EntitySet";
import { FilterInstance } from "../../entity/FilterInstance";
import React from 'react';
import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import HighchartsReact from 'highcharts-react-official';
import * as BrandVueApi from "../../BrandVueApi";
import {
    AverageMultiEntityChartModel,
    AverageTotalRequestModel,
    AverageType,
    CalculationPeriodSpan,
    ComparisonPeriodSelection,
    CompetitionResults,
    CrossbreakCompetitionResults,
    CrossMeasure,
    CuratedResultsModel,
    EntityWeightedDailyResults,
    Factory,
    MultiEntityRequestModel,
    MultiEntityRequestModelWithCrossbreaks,
    OverTimeAverageResults,
    SampleSizeMetadata
} from "../../BrandVueApi";
import { ViewHelper } from "./ViewHelper";
import { IColumnDataPoint } from "./ICommonDataPoint";
import { useResizeDetector } from 'react-resize-detector';
import ColourTransformations from "./ColourTransformations";
import BrandVueOnlyLowSampleHelper from "./BrandVueOnlyLowSampleHelper";
import _ from "lodash";
import { ChartFooterInformation } from "./ChartFooterInformation";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import { getColourMap } from "../helpers/ChromaHelper";
import { useMetricStateContext } from "../../metrics/MetricStateContext";
import { getMetricResultsSummaryFromCompetitionResults, MetricResultsSummary } from "../helpers/MetricInsightsHelper";
import { EntitySetAverage } from "../../entity/EntitySetAverage";
import { AxisLabelsFormatterContextObject, Options, PointLabelObject, SeriesColumnOptions } from "highcharts";
import Highcharts from "highcharts";
import { getFormattedLabel } from "../../helpers/HighchartHelper";
import {setGenericAverageResults, setGenericResults} from "../../state/resultsSlice";
import {AppDispatch, useAppDispatch, useAppSelector} from "../../state/store";
import {Dispatch} from "redux";
import { selectSubsetId } from '../../state/subsetSlice';
import { selectTimeSelection } from 'client/state/timeSelectionStateSelectors';
import { ITimeSelectionOptions } from "client/state/ITimeSelectionOptions";

const MAX_BRIGHTEN_AMOUNT = 0.7;

interface IColumnChartProps {
    activeBrand: EntityInstance;
    height: number;
    metrics: Metric[];
    curatedFilters: CuratedFilters;
    entitySet: EntitySet;
    filterInstance?: FilterInstance;
    breaks?: CrossMeasure;
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void;
    availableEntitySets: EntitySet[];
    updateAverageRequests: (averageRequests: AverageTotalRequestModel[]) => void;
    partId: number;
}

type AverageData = {
    name: string,
    results: OverTimeAverageResults
}

const possiblyAdjustColour = (colour: string, resultIndex: number, resultsLength: number) => {
    if (resultIndex === 0) return colour;
    return ColourTransformations.Brighten(colour, (MAX_BRIGHTEN_AMOUNT *resultIndex) / (resultsLength - 1));
}

function getSingleEntityAverageRequestModel(props: IColumnChartProps, average: EntitySetAverage, subsetId: string, timeSelection: ITimeSelectionOptions): CuratedResultsModel | undefined {
    const averageSet = average.getEntitySet(props.entitySet, props.availableEntitySets);

    if (averageSet) {
        const instancesForAverage = averageSet.getInstances();
        return ViewHelper.createCompetitionAverageRequestModelOrNull(instancesForAverage.getAll().map(b => b.id),
            props.metrics,
            props.curatedFilters,
            props.activeBrand.id,
            props.curatedFilters.comparisonPeriodSelection,
            subsetId,
            timeSelection) ?? undefined;
    }
    return undefined;
}

function getMultipleEntityAverageRequestModel(props: IColumnChartProps, average: EntitySetAverage, subsetId: string, timeSelection: ITimeSelectionOptions) {
    const averageSet = average.getEntitySet(props.entitySet, props.availableEntitySets);
    if (!averageSet) {
        return;
    }
    const requestModel = ViewHelper.createCompetitionAverageRequestModelForMultipleEntities(props.curatedFilters,
        props.metrics[0],
        averageSet,
        props.filterInstance ? [props.filterInstance] : [],
        subsetId,
        timeSelection);
    return new AverageMultiEntityChartModel ({
        averageType: AverageType.Mean,
        requestModel: requestModel
    });
}

const getColor = (entitySet: EntitySet, dataPoint: IColumnDataPoint) :string => {

    return entitySet.getInstanceColor(new EntityInstance(dataPoint.instanceId));
}

const ColumnChart: React.FC<IColumnChartProps> = (props: IColumnChartProps) => {
    const [chartOptions, setChartOptions] = useState<Options>(getChartOptions([], props.metrics, props.entitySet, props.filterInstance));
    const [sampleSizeMetadata, setSampleSizeMetadata] = React.useState<SampleSizeMetadata>(new CompetitionResults().sampleSizeMetadata);
    
    const [layoutReady, setLayoutReady] = useState(false);
    const [, handleError] = useState();
    const {enabledMetricSet} = useMetricStateContext();
    const { width, height, ref } = useResizeDetector<HTMLDivElement>({ refreshMode: layoutReady ? 'debounce' : undefined, refreshRate: 100 });
    const crossMeasureMetric = props.breaks && enabledMetricSet.getMetric(props.breaks.measureName);
    const dispatch = useAppDispatch();
    const chart = useRef<Highcharts.Chart>();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const handleCompetitionResults = (competitionResults: CompetitionResults) => {
        const latestPeriodFirst = _.orderBy(competitionResults.periodResults, r => r.period.endDate, "desc");
        const [latestResult,] = latestPeriodFirst;
        const orderedInstancesByResult = _.orderBy(latestResult.resultsPerEntity, r => r.weightedDailyResults[0].weightedResult, "desc").map((r, i) => [r.entityInstance.id, i]) as [number, number][];
        const entityInstanceToXPosition = new Map(orderedInstancesByResult);

        const series: Array<SeriesColumnOptions> = latestPeriodFirst.map((r, i, all) => {
            const instanceForLegend = props.entitySet.mainInstance && entityInstanceToXPosition.has(props.entitySet.mainInstance.id) ? props.entitySet.mainInstance : new EntityInstance(orderedInstancesByResult[0][0]);
            const legendColour = props.entitySet.getInstanceColor(instanceForLegend);
            const possiblyAdjustedLegendColour = possiblyAdjustColour(legendColour, i, latestPeriodFirst.length);
            const dataPointPainter = (dataPoint: IColumnDataPoint) => possiblyAdjustColour(getColor(props.entitySet, dataPoint), i, latestPeriodFirst.length);
            return getSeries(props.entitySet, props.metrics, r.period.name, latestResult.period.name, possiblyAdjustedLegendColour, dataPointPainter, r.resultsPerEntity, -i, entityInstanceToXPosition);
        });

        BrandVueOnlyLowSampleHelper.addLowSampleIndicators(series);
        setChartOptions(getChartOptions(series, props.metrics, props.entitySet, props.filterInstance));
        setSampleSizeMetadata(competitionResults.sampleSizeMetadata);
        props.updateMetricResultsSummary(getMetricResultsSummaryFromCompetitionResults(competitionResults));
        const averageData = getAverageData(props, props.metrics, dispatch, subsetId, timeSelection);

        averageData.then(averages => {
            const index = series.length;
            averages.map((average, i) => {
                appendAverage(series, average, i, index + i);
        });
            setChartOptions(getChartOptions(series, props.metrics, props.entitySet, props.filterInstance));

        })
    };


    
    const handleCrossbreakCompetitionResults = (results: CrossbreakCompetitionResults) => {
        const entityInstanceToXPosition = new Map(
            results.instanceResults[0].entityResults.map((r, i) => [r.entityInstance.id, i])
        );
        const colourMap = getColourMap(results.instanceResults.map(r => r.breakName));
        const audienceSuffix = crossMeasureMetric ? ` (${crossMeasureMetric.varCode})` : '';
        const series = results.instanceResults.map((r, i) => {
             const seriesColour = colourMap.get(r.breakName)!;
             const dataPointPainter = (dataPoint: IColumnDataPoint) => seriesColour
            return getSeries(props.entitySet, props.metrics, `${r.breakName}${audienceSuffix}`, '',seriesColour, dataPointPainter, r.entityResults, i, entityInstanceToXPosition, true);
        });
        BrandVueOnlyLowSampleHelper.addLowSampleIndicators(series);
        setChartOptions(getChartOptions(series, props.metrics, props.entitySet, props.filterInstance, props.breaks));
        setSampleSizeMetadata(results.sampleSizeMetadata);
    }

    useEffect(() => {
        chart.current?.showLoading();
        let isCancelled = false;
        if (props.breaks && props.curatedFilters.comparisonPeriodSelection === ComparisonPeriodSelection.CurrentPeriodOnly) {
            getCrossbreakCompetitionResults(props, subsetId, timeSelection).then(results => {
                if (!isCancelled) {
                    handleCrossbreakCompetitionResults(results);
                    chart.current?.hideLoading();
                }
            }).catch((e: Error) => {
                if (!isCancelled) {
                    handleError(() => {throw e});
                }
            });
        } else {
            getCompetitionResults(props, dispatch, subsetId, timeSelection).then(competitionResults => {
                if (!isCancelled) {
                    handleCompetitionResults(competitionResults);
                    chart.current?.hideLoading();
                }
            }).catch((e: Error) => {
                if (!isCancelled) {
                    handleError(() => {throw e});
                }
            });
        }
        return () => {
            isCancelled = true
        };
    }, [
        JSON.stringify(props.entitySet),
        props.filterInstance,
        props.breaks,
        JSON.stringify(props.curatedFilters),
    ]);

    // Stops flickering effect caused by chart loading then resizing
    useLayoutEffect(() => {
        setLayoutReady(true);
    }, []);

    const options = {
        ...chartOptions,
        chart: {
            ...chartOptions.chart,
            height: height,
            width: width
        }
    };

    return (
        <>
            <div ref={ref} className="chart-container">
                <HighchartsReact highcharts={Highcharts} options={options} callback={c => chart.current = c} />
            </div>
            <ChartFooterInformation
                sampleSizeMeta={sampleSizeMetadata}
                activeBrand={props.activeBrand}
                metrics={props.metrics}
                average={props.curatedFilters.average}
                doesHaveBrandMetric={props.entitySet.type.isBrand} />
        </>
    );
}

export default ColumnChart;

async function getAverageData(props: IColumnChartProps, metrics: Metric[], dispatch: AppDispatch, subsetId: string, timeSelection: ITimeSelectionOptions): Promise<AverageData[]> {
    const averages = props.entitySet.getAverages().getAll();
    let averageRequests = [] as AverageTotalRequestModel[];
    const validDates = props.curatedFilters.comparisonDates(false, timeSelection);
    const promises = averages.map(async average => {
        const entitySet = average.getEntitySet(props.entitySet, props.availableEntitySets);
        const curatedRequestModel = getSingleEntityAverageRequestModel(props, average, subsetId, timeSelection);
        if(curatedRequestModel != null)
        {
            averageRequests.push(new AverageTotalRequestModel({
                averageName: entitySet.name,
                requestModel: curatedRequestModel!
            }));
        }
        const averageMultiEntityRequestModel = getMultipleEntityAverageRequestModel(props, average, subsetId, timeSelection);
        const averageData = averageMultiEntityRequestModel
            ? await Factory.DataClient(err => err()).getOverTimeAverageResults(averageMultiEntityRequestModel)
            : new OverTimeAverageResults();

        averageData.weightedDailyResults = averageData.weightedDailyResults.filter(item =>
            (validDates.find(validDate => validDate.startDate.toISOString() == item.date.toISOString()))
        ).reverse();

        return { name: EntitySetAverage.getChartDisplayName(entitySet.name), rawName: entitySet.name, results: averageData, request: averageMultiEntityRequestModel!.requestModel };
    });
    const allAverageData = await Promise.all(promises);

    props.updateAverageRequests(averageRequests);
    
    dispatch(setGenericAverageResults({
        partId: props.partId,
        payload: allAverageData
    }));
    
    return allAverageData.filter(a => a.results.hasData);
}

async function getRequestModel(curatedFilters: CuratedFilters, metrics: Metric[], 
    entitySet: EntitySet, 
    filterInstance: FilterInstance | undefined, 
    activeEntity: EntityInstance | undefined, 
    subsetId: string, 
    timeSelection: ITimeSelectionOptions
) {
    const metric = metrics[0];
    const requestModel = await getMultipleEntityRequestModel(
        curatedFilters, metric, entitySet, subsetId, timeSelection, filterInstance, activeEntity?.id);
    return requestModel;
}

async function getCompetitionResults(props: IColumnChartProps, dispatch: Dispatch, subsetId: string, timeSelection: ITimeSelectionOptions): Promise<CompetitionResults> {
    const requestModel = await getRequestModel(props.curatedFilters, props.metrics, props.entitySet,
        props.filterInstance, props.activeBrand, subsetId, timeSelection);
    var results = await BrandVueApi.Factory.DataClient(throwError => throwError())
        .getCompetitionResults(requestModel);
    dispatch(setGenericResults({
        results: results,
        request: requestModel,
        averages: [],
        averagesSelected: props.entitySet.getAverages().getAll().length,
        partId: props.partId,
    }));
    return results;
}

async function getCrossbreakCompetitionResults(props: IColumnChartProps, subsetId: string, timeSelection: ITimeSelectionOptions): Promise<CrossbreakCompetitionResults> {
    if (!props.breaks) {
        throw new Error("No breaks provided");
    }
    if (props.curatedFilters.comparisonPeriodSelection !== ComparisonPeriodSelection.CurrentPeriodOnly) {
        throw new Error("Can only get data with breaks for current period");
    }

    const requestModel = await getRequestModel(props.curatedFilters, props.metrics, props.entitySet,
        props.filterInstance, props.activeBrand, subsetId, timeSelection);

    const requestModelWithBreaks = new MultiEntityRequestModelWithCrossbreaks({
        multiEntityRequestModel: requestModel,
        breaks: [props.breaks]
    });

    const results = await BrandVueApi.Factory.DataClient(throwError => throwError())
        .getGroupedCrossbreakCompetitionResultsMultiEntity(requestModelWithBreaks);

    return results.groupedBreakResults[0].breakResults;
}


async function getMultipleEntityRequestModel(curatedFilters: CuratedFilters,
    metric: Metric,
    splitBySet: EntitySet,
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    filterInstance?: FilterInstance,
    focusBrandId?: number): Promise<MultiEntityRequestModel> {
    const newRequestModel = ViewHelper.createMultiEntityRequestModel({
        curatedFilters: curatedFilters,
        metric: metric,
        splitBySet: splitBySet,
        filterInstances: filterInstance? [filterInstance]: [],
        continuousPeriod: false,
        focusEntityId: focusBrandId,
        subsetId: subsetId
    }, timeSelection);

    if (newRequestModel.period.average === "custom") {
        const customPeriods = await BrandVueApi.Factory.MetaDataClient(throwError => throwError()).getCustomPeriods();
        newRequestModel.period.comparisonDates =
            customPeriods.map(cp => new CalculationPeriodSpan({ name: cp.name, startDate: cp.startDate, endDate: cp.endDate }));
    }

    return newRequestModel;
}

function createUnpaintedDataPoints(metrics: Metric[], resultsPerEntity: EntityWeightedDailyResults[], seriesId: string, entityInstanceToXPosition: Map<number, number>): IColumnDataPoint[] {
    return resultsPerEntity.map((r, i) => {
        const result = r.weightedDailyResults[0];
        const entityInstanceName = r.entityInstance.name;
        const dataPointIdAndName = entityInstanceName ?? metrics[0].name;
        return {
            id: `${seriesId}-${dataPointIdAndName}`,
            name: dataPointIdAndName,
            instanceId: r.entityInstance.id,
            y: result?.weightedResult,
            sampleSize: result?.unweightedSampleSize,
            x: entityInstanceToXPosition.get(r.entityInstance.id)
        }
    });
}

function getSeries(
    entitySet: EntitySet,
    metrics: Metric[],
    seriesName: string | undefined,
    latestPeriod: string | undefined,
    seriesColour: string,
    dataPointPainter: (dataPoint: IColumnDataPoint) => string,
    results: EntityWeightedDailyResults[],
    seriesIndex: number,
    entityInstanceToXPosition: Map<number, number>,
    allowLegendItemClick?: boolean): SeriesColumnOptions
{
    const seriesId = `${entitySet.name}-${seriesName}`;

    const dataPoints = createUnpaintedDataPoints(metrics, results, seriesId, entityInstanceToXPosition);
    for (const dataPoint of dataPoints) {
        dataPoint.color = dataPointPainter(dataPoint);
        dataPoint.labelrank = seriesName == latestPeriod ? 1 : 0;
    }

    return {
        id: seriesId,
        type: "column",
        color: seriesColour,
        name: seriesName,
        data: dataPoints.sort((a, b) => a.x! - b.x!),
        events: {
            legendItemClick: allowLegendItemClick ? undefined : function (e) {
                e.preventDefault()
            }
        },
        index: seriesIndex
    };
}

function createUnpaintedAverageDataPoints(seriesId: number, name: string, result: number, sampleSize: number, xPos: number): IColumnDataPoint {
    return {
        id: `${seriesId}-${name}`,
        name: name,
        instanceId: 1,
        y: result,
        sampleSize: sampleSize,
        x: xPos,
    };
}

function appendAverage(series: Array<SeriesColumnOptions>, average: AverageData, averageIndex: number, index: number) {

    const base = 8-averageIndex % 9;
    const color = `#${base}0${base}0${base}0`;

    series.forEach((s, seriesIndex) => {

        if (seriesIndex < average.results.weightedDailyResults.length) {
            const dataPointPainter = (dataPoint: IColumnDataPoint) => possiblyAdjustColour(color, seriesIndex, index);

            const point = createUnpaintedAverageDataPoints(seriesIndex, average.name,
                average.results.weightedDailyResults[seriesIndex].weightedResult,
                average.results.weightedDailyResults[seriesIndex].unweightedSampleSize,
                s.data!.length);
            point.color = dataPointPainter(point);
            s.data!.push(point);
        }
    })
}

function getLabelText(context: AxisLabelsFormatterContextObject, entitySet: EntitySet, isUsingBreaks: boolean): string {
    const dataPoint = context.chart.get(context.value.toString())?.options as IColumnDataPoint;
    if (!dataPoint) return context.value.toString();

    const isActiveBrand = entitySet.mainInstance?.id == dataPoint.instanceId;

    if (isActiveBrand) {
        return getLabel(entitySet.mainInstance!, entitySet, !isUsingBreaks, true);
    }

    const instance = entitySet.getInstances().getById(dataPoint.instanceId);
    if (instance) {
        return getLabel(instance, entitySet, !isUsingBreaks, false);
    }

    return context.value.toString();
}

function getChartOptions(series: SeriesColumnOptions[], metrics: Metric[], entitySet: EntitySet, filterInstance?: FilterInstance, breaks?: CrossMeasure): Options {
    const isUsingBreaks = breaks != null;
    return {
        chart: {type: "column", animation: true},
        series: series,
        tooltip: {
            formatter: function (this: any): string {
                const name = this.point.name;
                const seriesName = this.series.name;
                const formattedValue = metrics[0].longFmt(this.y)
                const sampleSize = NumberFormattingHelper.format0Dp(this.point.sampleSize);
                const pointType = isUsingBreaks ? 'Break' : 'Period';

                return `<div class="custom-tooltip-title">
                            <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div>
                            <span>${name}</span>
                        </div>
                        <div class="custom-tooltip-point">${pointType}: ${seriesName}</div>
                        <div class="custom-tooltip-point">Value: ${formattedValue}</div>
                        <div class="custom-tooltip-point">n = ${sampleSize}</div>`;
            },
            backgroundColor: "#1A1A1A",
            borderColor: "#1A1A1A",
            borderWidth: 1,
            borderRadius: 10,
            padding: 15,
            style: {
                color: "#CCC",
                fontSize: "14px"
            },
            className: "custom-tooltip-container",
            shadow: false,
            useHTML: true
        },
        loading: {
            labelStyle: { top: "0" },
            style: { opacity: 1, backgroundColor: "rgba(255,255,255,0.6)" },
            showDuration: 500,
        },
        xAxis: {
            type: "category",
            labels: {
                formatter: (e: any): string => getFormattedLabel(e.axis, getLabelText(e, entitySet, isUsingBreaks), "normal"),
                style: {
                    color: '#666',
                },
            },
        },
        yAxis: {
            title: {
                text: metrics.length > 1 ? metrics[0].graphAxisTitle : metrics[0].yAxisTitle(filterInstance?.instance.name)
            },
            labels: {
                formatter: function (this: AxisLabelsFormatterContextObject): string {
                    const formatter = metrics[0].getNumberFormatForAxis(ViewHelper.calcMaxMinusMinValue(this.chart.series));
                    return formatter(this.value);
                }
            }
        },
        legend: {
            itemStyle: {
                fontWeight: "normal",
            },
            symbolRadius: 0,
        },
        plotOptions: {
            column: {
                dataLabels: {
                    enabled: true,
                    color: '#999',
                    formatter: function (this: PointLabelObject): string {
                        const metric = metrics[0];

                        return metric.isPercentage()
                            ? metric.fmt(this.point.y).replace('%', '')
                            : metric.longFmt(this.point.y);
                    },
                    y: -10 as any,
                    style: {
                        textOutline: '3px white',
                        fontWeight: '400',
                        fontSize: '14px'
                    },
                },
                groupPadding: 0.15
            },
            series: {
                animation: {
                    duration: 500
                },
                states: {
                    inactive: {
                        opacity: 1
                    }
                },
            }
        },
    }
}

function getLabel(instance: EntityInstance, entitySet: EntitySet, showColor: boolean, highlight: boolean): string {
    const labelText = _.truncate(instance.name, {length: 23, separator: '\u2026'});
    if (!showColor) {
        return `<span style="color:grey">${labelText}</span>`;
    }

    const fontWeight = highlight ? "bold" : "normal";
    return `<span class="brandBullet material-symbols-outlined" style="color:${entitySet.getInstanceColor(instance)}">\ue061</span><span style="font-weight:${fontWeight};">${labelText}</span>`;
}