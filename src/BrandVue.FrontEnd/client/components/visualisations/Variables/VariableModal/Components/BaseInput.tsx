import React from "react";

interface BaseInputProps {
    inputClassName: string;
    id: string;
    label: string;
    isDisabled?: boolean;
    name: string;
    value: string;
    setName: (value: string) => void;
}

const BaseInput: React.FC<BaseInputProps> = (props) => {
    return (
        <div className={props.inputClassName}>
            <label htmlFor={props.id} className="base-input-label">{props.label}</label>
            {props.isDisabled ?
                <div>{props.name}</div>
                :
                <input type="text"
                       id={props.id}
                       name={props.id}
                       className="base-input"
                       value={props.value} onChange={(e) => props.setName(e.target.value)}
                       autoFocus={!props.isDisabled}
                       autoComplete="off"/>
            }
        </div>
    );
}

export default BaseInput;