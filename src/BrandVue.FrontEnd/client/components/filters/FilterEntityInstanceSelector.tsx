import {EntityInstance, sortEntityInstances} from "../../entity/EntityInstance";
import {EntityInstanceSelector} from "./EntityInstanceSelector";
import React from "react";
import {IEntityType} from "../../BrandVueApi";
import {Metric} from "../../metrics/metric";
interface IFilterEntityInstanceSelectorProps { 
    entityType: IEntityType,
    allEntityInstances: EntityInstance[], 
    disabled: boolean, 
    onChange: (entityTypeId: string, entityInstanceId: string) => void, 
    selectedInstanceId?: number }

const FilterEntityInstanceSelector = (props: IFilterEntityInstanceSelectorProps) => {
    const onChange = (selectedOption: EntityInstance | null) => {
        props.onChange(props.entityType.identifier, selectedOption ? `${selectedOption.id}` : "");
    }

    const allEntityOption = props.entityType.isBrand ? [new EntityInstance(EntityInstance.AllInstancesId, "For each chosen " + props.entityType.displayNameSingular)] : [];
    const allOptions = [...allEntityOption, ...props.allEntityInstances.sort(sortEntityInstances)]
    return <span className="me-2">
            <EntityInstanceSelector
                onChange={onChange}
                activeValue={allOptions.find(ei => ei.id === props.selectedInstanceId) ?? null}
                optionValues={allOptions}
                title={props.entityType.displayNameSingular + " selector"}/>
        </span>;
}

export default FilterEntityInstanceSelector