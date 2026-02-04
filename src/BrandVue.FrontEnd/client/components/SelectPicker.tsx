import Select from "react-select";
import React from "react";
import { css } from "@emotion/css";

export class LabelValue {
    label: string;
    value: string;

    public static instanceOf(obj: any): obj is LabelValue {
        return "label" in obj && "value" in obj;
    }
}
export type Option<T> = T | GroupOption<T>;
export class GroupOption<T> {
    label: string;
    options: Option<T>[];

    constructor(label: string, options: Option<T>[]) {
        this.label = label;
        this.options = options;
    }
}

export interface ISelectPickerProps<T, TU extends (T | T[])> {
    onChange: (value: TU | null) => void;
    activeValue: TU | null;
    optionValues: Option<T>[];
    id?: string;
    getValue?: (option: T) => string;
    getLabel?: (option: T) => string;
    className?: string;
    title?: string;
    disabled?: boolean;
    placeholder?: string;
    allowMultipleSelections?: boolean;
}

class InternalOption<T> {
    label: string;
    value: string;
    obj: T;
}

 export interface ISelectPickerState<TU> {
    activeValue: TU | null | undefined
 }
function getDerivedStateFromPropsHelper<T, TU extends (T | T[])>(
    nextProps: ISelectPickerProps<T, TU>,
    prevState: ISelectPickerState<TU>
  ): ISelectPickerState<TU> | null {
    if (nextProps.activeValue !== prevState.activeValue) {
      return { activeValue: nextProps.activeValue };
    }
    return null;
  }

export abstract class BaseSelectPicker<T, TU extends (T | T[])> extends React.Component<ISelectPickerProps<T, TU>, ISelectPickerState<TU>> {
    protected constructor(props) {
        super(props);
        this.state = {
            activeValue: props.activeValue
        }

        this.internalOnChange = this.internalOnChange.bind(this);
    }

    static getDerivedStateFromProps(nextProps: any, prevState: any): ISelectPickerState<any> | null {
        return getDerivedStateFromPropsHelper(nextProps, prevState);
    }

    abstract isMultiple: () => boolean;
    abstract onChange(value: InternalOption<T> | InternalOption<T>[] | null | undefined): void;
    abstract onSelectGroup(props, event): void;

    getValue(obj: T) {
        if (this.props.getValue === undefined) {
            if (LabelValue.instanceOf(obj)) {
                return obj.value;
            } else if (typeof obj === "string") {
                return obj;
            } else {
                throw new Error("Type of Option not supported, you need to supply getLabel and getValue functions");
            }
        } else {
            return this.props.getValue(obj);
        }
    }

    getLabel(obj: T) {
        if (this.props.getLabel === undefined) {
            if (LabelValue.instanceOf(obj)) {
                return obj.label;
            } else if (typeof obj === "string") {
                return obj;
            } else {
                throw new Error("Type of Option not supported, you need to supply getLabel and getValue functions");
            }
        } else {
            return this.props.getLabel(obj);
        }
    }

    internalOnChange(options) {
        if (
            options instanceof GroupOption ||
            (options instanceof Array && options.some(x => x instanceof GroupOption))
            ) {
            throw new Error("SelectPecker internal error, GroupOptions should not leach outside the component");
        }

        this.onChange(options as InternalOption<T> | InternalOption<T>[] | null | undefined);
    }
    getOnSelectGroup(props: any) {
        return e => this.onSelectGroup(props, e);
    }

    render() {
        const classes = ["select-picker"];
        if (this.props.className) {
            classes.push(this.props.className);
        }

        const groupHeading = (props: any) => {
            const { className, cx, getStyles, theme, selectProps, ...cleanProps } = props;

            return <div
                className={cx(
                            css(getStyles('groupHeading', { theme, ...cleanProps })),
                            { 'group-heading-multiple': this.isMultiple()},
                            className)}
                {...cleanProps}
                onClick={this.getOnSelectGroup(props)} />;
        };

        return (
            <Select
                aria-label={this.props.title ? "Option drop down for: " + this.props.title : "Option drop down"}
                classNamePrefix={"react-select"}
                components={{ GroupHeading: groupHeading }}
                className={classes.join(" ")}
                value={this.getInternalOptionsFromValues(this.state.activeValue)}
                options={this.toInternalOptions(this.props.optionValues)}
                onChange={this.internalOnChange}
                isDisabled={this.props.disabled || false}
                isMulti={this.isMultiple()}
                closeMenuOnSelect={!this.isMultiple()}
                placeholder={this.props.placeholder}
                inputId={this.props.title ? "Selected-" + this.props.title : "Selected"}
            />
        );
    }

