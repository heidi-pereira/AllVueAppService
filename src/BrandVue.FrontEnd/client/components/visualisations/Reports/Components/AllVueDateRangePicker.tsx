import moment from "moment";
import { CustomDateRange, PeriodType, ReportOverTimeConfiguration } from "../../../../BrandVueApi";
import style from "./AllVueDateRangePicker.module.less";
import { useEffect, useMemo, useState } from "react";
import { customRangeCalculation } from "client/components/helpers/DateHelper";
import { DropdownToggle, DropdownMenu, DropdownItem, Dropdown } from 'reactstrap';
import SelectPicker from "client/components/SelectPicker";
import { ApplicationConfiguration } from "client/ApplicationConfiguration";
import { DateRange } from 'react-date-range';
import { enGB } from 'date-fns/locale';
import 'react-date-range/dist/styles.css';
import 'react-date-range/dist/theme/default.css';
import { customDateRangeToText, getDateRangeLookup, getFullPeriodsWithin } from "client/components/helpers/SurveyVueUtils";
import Tooltip from "client/components/Tooltip";
import WarningBanner from "client/components/visualisations/WarningBanner";
import { MixPanel } from "client/components/mixpanel/MixPanel";

interface IAllVueDateRangePickerProps {
    applicationConfiguration: ApplicationConfiguration;
    overtimeConfig: ReportOverTimeConfiguration | undefined;
    dropdownTitle: string;
    onRangeSelected: (range: string, start: Date, end: Date) => void;
    onCustomRangeSelected: (customRange: CustomDateRange, start: Date, end: Date) => void;
    onSavedRangeDeleted?: (customRange: CustomDateRange) => void;
    startDate?: Date;
    endDate?: Date;
    onDatesSelected?: (start: Date, end: Date) => void;
    disabled?: boolean;
}

function forceUtcNoon(date: Date) {
    // Set hour to 12:00 UTC to ensure it won’t cross local date boundaries
    return moment.utc(date).set({ hour: 12, minute: 0, second: 0, millisecond: 0 }).toDate();
}

function getDefaultCalendarSelection(startDate: Date | undefined, endDate: Date | undefined, applicationConfiguration: ApplicationConfiguration) {
    return {
        startDate: forceUtcNoon(startDate ?? applicationConfiguration.dateOfLastDataPoint),
        endDate: forceUtcNoon(endDate ?? applicationConfiguration.dateOfLastDataPoint),
        key: 'selection'
    };
}

function dateToTrackedString(date: Date): string {
    return date.toISOString().substring(0, 10);
}

enum PickerMode {
    None,
    CustomRange
}

const preferredInitialPeriod: PeriodType[] = [
    PeriodType.Month,
    PeriodType.Week,
    PeriodType.Day,
];

