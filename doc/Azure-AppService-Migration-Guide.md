# AllVue Migration Guide: VM to Azure App Service

## Overview
This guide details the migration of AllVue (BrandVue) from Azure Virtual Machine (IIS) deployment to Azure App Service. The application consists of a .NET Core backend API and a React frontend, both currently deployed to the same IIS site on a VM. This migration will deploy both components to a single Azure App Service.

## Current Architecture
- **Backend**: ASP.NET Core Web API (`BrandVue.FrontEnd.Core.csproj`)
- **Frontend**: React SPA (built with webpack, served via ASP.NET Core static files)
- **Deployment**: Azure Pipelines → Octopus Deploy → IIS on Azure VM
- **Package**: Single NuGet package containing both frontend and backend
- **Current Pipeline**: `BrandVue.yaml` in `src/`

---

## Part 1: Azure Portal Configuration

### 1.1 Create Azure App Service

#### Step 1: Navigate to Azure Portal
1. Go to [Azure Portal](https://portal.azure.com)
2. Search for "App Services" in the top search bar
3. Click "+ Create" → "Web App"

#### Step 2: Configure Basic Settings
Fill in the following details:

**Basics Tab:**
- **Subscription**: Select your Azure subscription
- **Resource Group**: Create new or select existing (e.g., `rg-allvue-prod`)
- **Name**: `app-allvue-{environment}` (e.g., `app-allvue-test`, `app-allvue-beta`, `app-allvue-live`)
- **Publish**: `Code`
- **Runtime stack**: `.NET 8` (or your current .NET version - check `BrandVue.FrontEnd.Core.csproj`)
- **Operating System**: `Windows` (recommended for existing .NET apps)
- **Region**: Select closest to your users (e.g., `UK South`, `West Europe`)

**App Service Plan:**
- **Plan**: Create new or select existing
- **Name**: `asp-allvue-{environment}`
- **SKU and size**: 
  - **Test/Beta**: `S1 Standard` (1 core, 1.75 GB RAM) - minimum recommended
  - **Production**: `P2v3 Premium` (2 cores, 8 GB RAM) - based on memory requirements noted in PostDeploy.ps1
  - Click "Change size" to select appropriate tier

#### Step 3: Configure Deployment Settings
**Deployment Tab:**
- **Continuous deployment**: Enable (will be configured with GitHub Actions/Azure Pipelines)
- Or skip and configure later with Octopus Deploy

**Networking Tab:**
- **Enable public access**: Yes (unless you need VNet integration)
- **Enable network injection**: Configure if you need private connectivity to databases

**Monitoring Tab:**
- **Enable Application Insights**: Yes (recommended)
- **Application Insights**: Create new or select existing

**Tags Tab:**
- Add tags for organization (e.g., `Environment: Production`, `Application: AllVue`, `Cost Center: your-cost-center`)

#### Step 4: Review and Create
1. Click "Review + create"
2. Review all settings
3. Click "Create"
4. Wait for deployment to complete (2-5 minutes)

### 1.2 Configure App Service Settings

After the App Service is created, configure the following:

#### Configuration Settings (Application Settings)
Navigate to: **App Service → Configuration → Application settings**

Add the following environment variables (based on your current configuration):

```
ASPNETCORE_ENVIRONMENT=Test  (or Beta/Production)
WEBSITE_TIME_ZONE=GMT Standard Time
WEBSITE_RUN_FROM_PACKAGE=1  (enables running from ZIP deployment)
```

**Connection Strings** (under Configuration → Connection strings):
Based on your current deployment, add connection strings from `appsettings.json`:
- `AnswersDbConnection`
- `MetaDbConnection`
- `VueExportConnection`
- Any other database connections

**Note**: Retrieve these from your current Octopus variables or VM configuration.

#### General Settings
Navigate to: **App Service → Configuration → General settings**

- **Stack settings**:
  - **.NET version**: Match your project version
  - **Platform**: 64-bit
  - **Always On**: On (for Production/Beta), Off (for Test to save costs)
  - **ARR affinity**: On (if using in-memory sessions)

- **Platform settings**:
  - **HTTP version**: 2.0
  - **Web sockets**: Off (unless required)
  - **Minimum TLS version**: 1.2

#### Scale Settings
Navigate to: **App Service → Scale out (App Service plan)**

For Production:
- **Manual scale** or **Custom autoscale**
- Configure autoscale rules based on CPU/Memory metrics if needed

### 1.3 Configure Deployment Slots (Optional but Recommended)

For Production environment, create deployment slots:

1. Navigate to: **App Service → Deployment slots**
2. Click "+ Add Slot"
3. Name: `staging`
4. Clone settings from production
5. Use this for blue-green deployments

---

## Part 2: Octopus Deploy Configuration

### 2.1 Update Octopus Deploy Project

#### Step 1: Add Azure Account
1. Go to Octopus Deploy → **Infrastructure → Accounts**
2. Click **ADD ACCOUNT** → **Azure Subscriptions**
3. Enter your Azure subscription details:
   - **Name**: `Azure Subscription - Savanta`
   - **Subscription ID**: From Azure Portal
   - **Authentication**: Use Azure Service Principal
   - **Client ID**, **Tenant ID**, **Client Secret**: Create in Azure AD if needed

#### Step 2: Create Azure Web App Deployment Target
1. Go to **Infrastructure → Deployment Targets**
2. Click **ADD DEPLOYMENT TARGET** → **Azure Web App**
3. Configure:
   - **Name**: `Azure App Service - AllVue Test` (repeat for Beta/Live)
   - **Environments**: Test (or Beta/Live)
   - **Account**: Select the Azure account created above
   - **Web App**: Select `app-allvue-test` (or appropriate environment)

#### Step 3: Update Deployment Process

Navigate to: **Projects → BrandVue AllVue → Process**

Replace the current IIS deployment steps with Azure Web App steps:

**Step 1: Deploy Azure Web App**
1. Click **ADD STEP** → **Azure** → **Deploy an Azure Web App**
2. Configure:
   - **Step Name**: `Deploy AllVue to Azure App Service`
   - **On Behalf Of**: Azure Web App deployment target (scoped to environment)
   - **Package**: `BrandVue` (same as current)
   - **Web App Name**: `#{AzureWebAppName}`
   - **Deployment Slot**: Leave empty or use `staging` for production

**Step 2: Azure App Service Settings (Optional)**
1. Click **ADD STEP** → **Azure** → **Run an Azure Script**
2. Use Azure PowerShell/CLI to configure app settings if not done via Portal
3. Example script:
```powershell
az webapp config appsettings set --resource-group rg-allvue-test --name app-allvue-test --settings @settings.json
```

#### Step 4: Update Variables

Navigate to: **Projects → BrandVue AllVue → Variables**

Add new variables:
- `AzureWebAppName`: 
  - Test: `app-allvue-test`
  - Beta: `app-allvue-beta`
  - Live: `app-allvue-live`
- `AzureResourceGroup`:
  - Test: `rg-allvue-test`
  - Beta: `rg-allvue-beta`
  - Live: `rg-allvue-live`

Keep existing variables for:
- Connection strings
- API keys
- Other configuration settings

**Remove/Update VM-specific variables:**
- `BrandVueWebsiteAndAppPoolName` (no longer needed)
- `IISWebsiteDomain` (no longer needed)
- Update `Domain` variable to use `.azurewebsites.net` or custom domain

### 2.2 Remove IIS-Specific Steps

1. Remove or disable the current **Deploy to IIS** steps
2. Remove **IIS Web Site** deployment targets
3. Archive the `PostDeploy.ps1` script (App Service doesn't use IIS warm-up the same way)

### 2.3 Update Health Checks

Replace IIS-specific health checks with:
- **Health Check URL**: `https://#{AzureWebAppName}.azurewebsites.net/health`
- Configure in Octopus health check settings

---

## Part 3: Azure Pipeline Modifications

### 3.1 Update BrandVue.yaml

The current pipeline already creates a suitable package for App Service. Only minor changes needed:

**Current pipeline (no changes required for package creation):**
```yaml
# Pack
- task: OctopusPackNuGet@6
  displayName: Pack BrandVue Site
  inputs:
    PackageId: BrandVue
    PackageVersion: $(packageVersion)
    SourcePath: src/BrandVue.FrontEnd/bin/publish
    OutputPath: $(Build.ArtifactStagingDirectory)
    NuGetDescription: 'BrandVue Site'
    NuGetAuthors: 'Savanta'
```

This package structure works for both IIS and App Service.

**Optional: Update PostDeploy.ps1 for App Service**

Create a new `PostDeploy.AppService.ps1` in the project root:

```powershell
Write-Host "Running PostDeploy.AppService.ps1 script"
$ErrorActionPreference = "Stop"

# These variables are set in Octopus Deploy
$ChannelName = $OctopusParameters["Octopus.Release.Channel.Name"]
$AzureWebAppName = $OctopusParameters["AzureWebAppName"]

Write-Host "ChannelName: $ChannelName"
Write-Host "AzureWebAppName: $AzureWebAppName"

if ($ChannelName -ne "Default") {
    Write-Host "We are on a feature branch deployment. No action required."
    return
}

Write-Host "Configuring App Service..."

# App Service automatically handles startup and warm-up
# AlwaysOn setting in App Service handles the equivalent of IIS startMode

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
        Break
    }
    catch {
        if ($i -eq $MaxAttempts) {
            Write-Host "Warning: Warm-up failed after $MaxAttempts attempts. App Service will continue starting in background."
            # Don't throw - App Service will still work, just might be slower on first request
        }
        else {
            Write-Host "Attempt $i failed, retrying..."
            Start-Sleep -s 5
        }
    }
}

Write-Host "PostDeploy.AppService.ps1 completed"
```

**Update Octopus to use the new script:**
- In Octopus Deploy process, update the PostDeploy script step to use `PostDeploy.AppService.ps1`
- Or conditionally select script based on deployment target type

### 3.2 Update Build Configuration (Optional)

If you want to add App Service-specific deployment artifacts:

Add to `BrandVue.yaml` after the pack step:

```yaml
# Optional: Create web.config for App Service (if not already in project)
- powershell: |
    $webConfig = @"
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
      <system.webServer>
        <handlers>
          <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
        </handlers>
        <aspNetCore processPath="dotnet" arguments=".\BrandVue.FrontEnd.Core.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
      </system.webServer>
    </configuration>
    "@
    $webConfig | Out-File -FilePath "src/BrandVue.FrontEnd/bin/publish/web.config" -Encoding UTF8
  displayName: 'Create web.config for App Service'
  condition: ne(variables['Build.Reason'], 'PullRequest')
```

**Note**: ASP.NET Core projects typically auto-generate `web.config`, so this may not be necessary.

---

## Part 4: Application Code Changes (Minimal)

### 4.1 Verify web.config (if exists)

Check if `src/BrandVue.FrontEnd/web.config` exists and ensure it has App Service-compatible configuration:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" 
                arguments=".\BrandVue.FrontEnd.Core.dll" 
                stdoutLogEnabled="false" 
                stdoutLogFile=".\logs\stdout" 
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

### 4.2 Update Startup.cs (if needed)

Verify `UseForwardedHeaders` is configured in `Startup.cs` for Azure App Service:

```csharp
// In Configure method
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

This is already present in similar projects (OpenEnds, UserManagement), check if BrandVue has it.

### 4.3 Update appsettings.json

Ensure `appsettings.json` uses environment-based configuration that works with App Service:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*"
}
```

App Service will inject connection strings and other settings via environment variables (from Azure Portal configuration).

---

## Part 5: Migration Execution Plan

### Phase 1: Test Environment (Low Risk)
1. **Week 1**: Create Test App Service in Azure Portal
2. Configure App Service settings, connection strings
3. Update Octopus Deploy with Test Azure Web App target
4. Update deployment process for Test environment only
5. Deploy to Test App Service
6. Validate all functionality
7. Monitor for 1 week

### Phase 2: Beta Environment (Medium Risk)
1. **Week 3**: Repeat Phase 1 steps for Beta environment
2. Deploy to Beta App Service
3. User acceptance testing
4. Performance testing and tuning
5. Monitor for 1 week

### Phase 3: Production Environment (High Risk)
1. **Week 5**: Create Production App Service with deployment slot
2. Deploy to staging slot first
3. Smoke testing on staging slot
4. Swap staging to production slot (zero downtime)
5. Monitor closely for first 24-48 hours
6. Keep VM running for 1 week as fallback

### Rollback Plan
If issues occur in Production:
1. Swap staging/production slots (if using slots) - **Instant rollback**
2. Or redeploy previous version via Octopus to App Service
3. Or redirect traffic back to VM (update DNS/load balancer)
4. Investigate issues in non-production environment

---

## Part 6: Post-Migration Tasks

### 6.1 Custom Domain Configuration
If using custom domains (not `.azurewebsites.net`):

1. Navigate to: **App Service → Custom domains**
2. Click **+ Add custom domain**
3. Enter domain name (e.g., `demo.savanta.com`)
4. Validate domain ownership (TXT record in DNS)
5. Add CNAME record pointing to `{app-name}.azurewebsites.net`
6. Click **Validate** and **Add**

### 6.2 SSL/TLS Certificate Configuration

1. Navigate to: **App Service → TLS/SSL settings**
2. **Option A - App Service Managed Certificate (Free)**:
   - Navigate to **Private Key Certificates (.pfx)**
   - Click **+ Create App Service Managed Certificate**
   - Select your custom domain
   - Click **Create**

3. **Option B - Upload Custom Certificate**:
   - Click **+ Upload certificate**
   - Select `.pfx` file and password
   - Upload

4. **Bind Certificate to Domain**:
   - Navigate to **Custom domains**
   - Click **Add binding** next to your domain
   - Select certificate
   - Choose **SNI SSL** (recommended)
   - Click **Add binding**

### 6.3 Monitoring and Alerts

Configure in Azure Monitor:
1. Navigate to: **App Service → Monitoring → Alerts**
2. Create alert rules for:
   - High CPU usage (> 80% for 5 minutes)
   - High Memory usage (> 85% for 5 minutes)
   - HTTP 5xx errors (> 10 in 5 minutes)
   - Response time (> 3 seconds average)

### 6.4 Application Insights Configuration

1. Verify Application Insights is connected
2. Navigate to: **Application Insights** resource
3. Configure:
   - **Availability tests**: Create ping test for health endpoint
   - **Smart Detection**: Enable anomaly detection
   - **Logs**: Set retention period (90 days recommended)

### 6.5 Decommission VM

After successful migration and 2-week monitoring period:
1. Stop VM (don't delete immediately)
2. Monitor for any issues
3. After 1 month, if no issues:
   - Take final snapshot/backup
   - Delete VM and associated resources
   - Cancel VM-related Azure costs

---

## Part 7: Key Differences: VM vs App Service

### What Changes
| Aspect | VM (IIS) | App Service |
|--------|----------|-------------|
| **Server Management** | Manual Windows updates, IIS config | Fully managed, automatic updates |
| **Scaling** | Manual VM resize | Built-in auto-scale |
| **Deployment** | IIS Web Deploy, file copy | ZIP deploy, Git, Docker |
| **Cost** | Pay for VM even when idle | Pay for App Service plan (can scale to zero for dev/test) |
| **Monitoring** | Manual setup | Built-in Application Insights |
| **SSL/TLS** | Manual certificate management | Free managed certificates |
| **Backup** | VM snapshots | Built-in backup for App Service |
| **Deployment Slots** | Not available | Built-in staging slots |

### What Stays the Same
- Application code (minimal changes)
- Database connections
- API endpoints and functionality
- Frontend React application
- Authentication and authorization logic
- Business logic

### Benefits of App Service
1. **Reduced Maintenance**: No OS patching, IIS configuration, or server management
2. **Better Scaling**: Auto-scale based on demand
3. **Deployment Slots**: Zero-downtime deployments with slot swaps
4. **Built-in Features**: Monitoring, diagnostics, backups included
5. **Cost Optimization**: Scale down/up based on usage
6. **Better DevOps**: Integrated with Azure Pipelines, GitHub Actions

---

## Part 8: Troubleshooting Common Issues

### Issue 1: Application doesn't start
**Symptoms**: HTTP 500 errors, "Application Error" page

**Solutions**:
1. Check Application Insights logs
2. Enable stdout logging in `web.config`:
   ```xml
   <aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
   ```
3. Download logs: **App Service → Advanced Tools (Kudu) → Debug console**
4. Check environment variables: **Configuration → Application settings**
5. Verify .NET runtime version matches application

### Issue 2: Static files (React app) not loading
**Symptoms**: Blank page, 404 for JS/CSS files

**Solutions**:
1. Verify `app.UseStaticFiles()` in `Startup.cs`
2. Check `wwwroot` folder is included in published output
3. Verify webpack build completed successfully in pipeline
4. Check file permissions in App Service

### Issue 3: Database connection fails
**Symptoms**: SQL errors, timeout errors

**Solutions**:
1. Verify connection string in **Configuration → Connection strings**
2. Check firewall rules on Azure SQL:
   - Add App Service outbound IP addresses
   - Or enable "Allow Azure services" (less secure)
3. Test connection from Kudu console:
   ```powershell
   # In Kudu PowerShell console
   Test-NetConnection -ComputerName your-sql-server.database.windows.net -Port 1433
   ```

### Issue 4: Performance issues
**Symptoms**: Slow response times, high memory usage

**Solutions**:
1. Scale up App Service plan (more CPU/memory)
2. Enable **Always On** setting
3. Check Application Insights for slow queries/operations
4. Consider adding Azure CDN for static content
5. Review database query performance

### Issue 5: Authentication not working
**Symptoms**: 401 errors, redirect loops

**Solutions**:
1. Verify authentication settings in `Startup.cs`
2. Check cookie domain settings (use `.azurewebsites.net` or custom domain)
3. Verify Azure AD/OAuth redirect URIs include new App Service URL
4. Check HTTPS redirect settings

---

## Part 9: Configuration Checklist

### Azure Portal Checklist
- [ ] App Service created for each environment (Test, Beta, Live)
- [ ] App Service Plan sized appropriately
- [ ] Application settings configured
- [ ] Connection strings configured
- [ ] **Always On** enabled (for Beta/Live)
- [ ] Application Insights connected
- [ ] Custom domains configured (if applicable)
- [ ] SSL certificates bound
- [ ] Deployment slots created (for Live)
- [ ] Alerts and monitoring configured
- [ ] Backup configured (optional)

### Octopus Deploy Checklist
- [ ] Azure account added
- [ ] Azure Web App deployment targets created (Test, Beta, Live)
- [ ] Deployment process updated with Azure Web App steps
- [ ] IIS deployment steps removed/disabled
- [ ] Variables updated (AzureWebAppName, AzureResourceGroup)
- [ ] VM-specific variables removed
- [ ] Health check URLs updated
- [ ] Post-deployment scripts updated
- [ ] Test deployment successful

### Azure Pipelines Checklist
- [ ] Build pipeline builds successfully
- [ ] NuGet package contains all necessary files
- [ ] Package includes frontend build output (wwwroot)
- [ ] Package includes backend DLLs
- [ ] Post-deploy script updated (if needed)
- [ ] Pipeline deploys to Octopus successfully

### Application Code Checklist
- [ ] `UseForwardedHeaders` configured in Startup.cs
- [ ] `web.config` is App Service compatible
- [ ] Environment variables used for configuration
- [ ] Logging configured for Application Insights
- [ ] Health check endpoint exists (`/health`)
- [ ] Static files middleware configured

### Testing Checklist
- [ ] Application loads in browser
- [ ] User authentication works
- [ ] API endpoints respond correctly
- [ ] Frontend React app loads and functions
- [ ] Database queries execute successfully
- [ ] File uploads/downloads work (if applicable)
- [ ] Background jobs run (if applicable)
- [ ] Performance is acceptable
- [ ] SSL/HTTPS works correctly

---

## Part 10: Estimated Costs

### Current VM Costs (Approximate)
- **Standard D4s v3 VM**: ~$200-300/month (8 cores, 32 GB RAM)
- **Disk Storage**: ~$20-50/month
- **Bandwidth**: Variable
- **Total**: ~$250-400/month per environment

### App Service Costs (Approximate)

#### Option 1: Standard Tier (Test/Dev)
- **S1 Standard**: $70/month (1 core, 1.75 GB RAM)
- **S2 Standard**: $140/month (2 cores, 3.5 GB RAM)
- Suitable for: Test environment, low-traffic applications

#### Option 2: Premium Tier (Production)
- **P1v3**: $125/month (2 cores, 8 GB RAM)
- **P2v3**: $250/month (4 cores, 16 GB RAM)
- **P3v3**: $500/month (8 cores, 32 GB RAM)
- Suitable for: Production, high-traffic applications

**Additional Costs:**
- **Application Insights**: ~$2-5/GB of data ingested
- **Outbound Bandwidth**: Variable based on traffic
- **Custom Domain**: Free (DNS hosting separate)
- **SSL Certificate**: Free (App Service Managed Certificate)

**Estimated Total for AllVue:**
- **Test**: ~$70-100/month (S1 or S2)
- **Beta**: ~$125-200/month (P1v3)
- **Live**: ~$250-350/month (P2v3)
- **Total All Environments**: ~$450-650/month

**Potential Savings**: 10-30% compared to VMs, with added benefits of PaaS features.

---

## Part 11: Support and Resources

### Microsoft Documentation
- [App Service Overview](https://docs.microsoft.com/azure/app-service/overview)
- [Deploy ASP.NET Core to App Service](https://docs.microsoft.com/azure/app-service/quickstart-dotnetcore)
- [App Service Best Practices](https://docs.microsoft.com/azure/app-service/app-service-best-practices)

### Octopus Deploy Documentation
- [Azure Web App Deployment](https://octopus.com/docs/deployments/azure/deploying-a-package-to-an-azure-web-app)
- [Azure Integration](https://octopus.com/docs/infrastructure/deployment-targets/azure)

### Internal Resources
- Current AllVue documentation: `Vue Runbook.md`
- Technical overview: `doc/Technical Overview.md`
- Contact: DevOps team for Octopus/Azure support

---

## Summary

This migration moves AllVue from IIS on Azure VM to Azure App Service, providing:
1. **Reduced operational overhead** - no server management
2. **Better scalability** - built-in auto-scale
3. **Improved deployment** - deployment slots for zero-downtime releases
4. **Cost optimization** - pay for what you use, scale up/down as needed
5. **Enhanced monitoring** - Application Insights integration

The migration requires:
- **Azure Portal**: Create and configure App Service (30-60 minutes per environment)
- **Octopus Deploy**: Update deployment targets and process (2-4 hours)
- **Azure Pipelines**: Minor updates to package deployment (1-2 hours)
- **Application Code**: Minimal changes, mostly verification (1-2 hours)

**Total effort**: ~1-2 days for configuration, ~2-3 weeks for phased rollout with validation.

**Recommended approach**: Start with Test, validate thoroughly, then Beta, then Production with deployment slots.
