import { IAverageDescriptor, MakeUpTo, TotalisationPeriodUnit } from "client/BrandVueApi";
import {isCustomPeriodAverage} from "../components/helpers/PeriodHelper";
import moment from "moment/moment";

export const formatDate = (date: Date): string => {
    return moment.utc(date).utc().format("YYYY-MM-DD HH:mm");
}

export class DateFormattingHelper {

    public static formatDatePoint(date: Date, averageDescriptor: IAverageDescriptor): string {

        const endDate = moment.utc(date);
        let formattedDate;

        switch (averageDescriptor.makeUpTo) {

            case MakeUpTo.QuarterEnd:
                if (averageDescriptor.numberOfPeriodsInAverage === 3) {
                    formattedDate = endDate.format("[Q]Q YYYY");
                }
                else {
                    formattedDate = endDate.format("DD MMM YYYY");
                }
                break;

            case MakeUpTo.HalfYearEnd:
                if (averageDescriptor.numberOfPeriodsInAverage === 6) {
                    const halfYearNumber = endDate.month() < 6 ? "1st" : "2nd";
                    formattedDate = endDate.format(`[${halfYearNumber} half of] YYYY`);
                }
                else {
                    formattedDate = endDate.format("DD MMM YYYY");
                }
                break;

            case MakeUpTo.CalendarYearEnd:
                formattedDate = DateFormattingHelper.formatDateRange(date, averageDescriptor);
                break;

            case MakeUpTo.MonthEnd:
                if (averageDescriptor.numberOfPeriodsInAverage === 1) {
                    formattedDate = endDate.format("MMM YYYY");
                }
                else {
                    formattedDate = DateFormattingHelper.formatDateRange(date, averageDescriptor);
                }
                break;

            case MakeUpTo.Day:
                formattedDate = endDate.format("DD MMM YYYY");
                if (averageDescriptor.numberOfPeriodsInAverage > 1) {
                    formattedDate += " (L" + averageDescriptor.displayName + ")";
                }
                break;

            default:
                formattedDate = endDate.format("DD MMM YYYY");
                break;
        }
        return formattedDate;
    }


    public static formatDateRange(date: Date, averageDescriptor: IAverageDescriptor): string {
        const speratator = " - ";
        const endDate = moment.utc(date);

        if (isCustomPeriodAverage(averageDescriptor)) {
            return "Overall"
        }

        let formattedDate;
        if (averageDescriptor.makeUpTo === MakeUpTo.MonthEnd && averageDescriptor.numberOfPeriodsInAverage === 1) {
            formattedDate = endDate.format("MMM YYYY");
        } else if (averageDescriptor.makeUpTo === MakeUpTo.CalendarYearEnd) {
            formattedDate = endDate.format("YYYY"); 
        } else if (averageDescriptor.makeUpTo === MakeUpTo.QuarterEnd && averageDescriptor.numberOfPeriodsInAverage === 3) {
            formattedDate = endDate.format("[Q]Q YYYY");
        } else if (averageDescriptor.makeUpTo === MakeUpTo.HalfYearEnd && averageDescriptor.numberOfPeriodsInAverage === 6) {
            const halfYearNumber = endDate.month() < 6 ? "1st" : "2nd";
            formattedDate = endDate.format(`[${halfYearNumber} half of] YYYY`);
        } else if (averageDescriptor.makeUpTo === MakeUpTo.MonthEnd) {
            formattedDate = endDate.format("MMM YYYY") + " (L" +averageDescriptor.numberOfPeriodsInAverage+"M)";
        } else if(averageDescriptor.totalisationPeriodUnit === TotalisationPeriodUnit.Day) {
            let startDate = moment.utc(date);
            startDate.add(-averageDescriptor.numberOfPeriodsInAverage, "day");
            startDate.add(1, "day"); //To make the dates inclusive...

            if (startDate.year() !== endDate.year()) {
                formattedDate = startDate.format("D MMM YYYY") + speratator + endDate.format("D MMM YYYY");
            } else if (startDate.month() !== endDate.month()) {
                formattedDate = startDate.format("D MMM") + speratator + endDate.format("D MMM YYYY");
            } else if (startDate.day() !== endDate.day()) {
                formattedDate = startDate.format("D MMM") + speratator + endDate.format("D MMM YYYY");
            } else {
                formattedDate = endDate.format("D MMM YYYY");
            }

        }
        return formattedDate;
    };

}
