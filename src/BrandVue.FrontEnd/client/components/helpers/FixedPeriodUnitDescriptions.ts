import moment from "moment";
import * as BrandVueApi from "../../BrandVueApi";
import { CompletePeriod } from "../../helpers/CompletePeriod";

export default class FixedPeriodUnitDescriptions {
    static getPeriodName(period: number, makeUpTo: BrandVueApi.MakeUpTo): string {
        switch (makeUpTo) {
            case BrandVueApi.MakeUpTo.QuarterEnd:
                return this.getQuarterName(period);
            case BrandVueApi.MakeUpTo.HalfYearEnd:
                return this.getHalfYearName(period);
            case BrandVueApi.MakeUpTo.CalendarYearEnd:
                return this.getYearName(period);
        }

        return "";
    }

    static getPeriodDescription(dateStart: Date, dateEnd: Date, makeUpTo: BrandVueApi.MakeUpTo): string {
        const firstDayInNextPeriodOfStartDate = CompletePeriod.getFirstDayInNextCompletePeriod(dateStart, makeUpTo);
        const lastDayInPeriodBeforeEndDate = CompletePeriod.getLastDayInLastCompletePeriod(dateEnd, makeUpTo);
        switch (makeUpTo) {
            case BrandVueApi.MakeUpTo.MonthEnd:
                return this.getMonthDescription(firstDayInNextPeriodOfStartDate, lastDayInPeriodBeforeEndDate);
            case BrandVueApi.MakeUpTo.QuarterEnd:
                return this.getQuarterDescription(firstDayInNextPeriodOfStartDate, lastDayInPeriodBeforeEndDate);
            case BrandVueApi.MakeUpTo.HalfYearEnd:
                return this.getHalfYearDescription(firstDayInNextPeriodOfStartDate, lastDayInPeriodBeforeEndDate);
            case BrandVueApi.MakeUpTo.CalendarYearEnd:
                return this.getYearDescription(firstDayInNextPeriodOfStartDate, lastDayInPeriodBeforeEndDate);
        }

        return "";
    }

    static getSelectedDropdownValueText(period: number, makeUpTo: BrandVueApi.MakeUpTo): string {
        switch (makeUpTo) {
        case BrandVueApi.MakeUpTo.MonthEnd:
                return this.getMonthDropdownText(period);
        case BrandVueApi.MakeUpTo.QuarterEnd:
                return this.getQuarterDropdownText(period);
        case BrandVueApi.MakeUpTo.HalfYearEnd:
                return this.getHalfYearDropdownText(period);
        }

        return period.toString();
    }

    static getPrimaryOptionsDropdownValueText(period: number, makeUpTo: BrandVueApi.MakeUpTo): string {
        switch (makeUpTo) {
        case BrandVueApi.MakeUpTo.QuarterEnd:
            return this.getQuarterDropdownText(period);
        case BrandVueApi.MakeUpTo.HalfYearEnd:
            return this.getHalfYearDropdownText(period);
        }

        return "";
    }

    static getSecondaryOptionsDropdownValueText(period: number, makeUpTo: BrandVueApi.MakeUpTo): string {
        switch (makeUpTo) {
        case BrandVueApi.MakeUpTo.QuarterEnd:
            return this.getQuarterName(period);
        case BrandVueApi.MakeUpTo.HalfYearEnd:
                return this.getHalfYearName(period);
        case BrandVueApi.MakeUpTo.MonthEnd:
                return this.getMonthDropdownText(period);
        case BrandVueApi.MakeUpTo.CalendarYearEnd:
            return period.toString();
        }

        return "";
    }

    static getPeriodLabel(makeUpTo: BrandVueApi.MakeUpTo): string {
        switch (makeUpTo) {
            case BrandVueApi.MakeUpTo.Day:
                return "Select a day";
            case BrandVueApi.MakeUpTo.WeekEnd:
                return "Select a week";
            case BrandVueApi.MakeUpTo.MonthEnd:
                return "Select a month";
            case BrandVueApi.MakeUpTo.QuarterEnd:
                return "Select a quarter";
            case BrandVueApi.MakeUpTo.HalfYearEnd:
                return "Select a half year";
            case BrandVueApi.MakeUpTo.CalendarYearEnd:
                return "Select a year";
        }

        return "Select a date";
    }

    private static getMonthDropdownText(period: number): string {
        return this.getMonthName(period);
    }

    private static getQuarterDropdownText(period: number): string {
        return `Q${period}`;
    }

    private static getHalfYearDropdownText(period: number): string {
        return `${this.convertHalfYearToRank(period)} Half`;
    }

    private static monthNames = [
        "",
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December"
    ];

