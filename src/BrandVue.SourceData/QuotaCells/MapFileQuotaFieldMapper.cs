namespace BrandVue.SourceData.QuotaCells
{
    public class MapFileQuotaFieldMapper : IQuotaFieldMapper
    {
        public string QuotaField { get; }
        private readonly IReadOnlyCollection<CategoryMapping> _categoryMappings;

        public MapFileQuotaFieldMapper(string quotaField, params CategoryMapping[] categoryMappings)
        {
            QuotaField = quotaField;
            _categoryMappings = categoryMappings;
        }

        public string GetCellKeyForProfile(IReadOnlyDictionary<string, int> fieldValues)
        {
            foreach (var mapping in _categoryMappings)
            {
                if (fieldValues.TryGetValue(mapping.FieldName, out var fieldValue) && mapping.TryGetQuotaCellKey(fieldValue, out string key))
                {
                    return key;
                }
            }

            return null;
        }

        public string[] GetAllQuotaCellKeys()
        {
            return _categoryMappings.SelectMany(m => m.GetAllQuotaCellKeys()).Distinct().ToArray();
        }

        public string GetDescriptionForQuotaCellKey(string quotaCellKey)
        {
            return quotaCellKey == null
                ? _categoryMappings.First().AllText
                : _categoryMappings.First().QuotaCellKeyToCategoryDescription[quotaCellKey];
        }
    }
}