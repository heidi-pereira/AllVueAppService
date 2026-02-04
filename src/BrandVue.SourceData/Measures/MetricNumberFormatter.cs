namespace BrandVue.SourceData.Measures;

public static class MetricNumberFormatter
{
    public const string PercentageInputNumberFormat = "0%";
    public const string PercentageUiNumberFormat = "0%";

    public const string IntegerInputNumberFormat = "0;-0;0";
    public const string SignedIntegerInputNumberFormat = "+0;-0;0";
    public const string IntegerUiNumberFormat = "0";

    public const string DecimalInputNumberFormat = "0.0;-0.0;0.0";
    public const string SignedDecimalInputNumberFormat = "+0.0;-0.0;0.0";
    public const string DecimalUiNumberFormat = "0.00";

    public static string ParseNumberFormat(string source) =>
        source?.Trim() switch
        {
            PercentageInputNumberFormat => PercentageUiNumberFormat,
            SignedIntegerInputNumberFormat => IntegerUiNumberFormat,
            IntegerInputNumberFormat => IntegerUiNumberFormat,
            SignedDecimalInputNumberFormat => DecimalUiNumberFormat,
            DecimalInputNumberFormat => DecimalUiNumberFormat,
            _ => PercentageUiNumberFormat
        };
}