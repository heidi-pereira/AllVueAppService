import * as BrandVueApi from "../BrandVueApi";
import moment from "moment";

export type XAxisOptions = {
    tickPositioner: ((min, max) => any[]) | undefined,
    labelXformatter: string
}

export class XAxisFormatting {
    public static formatAll(makeUpTo: BrandVueApi.MakeUpTo): XAxisOptions {
        switch (makeUpTo) {
            case BrandVueApi.MakeUpTo.WeekEnd:
                return XAxisFormatting.weekFormatter();
            case BrandVueApi.MakeUpTo.MonthEnd:
                return XAxisFormatting.monthFormatter();
            case BrandVueApi.MakeUpTo.QuarterEnd:
                return XAxisFormatting.quarterFormatter();
            case BrandVueApi.MakeUpTo.HalfYearEnd:
                return XAxisFormatting.halfYearFormatter();
            case BrandVueApi.MakeUpTo.CalendarYearEnd:
                return XAxisFormatting.yearFormatter();
            default:
                return { tickPositioner: undefined , labelXformatter: "DD<br>MMM" };
        }
    }

    public static weekFormatter(): XAxisOptions {
        return {
            tickPositioner: (min, max) => {
                var ticks: any = [];
                if (min !== undefined && max !== undefined && min <= max) {
                    const minm = moment.utc(min).endOf("week").startOf('day');
                    const maxm = moment.utc(max);
                    while (minm <= maxm) {
                        ticks.push(minm.valueOf());
                        minm.startOf("week").add(1, "week").endOf("week").startOf('day');
                    }
                }
                return ticks;
            },
            labelXformatter: "[w/e ]DD MMM"
        };
    }

    public static monthFormatter(): XAxisOptions {
        return {
            tickPositioner: (min, max) => {
                var ticks: any = [];
                if (min !== undefined && max !== undefined && min <= max) {
                    const minm = moment.utc(min).endOf("month").startOf('day');
                    const maxm = moment.utc(max);
                    while (minm <= maxm) {
                        ticks.push(minm.valueOf());
                        minm.startOf("month").add(1, "month").endOf("month").startOf('day');
                    }
                }
                return ticks;
            },
            labelXformatter: "MMM<br>YY"
        };
    }

    public static quarterFormatter(): XAxisOptions {
        return {
            tickPositioner: (min, max) => {
                var ticks: any = [];
                if (min !== undefined && max !== undefined && min <= max) {
                    const minm = moment.utc(min).endOf("quarter").startOf('day');
                    const maxm = moment.utc(max);
                    while (minm <= maxm) {
                        ticks.push(minm.valueOf());
                        minm.startOf("quarter").add(1, "quarter").endOf("quarter").startOf('day');
                    }
                }
                return ticks;
            },
            labelXformatter: "[Q]Q YYYY"
        };
    }

    public static halfYearFormatter(): XAxisOptions {
        return {
            tickPositioner: (min, max) => {
                var ticks: any = [];
                if (min !== undefined && max !== undefined && min <= max) {
                    const minDate = moment.utc(min);
                    let minm: moment.Moment;
                    if (minDate.month() < 6) {
                        minm = moment.utc([minDate.year(), 5, 1]).endOf("quarter").startOf('day');
                    } else {
                        minm = moment.utc([minDate.year(), 11, 1]).endOf("quarter").startOf('day');
                    }

                    const maxm = moment.utc(max);
                    while (minm <= maxm) {
                        ticks.push(minm.valueOf());
                        minm.startOf("quarter").add(2, "quarter").endOf("quarter").startOf('day');
                    }
                }
                return ticks;
            },
            labelXformatter: "[$$HY$$ half of] YYYY"
        };
    } 

    public static yearFormatter(): XAxisOptions {
        return {
            tickPositioner: (min, max) => {
                var ticks: any = [];
                if (min && max && min <= max) {
                    const minm = moment.utc(min).endOf("year").startOf('day');
                    const maxm = moment.utc(max);

                    while (minm <= maxm) {
                        ticks.push(minm.valueOf());
                        minm.startOf("year").add(1, "year").endOf("year").startOf('day');
                    }
                }

                return ticks;
            },
            labelXformatter: "YYYY"
        };
    }
}
