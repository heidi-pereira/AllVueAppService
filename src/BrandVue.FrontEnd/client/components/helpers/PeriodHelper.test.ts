 import { MetricSet } from './../../metrics/metricSet';
import { Metric } from './../../metrics/metric';
import { isPeriodValueValid, calculateAllPeriods, getStartDateForDefault13MonthPeriod, getEndDateOfFullMonth } from "./PeriodHelper";
import { MakeUpTo } from "../../BrandVueApi";
import moment from "moment";

// Months are 0 based, days are 1 based
const getDateInUtc = (year: number, month: number, day: number): Date => new Date(Date.UTC(year, month - 1, day));
const getDateAndTimeInUtc = (year: number, month: number, day: number, hour: number, minute: number, second: number, ms: number): Date => new Date(Date.UTC(year, month - 1, day, hour, minute, second, ms));

describe("isPeriodValueValid", () => {

    it.each`
        periodGranularity        | periodValue | dataValidFrom
        ${MakeUpTo.MonthEnd}     | ${1}        | ${getDateInUtc(2019, 1, 2)}
        ${MakeUpTo.MonthEnd}     | ${3}        | ${getDateInUtc(2019, 3, 3)}
        ${MakeUpTo.MonthEnd}     | ${3}        | ${getDateInUtc(2019, 3, 15)}
        ${MakeUpTo.MonthEnd}     | ${3}        | ${getDateInUtc(2019, 3, 31)}
        ${MakeUpTo.MonthEnd}     | ${3}        | ${getDateInUtc(2019, 4, 1)}
        ${MakeUpTo.QuarterEnd}   | ${3}        | ${getDateInUtc(2019, 10, 2)}
        ${MakeUpTo.QuarterEnd}   | ${3}        | ${getDateInUtc(2019, 11, 15)}
        ${MakeUpTo.QuarterEnd}   | ${3}        | ${getDateInUtc(2019, 12, 1)}
        ${MakeUpTo.HalfYearEnd}  | ${2}        | ${getDateInUtc(2019, 7, 2)}
        ${MakeUpTo.HalfYearEnd}  | ${2}        | ${getDateInUtc(2019, 8, 15)}
        ${MakeUpTo.HalfYearEnd}  | ${2}        | ${getDateInUtc(2019, 12, 31)}
    `("should be false if period's start date exceeds validity ($periodGranularity, 2019/$periodValue, dataValidFrom: $dataValidFrom)",
        ({periodGranularity, periodValue, dataValidFrom}) => {
        const isValid = isPeriodValueValid(periodGranularity, periodValue, 2019, dataValidFrom, getDateInUtc(2030, 1, 1));
        expect(isValid).toBe(false);
    });    

    it.each`
        periodValue    | dataValidFrom
        ${2019}        | ${getDateInUtc(2019, 1, 2)}
        ${2019}        | ${getDateInUtc(2019, 2, 1)}
        ${2019}        | ${getDateInUtc(2019, 3, 1)}
        ${2019}        | ${getDateInUtc(2019, 8, 18)}
        ${2019}        | ${getDateInUtc(2019, 12, 31)}
    `("should be false if period's start exceeds validity (year: $periodValue, dataValidFrom: $dataValidFrom)", ({periodValue, dataValidFrom}) => {
        const isValid = isPeriodValueValid(MakeUpTo.CalendarYearEnd, periodValue, 2019, dataValidFrom, getDateInUtc(2030, 1, 1));
        expect(isValid).toBe(false);
    });

    it.each`
        periodGranularity        | periodValue | dataValidTo
        ${MakeUpTo.MonthEnd}     | ${1}        | ${getDateInUtc(2019, 1, 31)}
        ${MakeUpTo.MonthEnd}     | ${6}        | ${getDateInUtc(2019, 6, 31)}
        ${MakeUpTo.MonthEnd}     | ${3}        | ${getDateInUtc(2019, 3, 15)}
        ${MakeUpTo.MonthEnd}     | ${3}        | ${getDateInUtc(2019, 3, 31)}
        ${MakeUpTo.MonthEnd}     | ${3}        | ${getDateInUtc(2019, 2, 1)}
        ${MakeUpTo.QuarterEnd}   | ${3}        | ${getDateInUtc(2019, 7, 2)}
        ${MakeUpTo.QuarterEnd}   | ${3}        | ${getDateInUtc(2019, 9, 15)}
        ${MakeUpTo.QuarterEnd}   | ${3}        | ${getDateInUtc(2019, 9, 30)}
        ${MakeUpTo.HalfYearEnd}  | ${2}        | ${getDateInUtc(2019, 7, 2)}
        ${MakeUpTo.HalfYearEnd}  | ${2}        | ${getDateInUtc(2019, 8, 15)}
        ${MakeUpTo.HalfYearEnd}  | ${2}        | ${getDateInUtc(2019, 12, 1)}
        ${MakeUpTo.HalfYearEnd}  | ${2}        | ${getDateInUtc(2019, 12, 30)}
    `("should be false if period's end date exceeds validity ($periodGranularity, $periodValue, dataValidTo: $dataValidTo)",
        ({periodGranularity, periodValue, dataValidFrom: dataValidTo}) => {
        const isValid = isPeriodValueValid(periodGranularity, periodValue, 2019, getDateInUtc(1990, 0, 1), dataValidTo);
        expect(isValid).toBe(false);
    });

    it.each`
        periodValue    | dataValidFrom
        ${2019}        | ${getDateInUtc(2019, 1, 2)}
        ${2019}        | ${getDateInUtc(2019, 2, 1)}
        ${2019}        | ${getDateInUtc(2019, 7, 18)}
        ${2019}        | ${getDateInUtc(2019, 12, 31)}
    `("should be false if period's start date exceeds validity (year: $periodValue, dataValidFrom: $dataValidFrom)", ({periodValue, dataValidFrom}) => {
        const isValid = isPeriodValueValid(MakeUpTo.CalendarYearEnd, periodValue, 2019, dataValidFrom, getDateInUtc(2030, 1, 1));
        expect(isValid).toBe(false);
    });

    it.each`
        periodValue  | dataValidFrom
        ${1}         | ${getDateInUtc(2019, 1, 1)}
        ${2}         | ${getDateInUtc(2019, 2, 1)}
        ${11}        | ${getDateInUtc(2019, 11, 1)}
    `("should be true if period's start and end dates are equal to validity dates (MonthEnd, 2019/$periodValue, dataValidFrom: $dataValidFrom)",
        ({periodValue, dataValidFrom}) => {
        const isValid = isPeriodValueValid(MakeUpTo.MonthEnd, periodValue, 2019, dataValidFrom, moment.utc(dataValidFrom).endOf("month").toDate());
        expect(isValid).toBe(true);
    });

    it.each`
        periodValue  | dataValidFrom
        ${1}         | ${getDateInUtc(2019, 1, 1)}
        ${2}         | ${getDateInUtc(2019, 4, 1)}
        ${3}         | ${getDateInUtc(2019, 7, 1)}
        ${4}         | ${getDateInUtc(2019, 10, 1)}
    `("should be true if period's start and end dates are equal to validity dates (QuarterEnd, 2019/$periodValue, dataValidFrom: $dataValidFrom)",
        ({periodValue, dataValidFrom}) => {
        const isValid = isPeriodValueValid(MakeUpTo.QuarterEnd, periodValue, 2019, dataValidFrom, moment.utc(dataValidFrom).add(3, "month").endOf("month").toDate());
        expect(isValid).toBe(true);
    });

    it.each`
        periodValue  | dataValidFrom
        ${1}         | ${getDateInUtc(2019, 1, 1)}
        ${2}         | ${getDateInUtc(2019, 7, 1)}
    `("should be true if period's start and end dates are equal to validity dates (HalfYearEnd, 2019/$periodValue, dataValidFrom: $dataValidFrom)",
        ({periodValue, dataValidFrom}) => {
        const isValid = isPeriodValueValid(MakeUpTo.HalfYearEnd, periodValue, 2019, dataValidFrom, moment.utc(dataValidFrom).add(6, "month").endOf("month").toDate());
        expect(isValid).toBe(true);
    });

    it.each`
        year
        ${2017}
        ${2018}
        ${2019}
    `("should be true if period start and end dates are equal to year beginning and end (CalendarYearEnd, $year)",
        ({year}) => {
        const dataValidFrom = getDateInUtc(year, 1, 1);
        const isValid = isPeriodValueValid(MakeUpTo.CalendarYearEnd, year, 2019, dataValidFrom, moment.utc(dataValidFrom).endOf("year").toDate());
        expect(isValid).toBe(true);
    });

    it("should be false for CalendarYearEnd with MonthEnd subperiod granularity if the validity dates are less than any one month", () => {
        const isValid = isPeriodValueValid(MakeUpTo.CalendarYearEnd, 2019, 2019, getDateInUtc(2019, 1, 1), getDateInUtc(2019, 1, 31), MakeUpTo.MonthEnd);
        expect(isValid).toBe(false);
    });

    it("should be false for CalendarYearEnd with QuarterEnd subperiod granularity if the validity dates are less than any one quarter", () => {
        const isValid = isPeriodValueValid(MakeUpTo.CalendarYearEnd, 2019, 2019, getDateInUtc(2019, 1, 1), getDateInUtc(2019, 3, 31), MakeUpTo.QuarterEnd);
        expect(isValid).toBe(false);
    });

    it("should be false for CalendarYearEnd with HalfYearEnd subperiod granularity if the validity dates are less than any one half year", () => {
        const isValid = isPeriodValueValid(MakeUpTo.CalendarYearEnd, 2019, 2019, getDateInUtc(2019, 1, 1), getDateInUtc(2019, 6, 30), MakeUpTo.HalfYearEnd);
        expect(isValid).toBe(false);
    });

    it.each`
        periodGranularity               | periodValue    | dataValidFrom                | dataValidTo                   | subperiodGranularity
        ${MakeUpTo.CalendarYearEnd}     | ${2019}        | ${getDateInUtc(2019, 1, 31)} | ${getDateInUtc(2019, 3, 15)}  | ${MakeUpTo.MonthEnd}
        ${MakeUpTo.CalendarYearEnd}     | ${2019}        | ${getDateInUtc(2019, 1, 1)}  | ${getDateInUtc(2019, 2, 1)}   | ${MakeUpTo.MonthEnd}
        ${MakeUpTo.CalendarYearEnd}     | ${2019}        | ${getDateInUtc(2019, 2, 1)}  | ${getDateInUtc(2019, 7, 1)}   | ${MakeUpTo.QuarterEnd}
        ${MakeUpTo.CalendarYearEnd}     | ${2019}        | ${getDateInUtc(2019, 10, 1)} | ${getDateInUtc(2020, 1, 1)}   | ${MakeUpTo.QuarterEnd}
        ${MakeUpTo.CalendarYearEnd}     | ${2019}        | ${getDateInUtc(2019, 1, 1)}  | ${getDateInUtc(2019, 8, 1)}   | ${MakeUpTo.HalfYearEnd}
        ${MakeUpTo.CalendarYearEnd}     | ${2019}        | ${getDateInUtc(2019, 4, 1)}  | ${getDateInUtc(2020, 1, 1)}   | ${MakeUpTo.HalfYearEnd}
    `("should be true if period exceeds validity but any allowed subperiod does not ($args)",
        (args) => {
        const isValid = isPeriodValueValid(args.periodGranularity, args.periodValue, 2019, args.dataValidFrom, args.dataValidTo, args.subperiodGranularity);
        expect(isValid).toBe(true);
    });
});

