namespace BrandVue.SourceData.Respondents
{
    /// <summary>
    /// We need to know a bunch of core fields in order to build quota cells. Hence in the ProfilingFields sheet of the map file, the name column should include these.
    /// You can probably get away without fields unrelated to the survey's geography.
    /// </summary>
    public static class RespondentFields
    {
        public const string Age = "Age";
        public const string Country = "Country";
        public const string Gender = "Gender";
        public const string UnweightedProfileField = "All_Respondents";
        public const string HouseholdIncome = "Household_Income";
        public const string Id = "id";
        public const string SocioEconomicGroup1 = "SEG1";
        public const string SocioEconomicGroup2 = "SEG2";
        public const string StartTime = "StartTime";
        public const string Region = "Region";
        public static string DEFAULT_SEG_FIELD_IDENTIFIER = "default";

        private static readonly Dictionary<string, string[]> CountryCodeToSegFields = new Dictionary<string, string[]>
        {
            { Iso2LetterCountryCodesLowercase.GB, new []{ SocioEconomicGroup1, SocioEconomicGroup2 }},
            {DEFAULT_SEG_FIELD_IDENTIFIER, new []{ HouseholdIncome}}
        };

        public static IReadOnlyCollection<string> CommonFieldsForCountryCode(string countryCode)
        {
            var fieldNames = new List<string>
            {
                Age,
                Gender,
                Region
            };

            var segFieldNames = CountryCodeToSegFields.TryGetValue(countryCode.ToLower(), out var customSegFieldNames)
                ? customSegFieldNames
                : CountryCodeToSegFields[DEFAULT_SEG_FIELD_IDENTIFIER];
            fieldNames.AddRange(segFieldNames);
            return fieldNames;
        }

        public static string[] GetSegFieldsForCountryCode(string countryCode) =>
            CountryCodeToSegFields.TryGetValue(countryCode.ToLower(), out var segFields) ? segFields : CountryCodeToSegFields[DEFAULT_SEG_FIELD_IDENTIFIER];
    }
}
