import {
    BaseExpressionDefinition,
    CrossMeasure,
    CrossMeasureFilterInstance,
    CrosstabRequestOptions,
    CrosstabSignificanceType,
    CrosstabRequestModel,
    Period,
    IEntityType,
    EntityInstanceRequest,
    ComparisonPeriodSelection,
    SigConfidenceLevel,
    DisplaySignificanceDifferences
} from "../../../BrandVueApi";
import { IEntityConfiguration } from "../../../entity/EntityConfiguration";
import { EntityInstance } from "../../../entity/EntityInstance";
import { EntitySet } from "../../../entity/EntitySet";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import { Metric } from "../../../metrics/metric";
import { PaginationData } from "../PaginationData";
import { getCompositeFilterModel, getDemographicFilterModel } from "../Reports/Filtering/FilterHelper";
import { ITimeSelectionOptions } from "../../../state/ITimeSelectionOptions";

export function getCrosstabRequestModel(
    metric: Metric,
    allMetrics: Metric[],
    categories: CrossMeasure[],
    activeEntitySet: EntitySet | undefined,
    secondaryEntitySets: EntitySet[],
    curatedFilters: CuratedFilters,
    entityConfiguration: IEntityConfiguration,
    currentPaginationData: PaginationData,
    isSurveyVue: boolean,
    highlightSignificance: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    significanceType: CrosstabSignificanceType,
    isDataWeighted: boolean,
    hideEmptyColumns: boolean,
    focusInstance: EntityInstance | undefined,
    baseExpressionOverride: BaseExpressionDefinition | undefined,
    subsetId: string,
    sigConfidenceLevel: SigConfidenceLevel,
    showMultipleTablesAsSingle: boolean,
    timeSelection: ITimeSelectionOptions,
    calculateIndexScores: boolean
) : CrosstabRequestModel
{
    return createMultiEntityRequestModel(metric,
        allMetrics,
        categories,
        activeEntitySet,
        secondaryEntitySets,
        curatedFilters,
        entityConfiguration,
        currentPaginationData,
        isSurveyVue,
        highlightSignificance,
        displaySignificanceDifferences,
        significanceType,
        isDataWeighted,
        hideEmptyColumns,
        focusInstance,
        baseExpressionOverride,
        subsetId,
        sigConfidenceLevel,
        showMultipleTablesAsSingle,
        timeSelection,
        calculateIndexScores
    );
}

function getSetForType(type: IEntityType, activeEntitySet: EntitySet | undefined, secondaryEntitySets: EntitySet[]): EntitySet | undefined {
    if (type.identifier === activeEntitySet?.type.identifier) {
        return activeEntitySet;
    } else {
        return secondaryEntitySets.find(set => type.identifier === set.type.identifier)
    }
}

function createMultiEntityRequestModel(
    metric: Metric,
    allMetrics: Metric[],
    categories: CrossMeasure[],
    activeEntitySet: EntitySet | undefined,
    secondaryEntitySets: EntitySet[],
    curatedFilters: CuratedFilters,
    entityConfiguration: IEntityConfiguration,
    currentPaginationData: PaginationData,
    isSurveyVue: boolean,
    highlightSignificance: boolean,
    displaySignificanceDifferences: DisplaySignificanceDifferences,
    significanceType: CrosstabSignificanceType,
    isDataWeighted: boolean,
    hideEmptyColumns: boolean,
    focusInstance: EntityInstance | undefined,
    baseExpressionOverride: BaseExpressionDefinition | undefined,
    subsetId: string,
    sigConfidenceLevel: SigConfidenceLevel,
    showMultipleTablesAsSingle: boolean,
    timeSelection: ITimeSelectionOptions,
    calculateIndexScores: boolean)
{
    if (metric.entityCombination.length > 0 && !activeEntitySet) {
        throw new Error("Missing entity set");
    }

    const activeBrandId = focusInstance?.id ?? -1;

    const primaryInstancesRequest = (entitySet: EntitySet | undefined) => {
        if (entitySet) {
            const primaryInstances = entitySet.getInstances().getAll().map(entity => entity.id);

            return new EntityInstanceRequest({
                type: entitySet.type.identifier,
                entityInstanceIds: primaryInstances
            })
        }
    }

    const filterInstancesRequest = (entitySet: EntitySet | undefined) => {
        let filterInstances: EntityInstanceRequest[] = [];
        if (entitySet) {
            const splitByType = entitySet.type;
            const filterByTypes = metric.entityCombination?.filter(et => et.identifier !== splitByType.identifier) ?? [];
            const filterInstanceSets = filterByTypes.map(type => getSetForType(type, activeEntitySet, secondaryEntitySets) ?? entityConfiguration.getAllEnabledInstancesOrderedAsSet(type));
            filterInstances = filterInstanceSets.map(set => new EntityInstanceRequest({
                type: set.type.identifier,
                entityInstanceIds: set.getInstances().getAll().map(i => i.id)
            }));
        }

        return filterInstances;
    }
    
    return new CrosstabRequestModel({
        pageNo: currentPaginationData.currentPageNo,
        noOfCharts: currentPaginationData.noOfTablesPerPage,
        primaryMeasureName: metric.name,
        subsetId: subsetId,
        primaryInstances: primaryInstancesRequest(activeEntitySet),
        filterInstances: filterInstancesRequest(activeEntitySet),
        period: new Period({
            average: curatedFilters.average.averageId,
            comparisonDates: curatedFilters.comparisonDates(false, timeSelection, false, ComparisonPeriodSelection.CurrentPeriodOnly)
        }),
        crossMeasures: getCrossMeasuresWithSpecificInstances(categories, isSurveyVue, allMetrics, activeEntitySet, secondaryEntitySets),
        activeBrandId: activeBrandId,
        demographicFilter: getDemographicFilterModel(curatedFilters, metric),
        filterModel: getCompositeFilterModel(curatedFilters, metric),
        options: new CrosstabRequestOptions({
            calculateSignificance: highlightSignificance,
            displaySignificanceDifferences: displaySignificanceDifferences,
            significanceType: significanceType,
            sigConfidenceLevel: sigConfidenceLevel,
            isDataWeighted: isSurveyVue ? isDataWeighted: true,
            hideEmptyColumns: hideEmptyColumns,
            showMultipleTablesAsSingle:showMultipleTablesAsSingle,
            calculateIndexScores: calculateIndexScores,
        }),
        baseExpressionOverride: baseExpressionOverride
    });
}

function getCrossMeasuresWithSpecificInstances(categories: CrossMeasure[], isSurveyVue: boolean, allMetrics: Metric[], activeEntitySet: EntitySet | undefined, secondaryEntitySets: EntitySet[]) {
    if (isSurveyVue) {
        return categories;
    }

    return categories.map(c => {
        var metric = allMetrics.find(m => m.name == c.measureName);
        let filterInstances: CrossMeasureFilterInstance[] = [];

        // Currently we can't have multi-entity cross measures
        if (metric && metric.entityCombination.length == 1) {
            const entityType = metric.entityCombination[0];
            let entitySet = getSetForType(entityType, activeEntitySet, secondaryEntitySets);

            if (entitySet) {
                filterInstances = entitySet.getInstances().getAll().map(i => {
                    return new CrossMeasureFilterInstance({
                        instanceId: i.id,
                        filterValueMappingName: ""
                    });
                });
            }
        }

        c.filterInstances = filterInstances;
        c.childMeasures = getCrossMeasuresWithSpecificInstances(c.childMeasures, isSurveyVue, allMetrics, activeEntitySet, secondaryEntitySets);
        return c;
    });
}