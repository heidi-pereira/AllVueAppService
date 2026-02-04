import { WeightedDailyResult, IAverageDescriptor } from "../../BrandVueApi";
import { ICommonDataPoint } from "./ICommonDataPoint";
import { Metric } from "../../metrics/metric";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";

export class OverTimeDataPoint implements ICommonDataPoint {
    constructor(p: WeightedDailyResult, metric: Metric, averageDescriptor: IAverageDescriptor) {
        this.x = p.date;
        this.formattedDate = DateFormattingHelper.formatDatePoint(p.date, averageDescriptor);
        this.y = p.weightedResult;
        this.formatx = metric.longFmt(p.weightedResult);
        this.formatn = NumberFormattingHelper.format0Dp(p.unweightedSampleSize);
        this.sampleSize = p.unweightedSampleSize;
    }

    public x: any;
    public y: number | undefined;
    public formattedDate: string;
    public formatx: string;
    public formatn: string;
    public sampleSize: number;
}