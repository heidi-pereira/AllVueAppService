using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData.FeatureToggle;

[Table("Features")]
public class Feature
{
    [Key]
    public int Id { get; set; }

    [MaxLength(450)]
    [Required]
    public string Name { get; set; }

    [MaxLength(300)]
    [Required]
    public string DocumentationUrl { get; set; }

    [MaxLength(100)]
    [Required]
    public FeatureCode FeatureCode { get; set; }

    [Required]
    public bool IsActive { get; set; } = false;
}

// ReSharper disable All
/// <summary>
/// These must align with the FeatureCode in the Features table in the MetaData db
/// If you add a new Feature and want to add the FeatureToggleAttribute to your endpoints then you need to add the FeatureCode here
/// </summary>
public enum FeatureCode
{
    unknown = 0,

    llm_insights,
    llm_discovery,
    open_ends,
    data_export,
    overtime_data,
    user_management,
    table_builder,
    use_snowflake_for_text_count_calculation
}
// ReSharper restore All