const getTestSelectedPeriod = () => ({
    day: 1,
    month: 1,
    quarter: 1,
    halfYear: 1,
    year: 2019
});

const getTestActiveMetricsWithStartDates = (startDates: Array<Date | null>) => {
    const metrics = new Array<Metric>();
    const ms = new MetricSet();
    startDates.forEach(sd => {
        const metric = new Metric(ms);
        if (sd) {
            metric.startDate = sd;
        }
        metrics.push(metric);
    })
    return metrics;
}

describe("calculateAllPeriods", () => {
    it("should return 12 month periods", () => {
        const request = {
            periodGranularity: MakeUpTo.MonthEnd,
            currentSelectedPeriod: getTestSelectedPeriod(),
            activeMetrics: getTestActiveMetricsWithStartDates([ getDateInUtc(2019, 4, 15), getDateInUtc(2019, 6, 2)]),
            dateOfFirstDataPoint: getDateInUtc(1999, 1, 1),
            dateOfLastDataPoint: getDateInUtc(2019, 11, 30),
            forcePeriodsValid: false
        };
        
        const periods = calculateAllPeriods(request);
        
        // We expect 12 months
        expect(periods.length).toBe(12);
        for (var i = 0; i < 12; i++) {
            expect(periods[i].value).toBe(i+1);
        }
    });

    it("should return month periods with the right value", () => {
        const request = {
            periodGranularity: MakeUpTo.MonthEnd,
            currentSelectedPeriod: getTestSelectedPeriod(),
            activeMetrics: getTestActiveMetricsWithStartDates([ getDateInUtc(2019, 4, 15), getDateInUtc(2019, 6, 2)]),
            dateOfFirstDataPoint: getDateInUtc(1999, 1, 1),
            dateOfLastDataPoint: getDateInUtc(2019, 11, 30),
            forcePeriodsValid: false
        };
        
        const periods = calculateAllPeriods(request);
        
        // We expect valid months from July to November
        const expectedValidValues = [false, false, false, false, false, false, true, true, true, true, true, false];

        for (var i = 0; i < 12; i++) {
            expect(periods[i].valid).toBe(expectedValidValues[i]);
        }
    });

    it("should return 4 quarter periods", () => {
        const request = {
            periodGranularity: MakeUpTo.QuarterEnd,
            currentSelectedPeriod: getTestSelectedPeriod(),
            activeMetrics: getTestActiveMetricsWithStartDates([ getDateInUtc(2019, 4, 15), getDateInUtc(2019, 6, 2)]),
            dateOfFirstDataPoint: getDateInUtc(1999, 1, 1),
            dateOfLastDataPoint: getDateInUtc(2019, 11, 30),
            forcePeriodsValid: false
        };
        
        const periods = calculateAllPeriods(request);
        
        // We expect 4 quarters
        expect(periods.length).toBe(4);
        for (var i = 0; i < 4; i++) {
            expect(periods[i].value).toBe(i+1);
        }
    });

    it("should return quarter periods with the right value", () => {
        const request = {
            periodGranularity: MakeUpTo.QuarterEnd,
            currentSelectedPeriod: getTestSelectedPeriod(),
            activeMetrics: getTestActiveMetricsWithStartDates([ getDateInUtc(2019, 4, 15), getDateInUtc(2019, 6, 2)]),
            dateOfFirstDataPoint: getDateInUtc(1999, 1, 1),
            dateOfLastDataPoint: getDateInUtc(2019, 11, 30),
            forcePeriodsValid: false
        };
        
        const periods = calculateAllPeriods(request);
        
        // We expect valid 3rd quarter
        const expectedValidValues = [false, false, true, false];

        for (var i = 0; i < 4; i++) {
            expect(periods[i].valid).toBe(expectedValidValues[i]);
        }
    });

    it("should return 2 half year periods", () => {
        const request = {
            periodGranularity: MakeUpTo.HalfYearEnd,
            currentSelectedPeriod: getTestSelectedPeriod(),
            activeMetrics: getTestActiveMetricsWithStartDates([ getDateInUtc(2019, 4, 15), getDateInUtc(2019, 6, 2)]),
            dateOfFirstDataPoint: getDateInUtc(1999, 1, 1),
            dateOfLastDataPoint: getDateInUtc(2019, 11, 30),
            forcePeriodsValid: false
        };
        
        const periods = calculateAllPeriods(request);
                
        // We expect 2 half years
        expect(periods.length).toBe(2);
        for (var i = 0; i < 2; i++) {
            expect(periods[i].value).toBe(i+1);
        }
    });

    it("should return half periods with the right value", () => {
        const request = {
            periodGranularity: MakeUpTo.HalfYearEnd,
            currentSelectedPeriod: getTestSelectedPeriod(),
            activeMetrics: getTestActiveMetricsWithStartDates([ getDateInUtc(2019, 4, 15), getDateInUtc(2019, 5, 2)]),
            dateOfFirstDataPoint: getDateInUtc(1999, 1, 1),
            dateOfLastDataPoint: getDateInUtc(2020, 11, 30),
            forcePeriodsValid: false
        };
        
        const periods = calculateAllPeriods(request);
        
        // We expect valid 2 half year
        const expectedValidValues = [false, true];

        for (var i = 0; i < 2; i++) {
            expect(periods[i].valid).toBe(expectedValidValues[i]);
        }
    });

    it("should return year periods even for partial years", () => {
        const request = {
            periodGranularity: MakeUpTo.CalendarYearEnd,
            currentSelectedPeriod: getTestSelectedPeriod(),
            activeMetrics: getTestActiveMetricsWithStartDates([ null, null]),
            dateOfFirstDataPoint: getDateInUtc(2018, 12, 22),
            dateOfLastDataPoint: getDateInUtc(2020, 5, 30),
            forcePeriodsValid: false
        };
        
        const periods = calculateAllPeriods(request);
                
        expect(periods.length).toBe(3);
        expect(periods[0].value).toBe(2018);
        expect(periods[1].value).toBe(2019);
        expect(periods[2].value).toBe(2020);
    });

    it("should return valid years only when at least a full month of data is present (for subperiodGranularity of a month)", () => {
        const request = {
            periodGranularity: MakeUpTo.CalendarYearEnd,
            subperiodGranularity: MakeUpTo.MonthEnd,
            currentSelectedPeriod: getTestSelectedPeriod(),
            activeMetrics: getTestActiveMetricsWithStartDates([ null, null]),
            dateOfFirstDataPoint: getDateInUtc(2018, 12, 22),
            dateOfLastDataPoint: getDateInUtc(2020, 5, 30),
            forcePeriodsValid: false
        };
        
        const periods = calculateAllPeriods(request);
        
        expect(periods[0].valid).toBe(false);
        expect(periods[1].valid).toBe(true);
        expect(periods[2].valid).toBe(true);
    });

    it("should return valid years only when at least a full quarter of data is present (for subperiodGranularity of a quarter)", () => {
        const request = {
            periodGranularity: MakeUpTo.CalendarYearEnd,
            subperiodGranularity: MakeUpTo.QuarterEnd,
            currentSelectedPeriod: getTestSelectedPeriod(),
            activeMetrics: getTestActiveMetricsWithStartDates([ null, null]),
            dateOfFirstDataPoint: getDateInUtc(2018, 9, 22),
            dateOfLastDataPoint: getDateInUtc(2020, 2, 15),
            forcePeriodsValid: false
        };
        
        const periods = calculateAllPeriods(request);
        
        expect(periods[0].valid).toBe(true);
        expect(periods[1].valid).toBe(true);
        expect(periods[2].valid).toBe(false);
    });
});

