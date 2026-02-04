import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import {useState} from "react";
import React from 'react';
import "@Styles/genericDropdown.scss";

interface IGenericDropdownProps<T> {
    buttonTitle: string,
    defaultValue?: T | undefined,
    options: T[];
    getDisplayText(option: T | undefined): string;
    selectedOption?: T | undefined;
    setSelectedOption(option: T): void;
}

const Dropdown = <T,>(props: IGenericDropdownProps<T>) => {
    const [isOpen, setIsOpen] = useState(false)

    const defaultValue = () => {
        if (props.defaultValue) {
            return (
                <DropdownItem onClick={()=>{props.setSelectedOption(props.defaultValue as T)}}>
                    <span>{props.defaultValue}</span>
                </DropdownItem>
            );
        }
        return null;
    }

    const options = () => {
        return props.options.map(option => {
            const displayText = props.getDisplayText(option);
            return (
                <DropdownItem key={`${props.buttonTitle.replace(/\s/g, "-")}-${displayText.replace(/\s/g, "-")}`} onClick={()=>{props.setSelectedOption(option)}}>
                    <span>{displayText}</span>
                </DropdownItem>
            );
        });
    }

    return (
        <ButtonDropdown isOpen={isOpen} toggle={() => {setIsOpen(!isOpen)}}>
            <DropdownToggle className={`toggle-button ${(props.selectedOption && props.defaultValue !== props.selectedOption) ? "selected" : ""}`}>
                {(props.selectedOption && props.defaultValue === props.selectedOption) ? props.buttonTitle : props.getDisplayText(props.selectedOption)}
                <i className="material-symbols-outlined">arrow_drop_down</i>
            </DropdownToggle>
            <DropdownMenu>
                {defaultValue()}
                {props.defaultValue && <DropdownItem divider />}
                {options()}
            </DropdownMenu>
        </ButtonDropdown>
    );
}

export default Dropdown