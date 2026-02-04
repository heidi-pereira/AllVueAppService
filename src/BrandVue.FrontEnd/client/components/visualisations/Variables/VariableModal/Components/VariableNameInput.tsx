import React from "react";
import BaseInput from "./BaseInput";

interface IVariableNameInputProps {
    isBase?: boolean;
    name: string;
    setName: (name: string) => void;
    isDisabled?: boolean;
}

const VariableNameInput = (props: IVariableNameInputProps) => {
    const baseOrVariable = props.isBase ? "Base" : "Variable"

    return (
        <BaseInput label={baseOrVariable+ " name"}
            inputClassName={"variable-name-input-container"}
            id={"variable-name"}
            value={props.name}
            setName={props.setName}
            isDisabled={props.isDisabled}
            name={props.name} />
    );
}

export default VariableNameInput