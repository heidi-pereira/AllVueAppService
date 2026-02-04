import { useMemo } from 'react';
import moment from 'moment';
import BootStrapDateRangePicker from 'react-bootstrap-daterangepicker';
import 'bootstrap-daterangepicker/daterangepicker.css';
import { rangeCalculations } from "../helpers/DateHelper";

interface IDateRangePickerProps {
    dateOfFirstDataPoint: Date;
    dateOfLastDataPoint: Date;
    startDate?: moment.Moment;
    endDate?: moment.Moment;
    onDateChanged: (startDate: Date, endDate: Date, range: string | undefined) => void;
}

function getTitle(startDate?: moment.Moment, endDate?: moment.Moment) {
    if (startDate && endDate) {
        return startDate.format("MMMM D, YYYY") + " - " + endDate.format("MMMM D, YYYY");
    }
    return "Select dates";
}

const DateRangePicker: React.FC<IDateRangePickerProps> = (props: IDateRangePickerProps) => {
    const minDate = moment.utc(props.dateOfFirstDataPoint);
    const maxDate = moment.utc(props.dateOfLastDataPoint);
    const now = () => moment.utc(maxDate);
    const rangeLookup = useMemo(() => rangeCalculations(now, minDate, maxDate), [props.dateOfFirstDataPoint.getTime(), props.dateOfLastDataPoint.getTime()]);
    const ranges: { [r: string]: moment.Moment[] } = rangeLookup.reduce((acc, r) => {
        acc[r.uiString] = [r.start, r.end];
        return acc;
    }, {});

    const dateChanged = (event, picker) => {
        // The date picker uses local timezones and doesn't support always using UTC,
        // so we need to make a UTC date with the same date as was selected in the picker
        const startDate = new Date(Date.UTC(picker.startDate.year(), picker.startDate.month(), picker.startDate.date()));
        const endDate = new Date(Date.UTC(picker.endDate.year(), picker.endDate.month(), picker.endDate.date()));
        const range = rangeLookup.find(x => x.uiString === picker.chosenLabel);

        props.onDateChanged(startDate, endDate, range?.url);
    };

    return (
        <div className="btn-group bootstrap-select fit-width">
            <BootStrapDateRangePicker
                startDate={props.startDate}
                endDate={props.endDate}
                minDate={minDate}
                maxDate={maxDate}
                opens={"right"}
                ranges={ranges}
                onApply={dateChanged}
                containerClass="styled-dropdown"
                containerStyles={{ display: 'flex', alignItems: 'center'}}>
                <i className="material-symbols-outlined me-2">calendar_today</i>

                <button type="button" className="dropdown-toggle btn btn-secondary btn-menu styled-toggle">
                    <span>{getTitle(props.startDate, props.endDate)}</span>&nbsp;
                    <span className="bs-caret"><span className="caret" /></span>
                </button>
            </BootStrapDateRangePicker>
        </div>
    );
};

export default DateRangePicker;