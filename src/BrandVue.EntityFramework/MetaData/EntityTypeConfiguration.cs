using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NJsonSchema.Annotations;

namespace BrandVue.EntityFramework.MetaData
{
    public class EntityTypeConfiguration
    {
        [Key]
        public int Id { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string ProductShortCode { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string Identifier { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string DisplayNameSingular { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string DisplayNamePlural { get; set; }
        [StringLength(SqlTypeConstants.DefaultJsonVarcharLength)]
        public IReadOnlyCollection<string> SurveyChoiceSetNames { get; set; }
        [CanBeNull]
        public EntityTypeCreatedFrom? CreatedFrom { get; set; }
    }

    public enum EntityTypeCreatedFrom
    {
        Default,
        QuestionField,
        Variable
    }
}