import * as PageHandler from "../PageHandler";
import { dsession } from "../../dsession";
import React from "react";
import ScorecardPeriodSelector from "./ScorecardPeriodSelector";
import ScorecardDatePicker, {
    DatePickerDisplayConfiguration,
    ScorecardDatePickerType
} from "./ScorecardDatePicker";
import { CompletePeriod } from "../../helpers/CompletePeriod";
import {QueryStringParamNames, useWriteVueQueryParams} from "../helpers/UrlHelper";
import * as moment from 'moment';
import { ViewTypeEnum } from "../helpers/ViewTypeHelper";
import { IAverageDescriptor, MakeUpTo } from "../../BrandVueApi";
import { getNumberOfPeriodsToShow, getMakeUpToUnitOfTime } from "../helpers/PeriodHelper";
import { useEffect, useState } from "react";
import { ApplicationConfiguration } from "../../ApplicationConfiguration";
import { useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { useAppSelector } from "../../state/store";
import { selectTimeSelection } from "../../state/timeSelectionStateSelectors";

interface IScorecardFiltersProps {
    session: dsession;
    applicationConfiguration: ApplicationConfiguration;
    pageHandler: PageHandler.PageHandler;
    averages: IAverageDescriptor[];
}

const ScorecardFilters = (props: IScorecardFiltersProps) => {
    const average= useAppSelector(selectTimeSelection).scorecardAverage;
    const viewType = props.session.coreViewType;
    const datePickerConfig = getDatePickerConfiguration(average, viewType);
    const dateOfLastDataPoint = props.applicationConfiguration.dateOfLastDataPoint
    const [searchParams] = useSearchParams();
    const cappedEndDate = readEndDateOrCappedDefault(datePickerConfig, average, dateOfLastDataPoint,searchParams.get(QueryStringParamNames.end));
    const periods = calculatePeriods(cappedEndDate, datePickerConfig);
    const [datePickerDisplayConfiguration, setDatePickerDisplayConfiguration] = useState<DatePickerDisplayConfiguration>(datePickerConfig);
    const [endDate, setEndDate] = useState<Date>(cappedEndDate);
    const { setQueryParameters } = useWriteVueQueryParams(useNavigate(), useLocation());
    
    const updateFilterEnd = (end: Date) => {
        props.session.activeView.curatedFilters.setEndDate(end);
    }

    useEffect(() => {
        setNewEndDate(endDate, average, datePickerDisplayConfiguration)
    }, [endDate, average, datePickerDisplayConfiguration])

    const setNewEndDate = (date: Date, average: IAverageDescriptor, displayConfig: DatePickerDisplayConfiguration) => {
        const dateOfLastDataPoint = props.applicationConfiguration.dateOfLastDataPoint
        const cappedDate = capEndDate(date, average, dateOfLastDataPoint);
        if (JSON.stringify(cappedDate) !== JSON.stringify(endDate))
            setEndDate(cappedDate);
        if ( JSON.stringify(datePickerDisplayConfiguration) !=  JSON.stringify(displayConfig))
            setDatePickerDisplayConfiguration(displayConfig);
        const periods = calculatePeriods(cappedDate, displayConfig);
        const filterEndDate = displayConfig.pickerType === ScorecardDatePickerType.SinglePeriod ? periods[0] : periods[periods.length - 1];
        updateFilterEnd(filterEndDate);
        if (!props.applicationConfiguration.hasLoadedData) return;
        setQueryParameters([{ name: QueryStringParamNames.end, value: moment.utc(props.session.activeView.curatedFilters.endDate).format("YYYY-MM-DD") }]);
    }

    const handlePeriodChange = (selectedPeriod: IAverageDescriptor) => {
        const newDatePickerDisplayConfiguration = getDatePickerConfiguration(selectedPeriod, viewType);
        setNewEndDate(endDate, selectedPeriod, newDatePickerDisplayConfiguration);
    }

    if (!props.averages)
        return <></>;
    const dateChangeHandler = (newDate: Date) => setNewEndDate(newDate, average, datePickerDisplayConfiguration);
    return (
        <React.Fragment>
            <div className="not-exported scorecard-filter-row">
                <ScorecardPeriodSelector session={props.session} pageHandler={props.session.pageHandler} averages={props.averages} handlePeriodChange={handlePeriodChange}/>
                <ScorecardDatePicker displayConfiguration={datePickerDisplayConfiguration} periods={periods} endDate={endDate} setNewEndDate={dateChangeHandler} />
            </div>
        </React.Fragment>);
}
export default ScorecardFilters


const getDatePickerConfiguration = (period: IAverageDescriptor, viewType: number): DatePickerDisplayConfiguration => {
    let pickerType = ScorecardDatePickerType.SinglePeriod;
    const viewShowsPeriodChange = viewType === ViewTypeEnum.PerformanceVsPeers;
    let periodCount = 1;
    if (period) {
        const unitOfTime = getMakeUpToUnitOfTime(period.makeUpTo);

        if (viewType === ViewTypeEnum.Performance) {
            pickerType = ScorecardDatePickerType.RangeOfPeriods;
            periodCount = getNumberOfPeriodsToShow(period);
        }

        return { unitOfTime: unitOfTime, periodCount: periodCount, pickerType: pickerType, viewShowsPeriodChange: viewShowsPeriodChange };
    }
    return { unitOfTime: getMakeUpToUnitOfTime(MakeUpTo.MonthEnd), periodCount: periodCount, pickerType: pickerType, viewShowsPeriodChange: viewShowsPeriodChange };
}

const readEndDateOrCappedDefault = (displayConfig: DatePickerDisplayConfiguration, average: IAverageDescriptor, dateOfLastDataPoint: Date, endParam): Date => {
    let endDate: Date;
    if (endParam) {
        endDate = moment.utc(endParam).toDate();
    } else {
        const currentDate = moment.utc(dateOfLastDataPoint)
        const endOfCurrentPeriod = currentDate.clone().endOf(displayConfig.unitOfTime);
        if (currentDate.isSame(endOfCurrentPeriod, "day")) {
            endDate = endOfCurrentPeriod.toDate();
        } else {
            const startOfCurrentPeriod = currentDate.clone().startOf(displayConfig.unitOfTime);
            const endOfPreviousPeriod = startOfCurrentPeriod.subtract(1, displayConfig.unitOfTime).endOf(displayConfig.unitOfTime);
            endDate = endOfPreviousPeriod.toDate();
        }
    }
    return capEndDate(endDate, average, dateOfLastDataPoint);
}

const calculatePeriods = (endDate: Date, displayConfig: DatePickerDisplayConfiguration): Date[] => {

    const np: Date[] = [];
    for (let p = displayConfig.periodCount - 1; p >= 0; p--) {
        const dt = moment.utc(endDate)
            .startOf(displayConfig.unitOfTime)
            .add(-p, displayConfig.unitOfTime)
            .endOf(displayConfig.unitOfTime)
            .startOf('D').toDate();
        np.push(dt);
    }
    return np;
}

const capEndDate = (endDate: Date, average: IAverageDescriptor, dateOfLastDataPoint: Date): Date => {
    let maxAllowedDate = dateOfLastDataPoint;
    if (average) {
        const endOfLastCompletedPeriod = CompletePeriod.getLastDayInLastCompletePeriod(dateOfLastDataPoint, average.makeUpTo);
        const endOfCurrentYear = CompletePeriod.getLastDayInCurrentPeriod(dateOfLastDataPoint, average.makeUpTo);
        maxAllowedDate = average.makeUpTo === MakeUpTo.CalendarYearEnd ? endOfCurrentYear : endOfLastCompletedPeriod;
    }

    return moment.min(moment.utc(endDate), moment.utc(maxAllowedDate)).toDate();
}