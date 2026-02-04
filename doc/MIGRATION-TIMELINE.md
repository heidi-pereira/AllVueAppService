# AllVue App Service Migration - Smooth Migration Timeline

> **Goal**: Zero-downtime migration from Azure VM to App Service in 3 weeks

## Week 1: Preparation & Code Changes

### Day 1-2: Code Changes (2-4 hours)
**Assigned to**: Development team

- [ ] Create feature branch: `feature/app-service-migration`
- [ ] Add `UseForwardedHeaders()` to Startup.cs (see CODE-CHANGES-REQUIRED.md)
- [ ] Create `appsettings.Production.json`
- [ ] Add health check endpoint
- [ ] Update project file to include new appsettings
- [ ] Create PostDeploy.AppService.ps1
- [ ] Commit and push changes

**Verification**:
```bash
dotnet build src/BrandVue.sln --configuration Release
# Should succeed with no errors
```

### Day 3: Local Testing (4 hours)
**Assigned to**: Development team

- [ ] Test with Development config (existing)
- [ ] Test with Production config locally:
  ```powershell
  $env:ASPNETCORE_ENVIRONMENT = "Production"
  dotnet run --project src/BrandVue.FrontEnd/BrandVue.FrontEnd.Core.csproj
  ```
- [ ] Verify health endpoint: `https://localhost:5001/health`
- [ ] Test authentication, database access, file operations
- [ ] Fix any issues found
- [ ] Update feature branch

**Acceptance**: App runs successfully with Production configuration locally

### Day 4: Azure Infrastructure Setup (2-3 hours)
**Assigned to**: DevOps/Infrastructure team

**Test Environment Setup**:
- [ ] Create Test App Service in Azure Portal
  - Name: `app-allvue-test`
  - Runtime: .NET 8 on Windows
  - Plan: S2 Standard (2 cores, 3.5 GB RAM)
  - Region: UK South
- [ ] Enable Application Insights
- [ ] Configure App Settings in Azure Portal:
  ```
  ASPNETCORE_ENVIRONMENT = Test
  WEBSITE_TIME_ZONE = GMT Standard Time
  WEBSITE_RUN_FROM_PACKAGE = 1
  ```
- [ ] Configure Connection Strings (get from existing Octopus variables)
- [ ] Enable "Always On" = OFF (for Test to save costs)

**Note Outbound IPs**: Copy from App Service → Properties → "Outbound IP addresses"

### Day 5: Database & Network Configuration (1-2 hours)
**Assigned to**: Database/Infrastructure team

- [ ] Add App Service outbound IPs to SQL Server firewall
  - Azure Portal → SQL Server → Networking → Add firewall rules
  - Name rules: `AppService-AllVue-Test-1`, etc.
- [ ] Test connectivity from App Service:
  - Go to App Service → Advanced Tools (Kudu)
  - Debug Console → PowerShell
  - Run: `Test-NetConnection -ComputerName your-server.database.windows.net -Port 1433`
  - Should show "TcpTestSucceeded: True"

---

## Week 2: Test Environment Deployment

### Day 6: Octopus Configuration (2-3 hours)
**Assigned to**: DevOps team

- [ ] Add Azure account in Octopus Deploy
- [ ] Create Azure Web App deployment target for Test
- [ ] Update "BrandVue AllVue" project:
  - Add new step: "Deploy Azure Web App"
  - Configure for Test environment only
  - Keep existing IIS steps for Beta/Live (parallel deployment)
- [ ] Add Octopus variables:
  - `AzureWebAppName` (Test scope): `app-allvue-test`
  - Update connection strings to use SQL auth (not Windows auth)
- [ ] Add PostDeploy.AppService.ps1 script step

**Testing**: Create a test release but don't deploy yet

### Day 7: First Test Deployment (Morning)
**Assigned to**: DevOps + Development team

- [ ] Merge feature branch to main (or create release branch)
- [ ] Trigger Azure Pipeline build
- [ ] Verify build produces package successfully
- [ ] Create Octopus release
- [ ] Deploy to Test App Service
- [ ] Monitor deployment logs carefully

**If deployment fails**: 
- Check Octopus logs
- Check App Service logs in Kudu
- Check Application Insights
- Fix issues and redeploy

**Expected Duration**: 10-15 minutes for deployment

### Day 7-8: Initial Smoke Testing (Afternoon)
**Assigned to**: QA team + Development team

