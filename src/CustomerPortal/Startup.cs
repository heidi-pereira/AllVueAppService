using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Interfaces;
using CustomerPortal.Configurations;
using CustomerPortal.Controllers;
using CustomerPortal.Controllers.NSwag;
using CustomerPortal.Infrastructure;
using CustomerPortal.MixPanel;
using CustomerPortal.Services;
using CustomerPortal.Shared.Egnyte;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Mixpanel;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using Savanta.Logging.Extensions;
using System;
using System.Buffers;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vue.Common.Auth;
using Vue.Common.Auth.Permissions;
using Vue.Common.AuthApi;
using Vue.Common.FeatureFlags;

namespace CustomerPortal
{
    public class Startup
    {
        private const string MIXPANEL = "MixPanel";
        private const string COOKIE = "Cookies";
        private const string ALL_VUE_TOKEN = "MixPanel:AllVueToken";

        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var appSettings = new AppSettings();
            Configuration.GetSection("AppSettings").Bind(appSettings);
            appSettings.RunningEnvironmentDescription = Configuration["Environment"];
            services.AddLogging(loggingBuilder => loggingBuilder.AddSavantaLogging());

            services.AddDbContext<SurveyDbContext>(options =>
            {
                var connectionString = Configuration["ConnectionStrings:DefaultConnection"];
                if (!string.IsNullOrEmpty(connectionString))
                {
                    options.UseSqlServer(connectionString, x => x.MigrationsHistoryTable("__MigrationsHistoryForCustomerPortal"));
                }
                else
                {
                    options.UseInMemoryDatabase("CustomerPortalDatabase");
                }
            });
            services.AddDbContext<MetaDataContext>(options =>
            {
                var connectionString = Configuration["ConnectionStrings:MetaConnectionString"];
                if (!string.IsNullOrEmpty(connectionString))
                {
                    options.UseSqlServer(connectionString, x => x.MigrationsHistoryTable("__MigrationsHistoryForVue"));
                }
            });

