
import {EntityInstance} from "../../entity/EntityInstance";
import React from "react";
import {MultipleSelectPicker} from "../SelectPicker";

interface IMultipleEntityInstanceSelectorProps {
    onChange: (entityInstances: EntityInstance[] | null) => void;
    activeValue: EntityInstance[];
    optionValues: EntityInstance[];
    className?: string;
    title?: string;
    disabled?: boolean;
    placeholder?: string;
    allowMultipleSelection: boolean;
}

export const MultipleEntityInstanceSelector = (props: IMultipleEntityInstanceSelectorProps) => {
    return (
        <MultipleSelectPicker onChange={props.onChange}
                                            activeValue={props.activeValue}
                                            optionValues={props.optionValues}
                                            className={props.className}
                                            title={props.title}
                                            disabled={props.disabled}
                                            placeholder={props.placeholder}
                                            getValue={(entityInstance) => entityInstance.id.toString()}
                                            getLabel={(entityInstance) => entityInstance.name} 
                                            allowMultipleSelections={props.allowMultipleSelection}/>
    );
}
