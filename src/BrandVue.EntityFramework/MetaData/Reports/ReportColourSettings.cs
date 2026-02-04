using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData.Reports;

[Table("ReportColourSettings", Schema = "Reports")]
public class ReportColourSettings
{
    [Key]
    public int Id { get; set; }

    [MaxLength(450)]
    [Required]
    public string OrganisationId { get; set; }

    [MaxLength(450)]
    [Required]
    public string UpdatedByUserId { get; set; }

    [Required]
    public DateTime UpdatedDate { get; set; }

    [MaxLength(150)]
    [Required]
    public string Colours { get; set; }
}
