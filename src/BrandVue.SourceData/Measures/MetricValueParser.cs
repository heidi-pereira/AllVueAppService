namespace BrandVue.SourceData.Measures
{
    public static class MetricValueParser
    {
        public static (bool success, string errorMessage) TryParseMultipleValueSpecification(
            string fieldName,
            string rawFieldValue,
            Action<int, int> setRange,
            Action<int[]> setDiscreteValues)
        {
            try
            {
                ParseMultipleValueSpecification(rawFieldValue, out var range, out var discreteValues);
                if (discreteValues != null)
                {
                    setDiscreteValues(discreteValues);
                }
                else
                {
                    setRange(range.Item1, range.Item2);
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Invalid {fieldName} specification {rawFieldValue} - '{ex.Message}");
            }
        }

        public static void ParseMultipleValueSpecification(string value, out (int, int) range, out int[] discreteValues)
        {
            var rangeChar = '>';
            if (value != null && value.Contains(rangeChar))
            {
                discreteValues = null;
                range = GetMinAndMaxValues(value, rangeChar);
            }
            else
            {
                discreteValues = GetDiscreteValues(value ?? "");
                range = (-1, -1);
            }
        }

        private static (int min, int max) GetMinAndMaxValues(string rawFieldValue, char splitChar)
        {
            var parts = rawFieldValue.Split(
                new[] { splitChar },
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                throw new Exception($"splitting on '{splitChar}' yields {parts.Length} parts instead of the expected 2.");
            }

            var minimumValue = int.Parse(parts[0]);
            var maximumValue = int.Parse(parts[1]);
            return (minimumValue, maximumValue);
        }


        private static int[] GetDiscreteValues(string rawValue)
        {
            var values = new List<int>();
            foreach (var item in rawValue.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(item, out int result))
                {
                    values.Add(result);
                }
                else
                {
                    throw new Exception($"parsing of array item {item} failed");
                }
            }
            return values.ToArray();
        }

        public static int? ParseNullableInteger(object source)
        {
            var original = source?.ToString().Trim();
            return string.IsNullOrEmpty(original)
                ? null
                : (int?)int.Parse(original);
        }

        public static double? ParseNullableDouble(object source)
        {
            var original = source?.ToString().Trim();
            return string.IsNullOrEmpty(original)
                ? null
                : double.Parse(original);
        }
    }
}
