import { Metric } from "../../metrics/metric";
import React from "react";
import * as BrandVueApi from "../../BrandVueApi";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { EntityInstance } from "../../entity/EntityInstance";
import { ViewHelper } from "./ViewHelper";
import moment from "moment";
import { ChartFooterInformation } from "./ChartFooterInformation";
import AugmentedReactHighCharts from "./AugmentedReactHighCharts";
import LowSample from "./BrandVueOnlyLowSampleHelper";
import { OverTimeDataPoint } from "./OverTimeDataPoint";
import { XAxisFormatting } from "../../helpers/XAxisFormatting";
import AverageTotalRequestModel = BrandVueApi.AverageTotalRequestModel;
import { IEntityInstanceGroup } from "../../entity/IEntityInstanceGroup";
import { getMetricResultsSummaryFromBreakdownByAgeResults, MetricResultsSummary } from "../helpers/MetricInsightsHelper";
import { IGoogleTagManager } from "../../googleTagManager";
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from "../../state/subsetSlice";
import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

export interface IProfileChartOverTimeProps {
    googleTagManager: IGoogleTagManager,
    activeBrand: EntityInstance,
    keyBrands: IEntityInstanceGroup,
    height: number, metric: Metric,
    curatedFilters: CuratedFilters,
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void,
    updateAverageRequests: (averageRequests: AverageTotalRequestModel[]) => void;
}

const ProfileChartOverTime = (props: IProfileChartOverTimeProps) => {

    const [data, setData] = React.useState(new BrandVueApi.BreakdownByAgeResults());
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const hasEndDate = props.curatedFilters.endDate ? true : false;

    React.useEffect(() => {
        if (hasEndDate) {
            const curatedResultsRequestModel = ViewHelper.createCuratedRequestModel(props.keyBrands.getAll().map(b => b.id),
                [props.metric],
                props.curatedFilters,
                props.activeBrand.id,
                { continuousPeriod: true },
                subsetId,
                timeSelection);
            BrandVueApi.Factory.DataClient(throwErr => throwErr()).breakdownByAge(curatedResultsRequestModel)
                .then(r => setData(r));
        }
        },
        [
            props.keyBrands,
            props.activeBrand,
            JSON.stringify(props.curatedFilters),
            timeSelection
        ]);

    const makeLineChartFromDataset = (brandBrokenDownResults: BrandVueApi.BreakdownByAgeResults,
        metric: Metric,
        averageDescriptor: BrandVueApi.IAverageDescriptor) => {

        const yaxisTitle = metric.yAxisTitle();

        let chartData: any;
        if (brandBrokenDownResults) {
            chartData = brandBrokenDownResults.byAgeGroup
                .map(groupResults => ({
                    name: groupResults.category,
                    data: groupResults.weightedDailyResults.map(
                        result => new OverTimeDataPoint(result, metric, averageDescriptor))

                }));
            var averageName = "Total";
            chartData.push({
                name: averageName,
                color: '#000',
                dashStyle: 'shortdash',
                data: brandBrokenDownResults.total.map(
                    result => new OverTimeDataPoint(result, metric, averageDescriptor))
            });

            props.updateAverageRequests([]);
            var formatter = metric.getNumberFormatForAxis(ViewHelper.calcMaxMinusMinValueBySeries(chartData));
        }

        let { tickPositioner, labelXformatter } = XAxisFormatting.formatAll(props.curatedFilters.average.makeUpTo);

        const options = {
            chart: {
                height: props.height,
                type: 'line',
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
                tickPositioner: tickPositioner,
                labels: {
                    formatter: function (this: { value: any }) {
                        const dateToFormat = moment.utc(this.value);
                        return dateToFormat.format(labelXformatter)
                            .replace("$$HY$$", dateToFormat.month() < 6 ? "1st" : "2nd");
                    }
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
                },
            },
            legend: {
                enabled: true,
                verticalAlign: 'bottom',
                itemMarginTop: 5,
                itemMarginBottom: 5,
                itemStyle: {
                    fontWeight: "normal",
                },
            },
            plotOptions: {
                line: {
                    lineWidth: 1.5,
                    animation: false,
                    marker: {
                        radius: 3,
                        symbol: 'circle',
                        enabledThreshold: ViewHelper.enabledThreshold,
                    },
                    states: {
                        hover: {
                            lineWidth: 3
                        }
                    }
                }
            },

            series: chartData
        };

        return options;
    }


    const config = makeLineChartFromDataset(data, props.metric, props.curatedFilters.average);

    LowSample.addLowSampleIndicators(config.series);

    const afterRender = (chart) => {
        if (!data.hasData && props.keyBrands.getAll().length > 0) {
            chart.showLoading();
        }
    };

    props.updateMetricResultsSummary(getMetricResultsSummaryFromBreakdownByAgeResults(data));

    return (
        <React.Fragment>
            <AugmentedReactHighCharts config={config} afterRender={afterRender} googleTagManager={props.googleTagManager} />
            <ChartFooterInformation sampleSizeMeta={data.sampleSizeMetadata} activeBrand={props.activeBrand} metrics={[props.metric]} average={props.curatedFilters.average} />
        </React.Fragment>
    );
}

export default ProfileChartOverTime;
