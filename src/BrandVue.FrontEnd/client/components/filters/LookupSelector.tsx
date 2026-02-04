import {Metric} from "../../metrics/metric";
import SelectPicker, {GroupOption, MultipleSelectPicker, Option} from "../SelectPicker";
import {FilterValueMapping} from "../../metrics/metricSet";
import React from "react";
import {dashSeparatorRegex} from "../../filter/metricFilterState";

const LookupSelector = (props: { metric: Metric, changeOption: (option: string) => void, activeValue: string }) => {
    const filterMappings = props.metric.filterValueMapping;
    let options: Option<FilterValueMapping>[] = [];

    if (filterMappings.find(f => f.values[0] === "*")) {
        let currentGroup: GroupOption<FilterValueMapping>;
        filterMappings.forEach(fm => {
            if (fm.values[0] === "*") {
                currentGroup = new GroupOption<FilterValueMapping>(fm.text, []);
                options.push(currentGroup);
            } else {
                currentGroup.options.push(fm);
            }
        });

    } else {
        options = props.metric.filterValueMapping;
    }

    const getSelectedOption = () => {
        var selectedOptions = getSelectedOptions();
        if (selectedOptions && selectedOptions.length > 0) {
            var matched = selectedOptions.filter(option => option.values.join(",") === props.activeValue);
            if (matched.length === 1) {
                return matched[0];
            } else {
                return selectedOptions[0];
            }
        }
        return null;
    }

    const getSelectedOptions = () => {
        let selectedOptions: FilterValueMapping[] = [];

        const activeValues = props.activeValue.split(',');

        const isSelected = (value: string) => {
            const s = value.split(',');
            return s.filter(v => activeValues.indexOf(v) !== -1).length === s.length;
        }

        options.forEach(option => {

            let selectedGroupOptions: FilterValueMapping[] = [];

            if (option instanceof GroupOption) {
                selectedGroupOptions = flattenOptions(option.options).filter(o => isSelected(o.values.join(",")));
            } else if (isSelected(option.values.join(","))) {
                selectedGroupOptions = [option];
            }

            selectedOptions = selectedOptions.concat(selectedGroupOptions);

        });

        return selectedOptions;
    }

    const onChangeMultiple = (selectedOptions: FilterValueMapping[] | null) => {
        props.changeOption(selectedOptions ? selectedOptions.map(x => x.values.join(",")).join(",") : "");
    }
    const onChangeSingle = (selectedOption: FilterValueMapping | null) => {
        props.changeOption(selectedOption ? selectedOption.values.join(",") : "");
    }

    const flattenOptions = (options: Option<FilterValueMapping>[]): FilterValueMapping[] => {
        return options.reduce((result: FilterValueMapping[], x: Option<FilterValueMapping>) => {
            if (x instanceof GroupOption) {
                return result.concat(flattenOptions(x.options));
            } else {
                return result.concat(x);
            }
        }, []);
    }

    const getValue = (x: FilterValueMapping) => x.values.join(",");
    const getLabel = (x: FilterValueMapping) => x.text;
    const hasDashSeparatedRangeValue = props.metric.filterValueMapping.some(x=>x.values.some(x=>x.split(dashSeparatorRegex).length > 1));
    return (
        <span className="me-2">
            {props.metric.filterMulti && !hasDashSeparatedRangeValue ? 
                <MultipleSelectPicker<FilterValueMapping>
                    optionValues={options}
                    getValue={getValue}
                    getLabel={getLabel}
                    onChange={onChangeMultiple}
                    activeValue={getSelectedOptions()} /> : 
                <SelectPicker<FilterValueMapping>
                    optionValues={options}
                    getValue={getValue}
                    getLabel={getLabel}
                    onChange={onChangeSingle}
                    activeValue={getSelectedOption()} />
            }
        </span>
    );
}

export default LookupSelector