import React from 'react';
import moment from 'moment';
import {getEndOfDayDateUtc} from "../../../../../helpers/PeriodHelper";
import {DateRangeVariableComponent} from "../../../../../../BrandVueApi";

interface IVariableComponentDateTimeRangeProps {
    component: DateRangeVariableComponent;
    setComponentForGroup(component: DateRangeVariableComponent): void;
}

const getInputValueFromDate = (date: Date) : string => moment.utc(date).format("YYYY-MM-DD");

const getDateFromInputValue = (value: string) : Date => moment.utc(value, "YYYY-MM-DD").toDate();

const VariableComponentDateTimeRange = (props: IVariableComponentDateTimeRangeProps) => {

    const onStartDateValueChange = (newMinDateValue: string) => {
        const newMinDate = getDateFromInputValue(newMinDateValue);
        const component: DateRangeVariableComponent = new DateRangeVariableComponent({
            ...props.component,
            minDate: newMinDate
        });

        props.setComponentForGroup(component);
    };

    const onEndDateValueChange = (newMaxDateValue: string) => {
        const newMaxDateStartOfDay = getDateFromInputValue(newMaxDateValue);
        const newMaxDate = getEndOfDayDateUtc(newMaxDateStartOfDay);

        const component: DateRangeVariableComponent = new DateRangeVariableComponent({
            ...props.component,
            maxDate: newMaxDate
        });

        props.setComponentForGroup(component);
    };

    const minDate = getInputValueFromDate(props.component.minDate);
    const maxDate = getInputValueFromDate(props.component.maxDate);

    return (
        <div className="date-range-selector">
            <div className="range-boundary">
                <div className="label"><p>Start Date (inclusive)</p>
                    </div>
                <div><input className="datetime-picker" value={minDate} type="date" onChange={(e) => onStartDateValueChange(e.target.value)} />
                </div>
            </div>
            <div className="range-boundary">
                <div className="label"><p>End Date (inclusive)</p>
                    </div>
                <div><input className="datetime-picker" value={maxDate} type="date" onChange={(e) => onEndDateValueChange(e.target.value)} />
                </div>
            </div>
        </div >
    );
}
export default VariableComponentDateTimeRange;