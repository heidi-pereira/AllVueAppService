import { Slate } from "../helpers/ChromaHelper";
import { ICommonDataPoint } from "./ICommonDataPoint";

export const getLowSampleThreshold = () => {
    return BrandVueOnlyLowSampleHelper.lowSampleForEntity;
}

export default class BrandVueOnlyLowSampleHelper {
    static lowSampleForEntity: number;
    static noSampleForEntity: number;

    public static initialiseThresholds(lowSampleForEntity: number, noSampleForEntity: number) {
        BrandVueOnlyLowSampleHelper.lowSampleForEntity = lowSampleForEntity;
        BrandVueOnlyLowSampleHelper.noSampleForEntity = noSampleForEntity;
    }

    public static addLowSampleIndicators(allSeries: any[]) {
        for (let series of allSeries) {

            if (!series.data) {
                continue;
            }

            switch (series.type) {
                case "line":
                case undefined:
                    this.addLowSampleForLine(series);
                    break;
                case "column":
                case "columnrange":
                case "bar":
                case "pie":
                    this.addLowSampleForColumn(series);
                    break;
                case "scatter":
                    this.addLowSampleForScatter(series);
                    break;
            }
        }
    }

    private static addLowSampleForScatter(series: any) {
        for (var point of series.data!) {
            var pointWithSample = point as ICommonDataPoint;

            if (!pointWithSample) {
                continue;
            }
            
            if (pointWithSample.sampleSize <= this.lowSampleForEntity) {
                pointWithSample.marker!.lineColor = series.color;
                pointWithSample.marker!.lineWidth = 1;
                pointWithSample.marker!.fillColor = '#FFF';
            }
        }
    }

    private static addLowSampleForColumn(series: any) {
        for (var point of series.data!) {
            var pointWithSample = point as ICommonDataPoint;

            if (!pointWithSample) {
                continue;
            }

            if (pointWithSample.sampleSize <= this.lowSampleForEntity) {
                // Highcharts types don't currently seem to have this one... so we can uncloak the demon and briefly revel in its beauty.
                (pointWithSample as any).borderColor = pointWithSample.color || series.color;
                // Finished. That wasn't so bad was it?
                pointWithSample.color = '#FFF';

                if (pointWithSample.dataLabels) {
                    const dataLabelOptions = pointWithSample.dataLabels as Highcharts.DataLabelsOptions;
                    dataLabelOptions.color = Slate;
                }
            }
        }
    }

    private static addLowSampleForLine(series: any) {
        const zones: Highcharts.SeriesZonesOptionsObject[] = [];

        let inLowSample: boolean = false;

        let previousPoint: (Highcharts.PointOptionsObject & ICommonDataPoint) | undefined = undefined;

        for(let i = 0; i< series.data!.length; i++) {

            const pointWithSample = series.data![i] as (Highcharts.PointOptionsObject & ICommonDataPoint);

            if (!pointWithSample) {
                continue;
            }

            // Start a "low sample" zone if it's at or below the low sample threshold
            if (pointWithSample.sampleSize <= this.lowSampleForEntity) {
                pointWithSample.marker = {
                    fillColor: '#FFFFFF',
                    lineWidth: 1,
                    lineColor: (series.color as string) || '#84B5EE'
                }
                if (!inLowSample) {
                    inLowSample = true;
                    zones.push({ value: pointWithSample.x ? (previousPoint ? previousPoint.x : pointWithSample.x) : Math.max(i - 1, 0) });
                }
            } else {
                if (inLowSample) {
                    inLowSample = false;
                    zones.push({ value: pointWithSample.x ? pointWithSample.x : i, dashStyle: 'Dot' });
                }

            }

            // Do not plot the point at all if it's at or below the no sample threshold
            if (pointWithSample.sampleSize <= this.noSampleForEntity) {
                pointWithSample.y = ((undefined) as any);
            }

            previousPoint = pointWithSample;
        }

        // Low sample zone to end of chart if already in one
        if (inLowSample) {
            zones.push({
                dashStyle: 'Dot'
            });
        }

        if (zones.length) {
            const seriesChart = series;
            seriesChart.zoneAxis = 'x';
            seriesChart.zones = zones;
        }
    }
}