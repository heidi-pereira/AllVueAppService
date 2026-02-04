import {
    BaseExpressionDefinition,
    Factory,
    EntityType,
    OverTimeResults,
    AverageType,
    MainQuestionType,
    VariableConfigurationModel,
    AverageMultiEntityChartModel,
    OverTimeAverageResults,
    IEntityType,
} from '../../../../../BrandVueApi';
import { CuratedFilters } from '../../../../../filter/CuratedFilters';
import { ViewHelper } from '../../../ViewHelper';
import { FilterInstance } from '../../../../../entity/FilterInstance';
import {PartWithExtraData} from "../../ReportsPageDisplay";
import { getVerifiedAverageType } from '../../../AverageHelper';
import { Metric } from '../../../../../metrics/metric';
import { ITimeSelectionOptions } from "../../../../../state/ITimeSelectionOptions";

export async function getOvertimeResults(
    reportPart: PartWithExtraData,
    curatedFilters: CuratedFilters,
    averageTypes: AverageType[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    metrics: Metric[],
    variables: VariableConfigurationModel[],
    filterInstances: FilterInstance[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    splitByType?: IEntityType,
    baseExpressionOverride?: BaseExpressionDefinition,
    continuousPeriod?: boolean): Promise<{ results: OverTimeResults, averageResults: OverTimeAverageResults[] }>
{
    if (reportPart.metric!.entityCombination.length >= 2) {
        if (!splitByType || filterInstances.length === 0) {
            throw new Error("Multi entity metrics must provide a split by type and a filter instance");
        }
    }

    return await loadOvertimeResults(reportPart,
        filterInstances,
        curatedFilters,
        averageTypes,
        questionTypeLookup,
        metrics,
        variables,
        subsetId,
        timeSelection,
        baseExpressionOverride,
        continuousPeriod);
}

async function loadOvertimeResults(reportPart: PartWithExtraData,
    filterInstances: FilterInstance[],
    curatedFilters: CuratedFilters,
    averageTypes: AverageType[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    metrics: Metric[],
    variables: VariableConfigurationModel[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    baseExpressionOverride?: BaseExpressionDefinition,
    continuousPeriod?: boolean): Promise<{ results: OverTimeResults, averageResults: OverTimeAverageResults[] }>
{

    const requestModel = ViewHelper.createMultiEntityRequestModel({
        curatedFilters: curatedFilters,
        metric: reportPart.metric!,
        splitBySet: reportPart.selectedEntitySet!,
        filterInstances: filterInstances,
        continuousPeriod: continuousPeriod ?? false,
        baseExpressionOverride: baseExpressionOverride,
        subsetId: subsetId
    }, timeSelection);

    const results = await Factory.DataClient(throwError => throwError())
        .getOverTimeResultsForMultipleEntities(requestModel);

    if (results.entityWeightedDailyResults.every(r => r.entityInstance != null)) {
        //this does an alphabetical sort on the backend, so we put it back into entity instance ID order for AllVue
        results.entityWeightedDailyResults.sort((a, b) => a.entityInstance.id - b.entityInstance.id);
    }

    const averages = await Promise.all(averageTypes.map(async a => {
        var verifiedAverageType = getVerifiedAverageType(a, reportPart.metric!, questionTypeLookup, true, metrics, variables);
        const averageMultiEntityChartModel = new AverageMultiEntityChartModel({
            averageType: verifiedAverageType,
            requestModel: requestModel
        });

        return await Factory.DataClient(throwError => throwError())
            .getOverTimeAverageResults(averageMultiEntityChartModel);
    }));

    return ({ results: results, averageResults: averages });
}