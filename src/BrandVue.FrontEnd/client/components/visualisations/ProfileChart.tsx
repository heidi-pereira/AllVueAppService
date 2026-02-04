import { Metric } from "../../metrics/metric";
import React from "react";
import * as BrandVueApi from "../../BrandVueApi";
import {BreakdownResults, ComparisonPeriodSelection, MultiEntityRequestModel} from "../../BrandVueApi";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { EntityInstance } from "../../entity/EntityInstance";
import { ViewHelper } from "./ViewHelper";
import { ChartFooterInformation } from "./ChartFooterInformation";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import AugmentedReactHighCharts from "./AugmentedReactHighCharts";
import LowSample from "./BrandVueOnlyLowSampleHelper";
import { ICommonDataPoint } from "./ICommonDataPoint";
import { EntitySet, defaultFocusColour } from "../../entity/EntitySet";
import { FilterInstance } from "../../entity/FilterInstance";
import BrokenDownResults = BrandVueApi.BrokenDownResults;
import AverageTotalRequestModel = BrandVueApi.AverageTotalRequestModel;
import CuratedResultsModel = BrandVueApi.CuratedResultsModel;
import CompressClientData = BrandVueApi.Factory;
import { EntitySetAverage } from "../../entity/EntitySetAverage";
import { getMetricResultsSummaryFromBreakdownResults, MetricResultsSummary } from "../helpers/MetricInsightsHelper";
import { QueryStringParamNames, useReadVueQueryParams } from "../helpers/UrlHelper";
import { IGoogleTagManager } from "../../googleTagManager";
import { EntityInstanceGroup } from "../../entity/EntityInstanceGroup";
import {useAppDispatch, useAppSelector} from "../../state/store";
import {setGenericAverageResults, setGenericResults} from "../../state/resultsSlice";
import { selectSubsetId } from '../../state/subsetSlice';

import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

interface IProfileChartProps {
    partId: number;
    googleTagManager: IGoogleTagManager;
    activeBrand: EntityInstance;
    entitySet: EntitySet;
    availableEntitySets: EntitySet[];
    filterInstance?: FilterInstance;
    height: number;
    metric: Metric;
    curatedFilters: CuratedFilters;
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void;
    updateAverageRequests: (averageRequests: AverageTotalRequestModel[]) => void;
}

const getEntitySetForSingleEntity = (metric: Metric, entitySet: EntitySet, activeBrand: EntityInstance, comparisonPeriodSelection: ComparisonPeriodSelection) => {
    if (comparisonPeriodSelection === ComparisonPeriodSelection.CurrentPeriodOnly) {
        return entitySet;
    }

    const activeEntityInstance = new EntityInstance(activeBrand.id, activeBrand.name);
    return entitySet.cloneSet(new EntityInstanceGroup([activeEntityInstance]));
}

