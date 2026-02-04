import { RootState, createAppSelector } from "./store";
import { createEntitySetFromSelection, EntitySet } from "../entity/EntitySet";
import { IEntityType } from "../BrandVueApi";
import { EntityInstance } from "../entity/EntityInstance";
import { IEntitySetSelection } from "./entitySelectionSlice";
import { FilterInstance } from "../entity/FilterInstance";
import { IEntityConfiguration } from "client/entity/EntityConfiguration";
import { IEntitySetFactory } from "client/entity/EntitySetFactory";
import { Params } from "./createAppSelectorParams";
import { throwIfNullish } from "../components/helpers/ThrowHelper";

// Simple selectors

export const selectEntitySelectionState = (state: RootState) => state.entitySelection;

export const selectActiveEntitySelectionWithDefault = (state: RootState): IEntitySetSelection | null => {
    const entitySelectionState = selectEntitySelectionState(state);
    if (!entitySelectionState.priorityOrderedEntityTypes.length) {
        return null;
    }
    const activeEntityType = entitySelectionState.priorityOrderedEntityTypes[0];
    return activeEntityType ? entitySelectionState.entitySets[activeEntityType.identifier] : null;
};

export const selectActiveEntitySelection = (state: RootState): IEntitySetSelection | null => {
    const activeSelection = selectActiveEntitySelectionWithDefault(state);
    if (activeSelection === null) {
        const entitySelectionState = selectEntitySelectionState(state);
        throwIfNullish(entitySelectionState.priorityOrderedEntityTypes.length > 0 ? {} : null, "Active entity selection");
    }
    return activeSelection;
};

export const selectEntitySelectionReady = (state: RootState): boolean => {
    const entitySelectionState = selectEntitySelectionState(state);
    return (
        entitySelectionState.priorityOrderedEntityTypes[0] != null &&
        entitySelectionState.entitySets[entitySelectionState.priorityOrderedEntityTypes[0].identifier] != undefined
    );
};

export const selectActiveEntityTypeOrNull = (state: RootState): IEntityType | null => {
    const entitySelectionState = selectEntitySelectionState(state);
    if (!entitySelectionState.priorityOrderedEntityTypes[0]) {
        return null;
    }
    return entitySelectionState.priorityOrderedEntityTypes[0];
};

export const selectActiveEntityType = (state: RootState) => {
    const activeEntityType = selectActiveEntityTypeOrNull(state);
    return throwIfNullish(activeEntityType, "Active entity type");
};

const createEntitySetFromSelectionWithDefault = (
    entityType: IEntityType,
    selection: IEntitySetSelection | null,
    entityConfiguration: IEntityConfiguration,
    entitySetFactory: IEntitySetFactory
): EntitySet => {
    const allEntitySets = entityConfiguration.getSetsFor(entityType);
    const sourceEntitySet =
        selection != null && selection.entitySetId != null
            ? allEntitySets.find((e) => e.id === selection.entitySetId) || entityConfiguration.getDefaultEntitySetFor(entityType)
            : entityConfiguration.getDefaultEntitySetFor(entityType);
    return createEntitySetFromSelection(selection!, allEntitySets, sourceEntitySet, entitySetFactory, entityConfiguration);
};

// Cached selectors which depend on looping through sets/instances
export const selectActiveEntitySetWithDefaultOrNull = createAppSelector(
    [selectActiveEntityTypeOrNull, selectActiveEntitySelectionWithDefault, ...Params.two<IEntityConfiguration, IEntitySetFactory>()],
    (activeEntityType, activeSelection, entityConfiguration, entitySetFactory) => {
        if (!activeEntityType || !activeSelection) {
            return null;
        }
        return createEntitySetFromSelectionWithDefault(activeEntityType, activeSelection, entityConfiguration, entitySetFactory);
    }
);

/**
 *Try and avoid using this selector, we want to stop returning "brand as default" in entity types and instead make it nullable
 */
export const selectActiveEntitySetWithBrandDefault = createAppSelector(
    [selectActiveEntityTypeOrNull, selectActiveEntitySelectionWithDefault, ...Params.two<IEntityConfiguration, IEntitySetFactory>()],
    (activeEntityType, activeSelection, entityConfiguration, entitySetFactory) => {
        if (!activeEntityType || !activeSelection) {
            const brandEntityType = entityConfiguration.getBrandEntityTypeOrNull();
            return brandEntityType != null ? createEntitySetFromSelectionWithDefault(brandEntityType, {}, entityConfiguration, entitySetFactory) : null;
        }
        return createEntitySetFromSelectionWithDefault(activeEntityType, activeSelection, entityConfiguration, entitySetFactory);
    }
);

