using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Interfaces;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OpenEnds.BackEnd;
using OpenEnds.BackEnd.Library;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vue.Common;
using Vue.Common.Auth;
using Vue.Common.Auth.Permissions;
using Vue.Common.AuthApi;
using Vue.Common.BrandVueApi;
using Vue.Common.FeatureFlags;
using MetaData = BrandVue.EntityFramework.MetaData;

const string COOKIE = "Cookies";

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
});

builder.Services.AddProblemDetails();

builder.Services.AddApplicationInsightsTelemetry();

builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);

builder.Services.AddControllers(o=>o.Filters.Add(typeof(JsonExceptionFilter)));

ConfigureServices(builder.Services, builder.Environment);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<OpenEndsFeatureMiddleware>();
app.UseMiddleware<AnalysisAccessPermissionMiddleware>();
app.UseMiddleware<ProjectAccessMiddleware>();

app.MapControllers();

app.MapHealthChecks("/health");

app.MapFallbackToFile("/index.html");

app.Run();


void ConfigureServices(IServiceCollection services, IWebHostEnvironment environment)
{
    services.AddHttpContextAccessor();
    services.AddSingleton(TimeProvider.System);
    services.AddHttpClient<ThemeClient>();
    services.AddHttpClient<SavantaTextThemeAnalyzer>((s, c) =>
    {
        var settings = s.GetRequiredService<IOptions<Settings>>().Value;
        c.BaseAddress = new Uri(settings.TextAnalysisEndpoint);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.TextAnalysisApiKey);
    });

    // https://stackoverflow.com/a/77896586/187030
    services.Configure<LoggerFilterOptions>(options =>
    {
        // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
        // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
        var toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

        if (toRemove is not null)
        {
            options.Rules.Remove(toRemove);
        }
    });

    // Seems to be required to get the API endpoints to return enums as strings.
    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) => { configuration.GetSection("Settings").Bind(settings); });

    services.AddDbContext<OpenEndsContext>((s, options) =>
    {
        var settings = s.GetRequiredService<IOptions<Settings>>().Value;
        options.UseSqlServer(settings.SurveyConnectionString);
    });

    services.AddDbContext<MetadataContext>((s, options) =>
    {
        var settings = s.GetRequiredService<IOptions<Settings>>().Value;
        options.UseSqlServer(settings.MetadataConnectionString);
    });

    services.AddScoped<ExportService>();
    services.AddScoped<OpenEndsService>();

    services.AddAuthentication("ApiKey")
        .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationSchemeHandler>(
            "ApiKey",
            opts => opts.ApiKey = "43E69A78-7F0A-4670-BEB5-6EA2519E38B4"
        );

    services.AddAuthentication(options =>
    {
        options.DefaultScheme = COOKIE;
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie(COOKIE, options =>
    {
        var settings = services.BuildServiceProvider().GetRequiredService<IOptions<Settings>>().Value;

        if (!string.IsNullOrEmpty(settings.ApplicationBasePath))
        {
            options.Cookie.Path = "/" + settings.ApplicationBasePath;
        }
    })
    .AddOpenIdConnect("oidc", options =>
    {
        var settings = services.BuildServiceProvider().GetRequiredService<IOptions<Settings>>().Value;

        options.ResponseType = "code id_token";
        options.Authority = settings.AuthAuthority;
        options.RequireHttpsMetadata = false; // TODO: Probably make this true for live
        options.ClientSecret = settings.AuthClientSecret;
        options.ClientId = settings.AuthClientId;
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
                if (context.Request.Path.StartsWithSegments("/api")
                    && context.Response.StatusCode == StatusCodes.Status200OK)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.HandleResponse();
                }
                else
                {
                    var hostParts = context.Request.Host.Host.Split('.');

                    var portalGroup = (hostParts.Length > 2
                                       && !context.Request.Host.Host.Contains("azurewebsites", StringComparison.OrdinalIgnoreCase))
                        ? hostParts[0].ToLower()
                        : "savanta";

                    context.ProtocolMessage.AcrValues = $"tenant:{portalGroup}";
                }

                if (!string.IsNullOrEmpty(settings.ApplicationBasePath))
                {
                        // Assume path = {org}.all-vue.com and therefore mash the cookies and redirects to work with this app as a virtual directory

                    var uriBuilder = new UriBuilder(new Uri(context.ProtocolMessage.RedirectUri));
                    uriBuilder.Path = "/" + settings.ApplicationBasePath + uriBuilder.Path;
                    context.ProtocolMessage.RedirectUri = uriBuilder.ToString();

                    options.CorrelationCookie.Path = $"/{settings.ApplicationBasePath}/";
                    options.NonceCookie.Path = $"/{settings.ApplicationBasePath}/";

                    var redirectUrl = context.Request.Query["redirectUrl"];

                    context.Properties.RedirectUri = redirectUrl;
                        //context.Properties.RedirectUri = $"/{settings.ApplicationBasePath}/";
                }
                else
                {
                        // Localhost
                    context.Properties.RedirectUri = "/";
                }
            }
            return Task.CompletedTask;
        };
    });

    if (environment.IsDevelopment())
    {
        services.AddScoped<IApiBaseUrlResolver, LocalApiBaseUrlResolver>();
    }
    else
    {
        services.AddScoped<IApiBaseUrlResolver, ApiBaseUrlResolver>();
    }
    services.AddScoped<IUserPermissionHttpClient, UserPermissionHttpClient>();

    // Register repository dependencies for FeatureQueryService
    services.AddDbContextFactory<MetaData.MetaDataContext>((serviceProvider, options) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<Settings>>().Value;
        options.UseSqlServer(settings.MetadataConnectionString);
    });
    services.AddScoped<IUserFeaturesRepository, MetaData.UserFeaturesRepository>();
    services.AddScoped<IOrganisationFeaturesRepository, MetaData.OrganisationFeaturesRepository>();
    services.AddScoped<IAuthApiClient, AuthApiClient>(provider =>
    {
        var settings = provider.GetRequiredService<IOptions<Settings>>().Value;
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var isDevAuthServer = environment.IsDevelopment() && 
                              (settings.AuthAuthority?.Contains(
                                  "localhost", StringComparison.OrdinalIgnoreCase) ?? false);
            
        return new AuthApiClient(isDevAuthServer, settings.AuthClientId!,
            settings.AuthClientSecret!,
            settings.AuthAuthority!, 
            httpClientFactory);
    });
    services.AddSingleton<IBrandVueApiClient, BrandVueApiClient>(provider =>
    {
        var settings = provider.GetRequiredService<IOptions<Settings>>().Value;
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        return new BrandVueApiClient(settings.BrandVueApiKey, settings.BrandVueApiBaseUrl, httpClientFactory);
    });

    services.AddScoped<IPermissionService, PermissionService>();
    services.AddScoped<IFeatureQueryService, FeatureQueryService>();

    services.AddScoped<IUserContext, UserContext>();
    services.AddScoped<IUserFeaturePermissionsService, UserFeaturePermissionsService>();
    services.AddScoped<IOpenEndsRepository, OpenEndsRepository>();
    services.AddScoped<IDataGroupProjectService, DataGroupProjectService>();
}
