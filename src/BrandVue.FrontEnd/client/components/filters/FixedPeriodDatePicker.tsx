import React from "react";
import moment from "moment";
import { PageHandler } from "../PageHandler";
import * as BrandVueApi from "../../BrandVueApi";
import { IAverageDescriptor } from "../../BrandVueApi";
import { CompletePeriod } from "../../helpers/CompletePeriod";
import FixedPeriodDaySelector from "./FixedPeriodDaySelector";
import FixedPeriodAverageSelector from "./FixedPeriodAverageSelector";
import FixedPeriodSelector from "./FixedPeriodSelector";
import { IDropdownToggleAttributes } from "../helpers/DropdownToggleAttributes";
import FixedPeriodUnitDescriptions from "../helpers/FixedPeriodUnitDescriptions";
import { ActionEventName, IGoogleTagManager, ICommonVariables } from "../../googleTagManager";
import {
    calculateAllPeriods,
    getEndDateOfFullMonth,
    getSelectedPeriodKeyByPeriodGranularity,
    getStartDateForDefault13MonthPeriod,
    getValidFromDate,
    getEndOfDayDateUtc,
    IPeriod,
    ISelectedPeriod
} from "../helpers/PeriodHelper";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import { IReadVueQueryParams, IWriteVueQueryParams, QueryStringParamNames } from "../helpers/UrlHelper";
import { getDateInUtc } from "../helpers/PeriodHelper";
import { selectBestAverage } from "../helpers/AveragesHelper";
import { ApplicationConfiguration } from "../../ApplicationConfiguration";
import { MixPanel } from "../mixpanel/MixPanel";
import { getEndFromQuery, getStartFromQuery, rangeCalculations } from "../helpers/DateHelper";

interface IState {
    selectedPeriods: ISelectedPeriod;
    average: BrandVueApi.IAverageDescriptor;
    startDate: Date;
    endDate: Date;
    yearPeriods: IPeriod[];
    periods: IPeriod[];
}

interface IProps {
    applicationConfiguration: ApplicationConfiguration;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    curatedFilters: CuratedFilters;
    activeMetrics: Metric[];
    userVisibleAverages: IAverageDescriptor[];
    showRollingAverages: boolean;
    buttonAttr?: IDropdownToggleAttributes;
    writeVueQueryParams: IWriteVueQueryParams;
    readVueQueryParams: IReadVueQueryParams;
}

export default class FixedPeriodDatePicker extends React.Component<IProps, IState> {
    constructor(props) {
        super(props);
        this.handleAverageChange = this.handleAverageChange.bind(this);
        this.addEvent = this.addEvent.bind(this);
        this.state = {
            ...this.refreshComponentWithNewProps(props)
        }
    }

    addEvent(eventName: ActionEventName, values?: ICommonVariables | null): void {
        this.props.googleTagManager.addEvent(eventName, this.props.pageHandler, { ...values, parentComponent: "FixedPeriodDatePicker" });
    }

    handleAverageChange(average: BrandVueApi.IAverageDescriptor) {
        this.props.curatedFilters.average = average;
        const averageParamToUpdate = [{ name: "Average", value: average.averageId }];
        this.addEvent("changeAverage", { value: average.displayName });
        MixPanel.track("averageChanged");
        this.setValidSessionDates(this.state.startDate, this.state.selectedPeriods, average, averageParamToUpdate);
    }

    private getQueryParameterDateConfig(start: Date, end: Date): { name: string, value: string }[] {
        return [
            { name: QueryStringParamNames.range, value: "" },
            { name: QueryStringParamNames.start, value: moment.utc(start).format("YYYY-MM-DD") },
            { name: QueryStringParamNames.end, value: moment.utc(end).format("YYYY-MM-DD") }
        ];
    }

    private setSessionDates(start: Date, end: Date, isReceivingProps?: boolean) {
        const startDateFormatted = moment.utc(start).format("YYYY-MM-DD");
        const endDateFormatted = moment.utc(end).format("YYYY-MM-DD");
        const startDate = moment.utc(startDateFormatted).toDate();
        const endDate = moment.utc(endDateFormatted).toDate();
        this.props.curatedFilters.setDates(startDate, endDate);
        if (!isReceivingProps) {
            this.addEvent("changeDate", { startDate: startDateFormatted, endDate: endDateFormatted });
        }
    }

    private getMonthForSelectedPeriodsAndAverage(makeUpTo: BrandVueApi.MakeUpTo, selectedPeriods: ISelectedPeriod): number {
        switch (makeUpTo) {
            case BrandVueApi.MakeUpTo.MonthEnd:
                return selectedPeriods.month;
            case BrandVueApi.MakeUpTo.QuarterEnd:
                return selectedPeriods.quarter * 3;
            case BrandVueApi.MakeUpTo.HalfYearEnd:
                return selectedPeriods.halfYear * 6;
            case BrandVueApi.MakeUpTo.CalendarYearEnd:
                return 12;
        }
        throw Error("Daily average not supported");
    }

