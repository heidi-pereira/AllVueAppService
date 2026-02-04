import moment, { Moment } from "moment";
import { CompletePeriod } from "../../helpers/CompletePeriod";
import { CustomDateRange, MakeUpTo, PeriodType } from "../../BrandVueApi";
import { getStartDateForDefault13MonthPeriod } from './PeriodHelper';
import { QueryStringParamNames, IReadVueQueryParams, IWriteVueQueryParams } from './UrlHelper';
import { UnreachableCaseError } from 'client/helpers/UnreachableCaseError';
import { ApplicationConfiguration } from 'client/ApplicationConfiguration';
import { getDateRangePickerTitleFromMoments } from 'client/components/helpers/SurveyVueUtils';

export const getEndOfLastMonthWithData = (dateOfLastDataPoint: Date): Date => CompletePeriod.getLastDayInLastCompletePeriod(dateOfLastDataPoint, MakeUpTo.MonthEnd);
export const getEndOfPreviousMonthWithData = (dateOfLastDataPoint): Date => moment.utc(CompletePeriod.getLastDayInLastCompletePeriod(dateOfLastDataPoint, MakeUpTo.MonthEnd)).subtract(1, "month").endOf("month").toDate();

export const getNameOfPeriodBetween = (startDate: Date, endDate: Date): string => {
    const start = moment.utc(startDate);
    const end = moment.utc(endDate);

    //thanks momentjs https://github.com/moment/moment/issues/832
    const startClone = start.clone();
    const endClone = end.clone();
    const bothStartOfMonth = start.isSame(startClone.startOf('month'), 'day') && end.isSame(endClone.startOf('month'), 'day');
    const bothEndOfMonth = start.isSame(startClone.endOf('month'), 'day') && end.isSame(endClone.endOf('month'), 'day');
    const startToEnd = start.isSame(startClone.startOf('month'), 'day') && end.isSame(endClone.endOf('month'), 'day');

    const pluralize = (name: string, value: number): string => {
        return value == 1 ? name : `${value} ${name}s`
    }

    if (startToEnd || bothStartOfMonth || bothEndOfMonth) {
        const months = startToEnd ?
            end.diff(start, 'months') + 1:
            end.diff(start, 'months');

        if (months % 12 == 0) {
            return pluralize('Year', months / 12);
        }
        return pluralize('Month', months);
    }

    const days = end.diff(start, 'days');
    if (days % 7 == 0) {
        return pluralize('Week', days / 7);
    }
    return pluralize('Day', days);
}

export function getEndFromQuery(maximum: Moment, readVueQueryParams: IReadVueQueryParams, searchParams: URLSearchParams) : Moment {
    const endString = readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.end)
        ?? searchParams.get(QueryStringParamNames.end)
        ?? readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.now);
    let end: moment.Moment;
    if (endString && endString.trim() !== '') {
        end = moment.utc(endString);
    } else {
        end = maximum.clone();
    }
    return end;
}

export function getStartFromQuery(minimum: Moment, maximum: Moment, end: Moment, readVueQueryParams: IReadVueQueryParams, searchParams: URLSearchParams): Moment {
    const rangeLookup = rangeCalculations(() => end.clone(), minimum, maximum);
    const rangeString = readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.range);
    const rangeDate = rangeLookup.find(l => l.url === rangeString);
    const startString = readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.start)
        ?? searchParams.get(QueryStringParamNames.start);
    let start: moment.Moment;
    if (startString && typeof startString === 'string' && startString.trim() !== '') {
        start = moment.utc(startString);
    } else if (rangeDate && rangeDate.start) {
        start = rangeDate.start.clone();
    } else {
        start = moment.utc(getStartDateForDefault13MonthPeriod(end.toDate(), minimum.toDate()));
    }
    return start;
}

