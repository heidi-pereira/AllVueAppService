using UserManagement.BackEnd.Models;

public record CompanyWithProductsAndProjects(
    string Id,
    string ShortCode,
    string DisplayName,
    string URL,
    bool HasExternalSSOProvider,
    List<string> ChildCompaniesId,
    List<ProjectIdentifier> Projects,
    List<ProductIdentifier> Products,
    bool SurveyVueEditingAvailable,
    bool SurveyVueFeedbackAvailable): Company(Id, ShortCode, DisplayName, URL, HasExternalSSOProvider);