const ProfileChart = (props: IProfileChartProps) => {

    const getDefaultAverageData = () => {
        return [{ name: 'Default', results: new BrandVueApi.BreakdownResults() }];
    }
    const { getQueryParameterInt } = useReadVueQueryParams();
    const [data, setData] = React.useState<BrandVueApi.BreakdownResults>(new BrandVueApi.BreakdownResults());
    const [averageData, setAverageData] = React.useState(getDefaultAverageData());
    const [metricResultsSummary, setMetricResultsSummary] = React.useState<MetricResultsSummary>();
    const dispatch = useAppDispatch(); 
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    React.useEffect(() => {
        props.updateMetricResultsSummary(metricResultsSummary!);
    }, [JSON.stringify(metricResultsSummary)]);

    const multiEntityModelFromProps = (): MultiEntityRequestModel => {
        const isMultiEntity = props.metric.entityCombination.length > 1;
        return ViewHelper.createMultiEntityRequestModel({
            curatedFilters: props.curatedFilters,
            metric: props.metric,
            splitBySet: isMultiEntity ? props.entitySet : getEntitySetForSingleEntity(props.metric, props.entitySet, props.activeBrand, props.curatedFilters.comparisonPeriodSelection),
            filterInstances: props.filterInstance ? [props.filterInstance]: [],
            continuousPeriod: false,
            focusEntityId: isMultiEntity ? props.activeBrand.id: undefined,
            subsetId: subsetId
        }, timeSelection);
    }

    const processProfileResult = (result: BreakdownResults): void => {
        setData(result);
        setMetricResultsSummary(getMetricResultsSummaryFromBreakdownResults(result));
    }

    const getAndProcessProfileResults = async () => {
        var request = multiEntityModelFromProps();
        await CompressClientData.DataClient(err => err()).breakdown(request
        ).then((results) => {
            processProfileResult(results);
            dispatch(setGenericResults({
                results: results,
                request: request,
                averages: [],
                averagesSelected: props.entitySet.getAverages().getAll().length,
                partId: props.partId,
            }))
        });
    };

    const getAndProcessProfileAverageResults = () => {
        switch (props.metric.entityCombination.length) {
            case 0:
                return;
            case 1:
                return getAndProcessProfileAverageResultsSingleEntity();
            default:
                return getAndProcessProfileAverageResultsMultiEntity();
        }
    }

    const requestAverageModelFromProps = (average: EntitySetAverage): CuratedResultsModel | null => {

        const averageSet = average.getEntitySet(props.entitySet, props.availableEntitySets);
        if (!averageSet) {
            return null;
        }
        const instancesForAverage = averageSet.getInstances();
        let averageModel = ViewHelper.createAverageRequestModelOrNull(instancesForAverage.getAll().map(b => b.id),
            [props.metric],
            props.curatedFilters,
            props.activeBrand.id,
            { continuousPeriod: false },
            subsetId,
            timeSelection);
        const averageName = ViewHelper.createAverageDescription([props.metric]);
        if(!averageModel) {
            return null;
        }
        var model = new AverageTotalRequestModel();
        model.averageName = averageName;
        model.requestModel = averageModel;
        //Needed for average in excel export
        return averageModel;
    }

    const getAndProcessProfileAverageResultsSingleEntity = async () => {
        setAverageData(getDefaultAverageData());
        let averageRequests = [] as AverageTotalRequestModel[];
        var promises = props.entitySet.getAverages().getAll().map(async average => {
            var averageRequestModel = requestAverageModelFromProps(average)!;
            var averageName = average.getEntitySet(props.entitySet, props.availableEntitySets).name;
            averageRequests.push(new AverageTotalRequestModel({
                    averageName: averageName,
                    requestModel: averageRequestModel
                }
            ));
            return await CompressClientData.DataClient(err => err()).breakdownAverageSingleEntity(averageRequestModel)
                .then(r => ({ 
                    name: averageName, 
                    results: r,
                    request: multiEntityRequestAverageModelFromProps(average)}));
        });
        Promise.all(promises).then((r) => {
            props.updateAverageRequests(averageRequests);
            setAverageData(r);
            dispatch(setGenericAverageResults({
                partId: props.partId,
                payload: r
            }));
        });
    }

    const getAndProcessProfileAverageResultsMultiEntity = async () => {
        setAverageData(getDefaultAverageData());
        let averageRequests = [] as AverageTotalRequestModel[];
        var promises = props.entitySet.getAverages().getAll().map(async average => {
            var averageRequestModel = requestAverageModelFromProps(average)!;
            var averageName = average.getEntitySet(props.entitySet, props.availableEntitySets).name;

            averageRequests.push(new AverageTotalRequestModel({
                averageName: averageName,
                requestModel: averageRequestModel
            }
        ));
            const averageRequest = multiEntityRequestAverageModelFromProps(average);
            return await CompressClientData.DataClient(err => err()).breakdownAverage(averageRequest)
                .then(results => ({ 
                    name: average.getEntitySet(props.entitySet, props.availableEntitySets).name, 
                    results: results, 
                    request: averageRequest}));
        });

        Promise.all(promises).then((r) => {
            props.updateAverageRequests(averageRequests)
            setAverageData(r);
            dispatch(setGenericAverageResults({
                partId: props.partId,
                payload: r
            }));
        });
    };

    React.useEffect(() => {
            setData(new BrandVueApi.BreakdownResults());
            getAndProcessProfileResults();
            getAndProcessProfileAverageResults();
        },
        [
            JSON.stringify(props.entitySet),
            props.filterInstance,
            props.activeBrand,
            props.height,
            JSON.stringify(props.curatedFilters),
        ]);

    const multiEntityRequestAverageModelFromProps = (average: EntitySetAverage): MultiEntityRequestModel => {
        const averageSet = average.getEntitySet(props.entitySet, props.availableEntitySets);
        if (!averageSet) {
            throw new Error("Cannot find average");
        }

        const averageModel = ViewHelper.createAverageRequestModelForMultipleEntities(props.curatedFilters,
            props.metric,
            averageSet,
            props.filterInstance ? [props.filterInstance] : [],
            false,
            subsetId,
            timeSelection);

        return averageModel;

    }

    const getLastDataPointOrPlaceholder = (categoryResults: BrandVueApi.CategoryResults[], metric: Metric, periodIndex: number) => {
        return categoryResults.map(groupResults =>
            new SingleBarChartDataPoint(metric,
                groupResults.weightedDailyResults[periodIndex],
                groupResults.category, undefined, props.curatedFilters.average));
    }

    const getPointsForBreakdown = (breakDown: BrandVueApi.CategoryResults[], metric: Metric, separatorPoint: SingleBarChartDataPoint, periodIndex: number) : SingleBarChartDataPoint[] => {
        if (breakDown.length < 2) return [];
        return [
            separatorPoint,
            ...getLastDataPointOrPlaceholder(breakDown, metric, periodIndex)
        ];
    }

    const getDataPoints = (brandResults: BrandVueApi.BrokenDownResults, metric: Metric, periodIndex: number) => {
        const separatorPoint = new SingleBarChartDataPoint(metric, undefined, "-", undefined, new BrandVueApi.AverageDescriptor());
        const total = [
            new SingleBarChartDataPoint(metric, brandResults.total[periodIndex], "Total", undefined, props.curatedFilters.average)
        ];

        return total
            .concat(getPointsForBreakdown(brandResults.byAgeGroup, metric, separatorPoint, periodIndex))
            .concat(getPointsForBreakdown(brandResults.byGender, metric, separatorPoint, periodIndex))
            .concat(getPointsForBreakdown(brandResults.byRegion, metric, separatorPoint, periodIndex))
            .concat(getPointsForBreakdown(brandResults.bySocioEconomicGroup, metric, separatorPoint, periodIndex));
    }

    const getDate = (brandResults: BrokenDownResults | undefined, index : number): Date => {
        if (brandResults != null &&
            brandResults.byAgeGroup != null &&
            brandResults.byAgeGroup[0].weightedDailyResults.length > 0) {
            return brandResults.byAgeGroup[0].weightedDailyResults[index].date;
        }
        return new Date();
    }

    const activeEntity = (): EntityInstance => {
        if (props.metric.isProfileMetric()) {
            return new EntityInstance(-1, "ProfileMetric");
        }
        if (props.entitySet.type.isBrand) {
            return props.activeBrand;
        }
        const allInstances = props.entitySet.getInstances();

        if (props.entitySet.mainInstance) {
            if (allInstances.getById(props.entitySet.mainInstance.id))
                return props.entitySet.mainInstance;
        }
        return allInstances.getAll()[0];
    }

    const fallBackToFirstBrokenDownResult = (brandBrokenDownResults: BrandVueApi.BrokenDownResults[]) => {
        //
        // This code should be gone by the end of 2023
        //
        // Temporary hack until we can correctly either visualize all the data
        // or select the required entity
        //
        if (!props.entitySet.type.isBrand && brandBrokenDownResults.length > 0) {
            return brandBrokenDownResults[0];
        }
        return undefined;
    }

    const getZeroedCategoryResults = (results: BrandVueApi.CategoryResults[]) => {
        return results.map(ag => new BrandVueApi.CategoryResults({
            category: ag.category,
            weightedDailyResults: ag.weightedDailyResults.map(wdr => new BrandVueApi.WeightedDailyResult({
                ...wdr,
                weightedResult: 0
            }))
        }));
    }

    const getZeroedBreakdownResults = (results: BrandVueApi.BrokenDownResults) => {
        return new BrokenDownResults({
            ...results,
            byAgeGroup: getZeroedCategoryResults(results.byAgeGroup),
            byGender: getZeroedCategoryResults(results.byGender),
            byRegion: getZeroedCategoryResults(results.byRegion),
            bySocioEconomicGroup: getZeroedCategoryResults(results.bySocioEconomicGroup),
            total: results.total.map(wdr => new BrandVueApi.WeightedDailyResult({
                ...wdr,
                weightedResult: 0
            }))
        });
    }

    const showDataLabels = ():boolean => {
        return getQueryParameterInt(QueryStringParamNames.showDataLabels) == 1;
    }
    const isWeightedDailResultCloseTo100Percent = (weightedDailyResults: BrandVueApi.WeightedDailyResult[]): boolean => {
        const percentCloseTo100 = 0.96;
        return weightedDailyResults.some(weightedDailyResult => weightedDailyResult.weightedResult > percentCloseTo100)
    }

    const isResultCloseTo100Percent = (group: BrandVueApi.CategoryResults[]): boolean =>
    {
        return group.some(categoryResults => isWeightedDailResultCloseTo100Percent(categoryResults.weightedDailyResults));
    }

    const isBrokenDownResultCloseTo100Percent = (brokenDownResults: BrokenDownResults): boolean => {
        return isResultCloseTo100Percent(brokenDownResults.byAgeGroup) ||
            isResultCloseTo100Percent(brokenDownResults.byGender) ||
            isResultCloseTo100Percent(brokenDownResults.byRegion) ||
            isResultCloseTo100Percent(brokenDownResults.bySocioEconomicGroup) ||
            isWeightedDailResultCloseTo100Percent(brokenDownResults.total)
    }

    const isBreakdownResultsCloseTo100Percent = (breakdownResult: BrandVueApi.BreakdownResults): boolean => {
        return breakdownResult.data.some(brokenDownResults =>
            isBrokenDownResultCloseTo100Percent(brokenDownResults));
    }

    const isResultsNear100Percent = (): boolean => {
        return isBreakdownResultsCloseTo100Percent(data) ||
            averageData.some(breakdownResult => isBreakdownResultsCloseTo100Percent(breakdownResult.results))
    }

    const makeChartFromSingleMetricBreakdown = (brandBrokenDownResults: BrandVueApi.BrokenDownResults[], metric: Metric) => {
        const firstPeriod = 0;
        let chartData: any = [];
        let currentlySelectedEntity = activeEntity();
        const isProfileMetric = metric.isProfileMetric();

        if (props.curatedFilters.comparisonPeriodSelection !== ComparisonPeriodSelection.CurrentPeriodOnly) {

            let intermediateSelectedResult = isProfileMetric ? (brandBrokenDownResults ? brandBrokenDownResults[0] : undefined) : brandBrokenDownResults.find(f => f.entityInstance.id === currentlySelectedEntity.id);

            if (intermediateSelectedResult == undefined) {
                intermediateSelectedResult = fallBackToFirstBrokenDownResult(brandBrokenDownResults);
                if (intermediateSelectedResult != undefined) {
                    currentlySelectedEntity = EntityInstance.convertInstanceFromApi(intermediateSelectedResult.entityInstance);
                }
            }
            const selectedResult = intermediateSelectedResult;
            if (selectedResult) {
                if (selectedResult.byAgeGroup != null && selectedResult.byAgeGroup[0].weightedDailyResults.length > 0) {
                    let ids = selectedResult.byAgeGroup[0].weightedDailyResults.length >= 2 ? [0, 1] : [0];
                    ids.map((periodIndex, index) => {
                        const id = !isProfileMetric ? selectedResult.entityInstance.id : index;
                        const name = !isProfileMetric ? selectedResult.entityInstance.name : metric.name;

                        chartData.push({
                            id: id,
                            period: selectedResult.byAgeGroup[0].weightedDailyResults[periodIndex].date,
                            name: name,
                            color: periodIndex === 1 ? defaultFocusColour : undefined,
                            data: getDataPoints(selectedResult, metric, periodIndex),
                            canToggle: false
                        })
                    });
                }
            } else {
                if (averageData.length && averageData[0].results.data.length) {
                    chartData.push({
                        id: currentlySelectedEntity.id,
                        period: getDate(averageData[0].results.data.slice(-1)[0], firstPeriod),
                        name: currentlySelectedEntity.name,
                        color: defaultFocusColour,
                        data: getDataPoints(getZeroedBreakdownResults(averageData[0].results.data[0]), metric, firstPeriod),
                        canToggle: false
                    })
                }
            }
        } else {
            brandBrokenDownResults.map((brandResults,index)  => {
                const id = !isProfileMetric ? brandResults.entityInstance.id : index;
                const name = !isProfileMetric ? brandResults.entityInstance.name : metric.name;
                chartData.push({
                    id: id,
                    name: name,
                    color: props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(brandResults.entityInstance)),
                    period: getDate(brandResults, firstPeriod),
                    lineWidth: id === currentlySelectedEntity.id ? 2 : 1,
                    marker: {
                        radius: id === currentlySelectedEntity.id ? 4 : 3
                    },
                    data: getDataPoints(brandResults, metric, firstPeriod)
                })
            });
        }

        const chartType = props.curatedFilters.comparisonPeriodSelection === ComparisonPeriodSelection.CurrentPeriodOnly ? "line" : "column";

        if (averageData.length && averageData[0].results.data.length) {
            averageData.map(average => {
                chartData.push({
                    name: EntitySetAverage.getChartDisplayName(average.name),
                    color: '#666',
                    period: getDate(average.results[0], firstPeriod),
                    data: getDataPoints(average.results.data[0], metric, firstPeriod),
                    canToggle: true,
                    legendIndex: 99,
                    dashStyle: chartType === "line" ? "dash" : "solid"
                });
            });
        }

        if (chartData.count > 0 && chartData[0].id != currentlySelectedEntity.id) {
            chartData.sort(function (x, y) { return x.id == currentlySelectedEntity.id ? -1 : y == currentlySelectedEntity.id ? 1 : 0; });
        }
        var formatter = metric.getNumberFormatForAxis(ViewHelper.calcMaxMinusMinValueBySeries(chartData));
        var highlightBrand = isProfileMetric ? metric.name : currentlySelectedEntity?.name;
        const categories = chartData.length ? chartData[0].data.map(c => c.name) : [];
        const maxYAxis = props.metric.isPercentage() ? ((showDataLabels() && isResultsNear100Percent()) ? 1.01 : 1) : undefined;
        LowSample.addLowSampleIndicators(chartData);
        return {
            chart: {
                height: props.height,
                type: chartType,
                backgroundColor: 'rgba(255,255,255,0)'
            },
            tooltip: {
                formatter: function(this: any): string {
                    const name = this.series.name;
                    const pointName = this.point.name;
                    const seriesName = this.point.formattedDate;
                    const formattedValue = this.point.formattedValue;
                    const sampleSize = this.point.formatn;

                    return `<div class="custom-tooltip-title">
                            <div class="custom-tooltip-dot" style="background: ${this.color};">&nbsp;</div>
                            <span>${name}</span>
                        </div>
                        <div class="custom-tooltip-point">${pointName}</div>
                        <div class="custom-tooltip-point">Date: ${seriesName}</div>
                        <div class="custom-tooltip-point">Value: ${formattedValue}</div>
                        <div class="custom-tooltip-point">n = ${sampleSize}</div>`;
                },
                outside: true
            },
            xAxis: {
                type: 'category',
                categories: categories,
                labels: {
                    formatter: function(this: any) {
                        const fontWeight = this.value === highlightBrand ? "bold" : "normal";
                        return `<span style="font-weight:${fontWeight};">${this.value}</span>`;
                    }
                }
            },
            yAxis: {
                ceiling: maxYAxis,
                title: {
                    text: metric.yAxisTitle()
                },
                labels: {
                    formatter: function(this: { value: any }) {
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
                itemStyle: {
                    fontWeight: "normal",
                },
                labelFormatter: function(this: { name: any, options: any }) {
                    if ((this.name === highlightBrand ) ) {
                        const formattedDate = " (" +
                            DateFormattingHelper.formatDateRange(this.options.period,
                                props.curatedFilters.average) +
                            ")";
                        if (this.name === highlightBrand)
                            return "<b>" + this.name + formattedDate + "</b>";
                        return this.name + formattedDate;
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
                    dataLabels: {
                        enabled: showDataLabels(),
                        color: '#999',
                        format: '{point.displayValue}',
                        y: -5 as any,
                        style: {
                            textOutline: '3px white',
                            fontWeight: '400',
                            fontSize: '12px',
                        },
                    },
                    animation: false,
                },
                series: {
                    dataLabels: {
                        enabled: false,
                        useHTML: true,
                        color: '#FFFFFF',
                        format: '{point.formattedValue}',
                        y: 20
                    }
                }
            },
            series: chartData
    };
    }

    const config = makeChartFromSingleMetricBreakdown(data.data, props.metric);

    const afterRender = (chart) => {
        if (!data.data && !averageData[0].results.hasData && (props.entitySet.getInstances().getAll().length > 0)) {
            chart.showLoading();
        }
    };

    return (
        <React.Fragment>
            <AugmentedReactHighCharts config={config} afterRender={afterRender} googleTagManager={props.googleTagManager} />
            <ChartFooterInformation sampleSizeMeta={data.sampleSizeMetadata} activeBrand={props.activeBrand} metrics={[props.metric]} average={props.curatedFilters.average} />
        </React.Fragment>
    );
};

export default ProfileChart;


export class SingleBarChartDataPoint implements ICommonDataPoint {
    constructor(metric: Metric, result?: BrandVueApi.WeightedDailyResult, name?: string, brandColor?: string, averageDescriptor?: BrandVueApi.IAverageDescriptor) {
        if (result) {
            this.y = result.weightedResult;
            this.sampleSize = result.unweightedSampleSize;
            this.formattedValue = metric.longFmt(result.weightedResult);

            // Rule is that for bar charts, all % metric values should be 0dp and with out % sign
            if (metric.isPercentage()) {
                this.displayValue = metric.fmt(result.weightedResult).replace('%', '');
            } else {
                this.displayValue = this.formattedValue;
            }

            this.formatn = NumberFormattingHelper.format0Dp(result.unweightedSampleSize);
            if (averageDescriptor) {
                this.formattedDate = DateFormattingHelper.formatDateRange(result.date, averageDescriptor);
            }
        }
        this.name = name;
        this.color = brandColor;

    }

    borderColor?: string;
    y?: number;
    displayValue?: string;
    formattedValue?: string;
    sampleSize: number;
    formatn?: string;
    name?: string;
    color?: string;
    formattedDate?: string;
}
