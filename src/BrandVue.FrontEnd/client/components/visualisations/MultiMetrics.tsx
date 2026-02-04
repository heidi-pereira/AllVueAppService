import React from "react";
import * as BrandVueApi from "../../BrandVueApi";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { EntityInstance } from "../../entity/EntityInstance";
import { Metric } from "../../metrics/metric";
import { ViewHelper } from "./ViewHelper";
import { DataSubsetManager } from "../../DataSubsetManager";
import { ChartFooterInformation } from "./ChartFooterInformation";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import AugmentedReactHighCharts from "./AugmentedReactHighCharts";
import LowSample from "./BrandVueOnlyLowSampleHelper";
import { ICommonDataPoint } from "./ICommonDataPoint";
import CuratedResultsModel = BrandVueApi.CuratedResultsModel;
import AverageTotalRequestModel = BrandVueApi.AverageTotalRequestModel;
import {ComparisonPeriodSelection, MultiMetricAverageResults, MultiMetricResults} from "../../BrandVueApi";
import CompressClientData = BrandVueApi.Factory;
import { EntitySet } from "../../entity/EntitySet";
import { IGoogleTagManager } from "../../googleTagManager";
import { ProductConfigurationContext } from "../../ProductConfigurationContext";
import { AxisLabelsFormatterContextObject } from "highcharts";
import { getFormattedLabel } from "../../helpers/HighchartHelper";
import { useAppSelector } from '../../state/store';
import { selectSubsetId } from '../../state/subsetSlice';

import { selectTimeSelection } from "../../state/timeSelectionStateSelectors";

interface IMultiMetricsProps {
    googleTagManager: IGoogleTagManager;
    title: string;
    height: number;
    curatedFilters: CuratedFilters;
    metrics: Metric[];
    entitySet: EntitySet;
    availableEntitySets: EntitySet[];
    activeBrand: EntityInstance;
    maxNumberOfEntries: number;
    updateAverageRequests: (averageRequests: AverageTotalRequestModel[]) => void;
}

