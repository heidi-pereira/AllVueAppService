using AuthServer.GeneratedAuthApi;
using BrandVue.EntityFramework.Answers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;
using UMDataPermissions = UserManagement.BackEnd.Application.UserDataPermissions.Services;
using UserManagement.BackEnd.Application.UserFeaturePermissions;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllPermissionFeatures;
using UserManagement.BackEnd.Infrastructure.Repositories.UserDataPermissions;
using UserManagement.BackEnd.Infrastructure.Repositories.UserFeaturePermissions;
using UserManagement.BackEnd.Library;
using UserManagement.BackEnd.Services;
using UserManagement.BackEnd.WebApi.Middleware;
using Vue.Common.Auth;
using Vue.Common.AuthApi;
using Vue.Common.BrandVueApi;
using MetaData = BrandVue.EntityFramework.MetaData;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Limits.MaxRequestHeadersTotalSize = 32768;
    });
}

ConfigureBuilder(builder);

var app = builder.Build();

ConfigureApp(app);
if (builder.Environment.IsDevelopment())
{
    RunDatabaseMigrations(app);
}

app.Run();

void ConfigureBuilder(WebApplicationBuilder builder)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    });

    builder.Services.AddProblemDetails();
    builder.Services.AddApplicationInsightsTelemetry();
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddTransient<IClaimsTransformation, LocalHostCustomClaimsTransformer>();
        builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
    }

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true; // Compress HTTPS responses
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Optimal;
    });
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Optimal;
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddOpenApi(options => options.AddSchemaTransformer(new StringEnumSchemaFilter()));
    builder.Services.AddSingleton(TimeProvider.System);

    builder.Services.Configure<LoggerFilterOptions>(options =>
    {
        var toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

        if (toRemove is not null)
        {
            options.Rules.Remove(toRemove);
        }
    });

    builder.Services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) => { configuration.GetSection("Settings").Bind(settings); });

    builder.Services.AddDbContextFactory<MetaData.MetaDataContext>((serviceProvider, options) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<Settings>>().Value;
        options.UseSqlServer(settings.MetadataConnectionString);
    });
    builder.Services.AddDbContextFactory<AnswersDbContext>((serviceProvider, options) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<Settings>>().Value;
        options.UseSqlServer(settings.AnswersConnectionString);
    });
    
    ConfigureDependencyInjections(builder.Services, builder.Environment.IsDevelopment());
    AddAuthentication(builder.Services);
    builder.Services.AddHealthChecks();
}

void RunDatabaseMigrations(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var metaDataContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MetaData.MetaDataContext>>();
        using (var metaDataContext = metaDataContextFactory.CreateDbContext())
        {
            metaDataContext.Database.Migrate();
        }
    }
}

void ConfigureApp(WebApplication app)
{
    ConfigureForwardedHeaders(app);

    app.UseResponseCompression();
    var baseUrl = app.Configuration.GetValue<string>("Settings:ApplicationBasePath");
    app.UsePathBase('/' + baseUrl);
    ConfigureStaticFiles(app);

    app.MapOpenApi();

    app.MapScalarApiReference();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseMiddleware<InternalTokenValidationMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.MapFallbackToFile("/index.html");

    app.UseMiddleware<UserManagement.BackEnd.Application.Middleware.ExceptionHandlingMiddleware>();
    app.MapHealthChecks("/health");
}

void ConfigureForwardedHeaders(WebApplication app)
{
    app.UseForwardedHeaders();
}

void ConfigureStaticFiles(WebApplication app)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

