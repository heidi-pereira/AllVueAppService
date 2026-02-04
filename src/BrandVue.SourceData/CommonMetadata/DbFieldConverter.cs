using System.Text;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.SourceData.Dashboard;
using Microsoft.Scripting.Utils;

namespace BrandVue.SourceData.CommonMetadata
{
    // temporary converter class that converts strings into objects and objects to strings in the same
    // way that the field converter does
    // intended to exist while map file and database versions of pages, panes and parts exist
    // will be removed once map file versions of these are no longer used
    public class DbFieldConverter
    {
        public static readonly char Delimiter = '|';
        public static readonly string DelimiterString = $"{Delimiter}";

        public static readonly char SecondLevelDelimiter = '+';
        public static readonly string SecondLevelDelimiterString = $"{SecondLevelDelimiter}";

        public static string EncodeString(string value)
        {
            // Prefer to store NULL values to empty strings
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value;
        }

        public static string EncodeArrayOfStringArrays(string[][] arrayOfStringArrays)
        {
            if (arrayOfStringArrays == null || arrayOfStringArrays.Length == 0)
                return null;
            StringBuilder sb = new StringBuilder();
            foreach (var innerArray in arrayOfStringArrays)
            {
                sb.Append((sb.Length > 0 ? DelimiterString : string.Empty) + string.Join(SecondLevelDelimiterString, innerArray));
            }

            return sb.ToString();
        }

        public static string[][] DecodeArrayOfStringArrays(string arrayOfStringArraysString)
        {
            List<string[]> listOfStringArrays = new List<string[]>();

            if (string.IsNullOrEmpty(arrayOfStringArraysString)) return listOfStringArrays.ToArray();

            foreach (var stringArray in arrayOfStringArraysString.Split(Delimiter))
            {
                listOfStringArrays.Add(stringArray.Split(SecondLevelDelimiter));
            }

            return listOfStringArrays.ToArray();
        }

        public static string EncodeArrayOfStrings(string[] arrayOfStrings)
        {
            if (arrayOfStrings == null || arrayOfStrings.Length == 0)
                return null;
            return string.Join(DelimiterString, arrayOfStrings);
        }

        public static string[] DecodeArrayOfStrings(string encodedArrayOfStrings)
        {
            if (string.IsNullOrEmpty(encodedArrayOfStrings)) return null;

            return encodedArrayOfStrings.Split(Delimiter);
        }

        public static string EncodeAxisRange(AxisRange axisRange)
        {
            if (axisRange.Min == null && axisRange.Max == null) return null;

            return $"{axisRange.Min}{Delimiter}{axisRange.Max}";
        }

        public static AxisRange DecodeAxisRange(string encodedAxisRange)
        {
            var result = new AxisRange();
            if (string.IsNullOrWhiteSpace(encodedAxisRange))
                return result;

            var tokens = encodedAxisRange.Split(Delimiter);
            if (tokens.Length == 2)
            {
                if (double.TryParse(tokens[0], out var min)) result.Min = min;
                if (double.TryParse(tokens[1], out var max)) result.Max = max;
            }

            return result;
        }

        public static string EncodeDataSortOrder(DataSortOrder dataSortOrder)
        {
            return dataSortOrder.ToString();
        }

        public static DataSortOrder DecodeDataSortOrder(string encodedDataSortOrder)
        {
            return string.IsNullOrEmpty(encodedDataSortOrder) || !Enum.TryParse<DataSortOrder>(encodedDataSortOrder, out var sortOrder)
                ? DataSortOrder.Ascending
                : sortOrder;
        }

        public static string EncodeSubsets(Subset[] subsets)
        {
            if (subsets == null || subsets.Length == 0)
                return null;
            return string.Join(DelimiterString, subsets.Select(s => s.Id));
        }

        public static Subset[] DecodeSubsets(ISubsetRepository subsetRepository, string encodedSubsets)
        {
            if (string.IsNullOrWhiteSpace(encodedSubsets))
                return null;
            return encodedSubsets.Split(Delimiter)
                .Where(subsetRepository.HasSubset)
                .Select(subsetRepository.Get).ToArray();
        }
    }
}