const MultiMetrics = (props: IMultiMetricsProps) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const getEmptyAverageResults = () => {
        return [{ name: 'Default', data: new BrandVueApi.MultiMetricAverageResults() }];
    }
    const [, handleError] = React.useState();
    const [results, setResults] = React.useState(new BrandVueApi.MultiMetricResults());
    const [averageResults, setAverageResults] = React.useState(getEmptyAverageResults());
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const processMultiMetricResponse = (result: MultiMetricResults): void => {
        setResults(result);


        setAverageResults(getEmptyAverageResults());

        if (props.metrics.every(metric => metric.isProfileMetric())) {
            return;
        }

        var orderMetrics: Metric[] = [];
        result.orderedMeasures.map(metric => orderMetrics.push(findMetric(metric)));
        
        var averageRequests = props.entitySet.getAverages().getAll().map(average => {

            const averageSet = average.getEntitySet(props.entitySet, props.availableEntitySets);

            const instancesForAverage = averageSet.getInstances();

            return {
                average: average,
                requestModel: ViewHelper.createAverageRequestModelOrNull(
                    instancesForAverage.getAll().map(b => b.id),
                    orderMetrics,
                    props.curatedFilters,
                    props.activeBrand.id,
                    {},
                    subsetId,
                    timeSelection
                )!
            };
        }).filter(x => x.requestModel != null);

        const promises = averageRequests.map(async x => {
            return await CompressClientData.DataClient(err => err()).getMultiMetricAverageResults(x.requestModel) 
                .then(r => {
                    return {
                        name: x.average.getEntitySet(props.entitySet, props.availableEntitySets).name,
                        data: r,
                        requestModel: x.requestModel
                    };
                });
        });
        Promise.all(promises).then((r) => {
            props.updateAverageRequests(r.filter(x => x.requestModel).map(x => new AverageTotalRequestModel({
                    averageName: x.name,
                    requestModel: x.requestModel
                }
            )));
            setAverageResults(r);
        });
    }

    const requestModelFromProps = (): CuratedResultsModel => {
        const enabledMetrics = DataSubsetManager.filterMetricByCurrentSubset(props.metrics);
        const brandIds = props.curatedFilters.comparisonPeriodSelection === ComparisonPeriodSelection.CurrentPeriodOnly ? props.entitySet.getInstances().getAll().map(b => b.id) : [props.activeBrand.id];

        return ViewHelper.createCuratedRequestModel(brandIds,
            enabledMetrics,
            props.curatedFilters,
            props.activeBrand.id,
            {},
            subsetId,
            timeSelection
        );
    }

    const load = async () => {
        await CompressClientData.DataClient(err => err()).getMultiMetricResults(requestModelFromProps()).then(processMultiMetricResponse)
            .catch((e: Error) => {
                handleError(() => { throw e });
            });
    }

    React.useEffect(() => {
        load();
    }, [
        JSON.stringify(props.entitySet),
        props.activeBrand,
        props.height,
        JSON.stringify(props.curatedFilters),
        timeSelection
    ]);

    const findMetric = (metricByName: string): Metric => {
        let result = props.metrics.find(m => m.name === metricByName);
        if (!result) {
            result = props.metrics[0];
        }
        return result!;
    }

    const getCategoryHierarchy = (values: string[]): any => {
        let categories: any = [];
        for (let i = 0; i < values.length; i++) {
            const parts = values[i].split(":");
            if (parts.length === 1) {
                categories.push(parts[0]);
            } else {
                if (categories.length === 0 || parts[0] !== categories[categories.length - 1].name) {
                    categories.push({ name: parts[0], categories: [] });
                }
                categories[categories.length - 1].categories.push(parts[1]);
            }
        }

        if (categories.length === 1) {
            // Flatten to straight array if only one level deep
            categories = categories[0].categories;
        }
        else {
            //Currently this causes issues so reset
            //This causes retail / ui / topline - summary / market - summary / competition to fail to load
            categories = values;
        }
        return categories;
    }

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

    const formatter = props.metrics[0].longFmt;
    const startPositionToRemoveData = props.maxNumberOfEntries;
    const orderedMeasures = results.orderedMeasures.slice();
    const numberOfElementsToRemove = orderedMeasures.length - startPositionToRemoveData;
    const trimData = (props.maxNumberOfEntries > 0) && (numberOfElementsToRemove > 0);

    if (trimData) {
        orderedMeasures.splice(startPositionToRemoveData, numberOfElementsToRemove);
    }
    const categories = getCategoryHierarchy(orderedMeasures);
    const series: any = [];

    const seriesBrand = results.activeSeries.entityInstance;
    const areResultsBrandSpecific = seriesBrand && seriesBrand.name != null; // Market-level results will not be brand specific

    if (results.activeSeries.orderedData.length) {

        const brandColor = areResultsBrandSpecific ? props.entitySet.getInstanceColor(props.activeBrand): undefined;

        let periodCount = results.activeSeries.orderedData[0].length;

        if (!areResultsBrandSpecific) {
            const maxNumberOfPeriodsForNoBrands = 7;
            periodCount = Math.min(maxNumberOfPeriodsForNoBrands, periodCount);
        }

        let periodStartIndex = 0;
        if (props.curatedFilters.comparisonPeriodSelection === ComparisonPeriodSelection.CurrentPeriodOnly) {
            periodStartIndex = periodCount - 1;
        }

        if (averageResults.length > 0 && averageResults[0].data.average.length) {
            averageResults.map(averageResult => {
                var seriesTitle = "Competitive average";
                if (props.curatedFilters.comparisonPeriodSelection !==
                    ComparisonPeriodSelection.CurrentPeriodOnly) {
                    const averageDateFormatted = DateFormattingHelper.formatDateRange(
                        averageResult.data.average[0].weightedDailyResult.date,
                        props.curatedFilters.average);
                    seriesTitle += "<br/>(" + averageDateFormatted + ")";
                }
                series.push({
                    name: seriesTitle,
                    isAverage: true,
                    data: averageResult.data.average.map(d => new SingleBarChartDataPoint(seriesTitle,
                        findMetric(d.metricName),
                        d.weightedDailyResult,
                        findMetric(d.metricName).name)),
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
                data: s.orderedData.map((d, i) => new SingleBarChartDataPoint(s.entityInstance.name, props.metrics[0],
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
        let colors = ["#7CB5EC", "#434348"];
        for (let periodIndex = periodStartIndex; periodIndex < periodCount; periodIndex++) {
            let seriesName;
            const seriesByDate = results.activeSeries.orderedData[0] != null
                ? DateFormattingHelper.formatDateRange(results.activeSeries.orderedData[0][periodIndex].date,
                    props.curatedFilters.average)
                : "";
            if (areResultsBrandSpecific) {
                 seriesName = seriesBrand.name;
            } else {
                // Use date of "brandless" period result for series name
                seriesName = seriesByDate;
            }
            //No worries if colors is indexed outof range as this gives us undefined. in which case Highcharts just nominates a color
            let color = areResultsBrandSpecific ? (periodIndex === periodCount - 1 ? brandColor : undefined) : colors[periodCount - periodIndex - 1];
            const period = results.activeSeries.orderedData[0][periodIndex].date;
            //Set fullName including date so toggling visibility via the legend triggers only one line
            let fullName = seriesName;
            if (seriesName === props.activeBrand.name) {
                const formattedDate = " (" +
                    DateFormattingHelper.formatDateRange(period, props.curatedFilters.average) +
                    ")";
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
                            props.metrics[dataIndex].isProfileMetric() ? seriesByDate : seriesName,
                            props.metrics[dataIndex],
                            dataItem[periodIndex],
                            orderedMeasures[dataIndex],
                            color)),
                    type: areResultsBrandSpecific ? 'line' : 'column',
                    color: color,
                    canToggle: true
                }
            );
        }

        if (trimData) {
            series.map(s => s.data.splice(startPositionToRemoveData, numberOfElementsToRemove));
        }

    }
    if (areResultsBrandSpecific) { //Low sample not implemented for market metrics yet: https://app.clubhouse.io/mig-global/story/21883/low-sample-indicator-for-market-metrics
        LowSample.addLowSampleIndicators(series);
    }
    insertSpacers(categories, series);

    const yaxisTitle = props.metrics[0].graphAxisTitle;
    const highlightBrand = props.activeBrand.name;

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
            categories: categories,
            labels: {
                formatter: (e: any): string => getFormattedLabel(e.axis, e.value.toString(), "normal")
            }
        },
        yAxis: {
            ceiling: props.metrics.every(m => m.isPercentage()) ? 1 : undefined,
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
            labelFormatter: function (this: { name: any, options: any}) {
                if (this.name === highlightBrand && !this.options.isAverage) {
                    const formattedDate = " (" +
                        DateFormattingHelper.formatDateRange(this.options.period,
                            props.curatedFilters.average) +
                        ")";

                    return "<b>" + this.name + formattedDate + "</b>";
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

    return (
        <React.Fragment>
            <AugmentedReactHighCharts config={config} afterRender={afterRender} googleTagManager={props.googleTagManager} />
            <ChartFooterInformation sampleSizeMeta={results.sampleSizeMetadata} activeBrand={props.activeBrand} metrics={props.metrics} average={props.curatedFilters.average} />
        </React.Fragment>
    );
}

export default MultiMetrics;

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
