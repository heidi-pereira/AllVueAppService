import {
    AverageMultiEntityChartModel,
    AverageType,
    BaseExpressionDefinition,
    CompetitionResults,
    CrosstabAverageResults,
    Factory,
    ReportOrder,
    IEntityType,
    MainQuestionType,
    VariableConfigurationModel,
} from '../../../../../BrandVueApi';
import { CuratedFilters } from '../../../../../filter/CuratedFilters';
import { ViewHelper } from '../../../ViewHelper';
import { FilterInstance } from '../../../../../entity/FilterInstance';
import {PartWithExtraData} from "../../ReportsPageDisplay";
import { getVerifiedAverageType } from '../../../../../components/visualisations/AverageHelper';
import { Metric } from "client/metrics/metric";
import { ITimeSelectionOptions } from "client/state/ITimeSelectionOptions";

export async function getCompetitionResults(reportPart: PartWithExtraData,
    curatedFilters: CuratedFilters,
    reportOrder: ReportOrder,
    showTop: number | undefined,
    filterInstances: FilterInstance[],
    averageTypes: AverageType[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    metrics: Metric[],
    variables: VariableConfigurationModel[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    splitByType?: IEntityType,
    baseExpressionOverride?: BaseExpressionDefinition): Promise<{ results: CompetitionResults, averages: CrosstabAverageResults[] }>
{
    if (reportPart.metric!.entityCombination.length >= 2) {
        if (!splitByType || filterInstances.length === 0) {
            throw new Error("Multi entity metrics must provide a split by type and a filter instance");
        }
    }

    const data = await loadCompetitionResults(reportPart,
        filterInstances,
        curatedFilters,
        averageTypes,
        questionTypeLookup,
        metrics,
        variables,
        subsetId,
        timeSelection,
        baseExpressionOverride);
    sortData(data.results, reportOrder);
    showTopForData(data.results, showTop);
    return data;
}

async function loadCompetitionResults(reportPart: PartWithExtraData,
    filterInstances: FilterInstance[],
    curatedFilters: CuratedFilters,
    averageTypes: AverageType[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    metrics: Metric[],
    variables: VariableConfigurationModel[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    baseExpressionOverride?: BaseExpressionDefinition): Promise<{ results: CompetitionResults, averages: CrosstabAverageResults[] }>
{
    const requestModel = ViewHelper.createMultiEntityRequestModel({
        curatedFilters: curatedFilters,
        metric: reportPart.metric!,
        splitBySet: reportPart.selectedEntitySet!,
        filterInstances: filterInstances,
        continuousPeriod: false,
        baseExpressionOverride: baseExpressionOverride,
        subsetId: subsetId
    }, timeSelection);

    const results = await Factory.DataClient(throwError => throwError())
        .getCompetitionResults(requestModel);
    if(averageTypes.length == 0) {
        return {results: results, averages: []};
    }

    const averages = await Promise.all(averageTypes.map(async average => {
        const verifiedAverageType = getVerifiedAverageType(average,
            reportPart.metric!,
            questionTypeLookup,
            true,
            metrics,
            variables);
        const averageMultiEntityChartModel = new AverageMultiEntityChartModel ({
            averageType: verifiedAverageType,
            requestModel: requestModel
        });

        return await Factory.DataClient(throwError => throwError())
            .getAverageForMultiEntityCharts(averageMultiEntityChartModel);
        })
    );
    return {results: results, averages: averages};
}

function sortByResultOrder(results: CompetitionResults) {
    //sort results in descending order by sum of values across periods
    const sortedIndices = results.periodResults[0].resultsPerEntity.map((_, index) => {
        return {
            index: index,
            sum: results.periodResults.reduce((sum, current) => {
                return sum + current.resultsPerEntity[index].weightedDailyResults[0].weightedResult;
            }, 0),
        }
    }).sort((a, b) => b.sum - a.sum);

    results.periodResults.forEach(periodResult => {
        const data = periodResult.resultsPerEntity;
        periodResult.resultsPerEntity = sortedIndices.map(i => data[i.index]);
    });
}

function sortData(results: CompetitionResults, order: ReportOrder) {
    //result ordering
    switch(order){
        case ReportOrder.ResultOrderDesc:
        case ReportOrder.ResultOrderAsc:
            sortByResultOrder(results);
            break;
    }

    //ascending vs descending
    switch (order) {
        case ReportOrder.ResultOrderAsc:
        case ReportOrder.ScriptOrderAsc:
            results.periodResults.forEach(periodResult => periodResult.resultsPerEntity.reverse());
            break;
    }
}

function showTopForData(results: CompetitionResults, showTop: number | undefined) {
    if (showTop != null) {
        results.periodResults.forEach(periodResult => {
            periodResult.resultsPerEntity.splice(showTop);
        });
    }
}