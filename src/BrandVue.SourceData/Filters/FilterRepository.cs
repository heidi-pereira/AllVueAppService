using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Filters
{
    public interface IFilterRepository : IEnumerable<FilterDescriptor>
    {
        FilterDescriptor[] GetAllFiltersForSubset(Subset selectedSubset);
        string NamedItemToVariable(string subsetId, string name);
    }

    public class FilterRepository : EnumerableBaseRepository<FilterDescriptor, string>, IFilterRepository
    {
        private bool _usingVariables = false;
        private Dictionary<string, string> _ageVariableName = new Dictionary<string, string>();
        private Dictionary<string, string> _genderVariableName = new Dictionary<string, string>();
        private Dictionary<string, string> _regionVariableName = new Dictionary<string, string>();
        private Dictionary<string, string> _segVariableName = new Dictionary<string, string>();

        public FilterRepository()
        {
            _objectsById = new Dictionary<string, FilterDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        static public FilterRepository Construct(ILogger<FilterRepository> logger, Dictionary<Subset, WeightingMetrics> weightings)
        {
            var repository = new FilterRepository();
            repository.Initalize(logger, weightings);
            return repository;
        }

        private void Initalize(ILogger<FilterRepository> logger, Dictionary<Subset, WeightingMetrics> weightings)
        {
            _usingVariables = true;
            int uniqueIndex = 0;
            foreach (var kvp in weightings)
            {
                var subset = kvp.Key;
                var weighting = kvp.Value;

                foreach (var measureDependency in weighting.AllMeasureDependencies)
                {
                    var filterInstance = GetOrCreate((uniqueIndex++).ToString());

                    filterInstance.VariableName = measureDependency.Metric.Name;
                    filterInstance.Field = measureDependency.Metric.HelpText;
                    filterInstance.Name = measureDependency.Metric.HelpText;
                    filterInstance.DisplayName = measureDependency.Metric.Description ?? measureDependency.Metric.HelpText;
                    filterInstance.FilterValueType = FilterValueTypes.Category;
                    filterInstance.Subset = new Subset[] { subset };
                    filterInstance.Categories = string.Join("|", measureDependency.EntityInstances
                        .Where(instance=> instance.EnabledForSubset(subset.Id))
                        .OrderBy(instance => instance.Id)
                        .Select(x => $"{x.Id}:{x.Name}"));
                    try
                    {
                        //This is very brittle but if it goes wrong then just use the default above.
                        var desiredOrder = measureDependency.Metric.FilterValueMapping?.Split('|').Select(X => X.Split(':')[1]).ToList();
                        if (desiredOrder?.Any() == true)
                        {
                            var lookup = desiredOrder.ToDictionary(nameOfInstance => nameOfInstance, nameOfInstance => measureDependency.EntityInstances.Single(e => e.Name == nameOfInstance));
                            filterInstance.Categories = string.Join("|", 
                                desiredOrder
                                .Where(nameOfInstance => lookup.ContainsKey(nameOfInstance) && lookup[nameOfInstance].EnabledForSubset(subset.Id))
                                .Select(nameOfInstance => $"{lookup[nameOfInstance].Id}:{nameOfInstance}"));
                        }
                    }
                    catch( Exception ex)
                    {
                        logger.LogWarning("Failed to configure {Metric}:{FilterValueMapping}:{Reason}", measureDependency.Metric.Name, measureDependency.Metric.FilterValueMapping, ex.Message);
                    }
                    switch(filterInstance.Name)
                    {
                        case DefaultQuotaFieldGroups.Age:
                            _ageVariableName[subset.Id] = filterInstance.VariableName;
                            break;
                        case DefaultQuotaFieldGroups.Region:
                            _regionVariableName[subset.Id] = filterInstance.VariableName;
                            break;
                        case DefaultQuotaFieldGroups.Gender:
                            _genderVariableName[subset.Id] = filterInstance.VariableName;
                            break;
                        case DefaultQuotaFieldGroups.Seg:
                            _segVariableName[subset.Id] = filterInstance.VariableName;
                            break;
                    }
                }
            }
        }

        public string NamedItemToVariable(string subsetId, string name)
        {
            if (_usingVariables)
            {
                switch(name)
                {
                    case DefaultQuotaFieldGroups.Age:
                        return _ageVariableName.ContainsKey(subsetId) ? _ageVariableName[subsetId]: DefaultQuotaFieldGroups.Age;
                    case DefaultQuotaFieldGroups.Region:
                        return _regionVariableName.ContainsKey(subsetId) ? _regionVariableName[subsetId]: DefaultQuotaFieldGroups.Region;
                    case DefaultQuotaFieldGroups.Seg:
                        return _segVariableName.ContainsKey(subsetId) ? _segVariableName[subsetId]: DefaultQuotaFieldGroups.Seg;
                    case DefaultQuotaFieldGroups.Gender:
                        return _genderVariableName.ContainsKey(subsetId) ? _genderVariableName[subsetId]: DefaultQuotaFieldGroups.Gender;
                }
            }
            return null;
        }

        protected override void SetIdentity(FilterDescriptor target, string identity)
        {
            CheckIdentityAndBleatIfInvalid(identity);
            target.Name = identity;
            target.InternalIndex = _objectsById.Count;
        }

        private void CheckIdentityAndBleatIfInvalid(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentNullException(
                    nameof(identity),
                    "Cannot add filter with null, empty, or whitespace only ID.");
            }
        }
        

        protected override IEnumerator<FilterDescriptor> GetEnumeratorInternal()
        {
            return _objectsById.Values.Where(descriptor => !descriptor.Disabled).
                OrderBy(descriptor => descriptor.DisplayName
            ).GetEnumerator();
        }
        public FilterDescriptor[] GetAllFiltersForSubset(Subset selectedSubset)
        {
            return _objectsById.Values.Where(Included).ToArray();


            bool Included(BaseMetadataEntity p)
            {
                return !p.Disabled
                       && (p.Subset == null || p.Subset.Any(s => s.Id == selectedSubset.Id));
            }
        }
    }
}