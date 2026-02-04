using System.IO;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Import
{
    public class CategoryMappingFactory
    {
        private const string FiltersCsvFieldColumnName = "field";
        private const string FiltersCsvAllTextColumnName = "allText";
        private const string ProfilingFieldsCsvNameColumnName = "name";
        private const string BothCsvsCategoriesColumnName = "categories";
        
        private readonly IReadOnlyCollection<IDictionary<string, string>> _profilingFieldsDictionaries;
        private readonly IReadOnlyCollection<IDictionary<string, string>> _filterDictionaries;

        public CategoryMappingFactory(IReadOnlyCollection<IDictionary<string, string>> profilingFieldsDictionaries,
            IReadOnlyCollection<IDictionary<string, string>> filterDictionaries)
        {
            _profilingFieldsDictionaries = profilingFieldsDictionaries;
            _filterDictionaries = filterDictionaries;
        }

        public static CategoryMappingFactory CreateFrom(IBrandVueDataLoaderSettings settings, ILoggerFactory loggerFactory)
        {
            var lowRentCsvLoader = new LowRentCsvLoader(loggerFactory.CreateLogger<LowRentCsvLoader>());
            var profilingFieldsDictionaries = lowRentCsvLoader.Load(settings.ProfilingFieldsMetadataFilepath, false);
            var filterDictionaries = lowRentCsvLoader.Load(settings.FilterMetadataFilepath, false);
            return new CategoryMappingFactory(profilingFieldsDictionaries.ToList(), filterDictionaries.ToList());
        }

        public CategoryMapping CreateMapperParameters(string fieldName, Subset subset, string filterRowField = null)
        {
            filterRowField ??= fieldName;

            var profileValueRangeToDescription = GetProfileValueRangeToDescription(fieldName, subset);

            var filterCategories = GetCategories(_filterDictionaries, FiltersCsvFieldColumnName, filterRowField,subset);
            var quotaCellKeyToCategoryDescription = GetKeyValuePairs(filterCategories);

            var allText = GetCsvData(_filterDictionaries, FiltersCsvFieldColumnName, filterRowField, FiltersCsvAllTextColumnName, subset);

            return new CategoryMapping(profileValueRangeToDescription, quotaCellKeyToCategoryDescription, allText, fieldName);
        }

        private List<KeyValuePair<string, string>> GetProfileValueRangeToDescription(string profileRowName, Subset subset)
        {
            var profilingFieldsCategories = GetCategories(_profilingFieldsDictionaries, ProfilingFieldsCsvNameColumnName, profileRowName,subset);
            return GetKeyValuePairs(profilingFieldsCategories).ToList();
        }

        internal IReadOnlyDictionary<string, string> GetKeyValuePairs(string categories)
        {
            var groupStringMappings = categories.Split('|');
            var categoryMappings = groupStringMappings.Select(m => m.Split(':'));
            return categoryMappings.ToDictionary(keyValue => keyValue[0], keyValue => keyValue[1]);
        }

        private string GetCategories(IEnumerable<IDictionary<string, string>> csvDictionaries, string fieldColumnName,
            string fieldName, Subset subset)
        {
            return GetCsvData(csvDictionaries, fieldColumnName, fieldName, BothCsvsCategoriesColumnName, subset);
        }

        private string GetCsvData(IEnumerable<IDictionary<string, string>> csvDictionaries, string fieldColumnName,
            string fieldName, string csvColumnName, Subset subset)
        {
            return csvDictionaries.Where(d => d[fieldColumnName] == fieldName && IsDefinedForSubset(subset, d))
                       .Select(d => d[csvColumnName]).Distinct().SingleOrDefault()
                   ?? throw new InvalidDataException(
                       $"Column {fieldColumnName} must contain exactly one '{fieldName}' in " +
                       (ReferenceEquals(csvDictionaries, _filterDictionaries) ? "filters.csv" : "profilingFields.csv"));
        }

        private static bool IsDefinedForSubset(Subset subset, IDictionary<string, string> row)
        {
            return !row.ContainsKey(CommonMetadataFields.Subset) ||
                   row[CommonMetadataFields.Subset] == string.Empty ||
                   row[CommonMetadataFields.Subset].Split('|').Contains(subset.Id);
        }
    }
}