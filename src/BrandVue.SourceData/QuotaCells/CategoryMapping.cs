using System.IO;

namespace BrandVue.SourceData.QuotaCells
{
    public class CategoryMapping
    {
        private IReadOnlyCollection<Category> ProfileValueRangeToDescription { get; }
        private IReadOnlyDictionary<string, string> CategoryDescriptionToQuotaCellKey { get; }
        public IReadOnlyDictionary<string, string> QuotaCellKeyToCategoryDescription { get; }
        public string AllText { get; }
        public string FieldName { get; }

        public CategoryMapping(IReadOnlyCollection<KeyValuePair<string, string>> profileValueRangeToDescription,
            IReadOnlyDictionary<string, string> quotaCellKeyToCategoryDescription, string allText,
            string fieldName)
        {
            ProfileValueRangeToDescription = profileValueRangeToDescription
                .Select(kvp => new Category(Condition.Parse(kvp.Key).ToList(), kvp.Value))
                .ToList();
            QuotaCellKeyToCategoryDescription = quotaCellKeyToCategoryDescription;
            AllText = allText;
            FieldName = fieldName;
            CategoryDescriptionToQuotaCellKey = QuotaCellKeyToCategoryDescription
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        }

        private string GetCategoryDescription(int profileValue)
        {
            var categoryDescription = ProfileValueRangeToDescription
                .FirstOrDefault(category => category.IsMatch(profileValue))?.ProfileValue;

            return categoryDescription;
        }

        internal bool TryGetQuotaCellKey(int fieldValue, out string quotaCellKey)
        {
            var categoryDescription = GetCategoryDescription(fieldValue);
            if (categoryDescription == null)
            {
                quotaCellKey = null;
                return false;
            }

            return CategoryDescriptionToQuotaCellKey.TryGetValue(categoryDescription, out quotaCellKey);
        }

        public string[] GetAllQuotaCellKeys()
        {
            return QuotaCellKeyToCategoryDescription.Keys.ToArray();
        }
    }
}