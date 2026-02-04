# Required Code Changes for App Service Migration

## File 1: Add ForwardedHeaders to Startup.cs

**File**: `src/BrandVue.FrontEnd/Startup.cs`

### Step 1: Add using statement (around line 32, with other using statements)

```csharp
using Microsoft.AspNetCore.HttpOverrides;
```

### Step 2: Modify Configure method

Find this line (around line 366):
```csharp
public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment hostEnvironment)
{
    app.UseCors(CORS_POLICY_ALLOW_HOST_AND_TOOLS);
```

Replace with:
```csharp
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
```

---

## File 2: Create appsettings.Production.json

**File**: `src/BrandVue.FrontEnd/appsettings.Production.json` (NEW FILE)

Create this file with production-specific overrides:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "SavantaLogging": {
    "CommonSettings": {
      "AppName": "BrandVue-Production",
      "Environment": "Production",
      "Overrides": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "LogAggregatorSinkSettings": {
      "LogEventLevel": "Error"
    },
    "ConsoleSinkSettings": {
      "LogEventLevel": "Warning"
    },
    "FileSinkSettings": {
      "LogEventLevel": "Information",
      "RetainedFileCountLimit": 30
    }
  },
  "AllVueUploadFolder": "D:\\home\\site\\wwwroot\\App_Data\\uploads",
  "baseDataPath": "D:\\home\\site\\wwwroot\\App_Data\\{ProductName}\\data",
  "baseMetadataPath": "D:\\home\\site\\wwwroot\\App_Data\\{ProductName}\\config",
  "ApplicationInsights": {
    "ConnectionString": ""
  },
  "AllowedHosts": "*"
}
```

**Note**: Connection strings, auth server URLs, and other secrets should be configured in Azure App Service Configuration (not in this file).

---

## File 3: Add Health Check Endpoint

**File**: `src/BrandVue.FrontEnd/Startup.cs`

### Step 1: In ConfigureServices method (around line 95)

Add after the existing services configuration:

```csharp
// Add health checks for App Service monitoring
services.AddHealthChecks()
    .AddDbContextCheck<AnswersDbContext>("database-answers")
    .AddDbContextCheck<MetaDbContext>("database-meta");
```

### Step 2: In Configure method (find the app.UseEndpoints section)

Find this code (around line 420):
```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    // ... other mappings
});
```

Add the health check mapping:
```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/health");
    endpoints.MapControllers();
    // ... other mappings
});
```

---

## File 4: Ensure Application Insights is Configured

**File**: `src/BrandVue.FrontEnd/Program.cs`

Verify this line exists (should already be there):

```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        })
        .ConfigureServices((context, services) =>
        {
            // Add this line if not present:
            services.AddApplicationInsightsTelemetry();
            
            // ... existing service configuration
        });
```

---

## File 5: Update Project File to Include appsettings.Production.json

**File**: `src/BrandVue.FrontEnd/BrandVue.FrontEnd.Core.csproj`

Ensure the new appsettings file is included in the build output:

```xml
<ItemGroup>
  <Content Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Update="appsettings.Development.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Update="appsettings.Production.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Update="settings.security.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

If there's already an ItemGroup with appsettings.json, just add the Production line to it.

---

## File 6: Create App_Data Directory Structure

**Directory**: `src/BrandVue.FrontEnd/App_Data/uploads/.gitkeep`

Create the directory structure that will be used in production:

```bash
mkdir -p src/BrandVue.FrontEnd/App_Data/uploads
echo "# Keep this directory in source control" > src/BrandVue.FrontEnd/App_Data/uploads/.gitkeep
```

This ensures the directory exists when deployed to App Service.

---

## File 7: Update PostDeploy Script for App Service

**File**: `PostDeploy.AppService.ps1` (NEW FILE - create in project root)

