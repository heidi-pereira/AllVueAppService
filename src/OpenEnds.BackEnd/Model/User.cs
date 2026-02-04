using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenEnds.BackEnd.Model;

public class User
{
    [Key]
    public int Id { get; set; }
    [StringLength(250)]
    public string Name { get; set; } = "";

    [NotMapped]
    public IEnumerable<OpenEndsRole> Roles { get; set; } = new List<OpenEndsRole>();

    [NotMapped]
    public string? CurrentOrgCode { get; set; }

    [NotMapped]
    public bool IsSavantaUser => Roles.Any(r => r >= OpenEndsRole.SavantaUser);

    /// <summary>
    /// Use the CurrentOrgCode as that is adapted to suit the current context for Savanta people
    /// </summary>
    public string? OrgCode { get; set; }

    public bool IsSavanta() => Name.EndsWith("@savanta.com");
}