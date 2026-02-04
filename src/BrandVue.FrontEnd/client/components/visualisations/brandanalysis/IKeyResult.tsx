import {CompetitionResults, CrossbreakCompetitionResults, WeightedDailyResult} from "../../../BrandVueApi";

export interface IKeyResult {
    entity: string,
    result: WeightedDailyResult,
}

export function keyResultFromCompetitionResult(results: CompetitionResults | null): IKeyResult[] | null {
    if (!results) {
        return results;
    }
    return results.periodResults[results.periodResults.length - 1].resultsPerEntity.map(r => ({ entity: r.entityInstance.name, result: r.weightedDailyResults[0] }));
}

export function keyResultFromCrossbreakResult(results: CrossbreakCompetitionResults | null): IKeyResult[] | null {
    if (!results) {
        return results;
    }
    return results?.instanceResults.map(r=>({ entity: r.breakName, result: r.entityResults[0].weightedDailyResults[0] }));
}
