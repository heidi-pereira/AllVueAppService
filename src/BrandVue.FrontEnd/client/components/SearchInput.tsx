import React from "react";
import { AriaRoles } from "../helpers/ReactTestingLibraryHelpers";
import { useEffect } from "react";

interface ISearchInputProps {
    id: string;
    name?: string;
    onChange(text: string): void;
    className?: string;
    autoFocus?: boolean;
    placeholder?: string;
    ariaLabel?: string;
    text: string;
}

const SearchInput: React.FunctionComponent<ISearchInputProps> = (props: ISearchInputProps) => {

    const onChange = (text: string): void => {
        props.onChange(text);
    }

    useEffect(() => {
    },
        []);

    return (
        <ControlledSearchInput
            text={props.text}
            onChange={onChange}
            id={props.id}
            name={props.name}
            className={props.className}
            autoFocus={props.autoFocus}
            placeholder={props.placeholder}
            ariaLabel={props.ariaLabel}
        />
    );
}

interface IControlledSearchInputProps extends ISearchInputProps {
    text: string;
}

export const ControlledSearchInput = (props: IControlledSearchInputProps) => {

    const onChange = (event: React.ChangeEvent<HTMLInputElement>): void => {
        props.onChange(event.target.value);
    }

    const clearText = () => {
        props.onChange("");
    }

    const hasText = props.text.length > 0;

    return (
        <div className={"search " + props.className}>
            <input role={AriaRoles.SEARCHBAR} type="search" id={props.id} className="search-input" name={props.name} placeholder={props.placeholder ?? "Search"} autoComplete="off" onChange={onChange} value={props.text} autoFocus={props.autoFocus} aria-label={props.ariaLabel ? props.ariaLabel : ""} />
            {!hasText &&
                <label htmlFor={props.id} className="material-symbols-outlined search-icon">search</label>
            }
            {hasText &&
                <label htmlFor={props.id} className="material-symbols-outlined search-icon clear-icon" onClick={clearText}>clear</label>
            }

        </div>
    );
}

export default SearchInput;