    private toInternalOptions(options: Option<T>[]) {
        return options.map(x => this.toInternalOption(x));
    }

    private toInternalOption(option: Option<T>) {
        if (option instanceof GroupOption) {
            return new GroupOption<InternalOption<T>>(option.label, this.toInternalOptions(option.options));
        } else {
            return this.toSingleInternalOption(option);
        }
    }

    private toSingleInternalOption(obj: T) {
        return {
            value: this.getValue(obj),
            label: this.getLabel(obj),
            obj: obj
        } as InternalOption<T>;
    }

    protected getInternalOptionsListFromValues(value: T | T[] | null | undefined): InternalOption<T>[] {
        var result = this.getInternalOptionsFromValues(value);
        if (result instanceof Array) {
            return result;
        } else if (result) {
            return [result];
        } else {
            return [];
        }
    }

    protected getInternalOptionsFromValues(value: T | T[] | null | undefined) {
        if (!value) {
            return null;
        } else if (value instanceof Array) {
            return this.flattenOptionsFromProps().filter(x => value.some(y => this.getValue(y) === this.getValue(x))).map(x => this.toSingleInternalOption(x));
        } else {
            var optionFound = this.flattenOptionsFromProps().find(x => this.getValue(value) === this.getValue(x));
            return optionFound ? this.toSingleInternalOption(optionFound) : null;
        }
    }

    private flattenOptionsFromProps(): T[] {
        return this.flattenOptions(this.props.optionValues);
    }

    private flattenOptions(options: Option<T>[]): T[] {
        return options.reduce((result: T[], x: Option<T>) => {
            if (x instanceof GroupOption) {
                return result.concat(this.flattenOptions(x.options));
            } else {
                return result.concat(x);
            }
        }, []);
    }
}

export default class SelectPicker<T> extends BaseSelectPicker<T, T> {

    constructor(props: ISelectPickerProps<T, T>) {
        super(props);
        this.onChange = this.onChange.bind(this);
        this.onSelectGroup = this.onSelectGroup.bind(this);
    }

    isMultiple = () => false;

    onChange(selectedOption: InternalOption<T> | InternalOption<T>[] | null | undefined): void {
        let newActiveValue: T | null;
        if (selectedOption instanceof Array) {
            newActiveValue = selectedOption.length > 0 ? selectedOption[0].obj : null;
        } else {
            newActiveValue = selectedOption ? selectedOption.obj : null;
        }
        this.setState({ activeValue: newActiveValue });
        this.props.onChange(newActiveValue);
    }

    onSelectGroup(props: any, event: any): void {
    }
}

export class MultipleSelectPicker<T> extends BaseSelectPicker<T, T[]> {

    constructor(props: ISelectPickerProps<T, T[]>) {
        super(props);
        this.onChange = this.onChange.bind(this);
        this.onSelectGroup = this.onSelectGroup.bind(this);
    }

    isMultiple = () => this.props.allowMultipleSelections ?? true;

    onChange(value: InternalOption<T> | InternalOption<T>[] | null | undefined): void {
        let newActiveValue: T[];
        if (value instanceof Array) {
            newActiveValue = value.map(x => x.obj);
        } else {
            newActiveValue = value !== null && value !== undefined ? [value.obj] : [];
        }
        this.setState({ activeValue: newActiveValue });
        this.props.onChange(newActiveValue);
    }

    onSelectGroup(props: any, event: any): void {
        const groupName = props.children;
        const group = props.selectProps.options.find(o => o.label === groupName);
        if (group && group.options) {
            const newItems = this.getInternalOptionsListFromValues(this.state.activeValue).concat(group.options);
            const newItemsUnique = Array.from(new Set(newItems));
            this.onChange(newItemsUnique);
        }
    }
}