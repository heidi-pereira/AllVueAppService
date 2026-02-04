import React from 'react';
import SelectPicker, { MultipleSelectPicker } from "../../components/SelectPicker";

export const textInputOption = (value: string, disabled: boolean, name: string, updateProperty: (text: string) => void) => {
    const id = name.replace(' ', '-');
    return (
        <div className='option'>
            <label htmlFor={id}>{name}</label>
            <input
                id={id}
                type='text'
                className='text-option'
                disabled={disabled}
                value={value}
                onChange={e => updateProperty(e.target.value)}
            />
        </div>
    );
}

export const textAreaOption = (value: string, disabled: boolean, name: string, updateProperty: (text: string) => void, maxCharacters?: number, placeholder?: string) => {
    const id = name.replace(' ', '-');
    return (
        <div className='option'>
            <div className='spaced-label-container'>
                <label htmlFor={id}>{name}</label>
                {maxCharacters && <label>{value.length} / {maxCharacters}</label>}
            </div>
            <textarea
                id={id}
                className='text-area-option'
                disabled={disabled}
                value={value}
                onChange={e => updateProperty(e.target.value)}
                placeholder={placeholder}
            />
        </div>
    );
}

export const numberInputOption = (value: number, disabled: boolean, name: string, updateProperty: (number: number) => void) => {
    const id = name.replace(' ', '-');
    return (
        <div className='option'>
            <label htmlFor={id}>{name}</label>
            <input
                id={id}
                type='number'
                className='number-option'
                disabled={disabled}
                step={1}
                value={value}
                onChange={e => {
                    const parsed = parseInt(e.target.value);
                    if (!isNaN(parsed)) updateProperty(parsed);
                }}
            />
        </div>
    );
}

export const checkboxOption = (checked: boolean, disabled: boolean, name: string, updateProperty: (checked: boolean) => void) => {
    const id = name.replace(' ', '-');
    return (
        <div className='option-row'>
            <label htmlFor={id} className={checked ? 'bold' : ''}>{name}</label>
            <input
                id={id}
                type='checkbox'
                disabled={disabled}
                checked={checked}
                onChange={e => updateProperty(e.target.checked)}
            />
        </div>
    );
}

export function dropdownOption<T>(
    value: T | null,
    options: T[],
    disabled: boolean,
    name: string,
    getLabel: (value: T) => string,
    updateProperty: (value: T) => void)
{
    const key = `dropdown-option-${name}-${value ? getLabel(value) : ''}`;
    return (
        <div key={key} className='option'>
            <label>{name}</label>
            <SelectPicker<T>
                activeValue={value}
                optionValues={options}
                disabled={disabled}
                getValue={getLabel}
                getLabel={getLabel}
                onChange={newValue => newValue ? updateProperty(newValue) : {}}
            />
        </div>
    );
}

export function readOnlyOption(
    name: string,
    value: string | null)
{
    const key = `readonly-option-${name}-${value}`;
    return (
        <div key={key} className='option'>
            <label>{name}</label>
            <label className='readonly-option'>{value}</label>
        </div>
    );
}

export function multiselectDropdownOption<T>(
    values: T[],
    options: T[],
    disabled: boolean,
    name: string,
    getLabel: (value: T) => string,
    updateProperty: (values: T[]) => void,
    placeholder?: string)
{
    const key = `multidropdown-option-${name}-${values ? values.map(getLabel).join('-') : ''}`;
    return (
        <div key={key} className='option'>
            <label>{name}</label>
            <MultipleSelectPicker<T>
                activeValue={values}
                optionValues={options}
                disabled={disabled}
                getValue={getLabel}
                getLabel={getLabel}
                onChange={newValue => newValue ? updateProperty(newValue) : {}}
                placeholder={placeholder}
            />
        </div>
    );
}