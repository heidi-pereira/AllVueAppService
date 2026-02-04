import {MultipleSelectPicker} from "../SelectPicker";
import {Metric} from "../../metrics/metric";
import React from "react";

const MultiMetricSelector = (props: { metrics: Metric[], changeOption: (option: string) => void, option: string }) => {
    const sharedStart = (array: string[]) => {
        const sorted = array.concat().sort();
        const a1 = sorted[0];
        const a2 = sorted[sorted.length - 1];
        let i = 0;
        while (i < a1.length && a1.charAt(i) === a2.charAt(i)) i++;
        return a1.substring(0, i);
    }

    const onChange = (selectedOptions: Metric[] | null) => {
        if (selectedOptions) {
            props.changeOption(selectedOptions.map(x => x.name).join(","));
        }
    }

    const sharedStartLength = sharedStart(props.metrics.map(m => m.name)).length;

    const selected = props.option.split(",");

    const selectedOptions = props.metrics.filter(x => selected.indexOf(x.name) > -1);

    return (
        <span className="me-2">
            <MultipleSelectPicker<Metric>
                onChange={onChange}
                activeValue={selectedOptions}
                getLabel={(x) => x.name.substring(sharedStartLength)}
                getValue={(x) => x.name}
                optionValues={props.metrics}
                className="metricDropDown" />
        </span>
    );
}
export default MultiMetricSelector