export function getStartEndDateUTCFromUrl(dateOfFirstDataPoint: Date, dateOfLastDataPoint: Date, updateQuery: boolean, readVueQueryParams: IReadVueQueryParams, writeVueQueryParams: IWriteVueQueryParams) {
    const minimum = moment.utc(dateOfFirstDataPoint);
    const maximum = moment.utc(dateOfLastDataPoint);
    const searchParams = new URLSearchParams(window.location.search);
    const endString = readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.end)
        ?? searchParams.get(QueryStringParamNames.end)
        ?? readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.now);
    let end = getEndFromQuery(maximum, readVueQueryParams, searchParams);
    const startString = readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.start)
        ?? searchParams.get(QueryStringParamNames.start);
    let start = getStartFromQuery(minimum, maximum, end, readVueQueryParams, searchParams);
    const rangeLookup = rangeCalculations(() => end.clone(), minimum, maximum);
    const rangeString = readVueQueryParams.getQueryParameter<string>(QueryStringParamNames.range);
    const matchingRange = rangeLookup.find(r =>
        r.start.format("YYYY-MM-DD") === start.format("YYYY-MM-DD") &&
        r.end.format("YYYY-MM-DD") === end.format("YYYY-MM-DD")
    );
    
    if (startString || endString) {
        if (updateQuery) {
            if (matchingRange) {
                const endShortDate = end.format("YYYY-MM-DD");
                writeVueQueryParams.setQueryParameters([
                    { name: QueryStringParamNames.range, value: matchingRange.url },
                    { name: QueryStringParamNames.start, value: "" },
                    { name: QueryStringParamNames.end, value: endShortDate === maximum.format("YYYY-MM-DD") ? "" : endShortDate }]);
            }
        }
    } else if (rangeLookup.length > 0 && !rangeString) {
        const defaultRange = "Last 13 months";
        const range = rangeLookup.find(r => r.name === defaultRange) || rangeLookup[0];
        if (range) {
            start = range.start.clone();
            end = range.end.clone();
        }
        if (updateQuery) {
            writeVueQueryParams.setQueryParameters([
                    { name: QueryStringParamNames.range, value: range.url },
                    { name: QueryStringParamNames.start, value: "" },
                    { name: QueryStringParamNames.end, value: "" }]);
        }
    }


    return {
        start: start,
        end: end
    };
}

export interface DateRange {
    uiString: string;
    dateUiString: string;
    name: string;
    url: string;
    start: moment.Moment;
    end: moment.Moment;
}

function forceInBounds(d: moment.MomentInput, minimum: moment.Moment, maximum: moment.Moment) {
    return moment.max(moment.min(moment.utc(d), maximum), minimum);
}

export function rangeCalculations(now: () => moment.Moment, minimum: moment.Moment, maximum: moment.Moment): DateRange[] {
    var monthOffset = 0;
    var dateNow = now().format('YYYY-MM-DD');
    var dateEndOfMonth = now().endOf("month").format('YYYY-MM-DD');
    if (dateNow === dateEndOfMonth) {
         monthOffset = 1;
    }

    const monthYearDateFormat = (s: moment.Moment, e: moment.Moment) => `${s.format("MMM YY")} - ${e.format("MMM YY")}`;
    const monthDateFormat = (s: moment.Moment, e: moment.Moment) => e.format("MMM YY");
    const weekDateFormat = (s: moment.Moment, e: moment.Moment) =>
        s.month() === e.month()
            ? `${s.format("D")}-${e.format("D MMM YY")}`
            : `${s.format("D MMM")} - ${e.format("D MMM YY")}`;
    return [
        {
            d: "All data",
            s: minimum.clone(),
            e: maximum.clone(),
            getLabel: monthYearDateFormat
        },
        {
            d: "This week",
            s: now().clone().isoWeekday(1).startOf('day'),
            e: now().clone().isoWeekday(7).endOf('day'),
            getLabel: weekDateFormat
        },
        {
            d: "Last month",
            s: now().clone().subtract({ month: 1 }).startOf("month"),
            e: now().clone().subtract({ month: 1 }).endOf("month"),
            getLabel: monthDateFormat
        },
        {
            d: "This month",
            s: now().clone().startOf("month"),
            e: now().clone().endOf("month"),
            getLabel: monthDateFormat
        },
        {
            d: "Last quarter",
            s: now().clone().subtract({ 'quarter': 1 }).startOf("quarter"),
            e: now().clone().subtract({ 'quarter': 1 }).endOf("quarter"),
            getLabel: monthYearDateFormat
        },
        {
            d: "This quarter",
            s: now().clone().startOf("quarter"),
            e: now().clone().endOf("quarter"),
            getLabel: monthYearDateFormat
        },
        {

            d: "Last 6 months",
            s: now().clone().subtract({ month: 6 - monthOffset }).startOf("month"),
            e: now().clone().subtract({ month: 1 - monthOffset }).endOf("month"),
            getLabel: monthYearDateFormat
        },
        {
            d: "This year",
            s: now().clone().startOf("year"),
            e: now().clone().endOf("year"),
            getLabel: monthYearDateFormat
        },
        {
            d: "Last 13 months",
            s: now().clone().subtract({ month: 13 - monthOffset }).startOf("month"),
            e: now().clone().subtract({ month: 1 - monthOffset }).endOf("month"),
            getLabel: monthYearDateFormat
        },
        {
            d: "Last 2 years",
            s: now().clone().subtract({ month: 25 - monthOffset }).startOf("month"),
            e: now().clone().subtract({ month: 1 - monthOffset }).endOf("month"),
            getLabel: monthYearDateFormat
        },
        {
            d: "Last 3 years",
            s: now().clone().subtract({ month: 37 - monthOffset }).startOf("month"),
            e: now().clone().subtract({ month: 1 - monthOffset }).endOf("month"),
            getLabel: monthYearDateFormat
        }
    ].map(r => {
        const s = forceInBounds(r.s, minimum, maximum), e = forceInBounds(r.e, minimum, maximum);
        const bounded = !(s.isSame(r.s) && e.isSame(r.e));
        const uiString = getDateUiString(r.d, s, e, bounded);
        const dateUiString = r.getLabel(s, e) + (bounded ? " *" : "");
        return {
            uiString: uiString,
            dateUiString: dateUiString,
            name: r.d,
            url: r.d.toLowerCase(),
            start: r.s,
            end: r.e
        };
    });
}

