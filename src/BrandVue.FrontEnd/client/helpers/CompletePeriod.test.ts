import * as BrandVueApi from "../BrandVueApi";
import { CompletePeriod } from "./CompletePeriod";
import { getDateInUtc } from "../components/helpers/PeriodHelper";
import MakeUpTo = BrandVueApi.MakeUpTo;

const getLastDayInCurrentCompletePeriodCases: [Date, Date, MakeUpTo][] = [
    [getDateInUtc(2018, 6, 30, 23, 59, 59),  getDateInUtc(2018, 6, 30), MakeUpTo.MonthEnd],
    [getDateInUtc(2018, 6, 30, 23, 59, 59),  getDateInUtc(2018, 6, 15), MakeUpTo.MonthEnd],
    [getDateInUtc(2018, 9, 30, 23, 59, 59),  getDateInUtc(2018, 9, 30), MakeUpTo.QuarterEnd],
    [getDateInUtc(2018, 9, 30, 23, 59, 59),  getDateInUtc(2018, 9, 15), MakeUpTo.QuarterEnd],
    [getDateInUtc(2018, 3, 31, 23, 59, 59),  getDateInUtc(2018, 2, 15), MakeUpTo.QuarterEnd],
    [getDateInUtc(2018, 6, 30, 23, 59, 59),  getDateInUtc(2018, 2, 15), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 6, 30, 23, 59, 59),  getDateInUtc(2018, 5, 15), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 6, 30, 23, 59, 59),  getDateInUtc(2018, 6, 30), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 12, 31, 23, 59, 59), getDateInUtc(2018, 9, 30), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 12, 31, 23, 59, 59), getDateInUtc(2018, 9, 30), MakeUpTo.CalendarYearEnd],
    [getDateInUtc(2018, 12, 31, 23, 59, 59), getDateInUtc(2018, 12, 31), MakeUpTo.CalendarYearEnd],
    [getDateInUtc(2018, 9, 30, 23, 59, 59),  getDateInUtc(2018, 9, 30), MakeUpTo.Day]
];

test.each(getLastDayInCurrentCompletePeriodCases)("Should return %s when calculating max date from %s when making up to %s",
    (expected: Date, date: Date, makeUpTo: MakeUpTo) => {
        expect(CompletePeriod.getLastDayInCurrentPeriod(date, makeUpTo).toString()).toBe(expected.toString());
    });


const getFirstDayInCurrentCompletePeriodCases: [Date, Date, MakeUpTo][] = [
    [getDateInUtc(2018, 6, 1),  getDateInUtc(2018, 6, 1), MakeUpTo.MonthEnd],
    [getDateInUtc(2018, 6, 1),  getDateInUtc(2018, 6, 15), MakeUpTo.MonthEnd],
    [getDateInUtc(2018, 7, 1),  getDateInUtc(2018, 7, 1), MakeUpTo.QuarterEnd],
    [getDateInUtc(2018, 7, 1),  getDateInUtc(2018, 9, 15), MakeUpTo.QuarterEnd],
    [getDateInUtc(2018, 1, 1),  getDateInUtc(2018, 2, 15), MakeUpTo.QuarterEnd],
    [getDateInUtc(2018, 1, 1),  getDateInUtc(2018, 2, 15), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 1, 1),  getDateInUtc(2018, 5, 15), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 1, 1),  getDateInUtc(2018, 6, 1), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 7, 1),  getDateInUtc(2018, 9, 1), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 1, 1),  getDateInUtc(2018, 9, 1), MakeUpTo.CalendarYearEnd],
    [getDateInUtc(2018, 1, 1),  getDateInUtc(2018, 12, 31), MakeUpTo.CalendarYearEnd],
    [getDateInUtc(2018, 9, 30), getDateInUtc(2018, 9, 30), MakeUpTo.Day]
];

test.each(getFirstDayInCurrentCompletePeriodCases)("Should return %s when calculating min date from %s when making up to %s",
    (expected: Date, date: Date, makeUpTo: MakeUpTo) => {
        expect(CompletePeriod.getFirstDayInCurrentPeriod(date, makeUpTo).toString()).toBe(expected.toString());
    });

