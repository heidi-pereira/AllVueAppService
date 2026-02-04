import { BreakdownByAgeResults, BreakdownResults, CompetitionResults, EntityInstance, EntityWeightedDailyResults, SampleSizeMetadata, WeightedDailyResult,
    RankingTableResult
} from "../../BrandVueApi";
import { getSimpleSampleSizeDescription } from "./SampleSizeHelper";
import { ViewTypeEnum } from "./ViewTypeHelper";

export type EntityInstanceResult = {
    entityInstance: EntityInstance,
    weightedDailyResults: WeightedDailyResult[],
}

export type MetricResultsSummary = {
    results: EntityInstanceResult[],
    sampleSizeDescription: string,
}

export const ViewTypesValidForMetricInsights: ViewTypeEnum[] = [
    ViewTypeEnum.Competition,
    ViewTypeEnum.Ranking
];

const metricResultsSummary = (entityInstanceResults: EntityInstanceResult[], sampleSizeMetadata: SampleSizeMetadata) => {
    return {
        results: entityInstanceResults,
        sampleSizeDescription: sampleSizeMetadata ? getSimpleSampleSizeDescription(sampleSizeMetadata) : ""
    };
}

export const getMetricResultsSummaryFromCompetitionResults = (results: CompetitionResults): MetricResultsSummary => {
    const entityInstanceResults = results.periodResults.flatMap<EntityInstanceResult>(r =>
        r.resultsPerEntity.map(rpe => {
            return {
                entityInstance: rpe.entityInstance,
                weightedDailyResults: rpe.weightedDailyResults
            }
        })
    );

    return metricResultsSummary(entityInstanceResults, results.sampleSizeMetadata);
}

export const getMetricResultsSummaryFromEntityWeightedDailyResults = (results: EntityWeightedDailyResults[], sampleSizeMetadata: SampleSizeMetadata): MetricResultsSummary => {
    const entityInstanceResults = results.map(r => {
        return {
            entityInstance: r.entityInstance,
            weightedDailyResults: r.weightedDailyResults
        }
    });

    return metricResultsSummary(entityInstanceResults, sampleSizeMetadata);
}

export const getMetricResultsSummaryFromRankingTableResult = (results: RankingTableResult[], sampleSizeMetadata: SampleSizeMetadata): MetricResultsSummary => {
    const entityInstanceResults = results.map(r => {
        return {
            entityInstance: r.entityInstance,
            weightedDailyResults: [r.currentWeightedDailyResult]
        }
    });

    return metricResultsSummary(entityInstanceResults, sampleSizeMetadata);
}

export const getMetricResultsSummaryFromBreakdownResults = (results: BreakdownResults): MetricResultsSummary => {
    const entityInstanceResults = results.data.map(bdr => {
        return {
            entityInstance: bdr.entityInstance,
            weightedDailyResults: bdr.total
        }
    });

    return metricResultsSummary(entityInstanceResults, results.sampleSizeMetadata);
}

export const getMetricResultsSummaryFromBreakdownByAgeResults = (results: BreakdownByAgeResults): MetricResultsSummary => {
    const entityInstanceResults = [{
        entityInstance: results.entityInstance,
        weightedDailyResults: results.total
    }];

    return metricResultsSummary(entityInstanceResults, results.sampleSizeMetadata);
}