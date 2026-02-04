import React from "react";
import BaseInput from "./BaseInput";

interface IVariableNameInputProps {
    description: string;
    setDescription: (name: string) => void;
    isDisabled?: boolean;
}

const VariableDescriptionInput = (props: IVariableNameInputProps) => {

    return (
        <BaseInput label={"Description"}
            inputClassName={"variable-description-input-container"}
            id={"variable-description"}
            value={props.description}
            setName={props.setDescription}
            isDisabled={props.isDisabled}
            name={props.description} />
    );
}

export default VariableDescriptionInput