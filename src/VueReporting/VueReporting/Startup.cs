using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Savanta.Logging;
using VueReporting.Models;
using VueReporting.Services;

namespace VueReporting
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            HostingEnvironment = environment;
        }

        public IWebHostEnvironment HostingEnvironment { get; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddMvcOptions(x =>
            {
                var authenticatedUserPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(Constants.ReportingCookie)
                    .RequireAuthenticatedUser()
                    .RequireClaim("role", "SystemAdministrator", "Morar HPI administrator")
                    .Build();

                x.Filters.Add(new AuthorizeLocalFilter(authenticatedUserPolicy));
            });

            var loggerFactory = SavantaLogging.CreateFactory();

            ConfigureIoC(services, Configuration, loggerFactory, o => o.UseSqlServer(Configuration.GetConnectionString("VueReportingDatabaseConnection")));
        }

        public static void ConfigureIoC(IServiceCollection services, IConfiguration configuration, ILoggerFactory loggerFactory,
            Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
        {
            // Scoped
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDbContext<ReportRepository>(dbContextOptionsBuilder);
            services.AddScoped<IBrandVueService, BrandVueService>();
            services.AddScoped<IReportGeneratorService, ReportGeneratorService>();
            services.AddScoped<IReportTemplateService, ReportTemplateService>();

            // Singletons
            services.AddSingleton<IAppSettings, AppSettings>();
            services.AddSingleton(typeof(ILoggerFactory), loggerFactory);

            services.AddAuthentication(Constants.ReportingCookie)
                .AddCookie(Constants.ReportingCookie, o =>
                {
                    o.LoginPath = "/NotAuthorised";
                    o.AccessDeniedPath = "/AccessDenied";
                    o.Cookie.Name = ".AspNet.SharedCookie";
                    o.DataProtectionProvider = DataProtectionProvider.Create(
                        new DirectoryInfo(Path.Combine(GetWritableAppDataFolder(), "MIG", "BrandVue", "Keys")));
                });

            services.AddHttpClient(Constants.DefaultReportingClient, ReportingHttpClientDefaults());
            services.AddHttpClient(Constants.BookmarkUrlClient, ReportingHttpClientDefaults())
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        }
        
        private static string GetWritableAppDataFolder()
        {
            string basePath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: C:\ProgramData\YourAppName (shared, writable)
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: /Library/Application Support/YourAppName
                basePath = Path.Combine("/Library", "Application Support");
            }
            else
            {
                // Linux and others: /var/lib/YourAppName
                basePath = "/var/lib";
            }
            
            return basePath;
        }

        private static Action<HttpClient> ReportingHttpClientDefaults() =>
            client => client.Timeout = TimeSpan.FromMinutes(5);

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<ReportRepository>();
                context.Database.Migrate();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }
}
