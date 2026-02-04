import { Metric } from './../../metrics/metric';
import { IAverageDescriptor, MakeUpTo, TotalisationPeriodUnit } from "../../BrandVueApi";
import moment from "moment";
import { UnitOfTime } from "./../scorecards/ScorecardDatePicker";

export interface ISelectedPeriod {
    day: number;
    month: number;
    quarter: number;
    halfYear: number;
    year: number;
}

export interface IPeriodCalculationRequest {
    periodGranularity: MakeUpTo;
    subperiodGranularity?: MakeUpTo;
    currentSelectedPeriod: ISelectedPeriod;
    dateOfFirstDataPoint: Date;
    dateOfLastDataPoint: Date;
    activeMetrics: Metric[];
    forcePeriodsValid: boolean;
}

export interface IPeriod {
    value: number,
    valid: boolean
}

export const AverageIds = {
    //any changes here must also be made in the backend (AverageConsts.cs)
    CustomPeriod: "CustomPeriod",
    CustomPeriodNotWeighted: "CustomPeriodNotWeighted"
}

export const getDescriptiveNameForAveragePeriod = (average: IAverageDescriptor) : string => {
    if (average.averageId === AverageIds.CustomPeriod) {
        return "Period including all data";
    }

    const numPeriods = average.numberOfPeriodsInAverage;
    const periodType = getMakeUpToName(average.makeUpTo);

    switch(average.totalisationPeriodUnit) {
        case TotalisationPeriodUnit.Month:
            if ((average.makeUpTo === MakeUpTo.CalendarYearEnd && numPeriods === 12) ||
                (average.makeUpTo === MakeUpTo.HalfYearEnd && numPeriods === 6) ||
                (average.makeUpTo === MakeUpTo.QuarterEnd && numPeriods === 3) ||
                (average.makeUpTo === MakeUpTo.MonthEnd && numPeriods === 1))
            {
                return `${periodType}`;
            }
            return `${periodType} (${numPeriods} month average)`;
        case TotalisationPeriodUnit.Day:
            if (numPeriods > 28 && numPeriods % 7 == 0) {
                return `${periodType} (${numPeriods / 7} week average)`;
            }
            return numPeriods === 1 ? `${periodType}` : `${periodType} (${numPeriods} day average)`;
    }

    return `${average.displayName} period`;
}

const getMakeUpToName = (makeUpTo: MakeUpTo): string => {
    switch (makeUpTo) {
        case MakeUpTo.CalendarYearEnd:
            return "Calendar year";
        case MakeUpTo.HalfYearEnd:
            return "Half year";
        case MakeUpTo.QuarterEnd:
            return "Quarter";
        case MakeUpTo.MonthEnd:
            return "Month";
        case MakeUpTo.Day:
            return "Day";
    }
    throw new Error(`Unknown MakeUpTo: ${makeUpTo}`);
}

export const getMakeUpToUnitOfTime = (makeUpTo: MakeUpTo): UnitOfTime => {
    switch (makeUpTo) {
    case MakeUpTo.CalendarYearEnd:
        return 'y';
    case MakeUpTo.QuarterEnd:
        return 'Q';
    case MakeUpTo.MonthEnd:
        return 'M';
    case MakeUpTo.WeekEnd:
        return 'w';
    }
    throw new Error(`Unknown MakeUpTo: ${makeUpTo}`);
}

export const getSelectedPeriodKeyByPeriodGranularity = (periodGranularity: MakeUpTo) : (keyof ISelectedPeriod) => {
    switch (periodGranularity) {
        case MakeUpTo.CalendarYearEnd:
            return "year";
        case MakeUpTo.HalfYearEnd:
            return "halfYear";
        case MakeUpTo.QuarterEnd:
            return "quarter";
        case MakeUpTo.MonthEnd:
            return "month";
        case MakeUpTo.Day:
        case MakeUpTo.WeekEnd:
            return "day";
    }

    throw new Error("Unknown period granularity");
}

export const getNumberOfPeriodsToShow = (average: IAverageDescriptor): number => {
    switch (average.makeUpTo) {

    case MakeUpTo.MonthEnd:
        return 3;
    case MakeUpTo.QuarterEnd:
        return 4;
    case MakeUpTo.WeekEnd:
        return 3;
    default:
        return 2;
    }
}

