import React from 'react';
import { IEntityType } from "../../BrandVueApi";
import SelectPicker from "../SelectPicker";

interface IEntityTypeSelectorProps {
    onChange: (entityType: IEntityType | null) => void;
    activeValue: IEntityType | null;
    optionValues: IEntityType[];
    className?: string;
    title?: string;
    disabled?: boolean;
    placeholder?: string;
}

class EntityTypeSelectPicker extends SelectPicker<IEntityType> {}

export const EntityTypeSelector = (props: IEntityTypeSelectorProps) => {
    return (
        <EntityTypeSelectPicker onChange={props.onChange}
            activeValue={props.activeValue}
            optionValues={props.optionValues}
            className={props.className}
            title={props.title}
            disabled={props.disabled}
            placeholder={props.placeholder}
            getValue={(entityType) => entityType.identifier}
            getLabel={(entityType) => entityType.displayNameSingular} />
    );
}