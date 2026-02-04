
namespace Vue.Common.AuthApi
{
    public record WhiteLabelThemeDetails(
        string LogoUrl,
        string HeaderTextColour,
        string HeaderBackgroundColour,
        string HeaderBorderColour,
        string FaviconUrl,
        bool ShowHeaderBorder
    );
    public record WhiteLabelComponents(string CompanyDisplayName, string CompanySecurityGroup, WhiteLabelThemeDetails ThemeDetails);

    public interface IWhiteLabellingService
    {
        Task<WhiteLabelComponents> GetWhiteLabelUI(string shortCode, CancellationToken cancellationToken);
        Task<string> GetReportTemplatePathAsync(string shortCode, bool useCustomReport, CancellationToken cancellationToken);

    }
}