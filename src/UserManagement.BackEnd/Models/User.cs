using UserManagement.BackEnd.WebApi.Models;

namespace UserManagement.BackEnd.Models;

public class User
{
    public string Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool? Verified { get; set; }
    public DateTimeOffset? LastLogin { get; set; }
    public string OwnerCompanyDisplayName { get; set; }
    public string OwnerCompanyId { get; set; }
    public string Role { get; set; }
    public int? RoleId { get; set; }
    public bool IsExternalLogin { get; set; }
    public List<UserProject> Projects { get; set; }
    public List<ProductIdentifier> Products { get; set; }
    public bool SurveyVueEditingAvailable { get; set; }
    public bool SurveyVueFeedbackAvailable { get; set; }
}
