using System.Globalization;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services.Exporter
{
    public static class FormattingExtensions
    {
        public static string AddCommaSeparators(this double number, Subset subset) => number.ToString("N0", GetCulture(subset));

        private static CultureInfo GetCulture(Subset subset)
        {
            var identifier = subset.Iso2LetterCountryCode.ToLower() switch
            {
                "us" => "en-US",
                "gb" => "en-GB",
                _ => "de-DE",
            };
            return new CultureInfo(identifier);
        }
    }
}
