import { getVerifiedAverageType } from '../../../../../components/visualisations/AverageHelper';
import { AverageType, BaseExpressionDefinition, Factory, OverTimeAverageResults, AverageStackedMultiEntityChartsModel, IEntityType,
    StackedMultiEntityResults, 
    MainQuestionType,
    SelectedEntityInstances,
    VariableConfigurationModel,
    ReportOrder} from '../../../../../BrandVueApi';
import { IEntityConfiguration } from '../../../../../entity/EntityConfiguration';
import { CuratedFilters } from '../../../../../filter/CuratedFilters';
import { Metric } from '../../../../../metrics/metric';
import { ViewHelper } from '../../../ViewHelper';
import { ITimeSelectionOptions } from "../../../../../state/ITimeSelectionOptions";

export async function getStackedMultiEntityResults(metric: Metric,
    entityConfiguration: IEntityConfiguration,
    curatedFilters: CuratedFilters,
    reportOrder: ReportOrder,
    filterByEntityType: IEntityType,
    splitByEntityType: IEntityType,
    averageTypes: AverageType[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    metrics: Metric[],
    variables: VariableConfigurationModel[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    baseExpressionOverride?: BaseExpressionDefinition,
    splitByInstancesOverride?: SelectedEntityInstances|undefined): Promise<{results: StackedMultiEntityResults, averages: OverTimeAverageResults[][]}>
{
    if (metric.entityCombination.length > 2) {
        throw new Error("Cannot show stacked results for metrics with more than two entities");
    } else if (metric.entityCombination.length == 2) {
        const data = await loadForMultipleEntity(metric,
            entityConfiguration,
            curatedFilters,
            filterByEntityType,
            splitByEntityType,
            averageTypes,
            questionTypeLookup,
            metrics,
            variables,
            subsetId,
            timeSelection,
            baseExpressionOverride,
            splitByInstancesOverride
        );
        sortData(data.results, reportOrder);
        return data;
    } else {
        throw new Error("Non multi-entity metrics should use ReportsPageCardChart");
    }
}

async function loadForMultipleEntity(metric: Metric,
    entityConfiguration: IEntityConfiguration,
    curatedFilters: CuratedFilters,
    filterByEntityType: IEntityType,
    splitByEntityType: IEntityType,
    averageTypes: AverageType[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    metrics: Metric[],
    variables: VariableConfigurationModel[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    baseExpressionOverride?: BaseExpressionDefinition,
    splitByInstancesOverride?: SelectedEntityInstances): Promise<{results: StackedMultiEntityResults, averages: OverTimeAverageResults[][]}>
{
    const requestModel = ViewHelper.createStackedMultiEntityRequestModel(
        curatedFilters,
        metric,
        splitByInstancesOverride
            ? entityConfiguration.getSelectedInstancesOrderedAsSet(splitByEntityType, splitByInstancesOverride) 
            : entityConfiguration.getAllEnabledInstancesOrderedAsSet(splitByEntityType),
        entityConfiguration.getAllEnabledInstancesOrderedAsSet(filterByEntityType),
        false,
        subsetId,
        timeSelection,
        baseExpressionOverride
    );

    const results = await Factory.DataClient(throwError => throwError())
        .getStackedResultsForMultipleEntities(requestModel);

    if(averageTypes.length == 0){
        return {results: results, averages: []}
    }

    const averages = await Promise.all(averageTypes.map(async a => {
        let verifiedAverage = a;
        if(a == AverageType.Mean) {
            verifiedAverage = getVerifiedAverageType(a, metric, questionTypeLookup, true, metrics, variables)
        }
        const averageStackedMultiEntityChartsModel = new AverageStackedMultiEntityChartsModel({
            averageType: verifiedAverage,
            stackedMultiEntityRequestModel: requestModel,
        });
    
        return await Factory.DataClient(throwError => throwError())
            .getAverageForStackedMultiEntityCharts(averageStackedMultiEntityChartsModel);
        })
    );

    return {results: results, averages: averages}
}

function sortByResultOrder(results: StackedMultiEntityResults) {
    const sortedIndices = results.resultsPerInstance.map((instance, index) => {
        return {
            index: index,
            sum: instance.data.reduce((sum, current) => {
                return sum + current.weightedDailyResults[0].weightedResult
            }, 0),
        }
    }).sort((a, b) => b.sum - a.sum);

    const originalData = results.resultsPerInstance;
    results.resultsPerInstance = sortedIndices.map(i => originalData[i.index]);
}

function sortData(results: StackedMultiEntityResults, order: ReportOrder) {
    switch (order) {
        case ReportOrder.ResultOrderDesc:
        case ReportOrder.ResultOrderAsc:
            sortByResultOrder(results);
            break;
    }

    switch (order) {
        case ReportOrder.ResultOrderAsc:
        case ReportOrder.ScriptOrderAsc:
            results.resultsPerInstance.reverse();
            break;
    }
}