**Smoke Test Checklist**:
- [ ] App loads in browser: `https://app-allvue-test.azurewebsites.net`
- [ ] Login works (authentication)
- [ ] Dashboard loads
- [ ] Can create/view reports
- [ ] Database queries work
- [ ] File upload/download works (if applicable)
- [ ] API endpoints respond correctly
- [ ] No JavaScript errors in console
- [ ] No errors in Application Insights

**If issues found**: Fix, redeploy, retest

### Day 9-12: Full Regression Testing (4 days)
**Assigned to**: QA team

- [ ] Run complete regression test suite
- [ ] Test all major user workflows
- [ ] Performance testing (response times, load times)
- [ ] Security testing (authentication, authorization)
- [ ] Test error handling
- [ ] Test edge cases

**Daily standup**: Review issues, prioritize fixes

### Day 13: Test Environment Sign-Off
**Assigned to**: Tech Lead + QA Lead

**Acceptance Criteria**:
- [ ] All critical bugs fixed
- [ ] No high-priority bugs
- [ ] Performance is acceptable (comparable to VM)
- [ ] No increase in error rates vs. VM
- [ ] Application Insights shows healthy metrics

**Decision**: GO/NO-GO for Beta deployment

---

## Week 3: Beta & Production Deployment

### Day 14-15: Beta Environment Setup (4 hours)
**Assigned to**: DevOps/Infrastructure team

**Repeat Day 4-5 process for Beta**:
- [ ] Create Beta App Service (P1v3 Premium - 2 cores, 8 GB RAM)
- [ ] Enable Always On = ON
- [ ] Configure App Settings and Connection Strings
- [ ] Add SQL firewall rules for Beta IPs
- [ ] Update Octopus with Beta deployment target
- [ ] Add Beta variables to Octopus

### Day 16: Beta Deployment (Morning)
**Assigned to**: DevOps team

- [ ] Deploy to Beta App Service via Octopus
- [ ] Monitor deployment
- [ ] Run smoke tests
- [ ] Notify users Beta is on App Service

### Day 16-19: Beta User Acceptance (3-4 days)
**Assigned to**: Business users + Support team

- [ ] Real users test Beta environment
- [ ] Collect feedback
- [ ] Monitor Application Insights for issues
- [ ] Fix any issues found
- [ ] **Key**: Get user sign-off that Beta is stable

**Success Criteria**:
- [ ] No critical issues reported by users
- [ ] Performance is acceptable to users
- [ ] Application Insights shows stable metrics
- [ ] Users approve for Production rollout

### Day 20: Production Planning (2 hours)
**Assigned to**: Tech Lead + DevOps + Management

**Go/No-Go Meeting**:
- Review Test and Beta results
- Review any open issues
- Confirm rollback plan
- Set production deployment window
- Brief support team
- Notify stakeholders

**Recommended Deployment Window**: 
- **Tuesday 10 AM** (avoid Monday, avoid end of week)
- Have full team available
- Low-traffic time if possible

### Day 21: Production Deployment (4-6 hours)

#### Phase 1: Setup (Morning - 2 hours)
**Assigned to**: DevOps/Infrastructure team

- [ ] Create Production App Service with **deployment slot**
  - Name: `app-allvue-live`
  - Runtime: .NET 8 on Windows
  - Plan: P2v3 Premium (4 cores, 16 GB RAM)
  - Region: UK South
  - **Create staging slot**: `app-allvue-live/staging`
- [ ] Configure Production settings in **both slots** (production and staging)
- [ ] Enable Always On = ON
- [ ] Add SQL firewall rules for Production IPs
- [ ] Update Octopus with Production deployment target (point to **staging slot**)

#### Phase 2: Deploy to Staging Slot (10 AM - 11 AM)
**Assigned to**: DevOps team

- [ ] Deploy to **staging slot** via Octopus
- [ ] Monitor deployment logs
- [ ] Run automated smoke tests against staging slot
  - URL: `https://app-allvue-live-staging.azurewebsites.net`

#### Phase 3: Staging Validation (11 AM - 12 PM)
**Assigned to**: QA team + Tech Lead

- [ ] Full smoke test on staging slot
- [ ] Verify health check: `https://app-allvue-live-staging.azurewebsites.net/health`
- [ ] Test authentication
- [ ] Test key workflows
- [ ] Check Application Insights
- [ ] Load test if possible

**Go/No-Go Decision**: Proceed with swap?

