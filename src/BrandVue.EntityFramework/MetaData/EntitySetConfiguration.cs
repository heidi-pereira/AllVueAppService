using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData
{
    [Table("EntitySetConfigurations")]
    public class EntitySetConfiguration
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(20)]
        public string ProductShortCode { get; set; }
        [MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        [MaxLength(50)]
        public string Organisation { get; set; }
        [Required, StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string Name { get; set; }
        [Required, StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string EntityType { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string Subset { get; set; }
        [Required]
        public string Instances { get; set; }
        public int MainInstance { get; set; }
        public bool IsFallback { get; set; }
        public bool IsSectorSet { get; set; }
        public bool IsDisabled { get; set; }
        [Required, MaxLength(450)]
        public string LastUpdatedUserId { get; set; }
        public bool IsDefault { get; set; }
        [InverseProperty(nameof(EntitySetAverageMappingConfiguration.ParentEntitySetConfiguration))]
        public ICollection<EntitySetAverageMappingConfiguration> ChildAverageMappings { get; set; }
    }
    
    public static class EntitySetConfigurationExtension
    {
        public static bool TypeSubsetAndOrganisationEquals(this EntitySetConfiguration entitySet, string entityTypeIdentifier, string subsetId, string organisation)
        {
            return entitySet.Subset == subsetId
                   && string.Equals(entitySet.EntityType, entityTypeIdentifier, StringComparison.CurrentCultureIgnoreCase)
                   && string.Equals(entitySet.Organisation, organisation, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
