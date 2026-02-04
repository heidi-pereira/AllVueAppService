import {EntityInstance, sortEntityInstances} from "../../entity/EntityInstance";
import React from "react";
import {IEntityType} from "../../BrandVueApi";
import {MultipleEntityInstanceSelector} from "./MultipleEntityInstanceSelectPicker";
interface IFilterEntityInstanceSelectorProps {
    entityType: IEntityType, 
    allEntityInstances: EntityInstance[], 
    disabled: boolean, 
    onChange: (entityTypeId: string, entityInstances: number[]) => void, 
    selectedInstances: number[],
    allowMultipleSelection: boolean,
}

const FilterMultipleEntityInstanceSelector = (props: IFilterEntityInstanceSelectorProps) => {
    const onChange = (selectedOptions: EntityInstance[] | null) => {
        props.onChange(props.entityType.identifier, selectedOptions?.map(ei => ei.id) ?? []);
    }

    const allOptions = [...props.allEntityInstances.sort(sortEntityInstances)]
    return <span className="me-2">
            <MultipleEntityInstanceSelector
                onChange={onChange}
                activeValue={allOptions.filter(ei => props.selectedInstances.some(x=>x == ei.id)) ?? null}
                optionValues={allOptions}
                title={props.entityType.displayNameSingular + " selector"}
                allowMultipleSelection={props.allowMultipleSelection}/>
        </span>;
}

export default FilterMultipleEntityInstanceSelector