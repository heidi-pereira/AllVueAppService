using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Dates;

namespace BrandVue.SourceData.CalculationPipeline
{
    public static class TotaliserFactory
    {
        public static IPeriodTotaliser Create(AverageDescriptor average)
        {
            switch (average.TotalisationPeriodUnit)
            {
                case TotalisationPeriodUnit.Day:
                    return new DailyRollingPeriodCellsTotaliser();

                case TotalisationPeriodUnit.Month:
                    return new FixedDiscretePeriodCellsTotaliser(new MonthlyDateBatcher());

                case TotalisationPeriodUnit.All:
                    return new AllPeriodCellsTotaliser();

                default:
                    throw new NotImplementedException(
                        $@"Totalisation by {average.TotalisationPeriodUnit} is not supported.");
            }
        }
    }
}