describe("getStartDateForDefault13MonthPeriod", () => {

    it.each`
        end                                | dateOfFirstDataPoint          | expectedStartDate    
        ${getDateInUtc(2020, 8, 31)}       | ${getDateInUtc(2018, 1, 1)}   | ${getDateInUtc(2019, 8, 1)}
        ${getDateInUtc(2020, 8, 31)}       | ${getDateInUtc(2020, 4, 1)}   | ${getDateInUtc(2020, 4, 1)}
        ${getDateInUtc(2020, 8, 31)}       | ${getDateInUtc(2020, 4, 14)}  | ${getDateInUtc(2020, 4, 14)}
        ${getDateInUtc(2020, 8, 15)}       | ${getDateInUtc(2018, 1, 1)}   | ${getDateInUtc(2019, 8, 1)}
    `("start date should be $expectedStartDate for (end: $end, dateOfFirstDataPoint: $dateOfFirstDataPoint)", ({ end, dateOfFirstDataPoint, expectedStartDate }) => {
        const actualStartDate = getStartDateForDefault13MonthPeriod(end, dateOfFirstDataPoint);
        expect(actualStartDate).toStrictEqual(expectedStartDate);
    });
});

describe("getDateOfFullMonth", () => {

    it.each`
        dateOfLastDatapoint                | expectedEndDateOfLastFullMonth
        ${getDateInUtc(2020, 8, 31)}       | ${getDateAndTimeInUtc(2020, 8, 31, 23, 59, 59, 999)}
        ${getDateInUtc(2020, 8, 15)}       | ${getDateAndTimeInUtc(2020, 7, 31, 23, 59, 59, 999)}
        ${getDateInUtc(2020, 3, 30)}        | ${getDateAndTimeInUtc(2020, 2, 29, 23, 59, 59, 999)}
    `("end data of last full month should be $expectedEndDateOfLastFullMonth for (dateOfLastDatapoint: $dateOfLastDatapoint)", ({ dateOfLastDatapoint, expectedEndDateOfLastFullMonth }) => {
        const actualEndDateOfLastFullMonth = getEndDateOfFullMonth(dateOfLastDatapoint);
        expect(actualEndDateOfLastFullMonth).toStrictEqual(expectedEndDateOfLastFullMonth);
    });
});

