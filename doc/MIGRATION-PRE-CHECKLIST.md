# AllVue App Service Migration - Pre-Flight Checklist

> **CRITICAL**: Complete ALL items before deploying to App Service

## ⚠️ BLOCKING ISSUES - Must Fix Before Migration

### 1. ❌ Add ForwardedHeaders Middleware (REQUIRED)

**Problem**: BrandVue.FrontEnd is missing `UseForwardedHeaders()` which is **critical** for App Service.

**Impact Without This**:
- Authentication will fail (cookies/OAuth won't work)
- HTTPS redirects will loop/fail
- Client IP addresses will be incorrect
- Security issues with reverse proxy headers

**Fix Required**: 

**File**: `src/BrandVue.FrontEnd/Startup.cs`

Add this as the **FIRST** middleware in the `Configure` method:

```csharp
public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment hostEnvironment)
{
    // ADD THIS FIRST - Critical for App Service behind reverse proxy
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                          ForwardedHeaders.XForwardedProto | 
                          ForwardedHeaders.XForwardedHost
    });
    
    // Existing code continues...
    app.UseCors(CORS_POLICY_ALLOW_HOST_AND_TOOLS);
    // ...
}
```

Also add the using statement at the top if not present:
```csharp
using Microsoft.AspNetCore.HttpOverrides;
```

**Reference**: OpenEnds and UserManagement already have this implemented correctly.

---

### 2. ⚠️ Fix File Path Dependencies

**Problem**: Hardcoded Windows paths in `appsettings.json` won't work in App Service:

```json
"AllVueUploadFolder": "C:\\ProgramData\\AllVue",  // ❌ Won't exist
"baseDataPath": "..\\..\\..\\testdata\\{ProductName}\\data",  // ❌ Problematic
```

**Fix Required**:

**Option A - App Service Local Storage** (Simple, works for single instance):
```json
// Create: src/BrandVue.FrontEnd/appsettings.Production.json
{
  "AllVueUploadFolder": "D:\\home\\site\\wwwroot\\App_Data\\uploads",
  "baseDataPath": "D:\\home\\site\\wwwroot\\App_Data\\{ProductName}\\data",
  "baseMetadataPath": "D:\\home\\site\\wwwroot\\App_Data\\{ProductName}\\config"
}
```

**Option B - Azure Blob Storage** (Recommended for production/scale):
```json
{
  "AllVueUploadFolder": "",  // Empty - use Azure Storage
  "AzureStorage": {
    "ConnectionString": "",  // Injected by App Service config
    "ContainerName": "allvue-uploads"
  }
}
```

Then update code to check if `AllVueUploadFolder` is empty and use Azure Blob Storage instead.

**For Test Data**: Don't deploy testdata folder to App Service - it's for local dev only.

---

### 3. ❌ Update Connection Strings for Azure SQL

**Problem**: Current connection string uses local SQL Server with Windows auth:
```json
"AnswersConnectionString": "Server=.\\sql2022;...;Integrated Security=True;"
```

**Fix Required**:

In **Octopus Deploy variables**, ensure connection strings use:
- ✅ **SQL Authentication** (not Windows/Integrated)
- ✅ **Fully qualified server name** (e.g., `yourserver.database.windows.net`)
- ✅ **Explicit User ID and Password**

Example format for Octopus:
```
Server=savanta-sql-prod.database.windows.net;Database=VueExport;User Id=allvue_user;Password=#{SqlPassword};Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;
```

**Also Required**: Add App Service outbound IPs to SQL Server firewall (see #4 below).

---

### 4. ❌ Configure SQL Server Firewall

**Problem**: App Service has different outbound IPs than your VM - SQL connections will be blocked.

**Fix Required**:

**After creating App Service** (but before deploying):

1. **Get App Service Outbound IPs**:
   - Azure Portal → Your App Service → Properties
   - Copy all IPs from "Outbound IP addresses" and "Additional outbound IP addresses"

2. **Add to SQL Server Firewall**:
   - Azure Portal → SQL Server → Security → Networking
   - Click "Add client IP" for each outbound IP
   - Name them: `AppService-AllVue-Test-1`, `AppService-AllVue-Test-2`, etc.

**Alternative** (easier but less secure for production):
- Enable "Allow Azure services and resources to access this server"
- ⚠️ This allows ANY Azure service - use firewall rules for production

**Best Practice** (production):
- Use **VNet Integration** + **Private Endpoint** for SQL (no public IP needed)

---

## ⚠️ CRITICAL CONFIGURATION ITEMS

### 5. ✅ Verify .NET Runtime

**Status**: ✅ Confirmed - .NET 8.0

When creating App Service in Azure Portal:
- **Runtime stack**: `.NET 8 (LTS)`
- **Operating System**: `Windows`

---

### 6. ⚠️ Add Health Check Endpoint

**Current Status**: Unknown - needs verification

**Why Required**:
- Octopus Deploy health checks
- Azure App Service health monitoring
- Load balancer health probes

**Fix Required**:

**File**: `src/BrandVue.FrontEnd/Startup.cs`

In `ConfigureServices` method:
```csharp
services.AddHealthChecks()
    .AddDbContextCheck<AnswersDbContext>("database-answers")
    .AddDbContextCheck<MetaDbContext>("database-meta");
```

In `Configure` method (after `app.UseRouting()`):
```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/health");
    endpoints.MapControllers();
});
```

**Test locally**: Browse to `https://localhost:5001/health` - should return "Healthy"

---

### 7. ⚠️ Configure Application Insights

**Why Required**:
- Critical for monitoring App Service
- Troubleshooting deployment issues
- Performance monitoring

**Fix Required**:

1. **Update** `src/BrandVue.FrontEnd/appsettings.json`:
```json
{
  "ApplicationInsights": {
    "ConnectionString": ""
  },
  "Logging": {
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning"
      }
    }
  }
}
```

2. **Verify** in `Program.cs` or `Startup.cs`:
```csharp
// Should already be there, but verify:
builder.Services.AddApplicationInsightsTelemetry();
```

3. **In Azure Portal**: App Service will auto-inject the connection string when you enable App Insights

---

### 8. ⚠️ Data Protection Keys for Scale-Out

**Problem**: If you run multiple instances, users will get logged out randomly.

**Why**: Data protection keys (used for cookies, anti-forgery tokens) must be shared.

**Fix Required** (if scaling to >1 instance):

**File**: `src/BrandVue.FrontEnd/Startup.cs` in `ConfigureServices`:

**Option A - Use Database** (simplest):
```csharp
services.AddDataProtection()
    .SetApplicationName("AllVue")
    .PersistKeysToDbContext<MetaDbContext>();
```

**Option B - Use Azure Blob Storage**:
```csharp
services.AddDataProtection()
    .SetApplicationName("AllVue")
    .PersistKeysToAzureBlobStorage(
        new Uri(Configuration["DataProtection:BlobStorageUri"]),
        new DefaultAzureCredential());
```

**For Test Environment**: Can skip if only running 1 instance.

---

### 9. ⚠️ Review Authentication Configuration

**Check Required**:
- OAuth/OpenID Connect redirect URIs must include new App Service URLs
- Cookie domains must be updated for `.azurewebsites.net` or custom domain

**Fix Required**:

If using OAuth (check `authServerUrl` in appsettings.json):
1. In Auth Server (Auth0/Azure AD/IdentityServer), add redirect URIs:
   - `https://app-allvue-test.azurewebsites.net/signin-oidc`
   - `https://app-allvue-test.azurewebsites.net/signout-callback-oidc`
   - Repeat for Beta and Live

2. Update Octopus variables for each environment

---

### 10. ⚠️ Security Headers and CORS

**Check Current Settings**:
Your app uses `settings.security.json` for CSP headers.

**Fix Required**:

Ensure `settings.security.json` includes App Service domains:
```json
{
  "ContentSecurityPolicy": {
    "default-src": ["'self'", "https://*.azurewebsites.net"],
    // ... other directives
  }
}
```

Update CORS if external APIs call your service:
```csharp
// In Startup.cs
options.AddPolicy("AllowAzureServices", builder =>
{
    builder
        .WithOrigins("https://*.azurewebsites.net")
        .SetIsOriginAllowedToAllowWildcardSubdomains()
        .AllowAnyHeader()
        .AllowAnyMethod();
});
```

---

## 📋 PRE-DEPLOYMENT VERIFICATION

### Local Testing (Before Any Deployment)

Run these tests locally FIRST:

```powershell
# 1. Set production-like environment
$env:ASPNETCORE_ENVIRONMENT = "Production"

# 2. Test with production config
dotnet run --project src/BrandVue.FrontEnd/BrandVue.FrontEnd.Core.csproj

# 3. Verify:
# - App starts without errors
# - Health check works: https://localhost:5001/health
# - Static files load
# - Can authenticate
# - Database connections work
```

### Build Pipeline Verification

```powershell
# Test the full build pipeline locally:
cd src
dotnet restore BrandVue.sln
dotnet build BrandVue.sln --configuration Release
dotnet publish BrandVue.sln --configuration Release --no-build

# Verify publish output contains:
# - BrandVue.FrontEnd.Core.dll
# - wwwroot folder with compiled JS/CSS
# - web.config
# - appsettings.json and appsettings.Production.json
```

---

## 🚀 DEPLOYMENT ORDER (NON-NEGOTIABLE)

### Phase 1: Test Environment ✅ Lowest Risk
1. Create Test App Service in Azure Portal
2. Configure all settings (connection strings, app settings)
3. Add SQL firewall rules for Test App Service IPs
4. Update Octopus Deploy with Test target
5. Deploy to Test
6. **Run full regression tests**
7. Monitor for 3-5 days

**Acceptance Criteria**:
- [ ] App loads and navigates correctly
- [ ] Authentication works
- [ ] All API endpoints respond
- [ ] Database queries execute
- [ ] File uploads work (if applicable)
- [ ] No errors in Application Insights logs
- [ ] Performance is acceptable

### Phase 2: Beta Environment ⚠️ Medium Risk
Repeat Phase 1 for Beta environment.

**Acceptance Criteria**:
- [ ] All Test criteria pass
- [ ] Real users test for 1 week
- [ ] No critical issues reported
- [ ] Performance under real load is acceptable

### Phase 3: Production 🔴 High Risk
1. Create Production App Service with **deployment slot**
2. Deploy to **staging slot** first
3. Test staging slot thoroughly
4. **Swap staging to production** (zero downtime)
5. Monitor production closely for 24-48 hours
6. Keep VM running for 1 week as emergency fallback

**Acceptance Criteria**:
- [ ] All Beta criteria pass
- [ ] Load testing shows acceptable performance
- [ ] Swap completes successfully
- [ ] No increase in error rates after swap
- [ ] Response times within SLA

---

## 🔥 ROLLBACK PROCEDURES

### If Issues in Test/Beta:
1. Redeploy previous version via Octopus
2. Investigate and fix issues
3. Retry deployment

### If Issues in Production:
1. **Immediate** (< 5 minutes): Swap staging/production slots back
2. **Quick** (< 30 minutes): Redirect traffic back to VM (if still running)
3. **Standard** (< 1 hour): Redeploy previous version to App Service
4. Investigate in lower environments

---

## 📝 FINAL CHECKLIST

Before deploying to **any** environment, confirm:

### Code Changes:
- [ ] Added `UseForwardedHeaders()` to Startup.cs
- [ ] Added health check endpoint (`/health`)
- [ ] Created `appsettings.Production.json` with correct paths
- [ ] Configured Application Insights
- [ ] Configured Data Protection (if scaling)
- [ ] Updated CORS and security headers for App Service domains

### Azure Portal:
- [ ] App Service created with correct .NET runtime
- [ ] Always On enabled (for Beta/Live)
- [ ] Application settings configured
- [ ] Connection strings configured (SQL Auth, not Windows Auth)
- [ ] Application Insights enabled
- [ ] Custom domain configured (if applicable)
- [ ] SSL certificate bound (if using custom domain)
- [ ] Deployment slots created (for Production)

### Octopus Deploy:
- [ ] Azure account added
- [ ] Azure Web App deployment target created
- [ ] Deployment process updated (Azure Web App step added)
- [ ] Variables updated (AzureWebAppName, connection strings)
- [ ] IIS-specific steps removed/disabled
- [ ] PostDeploy script updated for App Service

### Infrastructure:
- [ ] SQL Server firewall rules added for App Service IPs
- [ ] VNet integration configured (if using private endpoints)
- [ ] OAuth redirect URIs updated in auth provider
- [ ] DNS records ready (if using custom domain)

### Testing:
- [ ] Tested locally with Production configuration
- [ ] Build pipeline completes successfully
- [ ] Package contains all necessary files
- [ ] Health check endpoint responds

### Documentation:
- [ ] Team briefed on migration plan
- [ ] Rollback procedure documented and tested
- [ ] Monitoring dashboards configured
- [ ] On-call rotation aware of migration

---

## 🆘 COMMON ISSUES & SOLUTIONS

### Issue: App shows "Application Error" after deployment
**Cause**: Missing dependencies, incorrect runtime, or startup errors

**Fix**:
1. Enable `stdoutLogEnabled` in web.config temporarily
2. Go to Kudu console: `https://{app-name}.scm.azurewebsites.net`
3. Navigate to Debug console → PowerShell
4. Check logs in `D:\home\LogFiles\stdout_{timestamp}.txt`
5. Check Application Insights logs for detailed errors

### Issue: Database connection fails
**Cause**: Firewall rules or incorrect connection string

**Fix**:
1. Verify App Service outbound IPs are in SQL firewall
2. Test connection from Kudu console:
   ```powershell
   Test-NetConnection -ComputerName your-server.database.windows.net -Port 1433
   ```
3. Verify connection string uses SQL auth, not Windows auth
4. Check connection string in App Service configuration (may be URL encoded)

### Issue: Static files (CSS/JS) return 404
**Cause**: Files not in wwwroot, or incorrect path configuration

**Fix**:
1. Verify webpack build completed in pipeline
2. Check package contains wwwroot folder
3. Verify `app.UseStaticFiles()` in Startup.cs
4. Check custom static file middleware paths

### Issue: Authentication redirect loops
**Cause**: Missing ForwardedHeaders, incorrect redirect URIs, or cookie domain issues

**Fix**:
1. **Verify** `UseForwardedHeaders()` is FIRST in middleware pipeline
2. Check OAuth redirect URIs include App Service URL
3. Verify cookie domain allows `.azurewebsites.net`
4. Check HTTPS redirect isn't conflicting

### Issue: Users logged out randomly
**Cause**: Multiple instances without shared data protection keys

**Fix**:
1. Implement shared data protection (see #8 above)
2. Or scale down to single instance temporarily

---

## 📞 SUPPORT CONTACTS

- **Azure Support**: Azure Portal → Help + support
- **Octopus Deploy**: [Your Octopus admin/support]
- **DevOps Team**: [Your internal team contact]
- **Database Team**: [For SQL firewall/connection issues]

---

## PRIORITY ORDER

Complete in this order:

1. **TODAY**: Add ForwardedHeaders middleware (blocking issue)
2. **TODAY**: Create appsettings.Production.json with correct paths
3. **TODAY**: Add health check endpoint
4. **BEFORE TEST DEPLOY**: Update Octopus connection strings for SQL auth
5. **BEFORE TEST DEPLOY**: Test locally with Production config
6. **DURING TEST DEPLOY**: Configure SQL firewall rules
7. **AFTER TEST VALIDATION**: Plan Beta deployment
8. **AFTER BETA SUCCESS**: Plan Production with staging slot

**Estimated Time to Production-Ready**:
- Code changes: 2-4 hours
- Testing: 1-2 days
- Test environment validation: 3-5 days
- Beta environment validation: 1 week
- **Total: 2-3 weeks for safe migration**

---

## ✅ SIGN-OFF

Before proceeding with migration, sign off on:

- [ ] All blocking issues resolved (ForwardedHeaders, paths, connection strings)
- [ ] Local testing completed successfully
- [ ] Team trained on new deployment process
- [ ] Rollback plan tested and documented
- [ ] Monitoring and alerting configured
- [ ] Stakeholders informed of migration schedule

**Lead Engineer**: _____________________ Date: _______

**DevOps Lead**: _____________________ Date: _______

**Manager Approval**: _____________________ Date: _______
