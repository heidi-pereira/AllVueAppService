import { IAverageDescriptor, TotalisationPeriodUnit, MakeUpTo } from "../../BrandVueApi";

export const selectBestAverage = (averages: IAverageDescriptor[], requestedAverageId?: string): IAverageDescriptor => {
    return averages.find(a => a.averageId === requestedAverageId) || averages.find(a => a.isDefault) || averages[0];
}

export const selectMonthlyAverage = (averages: IAverageDescriptor[]): IAverageDescriptor | undefined => {
    return averages.find(isMonthlyAverage);
}

export const selectMonthlyAverageOrDefault = (averages: IAverageDescriptor[]): IAverageDescriptor => {
    return selectMonthlyAverage(averages) || averages.find(a => a.isDefault) || averages[0];
}

export const selectMonthlyAverageOver3Months = (averages: IAverageDescriptor[]): IAverageDescriptor | undefined => {
    return averages.find(a => a.totalisationPeriodUnit === TotalisationPeriodUnit.Month && a.makeUpTo === MakeUpTo.MonthEnd && a.numberOfPeriodsInAverage === 3);
}
export const selectMonthlyAverageOver12Months = (averages: IAverageDescriptor[]): IAverageDescriptor | undefined => {
    return averages.find(a => a.totalisationPeriodUnit === TotalisationPeriodUnit.Month && a.makeUpTo === MakeUpTo.MonthEnd && a.numberOfPeriodsInAverage === 12);
}

export const getMarketMetricAverages = (averages: IAverageDescriptor[]): IAverageDescriptor[] => {
    return averages.filter(isMonthlyAverage);
}

const isMonthlyAverage = (average: IAverageDescriptor): boolean =>
    average.totalisationPeriodUnit === TotalisationPeriodUnit.Month && average.numberOfPeriodsInAverage === 1;