            services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });
            
            IdentityModelEventSource.ShowPII = true;

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = COOKIE;
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie(COOKIE)
                .AddOpenIdConnect("oidc", options =>
                {
                    options.ResponseType = "code id_token";
                    options.Authority = appSettings.AuthAuthority;
                    options.RequireHttpsMetadata = false; // TODO: Probably make this true for live
                    options.ClientSecret = appSettings.AuthClientSecret;
                    options.ClientId = appSettings.AuthClientId;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("groups");
                    options.Scope.Add("role");
                    options.ClaimActions.Add(new MapAllClaimsAction());
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role",

                        // Need for logging out
                        SaveSigninToken = true,

                        // Need to to turn off issuer validation or set valid issuers to be the "{org}.authorisation" url
                        ValidateIssuer = false
                    };

                    options.Events.OnRedirectToIdentityProvider = context =>
                    {
                        if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
                        {
                            var requestContext = context.HttpContext.RequestServices.GetService<IRequestContext>();
                            context.ProtocolMessage.AcrValues = $"tenant:{requestContext.PortalGroup}";
                        }

                        return Task.FromResult(0);
                    };
                });

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "wwwroot";
            });

            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = int.MaxValue;
            });
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // if don't set default value is: 128 MB
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });

            services.AddOpenApiDocument(document =>
            {
                document.DocumentName = ApiController.InternalApiGroupName;
                document.ApiGroupNames = [ApiController.InternalApiGroupName];
                document.OperationProcessors.Add(new MarkAsRequiredIfNonNullableProcessor());
                document.SchemaSettings.SchemaProcessors.Add(new MarkAsRequiredIfNonNullableProcessor());
                document.SchemaSettings.DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;

            });

            // Add your own services here.
            services.AddSingleton(ctx => appSettings);
            services.AddHttpContextAccessor();
            services.AddScoped<ISurveyService, SurveyService>();
            services.AddScoped<IDocumentUrlProvider, SurveyService>();
            services.AddScoped<IAllVueProductConfigurationService, AllVueProductConfigurationService>();
            services.AddScoped<IVueContextService, VueContextService>();
            services.AddScoped<DocumentService>();
            services.AddScoped<IEgnyteFolderResolver, EgnyteFolderResolver>();
            services.AddScoped<IRequestContext, RequestContext>();
            services.AddScoped<IUserContext, UserContext>();
            services.AddScoped<IUserFeaturePermissionsService, UserFeaturePermissionsService>();
            services.AddScoped<ISecurityGroupService, SecurityGroupService>();

            if (HostingEnvironment.IsDevelopment())
            {
                services.AddScoped<IApiBaseUrlResolver, LocalApiBaseUrlResolver>();
            }
            else
            {
                services.AddScoped<IApiBaseUrlResolver, ApiBaseUrlResolver>();
            }
            services.AddScoped<IUserPermissionHttpClient, UserPermissionHttpClient>();
            
            services.AddScoped<IPermissionService, PermissionService>();

            services.AddScoped<IAuthApiClientCustomerPortal, AuthApiClient>(provider =>
            {
                var appSettingsService = provider.GetRequiredService<AppSettings>();
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                return new AuthApiClient(false, appSettingsService.AuthClientId, appSettingsService.AuthClientSecret, appSettingsService.AuthAuthority, httpClientFactory);
            });

            services.AddSingleton(c => new EmailService(appSettings.EmailUserName, appSettings.EmailPassword));
            services.AddSingleton<IEgnyteService>(c => new EgnyteService(appSettings.EgnyteDomain, appSettings.EgnyteClientId, appSettings.EgnyteUsername, appSettings.EgnytePassword, appSettings.EgnyteAccessToken));

            services.AddScoped<IAuthApiClient, AuthApiClient>(provider =>
            {
                var appSettingsService = provider.GetRequiredService<AppSettings>();
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                return new AuthApiClient(false, appSettingsService.AuthClientId, appSettingsService.AuthClientSecret, appSettingsService.AuthAuthority, httpClientFactory);
            });

            services.AddScoped<IDbContextFactory<MetaDataContext>, MetaDataContextFactory>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var config = new MetaDataFactoryConfiguration(
                    Configuration["ConnectionStrings:MetaConnectionString"], false);
                return new MetaDataContextFactory(loggerFactory, config);
            });
            services.AddScoped<IUserFeaturesRepository, UserFeaturesRepository>();
            services.AddScoped<IOrganisationFeaturesRepository, OrganisationFeaturesRepository>();
            services.AddScoped<IFeatureQueryService, FeatureQueryService>();
            services.Configure<MixPanelSettings>(Configuration.GetSection(MIXPANEL));
            services.AddHttpClient();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var customerPortalAppSettings = serviceScope.ServiceProvider.GetService<CustomerPortal.AppSettings>();
                serviceScope.ServiceProvider.GetService<SurveyDbContext>().Initialise(customerPortalAppSettings);
            }

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            };

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseGlobalExceptionHandler("/error", true);
            }

            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");

                endpoints.MapFallbackToController("Index", "Main");
            });

            if (env.IsDevelopment())
            {
                MapAuthUrl(app);
            }

            app.UseSpaStaticFiles();

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "dist";

                if (env.IsDevelopment())
                {
                    // Ensure that you start webpack-dev-server - run "build:hotdev" npm script
                    // Also if you install the npm task runner extension then the webpack-dev-server script will run when the solution loads
                    var webpackDevServer = new Uri($"http://localhost:{int.Parse(Configuration.GetSection("Development")["HMRPort"])}");
                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        try
                        {
                            socket.Connect(webpackDevServer.Host, webpackDevServer.Port);
                        }
                        catch (SocketException ex)
                        {
                            if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                            {
                                throw new Exception("Webpack dev server not running - make sure you run the npm script.");
                            }
                        }
                    }

                    spa.UseProxyToSpaDevelopmentServer(webpackDevServer);
                }
            });

            MixPanelInitialisation(app);
        }

        private void MixPanelInitialisation(IApplicationBuilder app)
        {
            var config = new MixpanelConfig
            {
                DataResidencyHandling = MixpanelDataResidencyHandling.EU,
                IpAddressHandling = MixpanelIpAddressHandling.None
            };
            var token = Configuration.GetValue<string>(ALL_VUE_TOKEN);
            var client = !string.IsNullOrEmpty(token) ? new MixpanelClient(token, config) : null;
            MixPanel.MixPanel.Init(client,
                app.ApplicationServices.GetRequiredService<ILogger<MixPanelLogger>>(),
                SavantaConstants.AllVueShortCode);
        }

        private static void MapAuthUrl(IApplicationBuilder app)
        {
            string authAuthority = app.ApplicationServices.GetRequiredService<AppSettings>().AuthAuthority;
            var httpClientFactory = app.ApplicationServices.GetRequiredService<IHttpClientFactory>();
            app.UseRouter(router =>
            {
                router.MapGet("auth/{**slug}", async (request, response, route) =>
                {
                    using var httpClient = httpClientFactory.CreateClient();
                    var requestUri = $"{authAuthority}/{route.Values["slug"]}";
                    await using var stream = await httpClient.GetStreamAsync(requestUri);

                    byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 8);
                    try
                    {
                        int length = 0;
                        while ((length = await stream.ReadAsync(buffer)) > 0)
                        {
                            await response.BodyWriter.WriteAsync(buffer.AsMemory(0, length));
                        }

                        await response.BodyWriter.FlushAsync();
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                });
            });
        }
    }
}