export const calculateAllPeriods = (request: IPeriodCalculationRequest): IPeriod[] => {
    if (request.periodGranularity === MakeUpTo.Day) {
        throw new Error("daily averages do not have periods");
    }

    const { periodGranularity, subperiodGranularity, currentSelectedPeriod, dateOfFirstDataPoint, dateOfLastDataPoint, activeMetrics, forcePeriodsValid } = request;
    const validFrom = getValidFromDate(dateOfFirstDataPoint, activeMetrics);
    const validTo = getEndOfDayDateUtc(dateOfLastDataPoint);

    const allPeriodValues = getAllPeriodValues(periodGranularity, validFrom, validTo);
    return allPeriodValues.map(periodValue => {
        return {
            value: periodValue,
            valid: forcePeriodsValid || isPeriodValueValid(periodGranularity, periodValue, currentSelectedPeriod.year, validFrom, validTo, subperiodGranularity)
        }
    });
}

export const getValidFromDate = (dateOfFirstDataPoint: Date, activeMetrics: Metric[]): Date => {
    if (!activeMetrics) {
        return dateOfFirstDataPoint;
    }

    const latestActiveMetricStartDate = activeMetrics.map(am => am.startDate).reduce(
        (prevStartDate, currStartDate) => {
            if (currStartDate && currStartDate > prevStartDate) {
                return currStartDate;
            }
            return prevStartDate;
        }, getDateInUtc(1900, 1, 1));

    return latestActiveMetricStartDate > dateOfFirstDataPoint ? latestActiveMetricStartDate : dateOfFirstDataPoint;
}

export const isPeriodValueValid = (periodGranularity: MakeUpTo, periodValue: number, currentSelectedYear: number, dataValidFrom: Date, dataValidTo: Date, subperiodGranularity?: MakeUpTo): boolean => {

    if (isPeriodWithinBounds(periodGranularity, periodValue, currentSelectedYear, dataValidFrom, dataValidTo)) {

        return true;
    }

    // Subperiods are only supported for calendar years
    if (periodGranularity !== MakeUpTo.CalendarYearEnd || !subperiodGranularity) {
        return false;
    }

    // If there are any valid subperiods in this period, then the whole period is valid
    const allSubPeriodValues = getAllPeriodValues(subperiodGranularity, dataValidFrom, dataValidTo);
    const valid = allSubPeriodValues.some(subperiodValue => isPeriodWithinBounds(subperiodGranularity, subperiodValue, periodValue, dataValidFrom, dataValidTo));
    return valid;
}

const isPeriodWithinBounds = (periodGranularity: MakeUpTo, periodValue: number, currentSelectedYear: number, dataValidFrom: Date, dataValidTo: Date): boolean => {
    const periodStartDate = getPeriodStartDate(periodGranularity, periodValue, currentSelectedYear);
    const periodEndDate = getPeriodEndDate(periodGranularity, periodValue, currentSelectedYear);

    const periodWithinBounds = periodStartDate >= dataValidFrom && periodEndDate <= dataValidTo;
    // For debugging:
    // console.log({
    //     periodGranularity,
    //     periodValue,
    //     currentSelectedYear,
    //     periodWithinBounds
    // });
    return periodWithinBounds;
}

