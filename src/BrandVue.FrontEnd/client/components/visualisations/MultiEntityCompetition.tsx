import React from "react";
import * as BrandVueApi from "../../BrandVueApi";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { EntityInstance } from "../../entity/EntityInstance";
import { Metric } from "../../metrics/metric";
import { ViewHelper } from "./ViewHelper";
import { ChartFooterInformation } from "./ChartFooterInformation";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import AugmentedReactHighCharts from "./AugmentedReactHighCharts";
import LowSample from "./BrandVueOnlyLowSampleHelper";
import { ICommonDataPoint } from "./ICommonDataPoint";
import AverageTotalRequestModel = BrandVueApi.AverageTotalRequestModel;
import {
    MultiMetricResults,
    Factory,
    WeightedDailyResult,
    OverTimeAverageResults,
    StackedMultiEntityResults,
    AverageType,
    AverageStackedMultiEntityChartsModel,
    MetricWeightedDailyResult,
    SampleSizeMetadata,
    MultiMetricSeries, ComparisonPeriodSelection,
} from "../../BrandVueApi";
import { EntitySet } from "../../entity/EntitySet";
import { IGoogleTagManager } from "../../googleTagManager";
import { FilterInstance } from "../../entity/FilterInstance";
import { useEntityConfigurationStateContext } from "../../entity/EntityConfigurationStateContext";
import { EntitySetAverage } from "../../entity/EntitySetAverage";
import { MetricResultsSummary } from "../helpers/MetricInsightsHelper";
import ColumnChart from "./ColumnChart";
import { getFormattedLabel } from "../../helpers/HighchartHelper";
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from "client/state/subsetSlice";
import { ITimeSelectionOptions } from "../../state/ITimeSelectionOptions";
import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

interface IMultiEntityCompetitionProps {
    partId: number;
    googleTagManager: IGoogleTagManager;
    title: string;
    height: number;
    curatedFilters: CuratedFilters;
    metric: Metric;
    entitySet: EntitySet;
    filterInstance?: FilterInstance;
    availableEntitySets: EntitySet[];
    activeInstance: EntityInstance;
    maxNumberOfEntries: number;
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void;
    updateAverageRequests: (averageRequests: AverageTotalRequestModel[]) => void;
}

type AverageData = {
    name: string,
    data: OverTimeAverageResults
}

type SeriesOrder = {
    index: number,
    value: number
}