export const selectActiveInstanceWithDefaultOrNull = createAppSelector(
    [selectActiveEntityTypeOrNull, selectActiveEntitySelectionWithDefault, ...Params.two<IEntityConfiguration, IEntitySetFactory>()],
    (activeEntityType, activeSelection, entityConfiguration, entitySetFactory) => {
        if (!activeEntityType || !activeSelection) {
            return null;
        }
        const entitySet = createEntitySetFromSelectionWithDefault(activeEntityType, activeSelection, entityConfiguration, entitySetFactory);
        return entitySet.getMainInstance();
    }
);

export const selectActiveInstanceWithBrandDefault = createAppSelector(
    [selectActiveEntityTypeOrNull, selectActiveEntitySelectionWithDefault, ...Params.two<IEntityConfiguration, IEntitySetFactory>()],
    (activeEntityType, activeSelection, entityConfiguration: IEntityConfiguration, entitySetFactory) => {
        if (!activeEntityType || !activeSelection) {
            return entityConfiguration.getDefaultEntitySetFor(entityConfiguration.defaultEntityType).getMainInstance();
        }
        const entitySet = createEntitySetFromSelectionWithDefault(activeEntityType, activeSelection, entityConfiguration, entitySetFactory);
        return entitySet.getMainInstance();
    }
);

export const selectActiveEntitySet = createAppSelector([selectActiveEntitySetWithDefaultOrNull], (activeEntitySet) => {
    return throwIfNullish(activeEntitySet, "Active entity set");
});

export const selectBrandSet = createAppSelector(
    [selectEntitySelectionState, ...Params.two<IEntityConfiguration, IEntitySetFactory>()],
    (entitySelectionState, entityConfiguration, entitySetFactory) => {
        const brandEntityType = entityConfiguration.getBrandEntityTypeOrNull();
        if (!brandEntityType) {
            return null;
        }
        const brandSelection = entitySelectionState.entitySets[brandEntityType.identifier];
        return createEntitySetFromSelectionWithDefault(brandEntityType, brandSelection, entityConfiguration, entitySetFactory);
    }
);

export const selectActiveInstance = createAppSelector([selectActiveInstanceWithDefaultOrNull], (activeInstance) => {
    return throwIfNullish(activeInstance, "Active instance");
});

//"filter instance" is the primary instance of the second entity type
export const selectFilterInstanceOrNull = createAppSelector(
    [selectActiveEntityTypeOrNull, selectEntitySelectionState, ...Params.three<IEntityType[], IEntityConfiguration, IEntitySetFactory>()],
    (activeEntityType, entitySelectionState, entityCombination, entityConfiguration, entitySetFactory) => {
        const filterEntityType = entityCombination.find((et) => et.identifier !== activeEntityType?.identifier);

        if (!filterEntityType) {
            return null;
        }

        const entitySelection = entitySelectionState.entitySets[filterEntityType.identifier];
        const entitySet = createEntitySetFromSelectionWithDefault(filterEntityType, entitySelection, entityConfiguration, entitySetFactory);

        return new FilterInstance(filterEntityType, entitySet.getMainInstance());
    }
);

export const selectAvailableFilterInstances = createAppSelector(
    [selectActiveEntityType, ...Params.two<IEntityType[], IEntityConfiguration>()],
    (activeEntityType, entityCombination, entityConfiguration) => {
        const filterEntityType = entityCombination.find((et) => et.identifier !== activeEntityType.identifier);
        return filterEntityType
            ? entityConfiguration
                  .getAllEnabledInstancesForType(filterEntityType)
                  .toSorted((a: EntityInstance, b: EntityInstance) => a.name.localeCompare(b.name))
            : [];
    }
);

export const selectAllActiveEntitySetsWithDefault = createAppSelector(
    [
        (state) => state.entitySelection.priorityOrderedEntityTypes,
        (state) => state.entitySelection.entitySets,
        ...Params.two<IEntityConfiguration, IEntitySetFactory>(),
    ],
    (priorityOrderedEntityTypes, activeSelections, entityConfiguration, entitySetFactory) => {
        return priorityOrderedEntityTypes.map((x: IEntityType) =>
            createEntitySetFromSelectionWithDefault(x, activeSelections[x.identifier], entityConfiguration, entitySetFactory)
        );
    }
);