    private calculateEndDateFromSelectedPeriods(average: IAverageDescriptor, selectedPeriods: ISelectedPeriod): Date {
        let date = getDateInUtc(selectedPeriods.year, selectedPeriods.month, selectedPeriods.day);
        if (average.totalisationPeriodUnit === BrandVueApi.TotalisationPeriodUnit.Month) {
            const month = this.getMonthForSelectedPeriodsAndAverage(average.makeUpTo, selectedPeriods);
            date = getDateInUtc(selectedPeriods.year, month, 1);
            return moment.utc(date).endOf("month").toDate();
        }
        return average.makeUpTo === BrandVueApi.MakeUpTo.WeekEnd ? moment.utc(date).endOf("week").toDate() : date;
    }

    onSelectDay(day: Date) {
        this.setSessionDates(this.state.startDate, day);
        this.setQueryParameters(this.state.startDate, day, []);
    }

    onSelectPeriod(periodNumber: number, periodGranularity: BrandVueApi.MakeUpTo, selectedPeriod: ISelectedPeriod) {
        const selectedPeriodKey = getSelectedPeriodKeyByPeriodGranularity(periodGranularity);
        const newSelectedPeriod = { ...selectedPeriod, [selectedPeriodKey as string]: periodNumber };
        MixPanel.track("dateRangeChanged");
        this.setValidSessionDates(this.state.startDate, newSelectedPeriod, this.state.average, []);
    }

    private setQueryParameters(start: Date, end: Date, extraQueryParams: { name: string, value: string }[]) {
        this.props.writeVueQueryParams.setQueryParameters(this.getQueryParameterDateConfig(start, end).concat(extraQueryParams));
    }

    private setValidSessionDates(startDate: Date, newSelections: ISelectedPeriod, average: BrandVueApi.IAverageDescriptor, extraQueryParams: { name: string, value: string }[], isReceivingProps?: boolean) {
        let endDate = this.calculateEndDateFromSelectedPeriods(average, newSelections);
        if (!average.allowPartial && moment.utc(endDate).diff(this.props.applicationConfiguration.dateOfLastDataPoint, "day") > 0) {
            endDate = CompletePeriod.getLastDayInLastCompletePeriod(this.props.applicationConfiguration.dateOfLastDataPoint,
                average.makeUpTo);
        }

        const startOfPeriodOfEndDate = CompletePeriod.getFirstDayInCurrentPeriod(endDate, average.makeUpTo);
        if (!average.allowPartial && moment.utc(startOfPeriodOfEndDate).diff(this.props.applicationConfiguration.dateOfFirstDataPoint, "day") < 0) {
            endDate = CompletePeriod.getFirstDayInNextCompletePeriod(this.props.applicationConfiguration.dateOfFirstDataPoint, average.makeUpTo);
            endDate = CompletePeriod.getLastDayInCurrentPeriod(endDate, average.makeUpTo);
        }

        this.setSessionDates(startDate, endDate, isReceivingProps);
        this.setQueryParameters(startDate, endDate, extraQueryParams);
    }

    private refreshComponentWithNewProps(props: IProps): IState {
        const averageId = props.readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.average);
        let average = selectBestAverage(props.userVisibleAverages, averageId);
        const minimum = moment.utc(this.props.applicationConfiguration.dateOfFirstDataPoint);
        const maximum = moment.utc(this.props.applicationConfiguration.dateOfLastDataPoint);
        const searchParams = new URLSearchParams(window.location.search);
        const endMoment = getEndFromQuery(maximum, props.readVueQueryParams, searchParams);
        const startMoment = getStartFromQuery(minimum, maximum, endMoment, props.readVueQueryParams, searchParams);
        
        const selectedPeriod = {
            day: endMoment.date(),
            month: endMoment.month() + 1,
            year: endMoment.year(),
            quarter: Math.floor(endMoment.month() / 3) + 1,
            halfYear: Math.floor(endMoment.month() / 6) + 1
        }

        let start = startMoment.toDate();
        let end = endMoment.toDate();

        this.setValidSessionDates(start, selectedPeriod, average, [], true);

        let periods = new Array<IPeriod>();
        let yearPeriods = new Array<IPeriod>();

