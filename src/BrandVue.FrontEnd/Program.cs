using Autofac.Extensions.DependencyInjection;
using BrandVue.EntityFramework;
using BrandVue.SourceData.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Vue.Common.Auth.Permissions;

namespace BrandVue
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            _ = NpmTaskHelper.RunTaskAsync("build-cached:api");
            using var host = CreateHostBuilder(args).Build();
            await host.StartAsync();
            Console.WriteLine("Started .NET AllVue API"); // We watch for this in launch.json to open browser
            await host.WaitForShutdownAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices((context, services) =>
                {
                    // Add Application Insights telemetry
                    services.AddApplicationInsightsTelemetry();
                    
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        services.AddScoped<IApiBaseUrlResolver, LocalApiBaseUrlResolver>();
                    }
                    else
                    {
                        services.AddScoped<IApiBaseUrlResolver, ApiBaseUrlResolver>();
                    }
                    services.AddScoped<IUserPermissionHttpClient, UserPermissionHttpClient>();
                });
    }
}
