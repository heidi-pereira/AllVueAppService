namespace BrandVue.Services
{
    public interface ISeleniumService
    {
        Task<byte[]> ExportChart(string appBase, string appUrl, string name, string viewType, int width, int height,
            string[] metrics, string userName, string remoteIpAddress, string optionalOrganization);
    }
}