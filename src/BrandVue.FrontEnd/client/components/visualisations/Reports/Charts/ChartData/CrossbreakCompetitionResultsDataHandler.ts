import {
    AverageMultiEntityChartModel,
    AverageType,
    BaseExpressionDefinition,
    CrossbreakCompetitionResults,
    CrossMeasure,
    CrosstabAverageResults,
    Factory,
    GroupedCrossbreakCompetitionResults,
    MultiEntityRequestModelWithCrossbreaks,
    ReportOrder,
    IEntityType,
    MainQuestionType,
    VariableConfigurationModel,
    SigConfidenceLevel
} from '../../../../../BrandVueApi';
import { CuratedFilters } from '../../../../../filter/CuratedFilters';
import { ViewHelper } from '../../../ViewHelper';
import { FilterInstance } from '../../../../../entity/FilterInstance';
import { getEntityInstanceIdsFromEntitySet } from '../../../../helpers/SurveyVueUtils';
import { PartWithExtraData } from "../../ReportsPageDisplay";
import { getVerifiedAverageType } from '../../../../../components/visualisations/AverageHelper';
import { Metric } from '../../../../../metrics/metric';
import { ITimeSelectionOptions } from "../../../../../state/ITimeSelectionOptions";

export enum SortType {
    EntityInstances,
    Breaks
}

function sortByResultOrder(results: CrossbreakCompetitionResults) {
    const sortedIndices = results.instanceResults[0].entityResults.map((_, index) => {
        return {
            index: index,
            sum: results.instanceResults.reduce((sum, current) => {
                return sum + current.entityResults[index].weightedDailyResults[0].weightedResult;
            }, 0),
        }
    }).sort((a, b) => b.sum - a.sum);

    results.instanceResults.forEach(breakResult => {
        const data = breakResult.entityResults;
        breakResult.entityResults = sortedIndices.map(i => data[i.index]);
    });
}

function sortEntityInstances(results: GroupedCrossbreakCompetitionResults, order: ReportOrder) {
    switch(order){
        case ReportOrder.ResultOrderDesc:
        case ReportOrder.ResultOrderAsc:
            results.groupedBreakResults.forEach(group => sortByResultOrder(group.breakResults));
            break;
    }
    switch (order) {
        case ReportOrder.ResultOrderAsc:
        case ReportOrder.ScriptOrderAsc:
            results.groupedBreakResults.forEach(group =>
                group.breakResults.instanceResults.forEach(breakResult => breakResult.entityResults.reverse())
            );
            break;
    }
}

function sortBreaks(results: GroupedCrossbreakCompetitionResults, order: ReportOrder) {
    switch(order){
        case ReportOrder.ResultOrderDesc:
        case ReportOrder.ResultOrderAsc:
            results.groupedBreakResults.forEach(group => group.breakResults.instanceResults.sort((a, b) =>
                b.entityResults[0].weightedDailyResults[0].weightedResult -
                a.entityResults[0].weightedDailyResults[0].weightedResult));
            break;
    }
    switch (order) {
        case ReportOrder.ResultOrderAsc:
        case ReportOrder.ScriptOrderAsc:
            results.groupedBreakResults.forEach(group => group.breakResults.instanceResults.reverse());
            break;
    }
}

function showTopEntityInstancesForData(results: GroupedCrossbreakCompetitionResults, showTop: number | undefined) {
    if (showTop != null) {
        results.groupedBreakResults.forEach(group =>
            group.breakResults.instanceResults.forEach(breakResult => breakResult.entityResults.splice(showTop))
        );
    }
}

function showTopBreaksForData(results: GroupedCrossbreakCompetitionResults, showTop: number | undefined) {
    if (showTop != null) {
        results.groupedBreakResults.forEach(group => group.breakResults.instanceResults.splice(showTop));
    }
}