    private static getMonthName(month: number): string {
        return FixedPeriodUnitDescriptions.monthNames[month];
    }

    private static getQuarterName(quarter: number): string {
        switch (quarter) {
            case 1:
                return "(Jan-Mar)";
            case 2:
                return "(Apr-Jun)";
            case 3:
                return "(Jul-Sept)";
            case 4:
                return "(Oct-Dec)";
        }
        throw new Error(`${quarter} is an invalid quarter`);
    }

    private static getHalfYearName(halfYear: number): string {
        switch (halfYear) {
            case 1:
                return "(Jan-Jun)";
            case 2:
                return "(Jul-Dec)";
        }
        throw new Error(`${halfYear} is an invalid half year`);
    }

    private static getYearName(year: number): string {
        return year ? year.toString() : "Choose Year";
    }

    private static convertHalfYearToRank(position: number): string {
        switch (position) {
            case 1:
                return "1st";
            case 2:
                return "2nd";
        }
        throw new Error(`${position} is an invalid half year`);
    }

    private static getMonthDescription(dateStart: Date, dateEnd: Date): string {
        let startDate = moment.utc(dateStart);
        let endDate = moment.utc(dateEnd);
        let description = `Full monthly data available <br />from ${startDate.format("MMM YYYY")} to ${endDate.format("MMM YYYY")}`;
        let numberOfMonths = moment.utc(dateEnd).diff(moment.utc(dateStart), "months");
        if (dateStart > dateEnd) {
            description = `No complete monthly data available`;
        } else if (numberOfMonths < 1) {
            description = `Full monthly data available for ${endDate.format("MMM YYYY")} only`;
        }
        return description;
    }

    private static getQuarterDescription(dateStart: Date, dateEnd: Date) {
        const startDate = moment.utc(dateStart);
        const endDate = moment.utc(dateEnd);
        const validStartQ = startDate.quarter();
        const validStartY = startDate.year();
        const validEndQ = endDate.quarter();
        const validEndY = endDate.year();

        let description = `Full quarterly data available from <br />Q${validStartQ} ${FixedPeriodUnitDescriptions.getQuarterName(validStartQ)} ${validStartY} to Q${validEndQ} ${FixedPeriodUnitDescriptions.getQuarterName(validEndQ)} ${validEndY}`;
        if (validStartQ === validEndQ && validStartY === validEndY) {
            description = `Full quarterly data available for Q${validStartQ} ${FixedPeriodUnitDescriptions.getQuarterName(validStartQ)} ${validStartY} only`;
        } else if (validStartY > validEndY || validStartY === validEndY && validStartQ > validEndQ) {
            description = `No complete quarters available`;
        }
        return description;
    }

    private static getHalfYearDescription(dateStart: Date, dateEnd: Date): string {
        const startDate = moment.utc(dateStart);
        const endDate = moment.utc(dateEnd);
        const validStartHy = startDate.quarter() === 1 || startDate.quarter() === 2 ? 1 : 2;
        const validStartY = startDate.year();
        const validEndHy = endDate.quarter() === 1 || endDate.quarter() === 2 ? 1 : 2;
        const validEndY = endDate.year();
        const startHalf = FixedPeriodUnitDescriptions.convertHalfYearToRank(validStartHy);
        const endHalf = FixedPeriodUnitDescriptions.convertHalfYearToRank(validEndHy);
        const startHalfDescription = FixedPeriodUnitDescriptions.getHalfYearName(validStartHy);
        const endHalfDescription = FixedPeriodUnitDescriptions.getHalfYearName(validEndHy);

        let description = `Full half yearly data available from the <br />${startHalf} half of ${validStartY} ${startHalfDescription} to the ${endHalf} half of ${validEndY} ${endHalfDescription}`;
        if (validStartHy === validEndHy && validStartY === validEndY) {
            description = `Full half yearly data available for the <br/>${endHalf} half of ${validEndY} ${endHalfDescription} only`;
        } else if (validStartY > validEndY || validStartY === validEndY && validStartHy > validEndHy){
            description = `No complete half years available`;
        }
        return description;
    }

    private static getYearDescription(dateStart: Date, dateEnd: Date): string {
        const yearStart = dateStart.getUTCFullYear();
        const yearEnd = dateEnd.getUTCFullYear();
        let description = `Full yearly data available from <br />${dateStart.getUTCFullYear() + 1} to ${dateEnd.getUTCFullYear()}`;

        if (yearStart === yearEnd) {
            description = `Full yearly data available for <br /> ${yearEnd} only`;
        } else if (yearStart > yearEnd) {
            description = `No full yearly data available`;
        }

        return description;
    }

}