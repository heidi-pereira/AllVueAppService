import { getWaveComparisonResults } from '../Charts/ChartData/WaveComparisonResultsDataHandler';
import { getCrossbreakCompetitionResults } from '../Charts/ChartData/CrossbreakCompetitionResultsDataHandler';
import { MainQuestionType, VariableConfigurationModel, BaseExpressionDefinition, CrossMeasure, ReportOrder } from '../../../../BrandVueApi';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import { Metric } from '../../../../metrics/metric';
import { PartWithExtraData } from '../ReportsPageDisplay';
import * as BrandVueApi from "../../../../BrandVueApi";
import { ITimeSelectionOptions } from "../../../../state/ITimeSelectionOptions";

export const getMultiFunnelResults = (reportPart: PartWithExtraData,
    curatedFilters: CuratedFilters,
    filterInstances: FilterInstance[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    metrics: Metric[],
    variables: VariableConfigurationModel[],
    order: ReportOrder,
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    baseExpressionOverride?: BaseExpressionDefinition,
    waves?: CrossMeasure,
    breaks?: CrossMeasure[]) => {
    if (waves) {
        return getWaveComparisonResults(
            reportPart,
            curatedFilters,
            waves,
            filterInstances,
            [],
            false,
            questionTypeLookup,
            metrics,
            variables,
            subsetId,
            timeSelection,
            undefined,
            undefined,
            baseExpressionOverride
        );
    }
    if (breaks) {
        return getCrossbreakCompetitionResults(
            reportPart,
            curatedFilters,
            breaks,
            order,
            undefined,
            filterInstances,
            [], // Average selector has been disabled for multi break charts
            questionTypeLookup,
            metrics,
            variables,
            subsetId,
            timeSelection,
            undefined,
            baseExpressionOverride,
            false,
            undefined
        );
    }
}
    
export const getLowSampleFromMultiFunnelResults = (results: BrandVueApi.WaveComparisonResults | BrandVueApi.GroupedCrossbreakCompetitionResults) => {
    if (results instanceof BrandVueApi.WaveComparisonResults) {
        return results.lowSampleSummary.length > 0;
    } else if (results instanceof BrandVueApi.GroupedCrossbreakCompetitionResults) {
        return results.groupedBreakResults.some(r => r.breakResults.lowSampleSummary.length > 0);
    };

    return false;
}

export const getMultiFunnelSampleSizeMetadata = (results: BrandVueApi.WaveComparisonResults | BrandVueApi.GroupedCrossbreakCompetitionResults) => {
    if (results instanceof BrandVueApi.GroupedCrossbreakCompetitionResults) {
        return results.groupedBreakResults[0].breakResults.sampleSizeMetadata;
    };

    return results.sampleSizeMetadata;
}