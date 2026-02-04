using System.IO;
using Autofac;
using BrandVue.AuthMiddleware;
using BrandVue.Services;
using Vue.AuthMiddleware;
using Vue.AuthMiddleware.Office;
using BrandVue.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Savanta.Logging;
using BrandVue.PublicApi.ModelBinding;
using Microsoft.Extensions.Logging;
using Vue.AuthMiddleware.Local;
using BrandVue.Filters;
using BrandVue.RouteConstraints;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Routing;
using BrandVue.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using static Vue.AuthMiddleware.Constants;
using System.Net.Http;
using System.Buffers;
using System.Runtime.InteropServices;
using BrandVue.AuthMiddleware.Api;
using Microsoft.Extensions.Hosting;
using BrandVue.EntityFramework;
using BrandVue.AuthMiddleware.AuthorizationHandlers;
using BrandVue.AuthMiddleware.FeatureToggle;
using BrandVue.Controllers.Api;
using BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions;
using BrandVue.PublicApi.Controllers;
using BrandVue.Settings;
using BrandVue.Services.Llm.OpenAiCompatible;
using Microsoft.Extensions.Options;
using BrandVue.PublicApi.NSwag;
using Microsoft.AspNetCore.Http.Timeouts;
using NJsonSchema.Generation;
using BrandVue.SourceData.LlmInsights;
using Vue.Common.Constants.Constants;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.NewtonsoftJson;
using Vue.Common.Auth.Permissions;
using BrandVue.SourceData.Snowflake;
using BrandVue.SourceData.CalculationLogging;
using Microsoft.AspNetCore.HttpOverrides;

namespace BrandVue
{
    public partial class Startup
    {
        private const string MIXPANEL = "MixPanel";
        private const string PRODUCT_SETTINGS = "productsToLoadDataFor";
        private const string AZURE_AI_CLIENT_SETTINGS = "Azure:OpenAI";
        private const string OPENAI_CLIENT_SETTINGS = "OpenAI";
        private const string FEATURE_FLAGS_SETTINGS = "FeatureFlags";
        private const string LLM_AZURE_COSMOS_DB_SETTINGS = "LlmAzureCosmosDbSettings";
        private const string SNOWFLAKE_DAPPER_DB_SETTINGS = "SnowflakeDapperDbSettings";
        private const string DOCUMENT_INGESTOR_API_SETTINGS = "DocumentIngestorApi";
        private const string CORS_POLICY_ALLOW_HOST_AND_TOOLS = "AllowHostAndTools";
        private readonly ILoggerFactory _loggerFactory;
        private readonly RequestTimeoutsInSeconds _requestTimeoutsInSeconds = new();
        private IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _loggerFactory = SavantaLogging.CreateFactory();
            var mixPanelSettings = new MixPanelSettings();
            _configuration = configuration;
            configuration.GetSection(MIXPANEL).Bind(mixPanelSettings);
            configuration.GetSection(nameof(RequestTimeoutsInSeconds)).Bind(_requestTimeoutsInSeconds);

            var productSettings = new ProductSettings
            {
                ProductToLoad = configuration.GetValue<string>(PRODUCT_SETTINGS)
            };
            IoCConfig = new IoCConfig(
                new AppSettings(configuration: configuration),
                _loggerFactory,
                Options.Create(mixPanelSettings),
                Options.Create(productSettings),
                configuration);
            ConfigureLocallyFabricatedClaims = options => options.FabricateClaimsIfLocal = AppSettings.AllowLocalToBypassConfiguredAuthServer;
        }

        public IoCConfig IoCConfig { get; set; }

        public Action<LocalAuthenticationOptions> ConfigureLocallyFabricatedClaims { get; set; }

        private AppSettings AppSettings => IoCConfig.AppSettings;

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            // "Scheme": Way of authenticating, e.g. cookie, api key, magically-by-being-local

            // Authentication phases https://docs.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-5.0#authenticate
            // "Authenticate": Did their request contain the info we needed? (e.g. Sent us a cookie or a jwt token)
            // "Challenge": Go and get the details we needed (e.g. via redirect)
            // "Sign In": Put details into the response so that future requests can include them (e.g. set-cookie header)

            // ASP Net auth phase logic summary:
            // * foreach (var scheme in request.schemes) if (!request.IsAuthenticated() && scheme.Authenticate()) scheme.SignIn()
            // * foreach (var scheme in request.schemes) if (!request.IsAuthenticated()) scheme.Challenge();

