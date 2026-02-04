import {
    AverageMultiEntityChartModel,
    AverageType,
    BaseExpressionDefinition,
    CrossMeasure,
    CrosstabAverageResults,
    Factory,
    MultiEntityWaveResultsModelWithCrossbreaks,
    IEntityType,
    WaveComparisonResults,
    MainQuestionType,
    VariableConfigurationModel,
    SigConfidenceLevel
} from '../../../../../BrandVueApi';
import { CuratedFilters } from '../../../../../filter/CuratedFilters';
import { ViewHelper } from '../../../ViewHelper';
import { FilterInstance } from '../../../../../entity/FilterInstance';
import { NoDataError } from '../../../../../NoDataError';
import { PartWithExtraData } from "../../ReportsPageDisplay";
import { getVerifiedAverageType } from "../../../AverageHelper";
import { Metric } from '../../../../../metrics/metric';
import { ITimeSelectionOptions } from "../../../../../state/ITimeSelectionOptions";

export async function getWaveComparisonResults(reportPart: PartWithExtraData,
    curatedFilters: CuratedFilters,
    waves: CrossMeasure | undefined,
    filterInstances: FilterInstance[],
    averageTypes: AverageType[],
    includeSignificance: boolean,
    questionTypeLookup: { [key: string]: MainQuestionType; },
    metrics: Metric[],
    variables: VariableConfigurationModel[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,    
    breaks?: CrossMeasure,
    splitByType?: IEntityType,
    baseExpressionOverride?: BaseExpressionDefinition,
    sigConfidenceLevel?: SigConfidenceLevel): Promise<{ results: WaveComparisonResults, averageResults: CrosstabAverageResults[] }> {
    if (!waves) {
        throw new NoDataError();
    }

    let data: { results: WaveComparisonResults, averageResults: CrosstabAverageResults[] };
    if (reportPart.metric!.entityCombination.length >= 2) {
        if (!splitByType || filterInstances.length === 0) {
            throw new Error("Multi entity metrics must provide a split by type and a filter instance");
        }
    }

    data = await loadComparisonResults(reportPart,
        filterInstances,
        curatedFilters,
        waves,
        includeSignificance,
        averageTypes,
        questionTypeLookup,
        metrics,
        variables,
        subsetId,
        timeSelection,
        breaks,
        baseExpressionOverride,
        sigConfidenceLevel);

    return data;
}

async function loadComparisonResults(reportPart: PartWithExtraData,
    filterInstances: FilterInstance[],
    curatedFilters: CuratedFilters,
    waves: CrossMeasure,
    includeSignificance: boolean,
    averageTypes: AverageType[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    metrics: Metric[],
    variables: VariableConfigurationModel[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    breaks?: CrossMeasure,
    baseExpressionOverride?: BaseExpressionDefinition,
    sigConfidenceLevel?: SigConfidenceLevel): Promise<{ results: WaveComparisonResults, averageResults: CrosstabAverageResults[] }> {

    const requestModel = new MultiEntityWaveResultsModelWithCrossbreaks({
        multiEntityRequestModel: ViewHelper.createMultiEntityRequestModel({
            curatedFilters: curatedFilters,
            metric: reportPart.metric!,
            splitBySet: reportPart.selectedEntitySet!,
            filterInstances: filterInstances,
            continuousPeriod: false,
            baseExpressionOverride: baseExpressionOverride,
            includeSignificance: includeSignificance,
            subsetId: subsetId,
            sigConfidenceLevel: sigConfidenceLevel ?? SigConfidenceLevel.NinetyFive
        }, timeSelection),
        waves: waves,
        breaks: breaks,
    });

    const results = await Factory.DataClient(throwError => throwError())
        .getWaveComparisonResultsMultiEntity(requestModel);
    if (averageTypes.length == 0) {
        return { results: results, averageResults: [] };
    }

    const averages = await Promise.all(averageTypes.map(async a => {
        var verifiedAverageType = getVerifiedAverageType(a, reportPart.metric!, questionTypeLookup, true, metrics, variables);
        const averageMultiEntityChartModel = new AverageMultiEntityChartModel({
            averageType: verifiedAverageType,
            requestModel: requestModel.multiEntityRequestModel,
            breaks: waves,
        });

        return await Factory.DataClient(throwError => throwError())
            .getAverageForMultiEntityCharts(averageMultiEntityChartModel);
    }));
    return ({ results: results, averageResults: averages })
}