export function getDateUiString(name: string, start: moment.Moment, end: moment.Moment, bounded: boolean): string {
    const star = bounded ? " *" : "";
    return name +
        " (" +
        start.format("MMM") +
        ((start.month() !== end.month() || start.year() !== end.year()) ? "-" + end.format("MMM") : "") +
        ")" +
        star;
}

export function customRangeCalculation(customRange: CustomDateRange, applicationConfiguration: ApplicationConfiguration):
    { start: moment.Moment; end: moment.Moment; bounded: boolean; dateUiString: string; }
{
    const minimum = moment.utc(applicationConfiguration.dateOfFirstDataPoint);
    const maximum = moment.utc(applicationConfiguration.dateOfLastDataPoint);
    const now = () => moment.utc(maximum);

    const { numberOfPeriods, periodType } = customRange;

    if (numberOfPeriods <= 0) {
        throw new Error("Number of periods must be greater than 0");
    }

    const unit = getMomentUnitsFromPeriodType(periodType);

    const endIsOnPeriodEnd = now().isSame(now().endOf(unit.period), "day");
    const end = endIsOnPeriodEnd
        ? now().endOf(unit.period)
        : now().subtract(1, unit.duration).endOf(unit.period);

    const start = end.clone().subtract(numberOfPeriods - 1, unit.duration).startOf(unit.period);
    const startClamped = forceInBounds(start, minimum, maximum);
    const endClamped = forceInBounds(end, minimum, maximum);
    const bounded = !(start.isSame(startClamped) && end.isSame(endClamped));
    const dateUiString = getDateRangePickerTitleFromMoments(startClamped, endClamped) + (bounded ? " *" : "");

    return { start: startClamped, end: endClamped, bounded: bounded, dateUiString: dateUiString };
}

export function getMomentUnitsFromPeriodType(periodType: PeriodType): {
    duration: moment.unitOfTime.DurationConstructor,
    period: moment.unitOfTime.StartOf
} {
    switch (periodType) {
        case PeriodType.Day:
            return {
                duration: "day",
                period: "day"
            };
        case PeriodType.Week:
            return {
                duration: "week",
                period: "isoWeek"
            };
        case PeriodType.Month:
            return {
                duration: "month",
                period: "month"
            };
        case PeriodType.Quarter:
            return {
                duration: "quarter",
                period: "quarter"
            };
        case PeriodType.Year:
            return {
                duration: "year",
                period: "year"
            };
        default:
            throw new UnreachableCaseError(periodType);
    }
}

export const getFormatedDate = (date: Date | undefined) => {
    if (date != null) {
        return moment(date).format("DD MMM YYYY");
    }
    return null;
};