void ConfigureDependencyInjections(IServiceCollection services, bool isDevelopmentEnvironment)
{
    services.AddScoped<IAllVueRuleRepository, AllVueRuleRepository>();
    services.AddScoped<IUserDataPermissionRepository, UserDataPermissionRepository>();
    services.AddScoped<UMDataPermissions.IUserDataPermissionsService, UMDataPermissions.UserDataPermissionsService>();
    services.AddScoped<ISurveyGroupService, SurveyGroupService>();
    services.AddScoped<IProjectsService, ProjectsService>();
    services.AddScoped<IProductsService, ProductsService>();
    services.AddScoped<ICompaniesService, CompaniesService>();
    services.AddScoped<IVariableService, VariableService>();
    services.AddScoped<IPermissionFeatureRepository, PermissionFeatureRepository>();
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetAllPermissionFeaturesQueryHandler>());
    services.AddScoped<IRoleRepository, RoleRepository>();
    services.AddScoped<IPermissionOptionRepository, PermissionOptionRepository>();
    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddScoped<IUserContext, UserContext>();
    services.AddScoped<IRoleValidationService, RoleValidationService>();
    services.AddSingleton<IAuthApiClient, CachedAuthApiClient>(provider =>
    {
        var appSettingsService = provider.GetRequiredService<IOptions<Settings>>().Value;
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var isDevAuthServer = isDevelopmentEnvironment && 
                              appSettingsService.AuthAuthority.Contains(
                                  "localhost", StringComparison.OrdinalIgnoreCase);
            
        return new CachedAuthApiClient(isDevAuthServer, appSettingsService.AuthClientId,
            appSettingsService.AuthClientSecret,
            appSettingsService.AuthAuthority, 
            httpClientFactory);
    });
    services.AddSingleton<IBrandVueApiClient, BrandVueApiClient>(provider =>
    {
        var appSettingsService = provider.GetRequiredService<IOptions<Settings>>().Value;
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        return new BrandVueApiClient(appSettingsService.BrandVueApiKey, appSettingsService.BrandVueApiBaseUrl, httpClientFactory);
    });
    services.AddScoped<IUserServiceByAuth>(provider =>
        (IUserServiceByAuth)provider.GetRequiredService <IAuthApiClient>());
    services.AddScoped<IUserOrchestratorService, UserOrchestratorService>();
    services.AddScoped<IExtendedAuthApiClient, ExtendedAuthApiClient>();
    services.AddScoped<IWhiteLabellingService, WhiteLabellingService>();
    services.AddScoped<IUserFeaturePermissionRepository, UserFeaturePermissionRepository>();
    services.AddScoped<IUserManagementService, UserManagementService>();
    services.AddHttpClient<ThemeClient>();
    services.AddScoped<IQuestionRepository, QuestionRepository>();
    services.AddScoped<IQuestionService, QuestionService>();
}

void AddAuthentication(IServiceCollection services)
{
    const string COOKIE = "Cookies";
    using var serviceProvider = services.BuildServiceProvider();
    var settings = serviceProvider.GetRequiredService<IOptions<Settings>>().Value;

    services.AddAuthentication(options =>
    {
        options.DefaultScheme = COOKIE;
        options.DefaultChallengeScheme = "oidc";
    })
        .AddCookie(COOKIE)
        .AddOpenIdConnect("oidc", options =>
        {
            options.ResponseType = "code id_token";
            options.Authority = settings.AuthAuthority;
            options.RequireHttpsMetadata = false; // TODO: Probably make this true for live
            options.ClientSecret = settings.AuthClientSecret;
            options.ClientId = settings.AuthClientId;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("role");
            options.Scope.Add("groups");

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
            options.Events.OnAccessDenied = context =>
            {
                context.Response.Redirect("/AccessDenied");
                context.HandleResponse();
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToIdentityProvider = context =>
            {
                if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
                {
                    // For API requests do not challenge the user, just return 401 Unauthorized
                    if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == StatusCodes.Status200OK)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.HandleResponse();
                    }

                    // Otherwise, set the acr_values based on the host and allow the standard challenge to proceed
                    else
                    {
                        var hostParts = context.Request.Host.Host.Split('.');

                        var portalGroup = (hostParts.Length > 2
                                           && !context.Request.Host.Host.Contains("azurewebsites", StringComparison.OrdinalIgnoreCase))
                            ? hostParts[0].ToLower()
                            : "savanta";

                        context.ProtocolMessage.AcrValues = $"tenant:{portalGroup}";
                    }
                }

                return Task.CompletedTask;
            };
        });
}