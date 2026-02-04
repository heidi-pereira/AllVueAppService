import React from 'react';
import moment from "moment";

export enum ScorecardDatePickerType {
    SinglePeriod,
    RangeOfPeriods,
}

export type UnitOfTime = "w" | "M" | "Q" | "y";
export type DatePickerDisplayConfiguration = { unitOfTime: UnitOfTime, periodCount: number, pickerType: ScorecardDatePickerType, viewShowsPeriodChange: boolean };

interface IScorecardDatePickerProps {
    displayConfiguration: DatePickerDisplayConfiguration;
    periods: Date[];
    endDate: Date;
    setNewEndDate: (date: Date) => void;
}

const ScorecardDatePicker =(props: IScorecardDatePickerProps) => {
    const lookupOfUnitOfTimeToFormat: {[TKey in UnitOfTime]: string} = { "w": "DD MMM", "M": "MMM", "Q": "[Q]Q", "y": "" };

    const move = (dir: number) => {
        const displayConfig = props.displayConfiguration;
        const newEndDate = moment.utc(props.endDate).startOf(displayConfig.unitOfTime).add(dir, displayConfig.unitOfTime).endOf(displayConfig.unitOfTime).toDate();
        props.setNewEndDate(newEndDate);
    }
    
    const displayConfig = props.displayConfiguration;
    return (
        <div className="scorecardDatePickerContainer">
            <div className="scorecardDatePicker">
                <button className="btn" onClick={() => move(-1)}><i className="material-symbols-outlined">keyboard_arrow_left</i></button>
                <div className="date-container">
                    {props.periods.map(p =>
                        <div key={p.toDateString()} className="date-text">
                            {displayConfig.unitOfTime !== "y" &&
                                <span>{moment.utc(p).format(lookupOfUnitOfTimeToFormat[displayConfig.unitOfTime])}</span>
                            }
                            &nbsp;
                            <span>{moment.utc(p).format("YYYY")}</span>
                    </div>)}
                </div>
                <button className="btn" onClick={() => move(1)}><i className="material-symbols-outlined">keyboard_arrow_right</i></button>
            </div>
            <div className="scorecard-info-text">
                {displayConfig.pickerType === ScorecardDatePickerType.SinglePeriod && displayConfig.viewShowsPeriodChange &&
                    <span>Showing change from {moment.utc(props.periods[0]).add(-1, displayConfig.unitOfTime).format(lookupOfUnitOfTimeToFormat[displayConfig.unitOfTime] + " YYYY")}</span>
                }
            </div>
        </div>
    );
}
export default ScorecardDatePicker;