namespace Vue.Common.AuthApi
{
    public class WhiteLabellingService : IWhiteLabellingService
    {
        private IAuthApiClient _authApiClient;

        public WhiteLabellingService(IAuthApiClient authApiClient)
        {
            _authApiClient = authApiClient;
        }

        public Task<string> GetReportTemplatePathAsync(string shortCode, bool useCustomReport, CancellationToken cancellationToken)
        {
            return _authApiClient.GetReportTemplatePathAsync(useCustomReport, shortCode, cancellationToken);
        }

        public async Task<WhiteLabelComponents> GetWhiteLabelUI(string shortCode, CancellationToken cancellationToken)
        {
            var themeDetailsTask = _authApiClient.GetThemeDetails(shortCode, cancellationToken);
            var faviconUrlTask = _authApiClient.GetFaviconUrl(shortCode, cancellationToken);
            var companyTask = _authApiClient.GetCompanyByShortcode(shortCode, cancellationToken);
            await Task.WhenAll(themeDetailsTask, faviconUrlTask, companyTask);

            var themeDetails = await themeDetailsTask;
            var company = await companyTask;
            var faviconUrl = await faviconUrlTask;

            var details = new WhiteLabelThemeDetails(
                themeDetails.LogoUrl,
                themeDetails.HeaderTextColour,
                themeDetails.HeaderBackgroundColour,
                themeDetails.HeaderBorderColour,
                faviconUrl,
                themeDetails.ShowHeaderBorder
            );
            return new WhiteLabelComponents(company.DisplayName,company.SecurityGroup, details);
        }
    }
}