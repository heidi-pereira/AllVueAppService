import { IEntityType } from "client/BrandVueApi";
import { IEntityConfiguration } from "client/entity/EntityConfiguration";
import { useEntityConfigurationStateContext } from "client/entity/EntityConfigurationStateContext";
import { EntityInstance } from "client/entity/EntityInstance";
import { EntitySet } from "client/entity/EntitySet";
import { IEntitySetFactory } from "client/entity/EntitySetFactory";
import { FilterInstance } from "client/entity/FilterInstance";
import { EntitySetFactoryContext } from "client/GlobalContext";
import { useContext } from "react";
import {
    selectActiveEntitySetWithDefaultOrNull,
    selectBrandSet,
    selectActiveInstance,
    selectFilterInstanceOrNull,
    selectAvailableFilterInstances,
    selectAllActiveEntitySetsWithDefault,
    selectActiveInstanceWithDefaultOrNull,
    selectActiveEntitySetWithBrandDefault, 
    selectActiveInstanceWithBrandDefault
} from "./entitySelectionSelectors";
import { RootState, useAppSelector } from "./store";
import { throwIfNullish } from "../components/helpers/ThrowHelper";
import { IActiveBreaks } from "./entitySelectionSlice";

// Convenience hooks - if we inject the dependencies another way we won't need these
function useEntityContextSelector<TResult>(
    selector: (state: RootState, entityConfiguration: IEntityConfiguration, entitySetFactory: IEntitySetFactory) => TResult
): TResult {
    const entityConfigurationContext = useEntityConfigurationStateContext();
    const entitySetFactory = useContext(EntitySetFactoryContext);
    return useAppSelector(state => selector(state, entityConfigurationContext.entityConfiguration, entitySetFactory));
}

export const useActiveEntitySetWithDefaultOrNull = (): EntitySet | null => useEntityContextSelector(selectActiveEntitySetWithDefaultOrNull);

/**
*Try and avoid using this hook, we want to stop returning "brand as default" in entity types and instead make it nullable
*/
export const useActiveEntitySetWithBrandDefaultOrNull = (): EntitySet | null => useEntityContextSelector(selectActiveEntitySetWithBrandDefault);
export const useActiveEntitySet = (): EntitySet => {
    const entitySet = useEntityContextSelector(selectActiveEntitySetWithDefaultOrNull);
    return throwIfNullish(entitySet, "Active entity set");
}
export const useActiveBrandSetWithDefault = (): EntitySet | null => useEntityContextSelector(selectBrandSet);
export const useActiveInstance = (): EntityInstance => useEntityContextSelector(selectActiveInstance);

/**
 *Try and avoid using this hook, we want to stop returning "brand as default" in entity types and instead make it nullable
 */
export const useActiveInstanceOrBrandDefault = (): EntityInstance => useEntityContextSelector(selectActiveInstanceWithBrandDefault);
export const useActiveInstanceWithDefaultOrNull = (): EntityInstance | null => useEntityContextSelector(selectActiveInstanceWithDefaultOrNull);
//filter instance are effectively the primary instance of the second entity type

export const useFilterInstanceWithDefaultOrNull = (ets: IEntityType[]): FilterInstance | null => useEntityContextSelector((state, ec, esf) => selectFilterInstanceOrNull(state, ets, ec, esf));

export const useAvailableFilterInstances = (ets: IEntityType[]): EntityInstance[] => useEntityContextSelector((state, ec) => selectAvailableFilterInstances(state, ets, ec));

export const useAllActiveEntitySetsWithDefault = (): EntitySet[] => useEntityContextSelector((state, ec, esf) => selectAllActiveEntitySetsWithDefault(state, ec, esf));

export const useSelectedBreaks = (): IActiveBreaks => useAppSelector(state => state.entitySelection.activeBreaks);