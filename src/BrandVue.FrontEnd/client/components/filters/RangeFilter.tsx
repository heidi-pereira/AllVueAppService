import SelectPicker, {LabelValue} from "../SelectPicker";
import {Metric} from "../../metrics/metric";
import {useEffect, useState} from "react";
import { Input } from 'reactstrap';
import React from "react";

export enum RangeTypes {
    GreaterOrEqual = ">=",
    LessOrEqual = "<=",
    Equal = "=",
    Between = "<>"
}

const RangeFilter = (props: { metric: Metric, changeOption: (min: number, max: number) => void, value:number[] }) =>{
    const [min, setMin] = useState<number | null>(null);
    const [max, setMax] = useState<number | null>(null);
    const [type, setType] = useState<string>(RangeTypes.Between);
    const [showWarning, setShowWarning] = useState<boolean>(false);

    useEffect(() => {
        // Need to handle "-1" when splitting so ignore first character when finding index of "-" to split
        if (props.value.length) {
            const minPart = props.value[0];
            const maxPart = props.value[1];
            const type = (minPart === maxPart) ?
                RangeTypes.Equal :
                (minPart === -1) ?
                    RangeTypes.LessOrEqual :
                    (maxPart === -1) ?
                        RangeTypes.GreaterOrEqual :
                        RangeTypes.Between;
            setMin(minPart)
            setMax(maxPart)
            setType(type)
        } else {
            setMin(null)
            setMax(null)
            setType(RangeTypes.Between)
        }
    }, [props.value])

    const onChange = () => {
        if (min || max) {
            let minimum: number|null = null;
            let maximum: number|null = null;
            switch (type) {
                case RangeTypes.GreaterOrEqual:
                    minimum = min;
                    maximum = -1;
                    break;
                case RangeTypes.LessOrEqual:
                    minimum = -1;
                    maximum = max;
                    break;
                case RangeTypes.Between:
                    minimum = min;
                    maximum = max;
                    break;
                case RangeTypes.Equal:
                    minimum = min;
                    maximum = minimum;
                    break;
            }
            if(minimum !== null && maximum !== null) {
                props.changeOption(minimum, maximum);
            }
        }
    }

    useEffect(() => {
        // Avoid repeatedly calling when scrolling through numbers, but don't wait so long that they click apply before this fires
        const delayDebounceFn = setTimeout(() => {
            onChange();
        }, 250)

        return () => clearTimeout(delayDebounceFn);
    }, [min, max, type])

    useEffect(() => {
        if (!min || !max || min <= max) {
            setShowWarning(false);
        } else {
            const delayDebounceFn = setTimeout(() => {
                setShowWarning(min > max);
            }, 1000);
            return () => clearTimeout(delayDebounceFn);
        }
    }, [min, max]);

    const changeType = (selectedOption: LabelValue | null) => {
        setType(selectedOption ? selectedOption.value : RangeTypes.Between);
    };

    const handleKeyPress = (e) => {
        const charCode = e.keyCode;
        const isNumericKey = ((charCode >= 48 && charCode <= 57) || (charCode >= 96 && charCode <= 105));
        const isControlKey = charCode <= 40;
        if (!(isNumericKey || isControlKey)) {
            e.preventDefault();
        }
    };

    const allOptions = [
        { label: "Between (inclusive)", value: RangeTypes.Between } as LabelValue,
        { label: "Greater than or equal to", value: RangeTypes.GreaterOrEqual } as LabelValue,
        { label: "Less than or equal to", value: RangeTypes.LessOrEqual } as LabelValue,
        { label: "Equal to", value: RangeTypes.Equal } as LabelValue
    ];

    return (
        <div>
            <SelectPicker<LabelValue>
                onChange={changeType}
                activeValue={allOptions.find(x => x.value === type) || allOptions[0]}
                optionValues={allOptions}
                className="btn-menu"
                title="Range type selector"/>


            <span className="d-inline-block ms-2 align-top position-relative">
                <span className={`filterWarning text-warning small ${showWarning ? 'visible' : 'hidden'}`}>
                    Min value should be less than or equal to max value
                </span>
                <span className="d-block">
                    {(type === RangeTypes.GreaterOrEqual || type === RangeTypes.Between || type === RangeTypes.Equal) &&
                        <Input
                            className="d-inline"
                            style={{ width: "100px" }}
                            value={min?.toString() ?? ""}
                            onChange={(e) => setMin(e.target.value ? parseInt(e.target.value) : null)}
                            type="number"
                            min="0"
                            max="99"
                            onKeyDown={handleKeyPress} aria-label={"Min range value"} />
                    }
                    {type === "<>" &&
                        <span className="ms-2">and</span>
                    }
                    {(type === RangeTypes.LessOrEqual || type === RangeTypes.Between) &&
                        <Input
                            className="ms-2 d-inline"
                            style={{ width: "100px" }}
                            value={max?.toString() ?? ""}
                            onChange={(e) => setMax(e.target.value ? parseInt(e.target.value) : null)}
                            type="number"
                            min="0"
                            max="99"
                            onKeyDown={handleKeyPress}
                            aria-label={"Max range value"} />
                    }
                </span>
                <span className="d-block small filterInfo">Values range from {props.metric.min} to {props.metric.max}</span>
            </span>
        </div>
    );
}

export default RangeFilter