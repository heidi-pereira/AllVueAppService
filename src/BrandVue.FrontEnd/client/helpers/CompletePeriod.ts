import * as BrandVueApi from "../BrandVueApi";
import moment from "moment";

export class CompletePeriod {
    public static getLastDayInCurrentPeriod(date: Date, makeUpTo: BrandVueApi.MakeUpTo): Date {
        const maximum = moment.utc(date);
        switch (makeUpTo) {
            case BrandVueApi.MakeUpTo.WeekEnd:
                return maximum.endOf("week").toDate();
            case BrandVueApi.MakeUpTo.MonthEnd:
                return maximum.endOf("month").toDate();
            case BrandVueApi.MakeUpTo.QuarterEnd:
                return maximum.endOf("quarter").toDate();
            case BrandVueApi.MakeUpTo.HalfYearEnd:
                if (maximum.quarter() === 1 || maximum.quarter() === 3) {
                    return maximum.add({ 'quarter': 1 }).endOf("quarter").toDate();
                }
                return maximum.endOf("quarter").toDate();
            case BrandVueApi.MakeUpTo.CalendarYearEnd:
                return maximum.utc().endOf("year").toDate();
        }
        return maximum.endOf("day").toDate();
    }

    public static getFirstDayInCurrentPeriod(date: Date, makeUpTo: BrandVueApi.MakeUpTo): Date {
        const maximum = moment.utc(date);
        switch (makeUpTo) {
        case BrandVueApi.MakeUpTo.WeekEnd:
            return maximum.startOf("week").toDate();
        case BrandVueApi.MakeUpTo.MonthEnd:
            return maximum.startOf("month").toDate();
        case BrandVueApi.MakeUpTo.QuarterEnd:
            return maximum.startOf("quarter").toDate();
        case BrandVueApi.MakeUpTo.HalfYearEnd:
            if (maximum.quarter() === 2 || maximum.quarter() === 4) {
                return maximum.subtract({ 'quarter': 1 }).startOf("quarter").toDate();
            }
            return maximum.startOf("quarter").toDate();
        case BrandVueApi.MakeUpTo.CalendarYearEnd:
            return maximum.utc().startOf("year").toDate();
        }
        return maximum.startOf("day").toDate();
    }

    public static getLastDayInLastCompletePeriod(date: Date, makeUpTo: BrandVueApi.MakeUpTo): Date {
        let returnDate = date;
        const originalMaximum = moment.utc(date);
        const currentMaximumAsMoment = moment.utc(date);
        switch (makeUpTo) {
            case BrandVueApi.MakeUpTo.WeekEnd:
                const weekDaysDiff = currentMaximumAsMoment.endOf("week").diff(originalMaximum, "days");
                if (weekDaysDiff !== 0) {
                    returnDate = currentMaximumAsMoment.subtract({ 'week': 1 }).toDate();
                }
                break;
            case BrandVueApi.MakeUpTo.MonthEnd:
                const monthDaysDiff = currentMaximumAsMoment.endOf("month").diff(originalMaximum, "days");
                if (monthDaysDiff !== 0) {
                    returnDate = currentMaximumAsMoment.subtract({ 'month': 1 }).toDate();
                }
                break;
            case BrandVueApi.MakeUpTo.QuarterEnd:
                const quarterDaysDiff = currentMaximumAsMoment.endOf("quarter").diff(originalMaximum, "days");
                if (quarterDaysDiff !== 0) {
                    returnDate = currentMaximumAsMoment.subtract({ 'quarter': 1 }).toDate();
                }
                break;
            case BrandVueApi.MakeUpTo.HalfYearEnd:
                let quarterOffset = 1;
                let halfYearDaysDiff = 0;
                if (currentMaximumAsMoment.quarter() === 1 || currentMaximumAsMoment.quarter() === 3) {
                    halfYearDaysDiff = currentMaximumAsMoment.add({ 'quarter': 1 }).endOf("quarter").diff(originalMaximum, "days");
                } else {
                    halfYearDaysDiff = currentMaximumAsMoment.endOf("quarter").diff(originalMaximum, "days");
                    quarterOffset = 2;
                }
                if (halfYearDaysDiff !== 0) {
                    returnDate = originalMaximum.subtract({ 'quarter': quarterOffset }).toDate();
                }
                break;
            case BrandVueApi.MakeUpTo.CalendarYearEnd:
                const yearDaysDiff = currentMaximumAsMoment.utc().endOf("year").diff(originalMaximum, "days");
                if (yearDaysDiff !== 0) {
                    returnDate = currentMaximumAsMoment.utc().subtract({ 'year': 1 }).toDate();
                }
                break;
        }
        return CompletePeriod.getLastDayInCurrentPeriod(returnDate, makeUpTo);
    }

    public static getFirstDayInNextCompletePeriod(date: Date, makeUpTo: BrandVueApi.MakeUpTo): Date {
        let returnDate = date;
        const originalMaximum = moment.utc(date);
        const currentMaximumAsMoment = moment.utc(date);
        switch (makeUpTo) {
        case BrandVueApi.MakeUpTo.WeekEnd:
            const weekDaysDiff = currentMaximumAsMoment.startOf("week").diff(originalMaximum, "days");
            if (weekDaysDiff !== 0) {
                returnDate = currentMaximumAsMoment.add({ 'week': 1 }).toDate();
            }
            break;
        case BrandVueApi.MakeUpTo.MonthEnd:
            const monthDaysDiff = currentMaximumAsMoment.startOf("month").diff(originalMaximum, "days");
            if (monthDaysDiff !== 0) {
                returnDate = currentMaximumAsMoment.add({ 'month': 1 }).toDate();
            }
            break;
        case BrandVueApi.MakeUpTo.QuarterEnd:
            const quarterDaysDiff = currentMaximumAsMoment.startOf("quarter").diff(originalMaximum, "days");
            if (quarterDaysDiff !== 0) {
                returnDate = currentMaximumAsMoment.add({ 'quarter': 1 }).toDate();
            }
            break;
        case BrandVueApi.MakeUpTo.HalfYearEnd:
            let quarterOffset = 1;
            let halfYearDaysDiff = 0;
            if (currentMaximumAsMoment.quarter() === 1 || currentMaximumAsMoment.quarter() === 3) {
                halfYearDaysDiff = currentMaximumAsMoment.startOf("quarter").diff(originalMaximum, "days");
                quarterOffset = 2;
            } else {
                halfYearDaysDiff = currentMaximumAsMoment.subtract({ 'quarter': 1 }).startOf("quarter").diff(originalMaximum, "days");
            }
            if (halfYearDaysDiff !== 0) {
                returnDate = originalMaximum.add({ 'quarter': quarterOffset }).toDate();
            }
            break;
        case BrandVueApi.MakeUpTo.CalendarYearEnd:
            const yearDaysDiff = currentMaximumAsMoment.startOf("year").diff(originalMaximum, "days");
            if (yearDaysDiff !== 0) {
                returnDate = currentMaximumAsMoment.add({ 'year': 1 }).toDate();
            }
            break;
        }
        return CompletePeriod.getFirstDayInCurrentPeriod(returnDate, makeUpTo);
    }

}