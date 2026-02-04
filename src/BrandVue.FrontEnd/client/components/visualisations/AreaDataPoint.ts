import { OverTimeDataPoint } from "./OverTimeDataPoint";
import { WeightedDailyResult, IAverageDescriptor } from "../../BrandVueApi";
import { Metric } from "../../metrics/metric";

export class AreaDataPoint extends OverTimeDataPoint {
    constructor(low: WeightedDailyResult, high: WeightedDailyResult, metric: Metric, averageDescriptor: IAverageDescriptor) {
        super(high, metric, averageDescriptor);

        this.low = low.weightedResult;
        this.high = high.weightedResult;
    }

    public low: number;
    public high: number;
}