using System.Data;
using System.Globalization;
using Newtonsoft.Json;

namespace BrandVue.SourceData.CommonMetadata
{
    public static class FieldExtractor
    {
        public static readonly char[] Delimiters = { '|' };
        public static readonly char[] SecondLevelDelimeters = { '+' };

        public static string ExtractString(
            string fieldName,
            string[] headers,
            string[] currentRecord,
            bool optional = false)
        {
            var fieldIndex = Array.FindIndex(
                headers,
                header => string.Equals(
                    header,
                    fieldName,
                    StringComparison.OrdinalIgnoreCase));

            if (fieldIndex < 0)
            {
                if (optional)
                {
                    return null;
                }
                else
                {
                    throw new InvalidOperationException(
                        $@"Required field '{
                                fieldName
                            }' is missing from measure definitions. Only '{
                                string.Join(", ", headers)
                            }' are present for record values '{
                                string.Join(", ", currentRecord)
                            }'.");
                }
            }

            return currentRecord[fieldIndex];
        }

        public static bool ExtractBoolean(
            string fieldName,
            string[] headers,
            string[] currentRecord,
            bool defaultValue = false)
        {
            var extracted = ExtractString(fieldName, headers, currentRecord, true);
            //  The convention for our metadata is that no value means false whilst
            //  any non-whitespace value means true. It's a bit like the JS concept
            //  of truthiness and falsiness.
            return string.IsNullOrWhiteSpace(extracted)
                ? defaultValue
                : true;
        }

        public static int ExtractInteger(
            string fieldName,
            string[] headers,
            string[] currentRecord,
            int defaultValue)
        {
            var extracted = ExtractString(fieldName, headers, currentRecord, true);
            return string.IsNullOrWhiteSpace(extracted)
                ? defaultValue
                : int.Parse(extracted, CultureInfo.InvariantCulture);
        }

        public static string[] ExtractStringArray(
            string fieldName,
            string[] headers,
            string[] currentRecord,
            char [] delimiters,
            bool optional = false)
        {
            var extracted = ExtractString(fieldName, headers, currentRecord, optional);
            return string.IsNullOrWhiteSpace(extracted)
                ? null //EmptyStringArray - null is faster for comparisons (no need to look up)
                : extracted.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] ExtractStringArray(
            string rawValue,
            char[] delimiters)
        {
            return string.IsNullOrWhiteSpace(rawValue)
                ? null //EmptyStringArray - null is faster for comparisons (no need to look up)
                : rawValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] ExtractStringArray(
            string fieldName,
            string[] headers,
            string[] currentRecord,
            bool optional = false)
        {
            return ExtractStringArray(
                fieldName, headers, currentRecord, Delimiters, optional);
        }

        public static string[] ExtractStringArray(string rawValue)
        {
            return ExtractStringArray(rawValue, Delimiters);
        }

        public static string[][] ExtractArrayOfStringArray(
            string fieldName,
            string[] headers,
            string[] currentRecord,
            bool optional = false)
        {
            return ExtractArrayOfStringArray(fieldName, headers, currentRecord, Delimiters,
                SecondLevelDelimeters, optional);
        }

        public static string[][] ExtractArrayOfStringArray(
            string fieldName,
            string[] headers,
            string[] currentRecord,
            char[] delimiters,
            char[] secondaryDelimiters,
            bool optional = false)
        {
            var extracted = ExtractStringArray(
                fieldName, headers, currentRecord, delimiters, optional);
            return extracted?.Select(x => x.Split(secondaryDelimiters))?.ToArray();
        }

        public static string[][] ExtractArrayOfStringArray(
            string fieldName,
            string[] headers,
            string[] currentRecord,
            char delimiter1,
            char delimiter2,
            bool optional = false)
        {
            var delimiters1 = new[] { delimiter1 };
            var delimiters2 = new[] { delimiter2 };
            return ExtractArrayOfStringArray(fieldName, headers, currentRecord, delimiters1, delimiters2, optional);
        }

        public static double[] ExtractDoubleArray(
            string fieldName,
            string[] headers,
            string[] currentRecord,
            bool optional = false)
        {
            var extracted = ExtractString(fieldName, headers, currentRecord, optional);
            return string.IsNullOrWhiteSpace(extracted)
                ? null //EmptyStringArray - null is faster for comparisons (no need to look up)
                : extracted.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToArray();
        }

        public static TEnum ExtractEnum<TEnum>(
            string fieldName,
            string[] headers,
            string[] currrentRecord,
            TEnum defaultValue = default(TEnum),
            bool optional = false) where TEnum : struct, IConvertible
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException(
                    $@"TEnum must be an enum but {
                        typeof(TEnum).Name
                    } is not.");
            }
            
            var extracted = ExtractString(
                fieldName, headers, currrentRecord, optional);

            if (string.IsNullOrWhiteSpace(extracted)
                && optional)
            {
                return defaultValue;
            }
            
            try
            {
                 return (TEnum) Enum.Parse(
                    typeof(TEnum), extracted, true);
            }
            catch (Exception ex)
            {
                throw new DataException(
                    $@"Error parsing {
                            extracted
                        } into enum of type {
                            typeof(TEnum).Name
                        } in data row {
                            JsonConvert.SerializeObject(currrentRecord)
                        } with headers {
                            JsonConvert.SerializeObject(headers)
                        }: {ex.Message}",
                    ex);
            }
        }

        public static DateTimeOffset? ParseStartDate(string fieldName,
            string[] headers,
            string[] currentRecord,
            bool optional = false)
        {
            var source = ExtractString(fieldName, headers, currentRecord, optional);
            var original = source?.ToString().Trim();
            if (string.IsNullOrEmpty(original))
            {
                return null;
            }

            return DateTime.ParseExact(
                original,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture).ToUtcDateOffset();
        }
    }
}