#### Phase 4: Production Swap (12 PM - 12:15 PM)
**Assigned to**: DevOps team

- [ ] **Final check**: Verify VM is still running (fallback)
- [ ] Azure Portal → App Service → Deployment slots
- [ ] Click "Swap"
- [ ] Source: staging, Target: production
- [ ] Review settings that will be swapped
- [ ] Click "Swap" and confirm
- [ ] **Swap completes in 2-5 minutes** ✨

**What happens**:
- Traffic immediately redirects to new App Service
- No downtime (Azure manages transition)
- Old production slot becomes staging (instant rollback available)

#### Phase 5: Production Monitoring (12:15 PM - 4 PM)
**Assigned to**: Full team on-call

**First 15 minutes (critical)**:
- [ ] Browse to production URL
- [ ] Test authentication
- [ ] Check Application Insights live metrics
- [ ] Watch for error rate spikes
- [ ] Monitor response times

**First Hour**:
- [ ] Monitor Application Insights
- [ ] Check for any user-reported issues
- [ ] Review error logs
- [ ] Monitor database performance
- [ ] Check file upload/download

**First 4 Hours**:
- [ ] Continue monitoring
- [ ] Support team ready for user issues
- [ ] Tech team on-call for rollback if needed

**Success Criteria**:
- [ ] No increase in error rates
- [ ] Response times similar to VM
- [ ] No critical user issues
- [ ] Application Insights shows healthy metrics

#### Phase 6: Rollback Plan (If Needed)
**Time to execute**: < 5 minutes

**If critical issues occur**:
1. **Immediate** (< 2 minutes):
   - Azure Portal → App Service → Deployment slots
   - Click "Swap" again (swap back staging and production)
   - Confirms rollback
   
2. **Alternative** (if swap fails):
   - Update load balancer/DNS to point back to VM
   - Takes 5-15 minutes for DNS propagation

**Then**: Investigate issues in staging slot

---

## Week 4: Post-Migration

### Day 22-28: Monitoring & Stabilization

**Daily Tasks**:
- [ ] Review Application Insights dashboards
- [ ] Check for any degraded performance
- [ ] Monitor error rates
- [ ] Review user feedback
- [ ] Fix any minor issues

**Keep VM Running**: Don't decommission yet!

### Day 29: One-Week Review
**Assigned to**: Full team

**Review Meeting**:
- Review Application Insights data (7 days)
- Compare metrics: App Service vs. VM
- Review any incidents or issues
- Collect team feedback
- Document lessons learned

**Decision**: 
- If stable → Plan VM decommissioning for Week 6
- If issues → Extend monitoring period

---

## Week 6: VM Decommissioning (If Stable)

