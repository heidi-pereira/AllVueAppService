using System.Diagnostics;
using BrandVue.EntityFramework.MetaData;
using JetBrains.Annotations;

namespace BrandVue.SourceData.Entity
{
    [DebuggerDisplay("{Name}-{Organisation}")]
    public class EntitySet
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public EntityInstance[] Instances { get; set; }
        //needed by reporting
        [CanBeNull]
        public string Organisation { get; set; }
        public EntityInstance MainInstance { get; set; }
        public bool IsSectorSet { get; set; }
        public bool IsDefault { get; set; }
        public bool IsFallback { get; set; }
        public EntitySetAverageMappingConfiguration[] Averages { get; set; }

        public EntitySet(int? id, string name, EntityInstance[] instances, string organisation, bool isSectorSet, bool isDefault, EntityInstance mainInstance = null)
        {
            Id = id;
            Name = name;
            Instances = instances;
            MainInstance = mainInstance;
            Organisation = organisation;
            IsSectorSet = isSectorSet;
            IsDefault = isDefault;
        }

        public EntitySet(int? id, string name, EntityInstance[] instances, string organisation, bool isSectorSet, bool isDefault, EntitySetAverageMappingConfiguration[] averageMappings, EntityInstance mainInstance = null)
        {
            Id = id;
            Name = name;
            MainInstance = mainInstance;
            Instances = instances;
            Organisation = organisation;
            IsSectorSet = isSectorSet;
            IsDefault = isDefault;
            Averages = averageMappings;
        }

        public bool EquivalentExceptInstances(EntitySet other)
        {
            return Name.Equals(other.Name) && IsDefault == other.IsDefault &&
                   IsFallback == other.IsFallback && IsSectorSet == other.IsSectorSet &&
                   Id == other.Id && Organisation == other.Organisation;
        }
    }
}
