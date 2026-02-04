using System;
using System.Threading.Tasks;
using BrandVue.Services;

namespace Test.BrandVue.FrontEnd.Mocks
{
    public class FakeSeleniumService : ISeleniumService
    {
        public Task<byte[]> ExportChart(string appBase, string appUrl, string name, string viewType, int width,
            int height, string[] metrics, string userName, string remoteIpAddress, string optionalOrganization)
        {
            throw new NotImplementedException();
        }
    }
}