### Final Steps:
- [ ] Stop VM (don't delete yet)
- [ ] Monitor for 1 week
- [ ] Take final VM snapshot/backup
- [ ] Document VM configuration for reference
- [ ] Delete VM and associated resources
- [ ] Update documentation
- [ ] Remove VM-specific Octopus configuration
- [ ] Archive VM-related scripts

---

## Risk Mitigation Strategies

### Strategy 1: Parallel Running
**Timeline**: Week 2-4
- Run both VM and App Service Test/Beta simultaneously
- Compare metrics between the two
- Build confidence before Production

### Strategy 2: Deployment Slots
**Production only**
- Zero-downtime deployment
- Instant rollback capability
- Test in production-like environment before swap

### Strategy 3: Gradual Traffic Shift (Optional)
If very risk-averse, consider:
- Use Azure Front Door or Traffic Manager
- Route 10% traffic to App Service, 90% to VM
- Gradually increase App Service traffic over days
- **Note**: Adds complexity, only needed for very high-risk scenarios

### Strategy 4: Feature Flags
Consider adding feature flags for:
- New App Service-specific features
- Ability to disable problematic features quickly
- Rollback without full deployment

---

## Communication Plan

### Week 1: Internal Communication
- **Day 1**: Kickoff meeting with full team
- **Daily**: Standup updates during development
- **End of week**: Demo to stakeholders

### Week 2: Test Deployment Communication
- **Before Test Deploy**: Email to tech team
- **After Test Deploy**: Status update to stakeholders
- **Daily**: Bug triage and progress updates

### Week 3: Production Communication
- **Day 14**: Email to all users - "Beta on new platform"
- **Day 19**: Email to all users - "Production migration scheduled for Day 21"
- **Day 20**: Reminder email with maintenance window
- **Day 21 (9 AM)**: "Maintenance starting in 1 hour" notification
- **Day 21 (12:15 PM)**: "Migration complete" notification
- **Day 22-28**: Daily status updates if issues

### Week 4+: Post-Migration
- **Weekly**: Status reports to management
- **End of Week 4**: Migration success summary

---

## Success Metrics

### Technical Metrics:
- **Uptime**: > 99.9% (target)
- **Response Time**: < 500ms p95 (same as VM or better)
- **Error Rate**: < 0.1% (same as VM)
- **Deployment Time**: < 15 minutes (down from 30+ on VM)

### Business Metrics:
- **User Complaints**: < 5 during migration period
- **Support Tickets**: No increase vs. baseline
- **User Satisfaction**: Maintain or improve

### Cost Metrics:
- **Infrastructure Cost**: Track App Service vs. VM costs
- **Ops Cost**: Reduced maintenance overhead
- **Total**: Aim for cost-neutral or savings within 3 months

---

## Lessons Learned Template

After migration, document:

### What Went Well:
- [To be filled after migration]

### What Could Be Improved:
- [To be filled after migration]

### Unexpected Issues:
- [To be filled after migration]

### Time vs. Estimate:
- [To be filled after migration]

### Recommendations for Future Migrations:
- [To be filled after migration]

---

## Emergency Contacts

During migration period, ensure these contacts are available:

- **Tech Lead**: [Name, Phone, Email]
- **DevOps Lead**: [Name, Phone, Email]
- **Database Admin**: [Name, Phone, Email]
- **Azure Support**: [Azure support case number]
- **On-Call Rotation**: [Schedule/Contact list]

---

## Final Checklist Before Production Deployment

**24 Hours Before**:
- [ ] All code changes merged and tested
- [ ] Test and Beta environments stable
- [ ] Production App Service created and configured
- [ ] SQL firewall rules configured
- [ ] Octopus configuration updated
- [ ] Rollback plan tested (in Test/Beta)
- [ ] Team briefed and available
- [ ] Users notified of maintenance window
- [ ] Support team briefed
- [ ] Monitoring dashboards ready

**1 Hour Before**:
- [ ] All team members online and ready
- [ ] VM still running (fallback)
- [ ] Recent database backup confirmed
- [ ] Application Insights dashboard open
- [ ] Octopus ready to deploy
- [ ] Communication channels open

**GO Decision**:
- [ ] No critical issues in Test/Beta
- [ ] All stakeholders approve
- [ ] Team ready
- [ ] Weather: No other major releases/changes today

---

## Estimated Effort

| Phase | Duration | Team Members | Effort (person-hours) |
|-------|----------|--------------|----------------------|
| Code Changes | 2 days | 1 dev | 8 hours |
| Local Testing | 1 day | 1 dev | 4 hours |
| Azure Setup (Test) | 1 day | 1 DevOps | 3 hours |
| Octopus Config | 1 day | 1 DevOps | 3 hours |
| Test Deployment | 2 days | 2 devs, 2 QA | 32 hours |
| Regression Testing | 4 days | 2 QA | 64 hours |
| Beta Setup & Deploy | 2 days | 1 DevOps, 1 QA | 16 hours |
| Beta UAT | 4 days | 3 users, 1 support | 32 hours |
| Production Setup & Deploy | 1 day | Full team | 24 hours |
| Monitoring (Week 1) | 5 days | 1 dev on-call | 10 hours |
| **Total** | **21 days** | **~8 people** | **~196 hours** |

**Cost**: ~$20,000-30,000 in team time (assuming $100-150/hour blended rate)

**ROI**: Reduced ongoing maintenance costs, faster deployments, better scalability

---

## Summary: Keys to Smooth Migration

1. ✅ **Make required code changes early** (Week 1)
2. ✅ **Test thoroughly in Test environment** (Week 2)
3. ✅ **Get real user feedback in Beta** (Week 3)
4. ✅ **Use deployment slots for Production** (zero downtime)
5. ✅ **Keep VM running as fallback** (1-2 weeks post-migration)
6. ✅ **Monitor closely post-deployment** (Application Insights)
7. ✅ **Communicate proactively** (users and stakeholders)
8. ✅ **Have clear rollback plan** (test it!)

**Follow this timeline, and your migration will be smooth, low-risk, and successful!** 🚀