        if (average.totalisationPeriodUnit === BrandVueApi.TotalisationPeriodUnit.Month) {
            periods = calculateAllPeriods({
                periodGranularity: average.makeUpTo,
                currentSelectedPeriod: selectedPeriod,
                activeMetrics: props.activeMetrics,
                dateOfFirstDataPoint: props.applicationConfiguration.dateOfFirstDataPoint,
                dateOfLastDataPoint: props.applicationConfiguration.dateOfLastDataPoint,
                forcePeriodsValid: average.allowPartial
            });
            yearPeriods = calculateAllPeriods({
                periodGranularity: BrandVueApi.MakeUpTo.CalendarYearEnd,
                subperiodGranularity: average.makeUpTo,
                currentSelectedPeriod: selectedPeriod,
                activeMetrics: props.activeMetrics,
                dateOfFirstDataPoint: props.applicationConfiguration.dateOfFirstDataPoint,
                dateOfLastDataPoint: props.applicationConfiguration.dateOfLastDataPoint,
                forcePeriodsValid: average.allowPartial
            })
        }

        if (average.makeUpTo === BrandVueApi.MakeUpTo.WeekEnd) {
            start = moment.utc(start).endOf("week").startOf("day").toDate();
            end = moment.utc(end).endOf("week").startOf("day").toDate();
        }

        return {
            average: average,
            startDate: start,
            endDate: end,
            periods: periods,
            yearPeriods: yearPeriods,
            selectedPeriods: selectedPeriod
        };
    }

    componentDidUpdate(prevProps, prevState) {
        const nextState = this.refreshComponentWithNewProps(this.props);
        if (prevProps.curatedFilters.average.averageId !== nextState.average.averageId) //Handles the case where the possible list of averages could change
            this.handleAverageChange(nextState.average);
        else if (JSON.stringify(prevState) !== JSON.stringify(nextState)) //Handles a genuine average dropdown change
            this.setState(nextState);
    }

    render() {

        const { buttonAttr } = this.props;

        if (!this.state.average || this.state.average.isHiddenFromUsers) {
            return <></>;
        }
        const validFixedAverages = this.props.userVisibleAverages.filter(a => a.makeUpTo !== BrandVueApi.MakeUpTo.Day);
        const validRollingAverages = this.props.showRollingAverages
            ? this.props.userVisibleAverages.filter(a => a.makeUpTo === BrandVueApi.MakeUpTo.Day && !a.isHiddenFromUsers)
            : [];

        const minimum = getValidFromDate(this.props.applicationConfiguration.dateOfFirstDataPoint, this.props.activeMetrics);
        const maximum = getEndOfDayDateUtc(this.props.applicationConfiguration.dateOfLastDataPoint);

        const averageSelector =
            <FixedPeriodAverageSelector validFixedAverages={validFixedAverages} validRollingAverages={validRollingAverages}
                                        selectedAverage={this.state.average} handleAverageChange={this.handleAverageChange}
                                        buttonAttr={buttonAttr}
            />;

        const makeUpTo = this.state.average.makeUpTo;
        const selectedPeriodKey = getSelectedPeriodKeyByPeriodGranularity(makeUpTo);
        const periodNumber = this.state.selectedPeriods[selectedPeriodKey];
        const isDaily = this.state.average.totalisationPeriodUnit === BrandVueApi.TotalisationPeriodUnit.Day;
        const daySelector = isDaily
            ? <FixedPeriodDaySelector
                day={this.state.endDate} dateStart={minimum} dateEnd={maximum}
                onSelectDay={(item) => this.onSelectDay(item)}
                buttonAttr={buttonAttr}
            />
            : null;

        let periodSelector: null | JSX.Element = null;
        let yearSelector: null | JSX.Element = null;
        if (!isDaily) {
            const yearlyDescription = makeUpTo === BrandVueApi.MakeUpTo.CalendarYearEnd ? FixedPeriodUnitDescriptions.getPeriodDescription(minimum, maximum, makeUpTo) : "";
            periodSelector =
                <FixedPeriodSelector period={periodNumber} periods={this.state.periods} makeUpTo={makeUpTo}
                                     onSelectPeriod={(periodNumber, periodGranularity) => this.onSelectPeriod(periodNumber, periodGranularity, this.state.selectedPeriods)} periodDescription={yearlyDescription}
                                     buttonAttr={buttonAttr}
                />;

            if (makeUpTo !== BrandVueApi.MakeUpTo.CalendarYearEnd) {
                const periodDescription = FixedPeriodUnitDescriptions.getPeriodDescription(minimum, maximum, makeUpTo);
                yearSelector =
                    <FixedPeriodSelector period={this.state.selectedPeriods.year} periods={this.state.yearPeriods} makeUpTo={BrandVueApi.MakeUpTo.CalendarYearEnd}
                                         onSelectPeriod={(periodNumber, periodGranularity) => this.onSelectPeriod(periodNumber, periodGranularity, this.state.selectedPeriods)} periodDescription={periodDescription}
                                         buttonAttr={buttonAttr}
                    />;
            }
        }

        return (
            <div className={"fixed-period-date-picker"}>
                {averageSelector}
                {daySelector}
                {yearSelector}
                {periodSelector}
            </div>
        );
    }
}