import React from "react";
import { ReactNode, useContext} from "react";
import { IGoogleTagManager } from "../googleTagManager";
import { EntitySet } from "../entity/EntitySet";
import { EntityInstance } from "../entity/EntityInstance";
import { IEntityType } from "../BrandVueApi";
import DropdownSelector, { DropdownItemGroup, renderItemWithSearch } from "./dropdown/DropdownSelector";
import { EntitySetFactoryContext } from "../GlobalContext";
import { EntitySetDropdownInstanceList } from "./EntitySetDropdownInstanceList";
import { PageHandler } from "./PageHandler";

interface IProps {
    googleTagManager?: IGoogleTagManager;
    pageHandler: PageHandler;
    activeEntitySet: EntitySet,
    availableInstances: EntityInstance[],
    entityType: IEntityType,
    availableEntitySets: EntitySet[],
    focusInstance: EntityInstance,
    updateFocusInstance(focusInstance: EntityInstance): void,
    updateActiveEntitySet(active: EntitySet): void,
    asButton: boolean,
    showLabel: boolean,
    preserveFocus?: boolean,
    materialIcon?: string,
    dropdownClassName?: string
    label?:string
}

const instancesMatch = (entitySet: EntitySet, filterQuery: string): boolean => {
    return entitySet.getInstances()
        .getAll()
        .some(i => i.name.toLowerCase().includes(filterQuery.toLowerCase()));
};

const isPartialMatch = (entitySet: EntitySet, filterQuery: string): boolean => {
    return entitySet.name.toLowerCase().includes(filterQuery.toLowerCase());
};

const filterPredicate = (entitySet: EntitySet, filterQuery: string): boolean => {
    return isPartialMatch(entitySet, filterQuery) || instancesMatch(entitySet, filterQuery);
};

const isExactMatch = (entitySet: EntitySet, filterQuery: string): boolean => {
    return entitySet.name.toLowerCase() === filterQuery.toLowerCase();
};

const sortFunction = (a: EntitySet, b: EntitySet, filterQuery: string): number => {
    if (filterQuery === "") return 0;

    if (isExactMatch(a, filterQuery)) return -1;
    if (isExactMatch(b, filterQuery)) return 1;

    const aIsPartialMatch = isPartialMatch(a, filterQuery);
    const bIsPartialMatch = isPartialMatch(b, filterQuery);
    if (aIsPartialMatch && !bIsPartialMatch) return -1;
    if (bIsPartialMatch && !aIsPartialMatch) return 1;
    if (aIsPartialMatch && bIsPartialMatch) return a.name.localeCompare(b.name);

    const instancesOfAMatch = instancesMatch(a, filterQuery);
    const instancesOfBMatch = instancesMatch(b, filterQuery);
    if (instancesOfAMatch && !instancesOfBMatch) return 1;
    if (instancesOfBMatch && !instancesOfAMatch) return -1;

    return a.name.localeCompare(b.name);
};

const getSortedInstanceList = (entitySet: EntitySet, filterQuery: string): Array<EntityInstance> => {
    const query = filterQuery.trim().toLowerCase();

    const instances = entitySet.getInstances().getAll();

    const instanceMatches = instances.filter(instance => instance.name.toLowerCase().includes(query))
        .sort((a, b) => a.name.localeCompare(b.name));

    const instanceNonMatches = instances.filter(instance => !instance.name.toLowerCase().includes(query))
        .sort((a, b) => a.name.localeCompare(b.name));

    return instanceMatches.concat(instanceNonMatches);
};

const EntitySetDropdownSelector: React.FunctionComponent<IProps> = (props: IProps) => {
    const entitySetFactory = useContext(EntitySetFactoryContext);

    const items: DropdownItemGroup<EntitySet>[] = [
        new DropdownItemGroup<EntitySet>(`My ${props.activeEntitySet.type.displayNameSingular.toLowerCase()} groups`, props.availableEntitySets.filter(e => !e.isSectorSet)),
        new DropdownItemGroup<EntitySet>(`Sector groups`, props.availableEntitySets.filter(e => e.isSectorSet)),
    ];

    const updateActiveEntitySet = (entitySet: EntitySet): void => {
        const mainInstance = entitySet.mainInstance ?? props.focusInstance;
        const newSet = entitySetFactory.getSetFromInstances(props.availableEntitySets, entitySet.getInstances(), mainInstance, entitySet.type);
        if (newSet.mainInstance!.id !== props.focusInstance.id && !props.preserveFocus) {
            props.updateFocusInstance(newSet.mainInstance!);
        }
        props.updateActiveEntitySet(newSet);
    };

    const renderContextPanel = (entitySet: EntitySet, filterQuery: string): ReactNode => {
        return <EntitySetDropdownInstanceList
            key={"instance-list"}
            entitySet={entitySet}
            sortedInstanceList={getSortedInstanceList(entitySet, filterQuery)}
            getItemMarkup={(name) => renderItemWithSearch(name, filterQuery)} />;
    };

    return <DropdownSelector<EntitySet>
                label={props.label ?? props.entityType.displayNamePlural}
                items={items}
                selectedItem={props.activeEntitySet}
                onSelected={updateActiveEntitySet}
                itemDisplayText={e => e.name}
                asButton={props.asButton}
                showLabel={props.showLabel}
                itemKey={e => e.name}
                filterPredicate={filterPredicate}
                sortFunction={sortFunction}
                renderItemContextPanel={renderContextPanel}
                materialIcon={props.materialIcon}
            />;
};

export default EntitySetDropdownSelector;