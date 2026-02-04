import Highcharts from "highcharts";

export interface ICommonDataPoint extends Highcharts.PointOptionsObject
{
    sampleSize: number;
}

export interface IColumnDataPoint extends ICommonDataPoint {
    instanceId: number;
}


