import * as React from 'react';
import { EntityInstance } from "../entity/EntityInstance";
import { FilterInstance } from "../entity/FilterInstance";
import DropdownSelector from "./dropdown/DropdownSelector";
import { useAvailableFilterInstances } from "client/state/entitySelectionHooks";
import { useAppDispatch } from "../state/store";
import { setFilterBy } from "../state/entitySelectionSlice";
import {IEntityType} from "../BrandVueApi";

interface ISingleInstanceDropdownSelectorProps 
{
    activeEntityTypes: IEntityType[]
    filterInstance: FilterInstance;
}

export const matchesSearch = (instance: EntityInstance, filterQuery: string): boolean => {
    return instance.name.toLowerCase().includes(filterQuery.toLowerCase());
}

const FilterInstanceDropdownSelector: React.FunctionComponent<ISingleInstanceDropdownSelectorProps> = (props: ISingleInstanceDropdownSelectorProps) => {
    var availableInstances = useAvailableFilterInstances(props.activeEntityTypes);
    var dispatch = useAppDispatch();
    return <DropdownSelector<EntityInstance>
        label={props.filterInstance.type.displayNameSingular}
        items={availableInstances}
        selectedItem={props.filterInstance.instance}
        onSelected={instance => dispatch(setFilterBy({ filterBy: instance, filterByType: props.filterInstance.type }))}
        itemKey={instance => instance.name}
        itemDisplayText={selected => selected.name}
        asButton={false}
        showLabel={true}
        filterPredicate={matchesSearch}
    />
}

export default FilterInstanceDropdownSelector;