            services.AddRequestTimeouts(options =>
                options.DefaultPolicy = new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(_requestTimeoutsInSeconds.Default) }
            );

            services.AddFusionCache(FeatureToggleServiceDecorator.CacheName).WithSerializer(new FusionCacheNewtonsoftJsonSerializer());

            // This creates the dictionary of auth schemes, the "Policy" in AddAuthorization defines when each is used
            services.AddAuthentication(auth =>
                {
                    auth.DefaultSignInScheme = Schemes.CookieScheme; // openid and localauth both sign in using the default scheme - cookie, rather than directly themselves
                    auth.DefaultAuthenticateScheme = Schemes.LocalOrUnauthorizedScheme; //Used during logout
                    auth.DefaultForbidScheme = Schemes.LocalOrUnauthorizedScheme;
                })
                .AddCookie(Schemes.CookieScheme, options =>
                {
                    SetCommonCookieOptions(options);
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnSigningIn = context =>
                        {
                            context.Options.Cookie.Path = context.HttpContext.Request.PathBase.Value?.TrimEnd('/') ?? "/";
                            return Task.CompletedTask;
                        },
                        OnSignedIn = async context =>
                        {
                            // add the cookie for reporting
                            await context.HttpContext.SignInAsync(Schemes.ReportingCookie, context.Principal, context.Properties);
                        },
                    };
                })
                .AddCookie(Schemes.ReportingCookie, options =>
                {
                    SetCommonCookieOptions(options);
                    options.Cookie.Path = "/reporting";
                })
                .AddPolicyScheme(Schemes.AuthForResource, "Choose correct auth for the requested resource", options =>
                {
                    options.ForwardDefaultSelector = AuthForResource;
                })
                .AddOpenIdConnect(Schemes.OpenIdScheme, ConfigureSavantaOpenId)
                .AddOAuth2Introspection(Schemes.ApiKeyScheme, ConfigureTokenIntrospection)
                .AddScheme<LocalAuthenticationOptions, LocalAuthenticationHandler>(Schemes.LocalOrUnauthorizedScheme, ConfigureLocallyFabricatedClaims);
            services.Configure<AzureAiClientSettings>(_configuration.GetSection(AZURE_AI_CLIENT_SETTINGS));
            services.Configure<OpenAiCompatibleChatServiceSettings>(_configuration.GetSection(OPENAI_CLIENT_SETTINGS));
            services.Configure<LlmAzureCosmosDbSettings>(_configuration.GetSection(LLM_AZURE_COSMOS_DB_SETTINGS));
            services.Configure<AiDocumentIngestorApiClientSettings>(_configuration.GetSection(DOCUMENT_INGESTOR_API_SETTINGS));

            services
                .AddControllers(options =>
                {
                    options.Filters.Add(new AuthorizeFilter());
                    options.Filters.Add(new ValidateModelFilter());
                    options.Filters.Add(new NoDataEmptyResponse());
                    options.ModelBinderProviders.Insert(0, new DateModelBinder.Provider());
                })
                .AddApplicationPart(typeof(Startup).Assembly)
                .AddControllersAsServices()
                .AddNewtonsoftJson(options => { BrandVueJsonConvert.InitializeSettings(options.SerializerSettings); });
            services.AddOpenApiDocument(document =>
            {
                document.DocumentName = ApiController.InternalApiGroupName;
                document.ApiGroupNames = [ApiController.InternalApiGroupName];
                document.OperationProcessors.Add(new MarkAsRequiredIfNonNullableProcessor());
                document.OperationProcessors.Add(new OnlyGeneratePostForCompressedUris());
                document.SchemaSettings.SchemaProcessors.Add(new MarkAsRequiredIfNonNullableProcessor());
                document.SchemaSettings.DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;

            });
            services.AddOpenApiDocument(document =>
            {
                document.DocumentName = PublicApiController.PublicApiGroupName;
                document.ApiGroupNames = [PublicApiController.PublicApiGroupName];
                document.DocumentProcessors.Add(new NSwagBrandVueOpenApiDocumentProcessor());
                document.SchemaSettings.DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
                document.SchemaSettings.IgnoreObsoleteProperties = false;
                document.DocumentTemplate = File.ReadAllText("SurveySetsOpenApi3Template.json");
                document.SchemaSettings.FlattenInheritanceHierarchy = true;
            });
            services.AddRazorPages();

            services.Configure<RouteOptions>(routeOptions =>
            {
                routeOptions.ConstraintMap.Add(ValidSubproductConstraint.Key, typeof(ValidSubproductConstraint));
            });

            services.AddAuthorization(authOptions =>
            {
                var defaultPolicy = new AuthorizationPolicyBuilder(Schemes.CookieScheme, Schemes.AuthForResource)
                    // Authenticate: First check if they have a valid cookie, otherwise move onto resource-specific checks
                    .RequireAuthenticatedUser()
                    .AddRequirements(DefaultRequirements())
                    .Build();
                authOptions.DefaultPolicy = defaultPolicy;
                authOptions.FallbackPolicy = authOptions.DefaultPolicy;

                AddUserRoleOrAbovePolicy(defaultPolicy, authOptions);
                AddFeaturePermissionPolicies(defaultPolicy, authOptions);
                AddPublicApiResourcePolicy(authOptions, defaultPolicy, ResourceNames.MetricResults);
                AddPublicApiResourcePolicy(authOptions, defaultPolicy, ResourceNames.RawSurveyData);
            });
            services.AddSingleton<IAuthorizationHandler, RequiredClaimsHandler>();
            services.AddSingleton<IAuthorizationHandler, CompanyAccessHandler>();
            services.AddSingleton<IAuthorizationHandler, ProjectAccessHandler>();
            services.AddSingleton<IAuthorizationHandler, RequestScopeAccessHandler>();
            services.AddSingleton<IAuthorizationHandler, TrialDateHandler>();
            services.AddSingleton<IAuthorizationHandler, PublicApiResourceAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, FeaturePermissionHandler>();
            services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationResultTransformer>();
            services.AddTransient<IClaimsTransformation, RoleClaimsTransformation>();
            services.AddHttpClient();

            services.AddCors(options =>
            {
                // This policy allows CORS requests from tools like Figma make and v0.app
                options.AddPolicy(CORS_POLICY_ALLOW_HOST_AND_TOOLS, builder =>
                {
                    builder
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .WithOrigins("https://*.figma.site", "https://figma.com", "https://v0.app", "https://*.vercel.app")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy<string, ApiSlidingWindowPolicy>(RateLimitPolicyNames.ApiSlidingWindow);
            });

            services.AddSingleton<ISnowflakeDbConnectionFactory>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var connectionString = configuration["SnowflakeConnectionString"];
                return new SnowflakeDbConnectionFactory(connectionString);
            });
            services.AddSingleton<ISnowflakeRepository, SnowflakeRepository>();
            services.AddSingleton<ICalculationLogger, CalculationLogger>();
            services.Configure<SnowflakeDapperDbSettings>(_configuration.GetSection(SNOWFLAKE_DAPPER_DB_SETTINGS));
        }

        private static void SetCommonCookieOptions(CookieAuthenticationOptions options)
        {
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.AccessDeniedPath = LoginFailedRedirect;
            options.DataProtectionProvider = DataProtectionProvider.Create(
                new DirectoryInfo(Path.Combine(GetWritableAppDataFolder(), "MIG", "BrandVue", "Keys")));
            options.Cookie.Name = CookieName;
            options.Cookie.SameSite = SameSiteMode.Lax;
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

        private static IAuthorizationRequirement[] DefaultRequirements() =>
            new IAuthorizationRequirement[]
            {
                new RequiredClaimsHandler.Requirement(RequiredClaimTypes),
                new CompanyAccessHandler.Requirement(),
                new ProjectAccessHandler.Requirement(),
                new RequestScopeAccessHandler.Requirement(),
                new TrialDateHandler.Requirement()
            };

        private static void AddUserRoleOrAbovePolicy(AuthorizationPolicy basePolicy, AuthorizationOptions authOptions)
        {
            authOptions.AddPolicy(UserRoleOrAbove, new AuthorizationPolicyBuilder()
                .Combine(basePolicy)
                .RequireRole(Roles.SystemAdministrator, Roles.Administrator, Roles.User, Roles.ReportViewer)
                .Build());
        }

        private static void AddFeaturePermissionPolicies(AuthorizationPolicy basePolicy, AuthorizationOptions authOptions)
        {
            authOptions.AddPolicy(FeatureRolePolicy.VariablesCreate_OR_VariableEdit, new AuthorizationPolicyBuilder()
                .Combine(basePolicy)
                .AddRequirements(new FeaturePermissionRequirement(
                    PermissionFeaturesOptions.VariablesCreate, 
                    PermissionFeaturesOptions.VariablesEdit))
                .Build());

            authOptions.AddPolicy(FeatureRolePolicy.VariablesCreate_OR_VariableEdit_OR_VariableDelete, new AuthorizationPolicyBuilder()
                .Combine(basePolicy)
                .AddRequirements(new FeaturePermissionRequirement(
                    PermissionFeaturesOptions.VariablesCreate, 
                    PermissionFeaturesOptions.VariablesEdit,
                    PermissionFeaturesOptions.VariablesDelete))
                .Build());

            foreach (PermissionFeaturesOptions feature in Enum.GetValues(typeof(PermissionFeaturesOptions)))
            {
                authOptions.AddPolicy(feature.PolicyName(), new AuthorizationPolicyBuilder()
                .Combine(basePolicy)
                .AddRequirements(new FeaturePermissionRequirement(feature))
                .Build());
            }
        }

        private static void AddPublicApiResourcePolicy(AuthorizationOptions authOptions, AuthorizationPolicy basePolicy, string apiResourceName)
        {
            authOptions.AddPolicy(PublicApiResourcePolicyPrefix + apiResourceName, new AuthorizationPolicyBuilder()
                .Combine(basePolicy)
                .AddRequirements(new PublicApiResourceAuthorizationHandler.Requirement(apiResourceName))
                .Build());
        }

        private string AuthForResource(HttpContext ctx)
        {
            if (!AppSettings.IsAuthServerConfigured()) return Schemes.LocalOrUnauthorizedScheme;
            var requestScope = ctx.GetOrCreateRequestScope();
            return requestScope.Resource switch
            {
                RequestResource.PublicApi => Schemes.ApiKeyScheme,
                // Locally: If has an auth header, authenticate using ApiKeyScheme 
                RequestResource.InternalApi when ctx.HasAuthHeader() => Schemes.ApiKeyScheme,
                // Locally: Reporting works by making a local request, non-locally this will just return unauthorized so the typescript can detect it and redirect the browser
                RequestResource.InternalApi => Schemes.LocalOrUnauthorizedScheme,
                // Reporting works by making a local request, in other cases we'd want to fall through to OpenIdScheme below
                RequestResource.Ui when ctx.IsLocalWithAuthBypass() => Schemes.LocalOrUnauthorizedScheme,
                _ => Schemes.OpenIdScheme
            };

        }

        // This runs after ConfigureServices so the things
        // here will override registrations made in ConfigureServices.
        // Don't build the container; that gets done for you by the factory.
        public virtual void ConfigureContainer(ContainerBuilder builder) => IoCConfig.Register(builder);

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment hostEnvironment)
        {
            // CRITICAL: Must be first for App Service to work correctly
            // App Service uses reverse proxy - we need to trust forwarded headers
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                                  ForwardedHeaders.XForwardedProto | 
                                  ForwardedHeaders.XForwardedHost
            });
            
            app.UseCors(CORS_POLICY_ALLOW_HOST_AND_TOOLS);
            
            app.UseSavantaExceptionHandling(AppSettings.AppDeploymentEnvironment);
            app.UseMiddleware<OfficeHyperlinkPlacaterMiddleware>();
            app.UseMiddleware<LegacyCookieRemovalMiddleware>();

            app.Use((context, next) =>
            {
                context.Response.Headers.Remove("Server");
                return next.Invoke();
            });

            //Get security settings
            var securitySettings = new ConfigurationBuilder()
                .AddJsonFile("settings.security.json")
                .Build()
                .GetSection("ContentSecurityPolicy")
                .Get<ContentSecurityPolicy>() ?? new ContentSecurityPolicy();

            AddSecurityHeaders(app, securitySettings, AppSettings);

            app.UseHttpsRedirection();

            app.UseStaticFiles(); // Serves wwwwroot
            UseProductMetadataStaticFiles(app, "pages");
            UseProductMetadataStaticFiles(app, "assets");
            IncludeStaticFilesFromDocumentation(app, hostEnvironment);

            app.UseRouting();
            app.UseRequestTimeouts();

            if (hostEnvironment.IsDevelopment() && AppSettings.IsAuthServerConfigured())
            {
                MapAuthUrl(app);
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.UseMiddleware<FeatureToggleMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }

        private void IncludeStaticFilesFromDocumentation(IApplicationBuilder app, IWebHostEnvironment hostEnvironment)
        {
            var contentTypeProvider = new FileExtensionContentTypeProvider
            {
                Mappings =
                {
                    [".ps1"] = "text/plain"
                },
            };
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(hostEnvironment.WebRootPath, @"developers")),
                RequestPath = new PathString("/developers"),
                ContentTypeProvider = contentTypeProvider
            });
        }

        private void UseProductMetadataStaticFiles(IApplicationBuilder app, string subdir)
        {
            string productSpecificAssets = Path.Combine(AppSettings.GetRootedPathWithProductNameReplaced("baseMetadataPath"), "..", subdir);
            if (!Directory.Exists(productSpecificAssets)) return;
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString($"/ClientData/{subdir}"),
                FileProvider = new PhysicalFileProvider(productSpecificAssets)
            });
        }

        private static void MapAuthUrl(IApplicationBuilder app)
        {
            string authAuthority = app.ApplicationServices.GetRequiredService<AppSettings>().GetAuthServerUrl();
            var httpClientFactory = app.ApplicationServices.GetRequiredService<IHttpClientFactory>();
            app.UseRouter(router =>
            {
                router.MapGet("auth/{**slug}", async (request, response, route) =>
                {
                    using var httpClient = httpClientFactory.CreateClient();
                    var requestUri = $"{authAuthority}/{route.Values["slug"]}";
                    var result = await httpClient.GetAsync(requestUri);
                    response.ContentType = result.Content.Headers.ContentType.MediaType;
                    await using var stream = await result.Content.ReadAsStreamAsync();

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
