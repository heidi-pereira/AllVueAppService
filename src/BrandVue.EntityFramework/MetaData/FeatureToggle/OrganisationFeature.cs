using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData.FeatureToggle;

[Table("OrganisationFeatures")]
public class OrganisationFeature
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Feature))]
    [Required]
    public int FeatureId { get; set; }

    public Feature Feature { get; set; }

    [MaxLength(450)]
    [Required]
    public string OrganisationId { get; set; }

    [MaxLength(450)]
    [Required]
    public string UpdatedByUserId { get; set; }

    public DateTime UpdatedDate { get; set; }
}