using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BrandVue.EntityFramework.MetaData
{
    [Table("EntitySetAverageMappingConfigurations")]
    public class EntitySetAverageMappingConfiguration
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int Id { get; set; }
        [Required, Column("ParentEntitySetId")] 
        public int ParentEntitySetId { get; set; }
        [Required] 
        public int ChildEntitySetId { get; set; }
        [Required] 
        public bool ExcludeMainInstance { get; set; }
        [ForeignKey(nameof(ChildEntitySetId))]
        [JsonIgnore]
        public EntitySetConfiguration ChildEntitySetConfiguration { get; set; }
        [ForeignKey(nameof(ParentEntitySetId))]
        // We can't have two foreign keys pointing to the same table both with cascade delete, so the child entity sets
        // have the cascade delete and the parent entity sets are cascaded manually in EntitySetConfigurationRepositorySql.
        [DeleteBehavior(DeleteBehavior.NoAction)]
        [JsonIgnore]
        public EntitySetConfiguration ParentEntitySetConfiguration { get; set; }

    }
}