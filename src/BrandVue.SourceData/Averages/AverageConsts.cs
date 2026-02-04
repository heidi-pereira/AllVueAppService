namespace BrandVue.SourceData.Averages
{
    public static class AverageDescriptorFields
    {
        public const string Id = "Id";
        public const string DisplayName = "DisplayName";
        public const string Subset = "Subset";
        public const string Disabled = "Disabled";
        public const string Environment = "Environment";
        public const string Order = "Order";
        public const string Group = "Group";
        public const string TotalisationPeriodUnit = "TotalisationPeriod";
        public const string NumberOfPeriodsInAverage = "NumberOfPeriodsInAverage";
        public const string WeightingMethod = "WeightingMethod";
        public const string WeightAcross = "WeightAcross";
        public const string AverageStrategy = "AverageStrategy";
        public const string MakeUpTo = "MakeUpTo";
        public const string IncludeResponseIds = "IncludeResponseIds";
        public const string IsDefault = "IsDefault";
        public const string AllowPartial = "AllowPartial";
    }

    public static class AverageIds
    {
        //any changes here must also be made in the front end (PeriodHelper.ts)
        public const string CustomPeriod = "CustomPeriod";
        public const string CustomPeriodNotWeighted = "CustomPeriodNotWeighted";
    }
}
