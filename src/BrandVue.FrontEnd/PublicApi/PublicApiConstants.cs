namespace BrandVue.PublicApi
{
    public static class PublicApiConstants
    {
        public const string ApiRoot = "/api/surveysets";

        public static class EntityResponseFieldNames
        {
            public const string ProfileId = "Profile_Id";
            [Obsolete("Use " + nameof(WeightingCellId))]
            public const string DemographicCellId = "Demographic_Cell_Id";
            public const string WeightingCellId = "Weighting_Cell_Id";
            public const string StartDate = "Start_Date";
        }

        public static class MetricResultsFieldNames
        {
            public const string EndDate = "EndDate";
            public const string Value = "Value";
            public const string SampleSize = "SampleSize";
            public const string InstanceId = "InstanceId";
        }
    }
}