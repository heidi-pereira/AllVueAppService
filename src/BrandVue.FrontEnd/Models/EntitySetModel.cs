using System.Diagnostics;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    [DebuggerDisplay("{Name}-{Organisation}")]
    public class EntitySetModel
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public IReadOnlyList<int> InstanceIds { get; set; }
        //needed by reporting
        public string Organisation { get; set; }
        public int? MainInstanceId { get; set; }
        public bool IsSectorSet { get; set; }
        public EntityType EntityType { get; set; }
        public bool IsDefault { get; set; }
        public bool IsFallback { get; set; }
        public IReadOnlyList<EntitySetAverageMappingModel> AverageMappings { get; set; }

        public EntitySetModel()
        {
            // Default constructor needed for ASP.NET model
        }

        public EntitySetModel(EntitySet entitySet, EntityInstance[] instances)
        {
            Id = entitySet.Id;
            Name = entitySet.Name;
            InstanceIds = instances.Select(i => i.Id).ToArray();
            MainInstanceId = entitySet.MainInstance?.Id ?? instances.FirstOrDefault()?.Id;
            Organisation = entitySet.Organisation;
            IsSectorSet = entitySet.IsSectorSet;
            IsDefault = entitySet.IsDefault;
            IsFallback = entitySet.IsFallback;
            AverageMappings = entitySet.Averages.Select(MapAverages).ToArray();
        }

        private EntitySetAverageMappingModel MapAverages(EntitySetAverageMappingConfiguration configuration)
        {
            return new EntitySetAverageMappingModel()
            {
                Id = configuration.Id,
                ParentEntitySetId = configuration.ParentEntitySetId,
                ChildEntitySetId = configuration.ChildEntitySetId,
                ExcludeMainInstance = configuration.ExcludeMainInstance
            };
        }
    }
}
