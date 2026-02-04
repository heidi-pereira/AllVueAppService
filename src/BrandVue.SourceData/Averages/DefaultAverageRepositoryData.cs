using BrandVue.EntityFramework.MetaData.Averages;

namespace BrandVue.SourceData.Averages
{
    public static class DefaultAverageRepositoryData
    {
        public static void CopyFallbackAveragesToRealRepo(AverageDescriptorRepository averageDescriptorRepository, IProductContext productContext)
        {
            AddDefaultAverages(averageDescriptorRepository, productContext.GenerateFromSurveyIds, productContext.DefaultAveragesToInclude, productContext.IncludeAllDefaultAverages);
        }

        internal static void AddDefaultAverages(AverageDescriptorRepository averageDescriptorRepository, bool productContextGenerateFromSurveyIds, string[] averagesToInclude, bool includeAllAverages)
        {
            var fallback = new FallbackAverageDescriptorRepository();
            foreach (var average in fallback)
            {
                if (includeAllAverages)
                {
                    averageDescriptorRepository.Add(average);
                }
                else if (averagesToInclude.Contains(average.AverageId))
                {
                    average.Disabled = false;
                    averageDescriptorRepository.Add(average);
                }
            }

            AddCustomPeriodAverages(averageDescriptorRepository, productContextGenerateFromSurveyIds);
        }

        public static void AddCustomPeriodAverages(AverageDescriptorRepository averageDescriptorRepository, bool productContextGenerateFromSurveyIds)
        {
            // This is true for BV360 and for any SurveyVue subproduct/survey
            if (productContextGenerateFromSurveyIds)
            {
                // These two averages are used to handle various weighting scenarios.
                // They are not meant to be displayed in UI.
                averageDescriptorRepository.Add(CustomPeriodAverage);
                averageDescriptorRepository.Add(CustomPeriodAverageUnweighted);
            }
        }

        public static IList<AverageDescriptor> GetFallbackAverages()
        {
            return new List<AverageDescriptor>
            {
                new AverageDescriptor
                {
                    AverageId = "14Days",
                    DisplayName = "14 days",
                    Order = 100,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
                    NumberOfPeriodsInAverage = 14,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.AllPeriods,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.Day
                },
                new AverageDescriptor
                {
                    AverageId = "28Days",
                    DisplayName = "28 days",
                    Order = 100,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
                    NumberOfPeriodsInAverage = 28,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.AllPeriods,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.Day
                },
                new AverageDescriptor
                {
                    AverageId = "12Weeks",
                    DisplayName = "12 weeks",
                    Order = 125,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
                    NumberOfPeriodsInAverage = 84,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.AllPeriods,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.Day,
                    Disabled = true,
                },
                new AverageDescriptor
                {
                    AverageId = "Weekly",
                    DisplayName = "Weekly",
                    Order = 150,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
                    NumberOfPeriodsInAverage = 7,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.SinglePeriod,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.WeekEnd
                },
                new AverageDescriptor
                {
                    AverageId = "Fortnightly",
                    DisplayName = "Fortnightly",
                    Order = 175,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
                    NumberOfPeriodsInAverage = 14,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.SinglePeriod,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.WeekEnd,
                    Disabled = true
                },
                new AverageDescriptor
                {
                    AverageId = "Monthly",
                    DisplayName = "Monthly",
                    Order = 200,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    NumberOfPeriodsInAverage = 1,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.SinglePeriod,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.MonthEnd
                },
                new AverageDescriptor
                {
                    AverageId = "MonthlyFullScheme",
                    DisplayName = "Monthly",
                    Order = 200,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    NumberOfPeriodsInAverage = 1,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.SinglePeriod,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.MonthEnd,
                    WeightingPeriodUnit = WeightingPeriodUnit.FullScheme,
                    Disabled = true
                },
                new AverageDescriptor
                {
                    AverageId = "MonthlyOver3Months",
                    DisplayName = "Monthly (over 3 months)",
                    Order = 250,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    NumberOfPeriodsInAverage = 3,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.SinglePeriod,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.MonthEnd
                },
                new AverageDescriptor
                {
                    AverageId = "MonthlyOver6Months",
                    DisplayName = "Monthly (over 6 months)",
                    Order = 255,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    NumberOfPeriodsInAverage = 6,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.SinglePeriod,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.MonthEnd,
                    Disabled = true,
                },
                new AverageDescriptor
                {
                    AverageId = "MonthlyOver12Months",
                    DisplayName = "Monthly (over 12 months)",
                    Order = 260,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    NumberOfPeriodsInAverage = 12,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.SinglePeriod,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.MonthEnd,
                    Disabled = true,
                },
                new AverageDescriptor
                {
                    AverageId = "Quarterly",
                    DisplayName = "Quarterly",
                    Order = 300,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    NumberOfPeriodsInAverage = 3,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.SinglePeriod,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.QuarterEnd
                },
                new AverageDescriptor
                {
                    AverageId = "HalfYearly",
                    DisplayName = "HalfYearly",
                    Order = 400,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    NumberOfPeriodsInAverage = 6,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.SinglePeriod,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.HalfYearEnd
                },
                new AverageDescriptor
                {
                    AverageId = "Annual",
                    DisplayName = "Annual",
                    Order = 500,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    NumberOfPeriodsInAverage = 12,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightAcross = WeightAcross.SinglePeriod,
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    MakeUpTo = MakeUpTo.CalendarYearEnd,
                    Disabled = true,
                }
            };
        }

        public static AverageDescriptor CustomPeriodAverage => new()
        {
            AverageId = AverageIds.CustomPeriod,
            DisplayName = "Custom Period",
            Order = 1,
            NumberOfPeriodsInAverage = 1,
            TotalisationPeriodUnit = TotalisationPeriodUnit.All,
            WeightingMethod = WeightingMethod.QuotaCell,
            WeightAcross = WeightAcross.AllPeriods,
            AverageStrategy = AverageStrategy.OverAllPeriods,
            MakeUpTo = MakeUpTo.Day,
            IsHiddenFromUsers = true
        };

        public static AverageDescriptor CustomPeriodAverageUnweighted => new()
        {
            AverageId = AverageIds.CustomPeriodNotWeighted,
            DisplayName = "Custom Period Not Weighted",
            Order = 2,
            NumberOfPeriodsInAverage = 1,
            TotalisationPeriodUnit = TotalisationPeriodUnit.All,
            WeightingMethod = WeightingMethod.None,
            WeightAcross = WeightAcross.AllPeriods,
            AverageStrategy = AverageStrategy.OverAllPeriods,
            MakeUpTo = MakeUpTo.Day,
            IsHiddenFromUsers = true
        };
    }
}
