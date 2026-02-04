import React from 'react';
import {EntityInstance} from "../../entity/EntityInstance";
import SelectPicker from "../SelectPicker";

interface IEntityInstanceSelectorProps {
    onChange: (entityInstance: EntityInstance | null) => void;
    activeValue: EntityInstance | null;
    optionValues: EntityInstance[];
    className?: string;
    title?: string;
    disabled?: boolean;
    placeholder?: string;
}

class EntityInstanceSelectPicker extends SelectPicker<EntityInstance> {}

export const EntityInstanceSelector = (props: IEntityInstanceSelectorProps) => {
    return (
        <EntityInstanceSelectPicker onChange={props.onChange}
            activeValue={props.activeValue}
            optionValues={props.optionValues}
            className={props.className}
            title={props.title}
            disabled={props.disabled}
            placeholder={props.placeholder}
            getValue={(entityInstance) => `${entityInstance.id}`}
            getLabel={(entityInstance) => entityInstance.name} />
    );
}

