using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    internal static class MeasureExtensions
    {
        public static string ExcelNumberFormat(this Measure measure, Subset currentSubset)
        {
            //This is a mimic of metric.ts
            string CurrencyFormat(int numDp)
            {
                string numberWithDpFormat = string.Concat("#,##0", numDp > 0 ? $".{new string('0',numDp)}" : string.Empty);
                return string.Concat(currentSubset.Iso2LetterCountryCode switch
                {
                    "us" => "$",
                    "gb" => "£",
                    _ => "€"
                }, numberWithDpFormat);
            }

            string LookupNumberFormat(string numFormat)
            {
                return numFormat switch
                {
                    "ukDressSizeHack" => "0.0",
                    "usDressSizeHack" => "0.0",
                    "time_minutes" => "0",
                    "currency" => CurrencyFormat(2),
                    "currency0Dp" => CurrencyFormat(0),
                    "0.0;-0.0;0.0" => "0.0;-0.0;0.0",
                    "0;-0;0" => "0.0;-0.0;0.0",
                    "0%" => measure.NumberFormat,
                    "+0;-0;0" => measure.NumberFormat, //NPS 0DP
                    "+0.0;-0.0;0.0" => measure.NumberFormat, //NPS 1DP
                    _ => "0.0%"
                };
            }

            if (measure.NumberFormat is not null && measure.NumberFormat.Contains("currencyAffix", StringComparison.OrdinalIgnoreCase))
            {
                string affix = measure.NumberFormat.Contains(":") ? measure.NumberFormat.Split(":")[1] : "";
                return $"{LookupNumberFormat("currency0Dp")} \\{affix}";
            }

            return LookupNumberFormat(measure.NumberFormat);
        }
    }
}
