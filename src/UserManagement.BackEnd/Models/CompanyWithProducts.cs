namespace UserManagement.BackEnd.Models
{
    public record CompanyWithProducts(
        string Id,
        string ShortCode,
        string DisplayName,
        string URL,
        bool HasExternalSSOProvider,
        List<CompanyWithProducts> ChildCompanies,
        List<ProductIdentifier> Products,
        bool SurveyVueEditingAvailable,
        bool SurveyVueFeedbackAvailable): Company(Id, ShortCode,DisplayName,URL, HasExternalSSOProvider);
}