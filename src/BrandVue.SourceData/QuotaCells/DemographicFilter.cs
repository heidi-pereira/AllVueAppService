using BrandVue.SourceData.Filters;
using System.ComponentModel.DataAnnotations;

namespace BrandVue.SourceData.QuotaCells
{
    public class DemographicFilter
    {
        IFilterRepository _filters;
        private readonly IDictionary<string, IReadOnlyCollection<string>> _fieldNameToValueLabels ;

        internal DemographicFilter() : this(null, new Dictionary<string, IReadOnlyCollection<string>>())
        {

        }
        public DemographicFilter(IFilterRepository filters) : this(filters, new Dictionary<string, IReadOnlyCollection<string>>())
        {
        }

        public DemographicFilter(IFilterRepository filters, string fieldName, params string[] valueLabels)
            :this(filters, new Dictionary<string, IReadOnlyCollection<string>>() {{fieldName, valueLabels}})
        {
        }

        private DemographicFilter(IFilterRepository filters, IDictionary<string, IReadOnlyCollection<string>> fieldNameToValueLabels)
        {
            _filters = filters;
            _fieldNameToValueLabels = fieldNameToValueLabels;
        }
        public DemographicFilter Patch(IFilterRepository filterRepository)
        {
            if (_filters == null)
            {
                _filters = filterRepository;
            }
            return this;
        }

        [Required] public IReadOnlyCollection<string> AgeGroups
        {
            get => GetValueLabelsForField(DefaultQuotaFieldGroups.Age);
            set => _fieldNameToValueLabels[DefaultQuotaFieldGroups.Age] = value;
        }

        [Required] public IReadOnlyCollection<string> Genders
        {
            get => GetValueLabelsForField(DefaultQuotaFieldGroups.Gender);
            set => _fieldNameToValueLabels[DefaultQuotaFieldGroups.Gender] = value;
        }

        [Required] public IReadOnlyCollection<string> Regions
        {
            get => GetValueLabelsForField(DefaultQuotaFieldGroups.Region);
            set => _fieldNameToValueLabels[DefaultQuotaFieldGroups.Region] = value;
        }

        [Required] public IReadOnlyCollection<string> SocioEconomicGroups
        {
            get => GetValueLabelsForField(DefaultQuotaFieldGroups.Seg);
            set => _fieldNameToValueLabels[DefaultQuotaFieldGroups.Seg] = value;
        }

        public IReadOnlyCollection<string> GetValueLabelsForField(string fieldName)
        {
            return _fieldNameToValueLabels.TryGetValue(fieldName, out var value) ? value : Array.Empty<string>();
        }

        public string MetricNameForNamedItem(string subsetId, string namedItem)
        {
            return _filters.NamedItemToVariable(subsetId, namedItem);
        }

        public IGroupedQuotaCells Apply(string subsetId, IGroupedQuotaCells orderedQuotaCells)
        {
            if (_fieldNameToValueLabels.Any(f => f.Value.Any()))
            {
                return orderedQuotaCells
                    .Where(cell=>IncludedByFilter(subsetId, cell));
            }
            return orderedQuotaCells;
        }

        private bool IncludedByFilter(string subsetId, QuotaCell cell)
        {
            return _fieldNameToValueLabels.All(kvp =>
            {
                var targetFieldValues = kvp.Value;
                var fieldGroup = _filters.NamedItemToVariable(subsetId, kvp.Key) ?? kvp.Key;
                return !targetFieldValues.Any() || targetFieldValues.Contains(cell.GetKeyPartForFieldGroup(fieldGroup));
            });
        }

        public DemographicFilter WithValuesFor(IFilterRepository filters, string filterName, params string[] values)
        {
            var fieldNameToValueLabels = new Dictionary<string, IReadOnlyCollection<string>>(_fieldNameToValueLabels) {[filterName] = values};
            return new DemographicFilter(filters, fieldNameToValueLabels);
        }
    }
}
