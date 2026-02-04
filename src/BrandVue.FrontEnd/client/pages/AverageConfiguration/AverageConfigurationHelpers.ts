import { AverageStrategy, MakeUpTo, TotalisationPeriodUnit, WeightAcross, WeightingMethod, WeightingPeriodUnit } from "../../BrandVueApi";

export function totalisationPeriodUnitName(unit: TotalisationPeriodUnit, pluralize: boolean): string {
    const suffix = pluralize ? 's' : '';
    switch (unit) {
        case TotalisationPeriodUnit.Day: return `Day${suffix}`;
        case TotalisationPeriodUnit.Month: return `Month${suffix}`;
        case TotalisationPeriodUnit.All: return "All";
    }
    return "UNKNOWN";
}

export function weightingMethodName(weightingMethod: WeightingMethod): string {
    switch (weightingMethod) {
        case WeightingMethod.None: return "None";
        case WeightingMethod.QuotaCell: return "Quota cell";
    }
    return "UNKNOWN";
}

export function weightAcrossName(weightAcross: WeightAcross): string {
    switch (weightAcross) {
        case WeightAcross.AllPeriods: return "All periods";
        case WeightAcross.SinglePeriod: return "Single period";
    }
    return "UNKNOWN";
}

export function averageStrategyName(averageStrategy: AverageStrategy): string {
    switch (averageStrategy) {
        case AverageStrategy.OverAllPeriods: return "Over all periods";
        case AverageStrategy.MeanOfPeriods: return "Mean of periods";
    }
    return "UNKNOWN";
}

export function makeUpToName(makeUpTo: MakeUpTo): string {
    switch (makeUpTo) {
        case MakeUpTo.Day: return "Day";
        case MakeUpTo.WeekEnd: return "Week end";
        case MakeUpTo.MonthEnd: return "Month end";
        case MakeUpTo.QuarterEnd: return "Quarter end";
        case MakeUpTo.HalfYearEnd: return "Half year end";
        case MakeUpTo.CalendarYearEnd: return "Calendar year end";
    }
    return "UNKNOWN";
}

export function weightingPeriodUnitName(unit: WeightingPeriodUnit): string {
    switch (unit) {
        case WeightingPeriodUnit.SameAsTotalization: return "Same as totalisation";
        case WeightingPeriodUnit.FullScheme: return "Full scheme";
    }
    return "UNKNOWN";
}