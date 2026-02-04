import { EntitySelectionState, IEntitySetSelection } from "../state/entitySelectionSlice";
import { CategorySortKey, ComparisonPeriodSelection, IEntityType } from "../BrandVueApi";
import { PageHandler } from "../components/PageHandler";
import { shouldPanesRestrictToCurrentPeriod } from "../components/helpers/PagesHelper";
import { QueryStringParamNames } from "../components/helpers/UrlHelper";
import { IEntityConfiguration } from "./EntityConfiguration";
import { legacyParamNames, paramNames } from "../components/helpers/UrlSync";
import { TimeSelectionState } from "../state/timeSelectionSlice";

export function createParamsFromRelevantState(
    entityState: EntitySelectionState,
    timeSelectionState: TimeSelectionState,
    entityTypesForUrl: IEntityType[] | null,
    pageHandler: PageHandler
): Record<string, string> {
    let params = {};
    const setParam = (key: string, suffix: string, value: any) => {
        params[key + suffix] = value?.toString();
    };
    const setArrayParam = (key: string, suffix: string, value?: any[]) => {
        if (value && value.length > 0) {
            setParam(key, suffix, value?.join("."));
        } else {
            setParam(key, suffix, null);
        }
    };
    let i = 1;
    if (entityState.activeBreaks.audienceId !== undefined) {
        setParam(paramNames.audienceId, "", entityState.activeBreaks.audienceId);
    } else {
        setParam(paramNames.audienceId, "", null);
    }
    if (entityState.activeBreaks.multipleChoiceByValue) {
        setParam(paramNames.audienceMultipleChoiceByValue, "", entityState.activeBreaks.multipleChoiceByValue);
    } else {
        setParam(paramNames.audienceMultipleChoiceByValue, "", null);
    }
    setParam(paramNames.sortKey, "", entityState.categorySortKey == CategorySortKey.None ? null : entityState.categorySortKey);
    setParam(legacyParamNames.splitBy, "", null);
    setParam(legacyParamNames.filterBy, "", null);

    if (shouldPanesRestrictToCurrentPeriod(entityState.activeBreaks, pageHandler.getPanesToRender())) {
        setParam(QueryStringParamNames.period, "", ComparisonPeriodSelection.CurrentPeriodOnly);
    }
    if (entityTypesForUrl) {
        setArrayParam(
            paramNames.entityTypes,
            "",
            entityTypesForUrl.map((e) => e.identifier)
        );
    } else {
        params[paramNames.entityTypes] = null; //unset as selection is same is default
    }
    if (pageHandler.session.activeView?.curatedFilters?.average != null && pageHandler.session.activeView.curatedFilters.average.averageId != timeSelectionState.scorecardPeriod) {
        params[paramNames.scorecardPeriod] = timeSelectionState.scorecardPeriod;
    } else {
        params[paramNames.scorecardPeriod] = null;
    }

    for (let entityType of entityState.priorityOrderedEntityTypes) {
        let suffix = i == 1 ? "" : i.toString();
        const selected = entityState.entitySets[entityType.identifier];
        setParam(paramNames.entitySetId, suffix, selected.entitySetId);
        setParam(paramNames.active, suffix, selected.active);
        setArrayParam(paramNames.highlighted, suffix, selected.highlighted);
        setArrayParam(paramNames.entitySetAverages, suffix, selected.entitySetAverages);
        i++;
    }
    return params;
}

/** Sanitizes entity selections from URL parameters to ensure only valid data goes into the store */
export const sanitizeEntitySelections = (
    selections: { entityType: string; entitySet: IEntitySetSelection }[],
    entityConfiguration: IEntityConfiguration,
    previousEntityTypes?: IEntityType[]
): { entityType: string; entitySet: IEntitySetSelection }[] => {
    const sanitizedSelections = [...selections];

    for (let i = 0; i < sanitizedSelections.length; i++) {
        const selection = sanitizedSelections[i];
        const entityType = entityConfiguration.getEntityType(selection.entityType);

        // This happens briefly when someone has just created an entity type and it's not yet in the configuration
        if (!entityType) continue;

        // Check if entity type has changed - reset related params if needed
        const previousEntityType = previousEntityTypes?.[i];
        if (previousEntityType && previousEntityType.identifier !== entityType.identifier) {
            selection.entitySet.active = undefined;
            selection.entitySet.highlighted = [];
        }

        const availableInstances = entityConfiguration.getAllEnabledInstancesForType(entityType);
        const availableEntitySets = entityConfiguration.getSetsFor(entityType);

        if (selection.entitySet.active !== undefined && !availableInstances.some((i) => i.id === selection.entitySet.active)) {
            selection.entitySet.active = undefined;
        }

        if (selection.entitySet.highlighted && selection.entitySet.highlighted.length > 0) {
            selection.entitySet.highlighted = selection.entitySet.highlighted.filter((highlightedId) => availableInstances.some((i) => i.id === highlightedId));
        }

        if (selection.entitySet.entitySetId !== undefined && !availableEntitySets.some((es) => es.id === selection.entitySet.entitySetId)) {
            selection.entitySet.entitySetId = undefined;
        }

        if (selection.entitySet.entitySetAverages && selection.entitySet.entitySetAverages.length > 0) {
            selection.entitySet.entitySetAverages = selection.entitySet.entitySetAverages.filter((id) => availableEntitySets.some((es) => es.id === id));
        }
    }

    return sanitizedSelections;
};

/**
 * Simplifies entity selection state by removing redundant properties that match with the entity set defaults
 */
export function simplifyEntitySelectionState(state: EntitySelectionState, entityConfiguration: IEntityConfiguration): EntitySelectionState {
    const simplifiedState = {
        ...state,
        entitySets: { ...state.entitySets },
    };

    // Process each entity type selection
    Object.keys(simplifiedState.entitySets).forEach((entityTypeId) => {
        const selection = { ...simplifiedState.entitySets[entityTypeId] };

        if (selection.entitySetId !== undefined) {
            const entityType = state.priorityOrderedEntityTypes.find((et) => et.identifier === entityTypeId);
            if (entityType) {
                const entitySet = entityConfiguration.getEntitySet(entityType.identifier, selection.entitySetId);

                // If entitySet exists, check and remove redundant properties
                if (entitySet) {
                    const mainInstance = entitySet.getMainInstance();
                    if (mainInstance && selection.active === mainInstance.id) {
                        delete selection.active;
                    }

                    const defaultHighlighted = entitySet
                        .getInstances()
                        .getAll()
                        .map((i) => i.id);
                    if (
                        selection.highlighted &&
                        selection.highlighted.length === defaultHighlighted.length &&
                        selection.highlighted.every((id) => defaultHighlighted.includes(id))
                    ) {
                        delete selection.highlighted;
                    }

                    const defaultEntitySetAverages = entitySet
                        .getAverages()
                        .getAll()
                        .map((a) => a.entitySetId);
                    if (
                        selection.entitySetAverages &&
                        selection.entitySetAverages.length === defaultEntitySetAverages.length &&
                        selection.entitySetAverages.every((id) => defaultEntitySetAverages.includes(id))
                    ) {
                        delete selection.entitySetAverages;
                    }
                }
            }
        }

        simplifiedState.entitySets[entityTypeId] = selection;
    });

    return simplifiedState;
}
