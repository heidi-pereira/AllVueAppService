using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData.FeatureToggle;

[Table("UserFeatures")]
public class UserFeature
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Feature))]
    [Required]
    public int FeatureId { get; set; }

    public Feature Feature { get; set; }

    [MaxLength(450)]
    [Required]
    public string UserId { get; set; }

    [Required]
    public bool IsEnable { get; set; } = true;

    [MaxLength(450)]
    [Required]
    public string UpdatedByUserId { get; set; }

    [Required]
    public DateTime UpdatedDate { get; set; }
}