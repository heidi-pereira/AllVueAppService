import {Metric} from "../../metrics/metric";
import React from "react";
import LookupSelector from "./LookupSelector";

const OptionSelector = (props: { metric: Metric, changeOption: (option: string) => void, option: string }) => {

    const renderRadioButtons = () => {
        return (
            <div className="d-block d-sm-inline-block mt-1">
                {props.metric.filterValueMapping.map(fi =>
                    <div key={fi.text} className="form-check form-check-inline">
                        <label className="label--checkbox-radio">
                            {fi.text}
                            <input
                                type="radio"
                                onChange={(e) => props.changeOption(e.target.value)}
                                className="input input--radio"
                                name={props.metric.name}
                                value={fi.values.join(",")}
                                checked={props.option === fi.values.join(",")} />
                        </label>
                    </div>
                )}
            </div>
        );
    }

    if (props.metric.filterValueMapping.length > 2 || props.metric.filterMulti) {
        return <LookupSelector metric={props.metric} activeValue={props.option} changeOption={props.changeOption} />;
    }
    return renderRadioButtons();
}

export default OptionSelector 