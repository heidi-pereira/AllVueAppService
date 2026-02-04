import { WeightedDailyResult } from "../../../BrandVueApi";

export function calculateIndexScore(result: WeightedDailyResult, total: WeightedDailyResult): number {
    if (result.weightedResult === 0 || total.weightedResult === 0) {
        return 0;
    }
    return Math.round((result.weightedResult / total.weightedResult) * 100);
}
