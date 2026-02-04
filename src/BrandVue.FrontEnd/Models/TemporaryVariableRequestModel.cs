using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.QuotaCells;
using System.ComponentModel.DataAnnotations;

namespace BrandVue.Models
{
    public class TemporaryVariableRequestModel : ISubsetIdProvider
    {
        [Required]
        public string SubsetId { get; init; }
        [Required]
        public Period Period { get; init; }
        [Required]
        public int ActiveBrandId { get; init; }
        [Required]
        public DemographicFilter DemographicFilter { get; init; }
        [Required]
        public CompositeFilterModel FilterModel { get; init; }

        [Required]
        public TemporaryVariableInstanceRequestModel[] Rows { get; init; }
        [Required]
        public TemporaryVariableInstanceRequestModel[] Breaks { get; init; }
    }

    public class TemporaryVariableInstanceRequestModel
    {
        public GroupedVariableDefinition Definition { get; init; }
        public EntityInstanceRequest[] FilterBy { get; init; }
    }
}
