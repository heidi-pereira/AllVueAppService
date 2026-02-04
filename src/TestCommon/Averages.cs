using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;

namespace TestCommon
{
    public class Averages
    {
        public static AverageDescriptorRepository CreateDefaultRepo(bool includeCustomAverages)
        {
            var a = new AverageDescriptorRepository();
            DefaultAverageRepositoryData.AddDefaultAverages(a, includeCustomAverages, null, true);
            return a;
        }

        public static AverageDescriptor SingleDayAverage { get; }= new AverageDescriptor
        {
            AverageId = "1 day",
            DisplayName = "1 day",
            Order = 100,
            TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
            NumberOfPeriodsInAverage = 1,
            WeightingMethod = WeightingMethod.QuotaCell,
            WeightAcross = WeightAcross.AllPeriods,
            AverageStrategy = AverageStrategy.OverAllPeriods,
            MakeUpTo = MakeUpTo.Day
        };
    }
}