const getLastDayInLastCompletePeriodCases: [Date, Date, MakeUpTo][] = [
    [getDateInUtc(2018, 6, 30, 23, 59, 59),  getDateInUtc(2018, 6, 30), MakeUpTo.MonthEnd],
    [getDateInUtc(2018, 5, 31, 23, 59, 59),  getDateInUtc(2018, 6, 15), MakeUpTo.MonthEnd],
    [getDateInUtc(2018, 9, 30, 23, 59, 59),  getDateInUtc(2018, 9, 30), MakeUpTo.QuarterEnd],
    [getDateInUtc(2018, 6, 30, 23, 59, 59),  getDateInUtc(2018, 9, 15), MakeUpTo.QuarterEnd],
    [getDateInUtc(2017, 12, 31, 23, 59, 59), getDateInUtc(2018, 2, 15), MakeUpTo.QuarterEnd],
    [getDateInUtc(2017, 12, 31, 23, 59, 59), getDateInUtc(2018, 2, 15), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2017, 12, 31, 23, 59, 59), getDateInUtc(2018, 5, 15), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 6, 30, 23, 59, 59),  getDateInUtc(2018, 6, 30), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 6, 30, 23, 59, 59),  getDateInUtc(2018, 9, 30), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2017, 12, 31, 23, 59, 59), getDateInUtc(2018, 9, 30), MakeUpTo.CalendarYearEnd],
    [getDateInUtc(2018, 12, 31, 23, 59, 59), getDateInUtc(2018, 12, 31), MakeUpTo.CalendarYearEnd],
    [getDateInUtc(2018, 9, 30, 23, 59, 59),  getDateInUtc(2018, 9, 30), MakeUpTo.Day]
];

test.each(getLastDayInLastCompletePeriodCases)("Should return %s when calculating max date from %s when making up to %s",
    (expected: Date, date: Date, makeUpTo: MakeUpTo) => {
        expect(CompletePeriod.getLastDayInLastCompletePeriod(date, makeUpTo).toString()).toBe(expected.toString());
    });


const getFirstDayInNextCompletePeriodCases: [Date, Date, MakeUpTo][] = [
    [getDateInUtc(2018, 7, 1),  getDateInUtc(2018, 7, 1), MakeUpTo.MonthEnd],
    [getDateInUtc(2018, 7, 1),  getDateInUtc(2018, 6, 15), MakeUpTo.MonthEnd],
    [getDateInUtc(2018, 10, 1),  getDateInUtc(2018, 10, 1), MakeUpTo.QuarterEnd],
    [getDateInUtc(2018, 10, 1),  getDateInUtc(2018, 9, 15), MakeUpTo.QuarterEnd],
    [getDateInUtc(2019, 1, 1),  getDateInUtc(2018, 12, 15), MakeUpTo.QuarterEnd],
    [getDateInUtc(2018, 7, 1),  getDateInUtc(2018, 2, 15), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2019, 1, 1),  getDateInUtc(2018, 9, 15), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 7, 1),  getDateInUtc(2018, 7, 1), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2018, 1, 1),  getDateInUtc(2018, 1, 1), MakeUpTo.HalfYearEnd],
    [getDateInUtc(2019, 1, 1),  getDateInUtc(2018, 9, 30), MakeUpTo.CalendarYearEnd],
    [getDateInUtc(2018, 1, 1),  getDateInUtc(2018, 1, 1), MakeUpTo.CalendarYearEnd],
    [getDateInUtc(2018, 9, 15), getDateInUtc(2018, 9, 15), MakeUpTo.Day]
];

test.each(getFirstDayInNextCompletePeriodCases)("Should return %s when calculating min date from %s when making up to %s",
    (expected: Date, date: Date, makeUpTo: MakeUpTo) => {
        expect(CompletePeriod.getFirstDayInNextCompletePeriod(date, makeUpTo).toString()).toBe(expected.toString());
    });