const MultiEntityCompetition = (props: IMultiEntityCompetitionProps) => {
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const getEmptyAverageResults = () => {
        return [{ name: 'Default', data: new BrandVueApi.MultiMetricAverageResults() }];
    }
    const [, handleError] = React.useState();
    const [results, setResults] = React.useState(new BrandVueApi.MultiMetricResults());
    const [averageResults, setAverageResults] = React.useState(getEmptyAverageResults());
    
    const WeightedDailyResultToObject = (weightedDailyResult: WeightedDailyResult): any => {
        return {
            date: weightedDailyResult.date,
            responseIdsForDay: [],
            unweightedSampleSize: weightedDailyResult.unweightedSampleSize,
            unweightedValueTotal: weightedDailyResult.unweightedValueTotal,
            weightedResult: weightedDailyResult.weightedResult,
            weightedSampleSize: weightedDailyResult.weightedSampleSize,
            weightedValueTotal: weightedDailyResult.weightedValueTotal
        };
    }

    const overtimeAverageResultsToMultiMetricAverageResults = (averageData: OverTimeAverageResults[], sortOrder: number[]): BrandVueApi.MultiMetricAverageResults => {
        let result = new BrandVueApi.MultiMetricAverageResults();
        result.average = averageData.map(a => {
            let mwdr = new MetricWeightedDailyResult();
            const latestAverage = a.weightedDailyResults.slice(-1)[0];
            mwdr.metricName = latestAverage.text;
            mwdr.weightedDailyResult = WeightedDailyResultToObject(latestAverage);
            return mwdr;
        });
        result.average = sortArrayByIndices(result.average, sortOrder);
        return result;
    }

    async function getAverageData(props: IMultiEntityCompetitionProps, metric: Metric, sortOrder: number[], timeSelection: ITimeSelectionOptions) {
        const averages = props.entitySet.getAverages().getAll();
        
        const promises = averages.map(async average => {
            const entitySet = average.getEntitySet(props.entitySet, props.availableEntitySets);

            const averageRequestModelOrNull = ViewHelper.createAverageRequestModelOrNull(
                entitySet.getInstances().getAll().map(b => b.id),
                [metric],
                props.curatedFilters,
                props.activeInstance.id,
                {},
                subsetId,
                timeSelection);
            const averageStackedMultiEntityChartsModel = new AverageStackedMultiEntityChartsModel({
                averageType: AverageType.Mean,
                stackedMultiEntityRequestModel: requestModelForAverageFromProps(entitySet)
            });
            const averageData =  await Factory.DataClient(throwError => throwError())
                .getAverageForStackedMultiEntityCharts(averageStackedMultiEntityChartsModel);

            return { name: EntitySetAverage.getChartDisplayName(entitySet.name), data: overtimeAverageResultsToMultiMetricAverageResults(averageData, sortOrder), requestModel: averageRequestModelOrNull };
        });

        Promise.all(promises).then((r) => {
            props.updateAverageRequests(r.filter(x => x.requestModel).map(x => new AverageTotalRequestModel({
                averageName: x.name,
                requestModel: x.requestModel!
            }
            )));
            setAverageResults(r);
        });
    }

    const sortArrayByIndices = (array: any[], indices: number[]) => {
        let result : any[] = [];
        for (let i = 0; i < indices.length; i++) {
            result.push(array[indices[i]]);
        }
        return result;
    }

    const createSampleSizeMetadata = (orderedMeasures: string[], activeSeries: MultiMetricSeries, entityInstanceName: string): SampleSizeMetadata => {
        const lastIndex = activeSeries.orderedData[0].length - 1;
        const resultForSample =  activeSeries.orderedData[0][lastIndex];
        const sampleSize = new BrandVueApi.UnweightedAndWeightedSample({
            unweighted: resultForSample.unweightedSampleSize,
            weighted: resultForSample.weightedSampleSize,
            hasDifferentWeightedSample: Math.abs(resultForSample.weightedSampleSize - resultForSample.unweightedSampleSize) >= 1
        });
        const sampleSizes = activeSeries.orderedData.map((e) => e[e.length - 1].unweightedSampleSize);
        const allSamplesAreEqual = sampleSizes.every((v) => v === sampleSize.unweighted);
        return new SampleSizeMetadata({
            sampleSizeEntityInstanceName: entityInstanceName,
            sampleSize: sampleSize,
            sampleSizeByEntity: {},
            sampleSizeByMetric: allSamplesAreEqual ? {} : Object.assign({}, ...activeSeries.orderedData.map((e, i) => ({
                [orderedMeasures[i]]: new BrandVueApi.UnweightedAndWeightedSample({
                    unweighted: e[e.length - 1].unweightedSampleSize,
                    weighted: e[e.length - 1].weightedSampleSize,
                    hasDifferentWeightedSample: Math.abs(e[e.length - 1].weightedSampleSize - e[e.length - 1].unweightedSampleSize) >= 1
                })
            }))),
            currentDate: activeSeries.orderedData[0][lastIndex].date,
        });
    }

    const stackedMutliEntityResultsToMultiMetricResultsAndSortOrder = (response: StackedMultiEntityResults): any => {
        let result = new MultiMetricResults();
        const mainInstance = props.entitySet.mainInstance ?? props.entitySet.getInstances().getAll()[0];

        result.orderedMeasures = response.resultsPerInstance.map(e => e.filterInstance.name);
        const series : any = [];
        for (let i = 0; i < response.resultsPerInstance[0].data.length; i++) {
            series.push({
                    entityInstance: response.resultsPerInstance[0].data[i].entityInstance,
                    orderedData: response.resultsPerInstance.map(e => e.data[i].weightedDailyResults)
                });
        }
        result.activeSeries = series.length == 1 ? series[0] : series.find((s) => s.entityInstance.id == props.activeInstance.id) ?? series[0];
        result.comparisonSeries = series.length > 1 ? series.filter((s) => s.entityInstance.id != props.activeInstance.id) : [];
        result.hasData = response.hasData;
        result.trialRestrictedData = response.trialRestrictedData;

        const lastIndex = result.activeSeries.orderedData[0].length - 1;
        const order: SeriesOrder[] = result.activeSeries.orderedData.map((e, i) => {
            return { index: i, value: e[lastIndex].weightedResult }
        } );
        const sortOrder = order.sort((a, b) => b.value - a.value).map((e) => e.index);
        result.activeSeries.orderedData = sortArrayByIndices(result.activeSeries.orderedData, sortOrder);
        result.comparisonSeries.forEach((s) => {
            s.orderedData = sortArrayByIndices(s.orderedData, sortOrder);
        });
        result.orderedMeasures = sortArrayByIndices(result.orderedMeasures, sortOrder);
        result.sampleSizeMetadata = createSampleSizeMetadata(result.orderedMeasures, result.activeSeries, mainInstance.name);
        
        return { result, sortOrder };
    }

    const processStackedMultiEntityResults = (res: StackedMultiEntityResults): void => {
        const r = stackedMutliEntityResultsToMultiMetricResultsAndSortOrder(res);
        setResults(r.result);

        setAverageResults(getEmptyAverageResults());

        if (props.metric.isProfileMetric()) {
            return;
        }

        getAverageData(props, props.metric, r.sortOrder, timeSelection);
    }

    const getDefaultFilterByEntitySet = () : EntitySet => {
        const filterByEntityType = props.metric.entityCombination.find(e => e.identifier !== props.entitySet.type.identifier);
        return entityConfiguration.getDefaultEntitySetFor(filterByEntityType!);
    }

    const requestModelFromProps = () => {
        const requestModel = ViewHelper.createStackedMultiEntityRequestModel(
            props.curatedFilters,
            props.metric,
            props.entitySet,
            getDefaultFilterByEntitySet(),
            false,
            subsetId,
            timeSelection
        );
        if (props.curatedFilters.comparisonPeriodSelection !== ComparisonPeriodSelection.CurrentPeriodOnly) {
            requestModel.splitBy.entityInstanceIds = [props.activeInstance.id];
        }
        return requestModel;
    }

    const requestModelForAverageFromProps = (entitySet: EntitySet) => {
        const requestModel = ViewHelper.createStackedMultiEntityRequestModel(
            props.curatedFilters,
            props.metric,
            entitySet,
            getDefaultFilterByEntitySet(),
            false,
            subsetId,
            timeSelection
        );
        return requestModel;
    }

    const load = async () => {
        await Factory.DataClient(err => err()).getStackedResultsForMultipleEntities(requestModelFromProps()).then(processStackedMultiEntityResults)
            .catch((e: Error) => {
                handleError(() => { throw e });
            });
    }

    React.useEffect(() => {
        load();
    }, [
        JSON.stringify(props.entitySet),
        props.activeInstance,
        props.height,
        JSON.stringify(props.curatedFilters),
    ]);

    const insertSpacers = (categories, series) => {
        if (categories != null && categories.length && categories[0].categories) {
            let position = 0;
            let categoryIndex = 0;
            while (categoryIndex < categories.length) {
                let category = categories[categoryIndex];
                if (categoryIndex > 0) {
                    categories.splice(categoryIndex, 0, { name: ' ', categories: [' '] });
                    for (let s of series) {
                        s.data.splice(position, 0, null);
                    }
                    categoryIndex++;
                    position++;
                }
                position = position + category.categories.length;
                categoryIndex++;
            }
        }
    }
    const formatter = props.metric.longFmt;
    const startPositionToRemoveData = props.maxNumberOfEntries;
    const orderedMeasures = results.orderedMeasures.slice();
    const numberOfElementsToRemove = orderedMeasures.length - startPositionToRemoveData;
    const trimData = (props.maxNumberOfEntries > 0) && (numberOfElementsToRemove > 0);

    if (trimData) {
        orderedMeasures.splice(startPositionToRemoveData, numberOfElementsToRemove);
    }
    const series: any = [];

    if (results.activeSeries.orderedData.length) {
        const brandColor = props.entitySet.getInstanceColor(props.activeInstance);

        let periodCount = results.activeSeries.orderedData[0].length;

        let periodStartIndex = 0;
        if (props.curatedFilters.comparisonPeriodSelection === ComparisonPeriodSelection.CurrentPeriodOnly) {
            periodStartIndex = periodCount - 1;
        }

        if (averageResults.length > 0 && averageResults[0].data.average.length) {
            averageResults.map(averageResult => {
                var seriesTitle = averageResult.name;
                if (props.curatedFilters.comparisonPeriodSelection !==
                    ComparisonPeriodSelection.CurrentPeriodOnly) {
                    const averageDateFormatted = DateFormattingHelper.formatDateRange(
                        averageResult.data.average[0].weightedDailyResult.date,
                        props.curatedFilters.average);
                    seriesTitle += `<br/>(${averageDateFormatted})`;
                }
                series.push({
                    name: seriesTitle,
                    isAverage: true,
                    data: averageResult.data.average.map((d, metricIndex) => new SingleBarChartDataPoint(seriesTitle,
                        props.metric,
                        d.weightedDailyResult,
                        orderedMeasures[metricIndex])),
                    type: 'line',
                    dashStyle: 'dash',
                    color: '#666',
                    canToggle: true,
                });
            });
        }

        if (props.curatedFilters.comparisonPeriodSelection === ComparisonPeriodSelection.CurrentPeriodOnly) {
            // Show comparison brands
            results.comparisonSeries.map(s => series.push({
                id: s.entityInstance.id,
                name: s.entityInstance.name,
                data: s.orderedData.map((d, i) => new SingleBarChartDataPoint(s.entityInstance.name, props.metric,
                    d ? d[d.length - 1] : undefined,
                    orderedMeasures[i],
                    props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(s.entityInstance)))),
                type: 'line',
                marker: {
                    radius: 3
                },
                lineWidth: 1,
                color: props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(s.entityInstance))
            }
            ));
        }
        for (let periodIndex = periodStartIndex; periodIndex < periodCount; periodIndex++) {
            let seriesName;
            const seriesByDate = results.activeSeries.orderedData[0] != null
                ? DateFormattingHelper.formatDateRange(results.activeSeries.orderedData[0][periodIndex].date,
                    props.curatedFilters.average)
                : "";
            seriesName = results.activeSeries.entityInstance.name;
            //No worries if colors is indexed outof range as this gives us undefined. in which case Highcharts just nominates a color
            let color = periodIndex === periodCount - 1 ? brandColor : undefined;
            const period = results.activeSeries.orderedData[0][periodIndex].date;
            //Set fullName including date so toggling visibility via the legend triggers only one line
            let fullName = seriesName;
            if (seriesName === props.activeInstance.name) {
                const formattedDate =  ` (${DateFormattingHelper.formatDateRange(period, props.curatedFilters.average)})`;
                fullName = seriesName + formattedDate;
            }

            series.push(
                {
                    fullName: fullName,
                    name: seriesName,
                    period: period,
                    isAverage: false,
                    data: results.activeSeries.orderedData.map(
                        (dataItem, dataIndex) => new SingleBarChartDataPoint(
                            props.metric.isProfileMetric() ? seriesByDate : seriesName,
                            props.metric,
                            dataItem[periodIndex],
                            orderedMeasures[dataIndex],
                            color)),
                    type: 'line',
                    color: color,
                    canToggle: true
                }
            );
        }

        if (trimData) {
            series.map(s => s.data.splice(startPositionToRemoveData, numberOfElementsToRemove));
        }

    }
    LowSample.addLowSampleIndicators(series);
    insertSpacers(orderedMeasures, series);

    const yaxisTitle = props.metric.graphAxisTitle;
    const highlightBrand = props.activeInstance.name;

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
        title: {
            text: props.title
        },
        tooltip: {
            formatter: function (this: any): string {
                const name = this.point.title;
                const pointName = this.point.name;
                const formattedValue = this.point.formattedLongValue;
                const sampleSize = this.point.formatn;
                const color = this.series.color;

                return `<div class="custom-tooltip-title">
                            <div class="custom-tooltip-dot" style="background: ${color};">&nbsp;</div> 
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
                formatter: (e: any): string => getFormattedLabel(e.axis, e.value.toString(), "normal")
            }
        },
        yAxis: {
            ceiling: props.metric.isPercentage() ? 1 : undefined,
            title: {
                text: yaxisTitle
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
            labelFormatter: function (this: { name: any, options: any }) {
                if (this.name === highlightBrand && !this.options.isAverage) {
                    const formattedDate = ` (${DateFormattingHelper.formatDateRange(this.options.period,
                            props.curatedFilters.average)})`;

                    return `<b>${this.name}${formattedDate}</b>`;
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

                lineWidth: 2,
                states: {
                    hover: {
                        lineWidth: 3
                    }
                }
            },
            column: {
                animation: false,
            },
            series: {
                dataLabels: {
                    enabled: false,
                    color: '#ccc',
                    format: '{point.formattedValue}',
                    y: 20,
                },
                states: {
                    inactive: {
                        opacity: 1
                    }
                }
            }
        },
        series: series
    };

    if (props.metric.entityCombination.length === 1) {
        return (
            <ColumnChart
                height={props.height}
                metrics={[props.metric]}
                curatedFilters={props.curatedFilters}
                activeBrand={props.activeInstance}
                entitySet={props.entitySet}
                filterInstance={props.filterInstance}
                updateMetricResultsSummary={props.updateMetricResultsSummary}
                availableEntitySets={props.availableEntitySets}
                updateAverageRequests={props.updateAverageRequests}
                partId={props.partId}
            />
        );
    }
    else {
        return (
            <React.Fragment>
                <AugmentedReactHighCharts config={config} afterRender={afterRender} googleTagManager={props.googleTagManager} />
                <ChartFooterInformation sampleSizeMeta={results.sampleSizeMetadata} activeBrand={props.activeInstance} metrics={[props.metric]} average={props.curatedFilters.average} doesHaveBrandMetric={true} />
            </React.Fragment>
        );
    }
}

export default MultiEntityCompetition;

export class SingleBarChartDataPoint implements ICommonDataPoint {
    constructor(title: string, metric?: Metric, resultOrNull?: BrandVueApi.WeightedDailyResult, name?: string, brandColor?: string) {
        if (resultOrNull
            && resultOrNull.weightedResult !== null
            && resultOrNull.weightedResult !== undefined
            && metric) {
            this.y = resultOrNull.weightedResult;
            this.sampleSize = resultOrNull.unweightedSampleSize;
            this.formattedValue = metric.fmt(resultOrNull.weightedResult);
            this.formattedLongValue = metric.longFmt(resultOrNull.weightedResult);
            this.formatn = NumberFormattingHelper.format0Dp(this.sampleSize);
            this.title = title;

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
    public title: string;
}

