namespace BrandVue.SourceData.CommonMetadata
{
    public class BaseMetadataEntity : IMetadataEntity, ICloneable
    {
        public Subset[] Subset { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public string[] Environment { get; set; }
        public string[] Roles { get; set; }
        public bool Disabled { get; set; }
        public IReadOnlyList<string> GetSubsets()
        {
            return Subset?.Select(s => s.Id).ToList();
        }

        public void SetSubsets(IEnumerable<string> subsets, ISubsetRepository repository)
        {
            Subset = subsets.Select(x=>repository.TryGet(x, out var sub) ? sub : null).Where(x=>x is not null).ToArray();
        }
    }
    
    public interface IMetadataEntity : IEnvironmentConfigurable, ISubsetConfigurable, IDisableable
    {
        public string[] Roles { get; set; }
        
        public bool Included(string role, params Subset[] subsets)  {
            var currentSubsets = GetSubsets();
            var includeForAllSubsets = currentSubsets == null || !currentSubsets.Any();
            return !Disabled
                && (includeForAllSubsets || !subsets.Any() ||
                    currentSubsets.Any(s => subsets.Any(subset => s == subset.Id)))
                && (Roles == null || Roles.Any(r => r.Equals(role, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}
