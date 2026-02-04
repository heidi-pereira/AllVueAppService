using BrandVue.SourceData.Utils;

namespace BrandVue.SourceData.QuotaCells
{
    public class Category
    {
        private readonly IReadOnlyCollection<Condition> _conditions;
        public string ProfileValue { get; }

        public Category(IReadOnlyCollection<Condition> conditions, string profileValue)
        {
            _conditions = conditions;
            ProfileValue = profileValue;
        }

        
        // PERF: This overload of "Any", Avoids allocating enumerator and closure over parameter since this happens for every single profile
        public bool IsMatch(int profileValue) => _conditions.Any(profileValue, (profileValue, c) => c.IsMatch(profileValue));
    }
}