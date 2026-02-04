namespace BrandVue.SourceData.Measures
{
    public static class MetricValueSetter
    {
        public static void SetLegacyPrimaryTrueValueRange(AllowedValues allowedValues, int minimumValue,
            int maximumValue)
        {
            allowedValues.Minimum = minimumValue;
            allowedValues.Maximum = maximumValue;
        }

        public static void SetPrimaryDiscreteTrueValues(AllowedValues allowedValues, int[] trueValues)
        {
            allowedValues.Values = trueValues;
        }


        public static void SetSecondaryTrueValueRange(
            Measure measure,
            int minimumValue,
            int maximumValue)
        {
            measure.LegacySecondaryTrueValues.Minimum = minimumValue;
            measure.LegacySecondaryTrueValues.Maximum = maximumValue;
        }

        public static void SetSecondaryDiscreteTrueValues(
            Measure measure,
            int[] trueValues)
        {
            measure.LegacySecondaryTrueValues.Values = trueValues;
        }

        public static void SetBaseValueRange(AllowedValues allowedValues, int minimumValue,
            int maximumValue)
        {
            SetRange(allowedValues, minimumValue, maximumValue);
        }

        public static void SetRange(
            AllowedValues values,
            int minimumValue,
            int maximumValue)
        {
            values.Minimum = minimumValue;
            values.Maximum = maximumValue;
        }

        public static void SetDiscreteBaseValues(AllowedValues allowedValues, int[] baseValues)
        {
            allowedValues.Values = baseValues;
        }
    }
}
