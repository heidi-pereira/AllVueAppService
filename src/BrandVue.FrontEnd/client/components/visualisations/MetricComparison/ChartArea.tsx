import React from "react";
import { ViewHelper } from "../ViewHelper";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import { XAxisFormatting } from "../../../helpers/XAxisFormatting";
import { OverTimeDataPoint } from "../../visualisations/OverTimeDataPoint";
import LowSample from "../BrandVueOnlyLowSampleHelper";
import AugmentedReactHighCharts from "../AugmentedReactHighCharts";
import moment from "moment";
import * as BrandVueApi from "../../../BrandVueApi";
import { Metric } from "../../../metrics/metric";
import { IGoogleTagManager } from "../../../googleTagManager";

export interface IChartingData {
    overtimeResults: BrandVueApi.MultiMetricResults | null;
    metric: Metric;
}

interface IChartAreaProps {
    googleTagManager: IGoogleTagManager;
    chartingData: IChartingData[];
    height: number;
    curatedFilters: CuratedFilters;
}

const chartArea: React.FunctionComponent<IChartAreaProps> = (props) => {
    const chartingDataWithValidResults = props.chartingData.filter(cd => cd.overtimeResults);
    if (chartingDataWithValidResults.length === 0) {
        return null;
    }

    const highchartsConfigParts = chartingDataWithValidResults.map(cd => {
        const brandSeriesData = cd.overtimeResults!.activeSeries.orderedData[0].map(w => {
            w.date = new Date(w.date); // For some reason these are not dates in the WeightedDailyResults for multimetrics
            return new OverTimeDataPoint(w, cd.metric, props.curatedFilters.average);
        });

        const brandSeries = [
            {
                id: `${cd.overtimeResults!.activeSeries.entityInstance.name}-${cd.metric.name}`,
                name: cd.overtimeResults!.activeSeries.entityInstance.name ?? '',
                data: brandSeriesData,
                color: cd.overtimeResults!.activeSeries.entityInstance.color
            }
        ];

        return {
            brandSeries: brandSeries,
            metric: cd.metric
        };
    });

    const { tickPositioner, labelXformatter } = XAxisFormatting.formatAll(props.curatedFilters.average.makeUpTo);

    const sortFunction = (part1, part2): number => {
        if (part1.metric.numFormat > part2.metric.numFormat) return 1;
        if (part2.metric.numFormat > part1.metric.numFormat) return -1;
        return 0;
    }

    highchartsConfigParts.sort(sortFunction);
    const yAxisConfig = [] as any[];
    let seriesMin = Number.MAX_SAFE_INTEGER;
    let seriesMax = Number.MIN_SAFE_INTEGER;
    let metricNames = [] as string[];
    let metricNumFormatCount = 0;

    for (let i = 0; i < highchartsConfigParts.length; i++) {
        const thisBrandSeries = highchartsConfigParts[i].brandSeries;
        const thisMetric = highchartsConfigParts[i].metric;

        highchartsConfigParts[i].brandSeries = thisBrandSeries.map(bs => {
            const brandSeriesValues: number[] = bs.data.map(otr => otr.y).filter(Boolean) as number[];
            const currentBrandSeriesMin = Math.min(...brandSeriesValues);
            const currentBrandSeriesMax = Math.max(...brandSeriesValues);

            if (currentBrandSeriesMin < seriesMin) {
                seriesMin = currentBrandSeriesMin;
            }
            if (currentBrandSeriesMax > seriesMax) {
                seriesMax = currentBrandSeriesMax;
            }

            return { ...bs, yAxis: metricNumFormatCount, name: `${bs.name}<br>(${thisMetric.name})` };
        });

        if (metricNames.indexOf(thisMetric.name) === -1) {
            metricNames.push(thisMetric.name);
        }

        if (i >= highchartsConfigParts.length - 1 || thisMetric.numFormat !== highchartsConfigParts[i + 1].metric.numFormat) {
            const formatter = thisMetric.getNumberFormatForAxis(seriesMax);
            const titleText = metricNames.join(' | ');
            let singleYAxisConfig = {
                title: {
                    text: titleText,
                },
                labels: {
                    formatter: function (this: { value: any }) {
                        return formatter(this.value);
                    }
                },
                opposite: true,
                min: seriesMin,
                max: seriesMax
            };

            if (yAxisConfig.length % 2 === 0) {
                singleYAxisConfig.opposite = false;
            }

            yAxisConfig.push(singleYAxisConfig);
            seriesMin = 0;
            seriesMax = 0;
            metricNames = [] as string[];
            metricNumFormatCount++;
        }
    }

    const chartSeries: any = highchartsConfigParts.map(hcp => hcp.brandSeries)
        .reduce((flatSeries, parts) => flatSeries.concat(parts));

    LowSample.addLowSampleIndicators(chartSeries);
    const config = {
        chart: {
            height: props.height
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
                    return dateToFormat.format(labelXformatter).replace("$$HY$$", dateToFormat.month() < 6 ? "1st" : "2nd");;
                }
            }
        },
        yAxis: yAxisConfig,
        legend: {
            enabled: true,
            align: 'center',
            verticalAlign: 'bottom',
            layout: 'horizontal',
            itemMarginTop: 2,
            itemMarginBottom: 2
        },
        plotOptions: {
            line: {
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
        series: chartSeries
    };

    return <AugmentedReactHighCharts config={config} googleTagManager={props.googleTagManager} />;
};

export default chartArea;