async function loadCompetitionResultsBase(
    reportPart: PartWithExtraData,
    filterInstances: FilterInstance[],
    curatedFilters: CuratedFilters,
    averageTypes: AverageType[],
    breaks: CrossMeasure[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    allMetrics: Metric[],
    variables: VariableConfigurationModel[],
    subsetId: string,
    returnForMultipleCharts: boolean,
    timeSelection: ITimeSelectionOptions,
    baseExpressionOverride?: BaseExpressionDefinition,
    includeSignificance?: boolean,
    primaryFilterInstance?: FilterInstance,
    significanceLevel?: SigConfidenceLevel,
    
): Promise<{results: GroupedCrossbreakCompetitionResults[], averages: CrosstabAverageResults[]}> {
    const ids = primaryFilterInstance ?
        [primaryFilterInstance.instance.id] :
        getEntityInstanceIdsFromEntitySet(reportPart.selectedEntitySet);
    
    const requestModel = new MultiEntityRequestModelWithCrossbreaks({
        multiEntityRequestModel: ViewHelper.createMultiEntityRequestModelForInstances({
            curatedFilters: curatedFilters,
            metric: reportPart.metric!,
            splitBySet: reportPart.selectedEntitySet,
            splitByEntityInstanceIds: ids,
            filterInstances: filterInstances,
            continuousPeriod: false,
            baseExpressionOverride: baseExpressionOverride,
            includeSignificance: includeSignificance,
            subsetId: subsetId,
            sigConfidenceLevel: significanceLevel
        }, timeSelection),
        breaks: breaks,
    });

    let results: GroupedCrossbreakCompetitionResults[];
    if (returnForMultipleCharts && filterInstances.length > 0) {
        results = await Factory.DataClient(throwError => throwError())
            .getGroupedCrossbreakCompetitionResultsMultiEntityMultiBreakMultiFilter(requestModel);
    } else {
        results = [await Factory.DataClient(throwError => throwError())
            .getGroupedCrossbreakCompetitionResultsMultiEntity(requestModel)];
    }

    if(averageTypes.length == 0) {
        return {results: results, averages: []};
    }

    const curatedRequestModel = ViewHelper.createMultiEntityRequestModelForInstances({
        curatedFilters: curatedFilters,
        metric: reportPart.metric!,
        splitBySet: reportPart.selectedEntitySet!,
        splitByEntityInstanceIds: ids,
        filterInstances: filterInstances,
        continuousPeriod: false,
        baseExpressionOverride: baseExpressionOverride,
        subsetId: subsetId,
    }, timeSelection);

    //TODO: how should averages behave with multiple breaks
    //https://app.shortcut.com/mig-global/story/69681/support-averages-with-multiple-breaks
    const averages = await Promise.all(averageTypes.map(async a => {
        let verifiedAverage = getVerifiedAverageType(a, reportPart.metric!, questionTypeLookup, true, allMetrics, variables);

        const averageMultiEntityChartModel = new AverageMultiEntityChartModel ({
            averageType: verifiedAverage,
            requestModel: curatedRequestModel,
            breaks: breaks[0],
        });

        return await Factory.DataClient(throwError => throwError())
            .getAverageForMultiEntityCharts(averageMultiEntityChartModel);
        })
    );
    return {results: results, averages: averages};
}

function applySortAndShowTop(
    results: GroupedCrossbreakCompetitionResults[],
    order: ReportOrder,
    showTop: number | undefined,
    sortType: SortType
) {
    if (sortType === SortType.EntityInstances) {
        results.forEach(result => {
            sortEntityInstances(result, order);
            showTopEntityInstancesForData(result, showTop);
        });
    } else if (sortType === SortType.Breaks) {
        results.forEach(result => {
            sortBreaks(result, order);
            showTopBreaksForData(result, showTop);
        });
    }
}

export async function getCrossbreakCompetitionResults(
    reportPart: PartWithExtraData,
    curatedFilters: CuratedFilters,
    breaks: CrossMeasure[],
    order: ReportOrder,
    showTop: number | undefined,
    filterInstances: FilterInstance[],
    averageTypes: AverageType[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    allMetrics: Metric[],
    variables: VariableConfigurationModel[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    splitByType?: IEntityType,
    baseExpressionOverride?: BaseExpressionDefinition,
    includeSignificance?: boolean,
    primaryFilterInstance?: FilterInstance,
    sortType: SortType = SortType.EntityInstances,
    sigConfidenceLevel: SigConfidenceLevel = SigConfidenceLevel.NinetyFive
): Promise<{results: GroupedCrossbreakCompetitionResults, averages: CrosstabAverageResults[]}> {
    if (reportPart.metric!.entityCombination.length >= 2) {
        if (!splitByType || filterInstances.length === 0) {
            throw new Error("Multi entity metrics must provide a split by type and a filter instance");
        }
    }
    let data = await loadCompetitionResultsBase(
        reportPart,
        filterInstances,
        curatedFilters,
        averageTypes,
        breaks,
        questionTypeLookup,
        allMetrics,
        variables,
        subsetId,
        false,
        timeSelection,
        baseExpressionOverride,
        includeSignificance,
        primaryFilterInstance,
        sigConfidenceLevel
    );

    applySortAndShowTop(data.results, order, showTop, sortType);

    return { results: data.results[0], averages: data.averages } as {results: GroupedCrossbreakCompetitionResults, averages: CrosstabAverageResults[]};
}

export async function getMultiChartCrossbreakCompetitionResults(
    reportPart: PartWithExtraData,
    curatedFilters: CuratedFilters,
    breaks: CrossMeasure[],
    order: ReportOrder,
    showTop: number | undefined,
    filterInstances: FilterInstance[],
    averageTypes: AverageType[],
    questionTypeLookup: { [key: string]: MainQuestionType; },
    allMetrics: Metric[],
    variables: VariableConfigurationModel[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    splitByType?: IEntityType,
    baseExpressionOverride?: BaseExpressionDefinition,
    includeSignificance?: boolean,
    primaryFilterInstance?: FilterInstance,
    sortType: SortType = SortType.EntityInstances,
    sigConfidenceLevel: SigConfidenceLevel = SigConfidenceLevel.NinetyFive
): Promise<{results: GroupedCrossbreakCompetitionResults[], averages: CrosstabAverageResults[]}> {
    if (reportPart.metric!.entityCombination.length >= 2) {
        if (!splitByType || filterInstances.length === 0) {
            throw new Error("Multi entity metrics must provide a split by type and a filter instance");
        }
    }
    let data = await loadCompetitionResultsBase(
        reportPart,
        filterInstances,
        curatedFilters,
        averageTypes,
        breaks,
        questionTypeLookup,
        allMetrics,
        variables,
        subsetId,
        true,
        timeSelection,
        baseExpressionOverride,
        includeSignificance,
        primaryFilterInstance,
        sigConfidenceLevel,
    );

    applySortAndShowTop(data.results, order, showTop, sortType);

    return data as {results: GroupedCrossbreakCompetitionResults[], averages: CrosstabAverageResults[]};
}