const getAllPeriodValues = (periodGranularity: MakeUpTo, from: Date, to: Date): number[] => {
    switch (periodGranularity) {
        case MakeUpTo.CalendarYearEnd:
            return getYears(from, to);
        case MakeUpTo.HalfYearEnd:
            return [1, 2];
        case MakeUpTo.QuarterEnd:
            return [1, 2, 3, 4];
        case MakeUpTo.MonthEnd:
            return [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    }
    throw new Error("daily averages do not have periods");
}

const getYears = (dateFrom: Date, dateTo: Date): number[] => {
    const years = [] as number[];
    let currentYear = dateFrom.getUTCFullYear();
    const lastYear = dateTo.getUTCFullYear();
    while (currentYear <= lastYear) {
        years.push(currentYear++);
    }
    return years;
}

const getPeriodStartDate = (periodGranularity: MakeUpTo, periodValue: number, currentSelectedYear: number): Date => {
    switch (periodGranularity) {
        case MakeUpTo.MonthEnd:
            return getDateInUtc(currentSelectedYear, periodValue, 1);
        case MakeUpTo.QuarterEnd:
            return getDateInUtc(currentSelectedYear, periodValue * 3 - 2, 1);
        case MakeUpTo.HalfYearEnd:
            return getDateInUtc(currentSelectedYear, periodValue * 6 - 5, 1);
        case MakeUpTo.CalendarYearEnd:
            return getDateInUtc(periodValue, 1, 1);
    }
    throw Error("Daily average not supported");
}

const getPeriodEndDate = (periodGranularity: MakeUpTo, periodValue: number, currentSelectedYear: number): Date => {
    const startDateMoment = moment.utc(getPeriodStartDate(periodGranularity, periodValue, currentSelectedYear));
    switch (periodGranularity) {
        case MakeUpTo.MonthEnd:
            return startDateMoment.endOf("month").toDate();
        case MakeUpTo.QuarterEnd:
            return startDateMoment.add(2, "month").endOf("month").toDate();
        case MakeUpTo.HalfYearEnd:
            return startDateMoment.add(5, "month").endOf("month").toDate();
        case MakeUpTo.CalendarYearEnd:
            return startDateMoment.endOf("year").toDate();
    }
    throw Error("Daily average not supported");
}

export const getStartDateForDefault13MonthPeriod = (end: Date, dateOfFirstDataPoint: Date): Date => {
    const start = moment.utc(end).startOf("month").subtract(12, "month").toDate();
    return start < dateOfFirstDataPoint
        ? dateOfFirstDataPoint : new Date(Date.UTC(start.getUTCFullYear(), start.getUTCMonth(), 1));
}

export const getEndDateOfFullMonth = (dateOfLastDataPoint: Date): Date => {
    return (moment.utc(dateOfLastDataPoint).endOf("month").isSame(dateOfLastDataPoint, "day")) ?
        moment.utc(dateOfLastDataPoint).endOf("month").toDate() :
        moment.utc(dateOfLastDataPoint).subtract(1, "month").endOf("month").toDate();
}


export const isCustomPeriodAverage = (average: IAverageDescriptor): boolean => {
    return average.averageId === AverageIds.CustomPeriod || average.averageId === AverageIds.CustomPeriodNotWeighted;
};

export const getDateInUtc = (year: number, month: number, day: number, hour: number = 0, minutes: number = 0, seconds: number = 0) : Date =>
    new Date(Date.UTC(year, month - 1, day, hour, minutes, seconds)); // Months are 0 based, days are 1 based in Javascript

export const getEndOfDayDateUtc = (dateOfLastDataPoint: Date) : Date => {
    return moment.utc(dateOfLastDataPoint).endOf("day").toDate();
}

export const getStartOfDayDateUtc = (date: Date) : Date => {
    return moment.utc(date).startOf("day").toDate();
}

export const getSpecificAveragePeriodAsText = (startDate: Date, makeUpTo: MakeUpTo) => {
    let period = "";
    let options = {};

    switch (makeUpTo) {
    case MakeUpTo.CalendarYearEnd:
        options = { year: 'numeric' };
        break;
    case MakeUpTo.HalfYearEnd:
        if (startDate.getMonth() < 6) {
            period = "H1";
        } else
            period = "H2";
        options = { year: 'numeric' };
        break;
    case MakeUpTo.QuarterEnd:
        const month = startDate.getMonth();
        switch (true) {
        case (month < 3):
            period = "Q1";
            break;
        case (month < 6):
            period = "Q2";
            break;
        case (month < 9):
            period = "Q3";
            break;
        case (month < 12):
            period = "Q4";
            break;
        default:
            period = "Q?";
            break;
        }
        options = { year: 'numeric' };
        break;
    case MakeUpTo.MonthEnd:
        options = { month: 'long', year: 'numeric' };
            break;
    case MakeUpTo.WeekEnd:
        period = "w/c";
        options = { day: 'numeric', month: 'long', year: 'numeric' };
        break;
    case MakeUpTo.Day:
        options = { day: 'numeric', month: 'long', year: 'numeric' };
        break;
    default:
        period = "";
    }

    return `${period} ${startDate.toLocaleDateString("en-GB", options)}`;
}