```powershell
Write-Host "Running PostDeploy.AppService.ps1 script"
$ErrorActionPreference = "Stop"

# These variables are set in Octopus Deploy
$ChannelName = $OctopusParameters["Octopus.Release.Channel.Name"]
$AzureWebAppName = $OctopusParameters["AzureWebAppName"]
$Environment = $OctopusParameters["Octopus.Environment.Name"]

Write-Host "ChannelName: $ChannelName"
Write-Host "AzureWebAppName: $AzureWebAppName"
Write-Host "Environment: $Environment"

if ($ChannelName -ne "Default") {
    Write-Host "We are on a feature branch deployment. No action required."
    return
}

if ([string]::IsNullOrWhitespace($AzureWebAppName)) {
    Write-Host "No AzureWebAppName specified. No action required."
    return
}

Write-Host "Configuring App Service post-deployment..."

# Ensure App_Data directories exist
$webRoot = $OctopusParameters["Octopus.Action.Package.InstallationDirectoryPath"]
$appDataPath = Join-Path $webRoot "App_Data"
$uploadsPath = Join-Path $appDataPath "uploads"

if (!(Test-Path $appDataPath)) {
    Write-Host "Creating App_Data directory..."
    New-Item -ItemType Directory -Path $appDataPath -Force | Out-Null
}

if (!(Test-Path $uploadsPath)) {
    Write-Host "Creating uploads directory..."
    New-Item -ItemType Directory -Path $uploadsPath -Force | Out-Null
}

Write-Host "Generating warm up URL..."
$WarmUpUrl = "https://$AzureWebAppName.azurewebsites.net/health"

Write-Host "Warm up URL: $WarmUpUrl"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# App Service typically starts faster than IIS, but still retry
$MaxAttempts = 10
For ($i = 1; $i -le $MaxAttempts; $i++) {
    Write-Host "Calling warm up URL, attempt $i..."
    try {
        $response = Invoke-WebRequest $WarmUpUrl -TimeoutSec 180 -UseBasicParsing
        Write-Host "Warm-up successful. Status: $($response.StatusCode)"
        Write-Host "Response: $($response.Content)"
        Break
    }
    catch {
        if ($i -eq $MaxAttempts) {
            Write-Host "Warning: Warm-up failed after $MaxAttempts attempts."
            Write-Host "Error: $_"
            Write-Host "App Service will continue starting in background."
            # Don't throw - App Service will still work, just might be slower on first request
        }
        else {
            Write-Host "Attempt $i failed: $_"
            Write-Host "Retrying in 10 seconds..."
            Start-Sleep -s 10
        }
    }
}

Write-Host "PostDeploy.AppService.ps1 completed successfully"
```

---

## Summary of Changes

### Required Changes (MUST DO):
1. ✅ Add `using Microsoft.AspNetCore.HttpOverrides;` to Startup.cs
2. ✅ Add `UseForwardedHeaders()` as first middleware in Configure method
3. ✅ Create `appsettings.Production.json` with App Service paths
4. ✅ Add health check endpoint (`/health`)
5. ✅ Update project file to include appsettings.Production.json

### Recommended Changes:
6. ✅ Create App_Data directory structure
7. ✅ Create PostDeploy.AppService.ps1 script
8. ✅ Verify Application Insights is configured

### Testing After Changes:

```bash
# 1. Build and test locally
cd src/BrandVue.FrontEnd
dotnet build
dotnet run

# 2. Test health endpoint
curl https://localhost:5001/health

# 3. Test with Production configuration
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet run

# 4. Verify logs show no errors
```

---

## Octopus Deploy Variable Updates

In Octopus Deploy, add/update these variables:

### New Variables:
- `AzureWebAppName`: 
  - Test: `app-allvue-test`
  - Beta: `app-allvue-beta`
  - Live: `app-allvue-live`

### Update Existing Connection Strings:
Change from Windows Auth:
```
Server=.\\sql2022;Database=VueExport;Integrated Security=True;
```

To SQL Auth:
```
Server=#{SqlServerName};Database=VueExport;User Id=#{SqlUserId};Password=#{SqlPassword};Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;
```

### Variables to Add:
- `SqlServerName`: `your-server.database.windows.net`
- `SqlUserId`: `allvue_app_user`
- `SqlPassword`: `#{SecurePassword}` (use Octopus sensitive variable)

Repeat for all database connections (AnswersConnectionString, MetaConnectionString, etc.)

---

## Deployment Order

1. **Commit these code changes to a feature branch**
2. **Test locally with Production config**
3. **Create Test App Service in Azure**
4. **Update Octopus for Test environment only**
5. **Deploy to Test App Service**
6. **Validate thoroughly**
7. **Then proceed to Beta and Live**

**DO NOT deploy directly to Live without testing in Test environment first!**