const AllVueDateRangePicker = (props: IAllVueDateRangePickerProps) => {
    const availablePeriodTypes = getFullPeriodsWithin(props.applicationConfiguration.dateOfFirstDataPoint, props.applicationConfiguration.dateOfLastDataPoint);
    const initialPeriodType = preferredInitialPeriod.find(p => availablePeriodTypes.includes(p)) || availablePeriodTypes[0];

    const [isOpen, setIsOpen] = useState(false);
    const [mode, setMode] = useState<PickerMode>(PickerMode.None);
    const [numberOfPeriods, setNumberOfPeriods] = useState(1);
    const [periodType, setPeriodType] = useState(initialPeriodType);
    const [calendarSelection, setCalendarSelection] = useState(getDefaultCalendarSelection(props.startDate, props.endDate, props.applicationConfiguration));

    useEffect(() => {
        if (isOpen) {
            setMode(PickerMode.None);
            setCalendarSelection(getDefaultCalendarSelection(props.startDate, props.endDate, props.applicationConfiguration));
        }
    }, [isOpen]);

    useEffect(() => {
        if (mode === PickerMode.CustomRange) {
            setNumberOfPeriods(props.overtimeConfig?.customRange?.numberOfPeriods ?? 1);
            setPeriodType(props.overtimeConfig?.customRange?.periodType ?? initialPeriodType);
        }
    }, [mode]);

    const ranges = useMemo(() => getDateRangeLookup(props.applicationConfiguration), [
        props.applicationConfiguration.dateOfFirstDataPoint.getTime(),
        props.applicationConfiguration.dateOfLastDataPoint.getTime()
    ]);

    const getPeriodLabel = (periodType: PeriodType) => {
        if (numberOfPeriods === 1) {
            return periodType.toLowerCase();
        }
        return periodType.toLowerCase() + "s";
    };

    const handleCustomSubmit = () => {
        if (numberOfPeriods > 0) {
            const customRange = new CustomDateRange({
                numberOfPeriods: numberOfPeriods,
                periodType: periodType,
            });
            submitCustomRange(customRange, "datePickerCustomRangeSelected");
            setMode(PickerMode.None);
            setIsOpen(false);
        }
    };

    const submitCustomRange = (customRange: CustomDateRange, trackedEvent: "datePickerCustomRangeSelected" | "datePickerSavedRangeSelected") => {
        const {start, end} = customRangeCalculation(customRange, props.applicationConfiguration);
        const startDate = start.toDate();
        const endDate = end.toDate();
        props.onCustomRangeSelected(customRange, startDate, endDate);

        MixPanel.trackWithContext(trackedEvent, customDateRangeToText(customRange), {
            DateStart: dateToTrackedString(startDate),
            DateEnd: dateToTrackedString(endDate)
        });
    }

    const deleteSavedRange = (savedRange: CustomDateRange) => {
        if (props.onSavedRangeDeleted) {
            props.onSavedRangeDeleted(savedRange);

            const {start, end} = customRangeCalculation(savedRange, props.applicationConfiguration);
            MixPanel.trackWithContext("datePickerSavedRangeDeleted", customDateRangeToText(savedRange), {
                DateStart: dateToTrackedString(start.toDate()),
                DateEnd: dateToTrackedString(end.toDate())
            });
        }
    }

    //react-date-range picks a date in local time, but the actual dates are UTC, so we create a UTC date with the same year, month, and day
    const convertDateToUTC = (date: Date) => new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));

    const handleCalendarSubmit = () => {
        if (props.onDatesSelected && calendarSelection.startDate && calendarSelection.endDate) {
            const startDate = convertDateToUTC(calendarSelection.startDate);
            const endDate = convertDateToUTC(calendarSelection.endDate);
            props.onDatesSelected(startDate, endDate);
            setMode(PickerMode.None);
            setIsOpen(false);

            MixPanel.track("datePickerCalendarDatesSelected", {
                DateStart: dateToTrackedString(startDate),
                DateEnd: dateToTrackedString(endDate)
            });
        }
    }

    const getRangeDropdownList = () => {
        return (
            <div className={style.listItems}>
                <div className={style.section}>
                    <div className={style.header}>
                        <span>Last data collected</span>
                        <Tooltip placement="top" title="All presets below are relative to available data">
                            <i className="material-symbols-outlined no-symbol-fill">info</i>
                        </Tooltip>
                    </div>
                    <div className={style.date}>
                        {props.applicationConfiguration.dateOfLastDataPoint.toLocaleDateString('en-GB', {
                            day: 'numeric',
                            month: 'short',
                            year: '2-digit'
                        })}
                    </div>
                </div>
                <div className={style.section}>
                    <div className={style.header}>Presets</div>
                    <div>
                        {ranges.map(r => (
                            <DropdownItem
                                key={r.url}
                                onClick={() => {
                                    const startDate = r.start.toDate();
                                    const endDate = r.end.toDate();
                                    MixPanel.trackWithContext("datePickerRangeSelected", r.name, {
                                        DateStart: dateToTrackedString(startDate),
                                        DateEnd: dateToTrackedString(endDate)
                                    });
                                    props.onRangeSelected(r.url, startDate, endDate);
                                }}
                                className={style.dropdownItem}
                            >
                                <div className={style.dateRangeItem}>
                                    <span>{r.name}</span>
                                    <span className={style.dates}>{r.dateUiString}</span>
                                </div>
                            </DropdownItem>
                        ))}
                    </div>
                </div>
                <div className={style.section}>
                    <div
                        className={`dropdown-item ${style.dropdownItem}`}
                        style={{ cursor: "pointer" }}
                        onClick={() => setMode(PickerMode.CustomRange)}
                    >
                        Custom range...
                    </div>
                </div>
            </div>
        );
    };

    const getCustomRangeInput = () => {
        const savedRanges = props.overtimeConfig?.savedRanges ?? [];
        //this is only set/used in report settings
        const canSaveRanges = props.onSavedRangeDeleted != undefined;

        const exceededMaxSavedRanges = canSaveRanges && savedRanges.length >= 5;
        return (
            <div className={style.listItems}>
                {!exceededMaxSavedRanges &&
                <>
                    <div className={style.section}>
                        <div className={style.header}>Custom range</div>
                        <div>Set a custom range in the past</div>
                        <div className={style.customDateRangePicker}>
                            <div className={style.dateInputs}>
                                <div>Last</div>
                                <input
                                    type="number"
                                    className={style.numberInput}
                                    min={1}
                                    value={numberOfPeriods}
                                    onChange={e => setNumberOfPeriods(Number(e.target.value))}
                                />
                                <SelectPicker<PeriodType>
                                    onChange={(value) => { if (value) setPeriodType(value) }}
                                    activeValue={periodType}
                                    optionValues={getFullPeriodsWithin(props.applicationConfiguration.dateOfFirstDataPoint, props.applicationConfiguration.dateOfLastDataPoint)}
                                    getLabel={(x) => getPeriodLabel(x)}
                                    getValue={(x) => x}
                                />
                            </div>
                        </div>
                    </div>
                    <div className={style.section}>
                        <div className={style.buttons}>
                            <button className={`${style.button} secondary-button`} onClick={() => setMode(PickerMode.None)}>Cancel</button>
                            <button className={`${style.button} primary-button`} onClick={handleCustomSubmit}>Apply</button>
                        </div>
                    </div>
                </>
                }
                {exceededMaxSavedRanges &&
                    <div className={style.section}>
                        <WarningBanner materialIconName="warning" message="You can only save up to 5 ranges. Remove one to add a new range." />
                    </div>
                }
                {savedRanges.length > 0 &&
                    <div className={style.section}>
                        <div className={style.header}>Saved ranges</div>
                        {savedRanges.map(r => {
                            const { dateUiString } = customRangeCalculation(r, props.applicationConfiguration);
                            return (
                                <DropdownItem
                                    key={`${r.numberOfPeriods}-${r.periodType}`}
                                    onClick={() => submitCustomRange(r, "datePickerSavedRangeSelected")}
                                    className={style.dropdownItem}
                                >
                                    <div className={style.dateRangeItem}>
                                        <span>{customDateRangeToText(r)}</span>
                                        <span className={style.dates}>
                                            <span>{dateUiString}</span>
                                            {canSaveRanges &&
                                                <i
                                                    className="material-symbols-outlined"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        deleteSavedRange(r);
                                                    }}
                                                >
                                                    close
                                                </i>
                                            }
                                        </span>
                                    </div>
                                </DropdownItem>
                            )
                        })}
                    </div>
                }
            </div>
        );
    };

    const getCalendarInput = () => {
        return (
            <div className={style.customDateRangePicker}>
                <DateRange
                    ranges={[calendarSelection]}
                    onChange={item => setCalendarSelection(item.selection)}
                    minDate={props.applicationConfiguration.dateOfFirstDataPoint}
                    maxDate={props.applicationConfiguration.dateOfLastDataPoint}
                    months={2}
                    direction="horizontal"
                    locale={enGB}
                />
                <div className={style.buttons}>
                    <button className={`${style.button} secondary-button`} onClick={() => setIsOpen(false)}>Cancel</button>
                    <button className={`${style.button} primary-button`} onClick={handleCalendarSubmit}>Apply</button>
                </div>
            </div>
        );
    };

    return (
        <Dropdown
            isOpen={isOpen}
            toggle={() => { setIsOpen(!isOpen); setMode(PickerMode.None); }}
            className="styled-dropdown"
        >
            <DropdownToggle caret className="btn-menu styled-toggle" disabled={props.disabled}>
                {props.dropdownTitle}
            </DropdownToggle>
            <DropdownMenu>
                <div className={style.twoPaneDropdownMenu}>
                    <div className={style.leftPane}>
                        {getRangeDropdownList()}
                    </div>
                    {mode === PickerMode.CustomRange && (
                        <div className={style.rightPane}>
                            {getCustomRangeInput()}
                        </div>
                    )}
                    {mode !== PickerMode.CustomRange && props.onDatesSelected && (
                        <div className={style.rightPane}>
                            {getCalendarInput()}
                        </div>
                    )}
                </div>
            </DropdownMenu>
        </Dropdown>
    );
};

export default AllVueDateRangePicker;
