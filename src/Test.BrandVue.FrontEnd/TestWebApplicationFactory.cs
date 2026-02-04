using System;
using System.IO;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using BrandVue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Test.BrandVue.FrontEnd
{
    public class TestWebApplicationFactory : WebApplicationFactory<Startup>
    {
        private readonly Startup _startup;

        public TestWebApplicationFactory(Startup startup)
        {
            _startup = startup;
        }

        protected override IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory()).ConfigureWebHostDefaults(
                x =>
                {
                    x.UseStartup(c => _startup).UseTestServer()
                        .UseContentRoot(Path.Combine(Path.GetDirectoryName(typeof(Startup).Assembly.Location), "../../../BrandVue.FrontEnd"));
                });
    }
}