using AuthServer.GeneratedAuthApi;
using BrandVue.EntityFramework;
using CustomerPortal.Infrastructure;
using CustomerPortal.Models;
using CustomerPortal.Shared.Egnyte;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vue.Common.Auth;
using Vue.Common.Auth.Permissions;
using Vue.Common.AuthApi;

namespace CustomerPortal.Services
{
    public class SurveyService : ServiceBase, ISurveyService, IDocumentUrlProvider
    {
        private readonly SurveyDbContext _surveyDbContext;
        private readonly IRequestContext _requestContext;
        private readonly IAuthApiClientCustomerPortal _authApiClient;
        private readonly IVueContextService _vueContextService;
        private readonly IUserContext _userContext;
        private readonly ISecurityGroupService _securityGroupService;
        private readonly IAllVueProductConfigurationService _allVueProductConfigurationService;
        private readonly IPermissionService _permissionService;
        private bool CanLoadProjectsForOtherCompanies => _userContext.IsInSavantaRequestScope;
        private bool ShouldFilterToUserProjects => !_userContext.IsAuthorizedSavantaUser;
        private ILookup<string, string> EmptyUserProjectsLookup => Enumerable.Empty<string>().ToLookup(x => x);

        public SurveyService(SurveyDbContext surveyDbContext, IRequestContext requestContext, IAuthApiClientCustomerPortal authApiClient,
            IVueContextService vueContextService, IUserContext userContext, ISecurityGroupService securityGroupService,
            IAllVueProductConfigurationService allVueProductConfigurationService,
            IPermissionService permissionsService)
        {
            _surveyDbContext = surveyDbContext;
            _requestContext = requestContext;
            _authApiClient = authApiClient;
            _vueContextService = vueContextService;
            _userContext = userContext;
            _securityGroupService = securityGroupService;
            _allVueProductConfigurationService = allVueProductConfigurationService;
            _permissionService = permissionsService;
        }

        public async Task<CompanyModel> GetCompanyForSurvey(Survey survey)
        {
            var requestAuthCompany = await _requestContext.GetAuthCompany();
            if (requestAuthCompany != null)
            {
                if (survey.AuthCompanyId == requestAuthCompany.Id)
                {
                    return requestAuthCompany;
                }

                var isSharedOwner = await _surveyDbContext.SurveySharedOwners
                    .AsNoTracking()
                    .AnyAsync(so => so.SurveyId == survey.Id && so.AuthCompanyId == requestAuthCompany.Id);

                if (isSharedOwner)
                {
                    return requestAuthCompany;
                }
            }
            return await _authApiClient.GetCompanyById(survey.AuthCompanyId, System.Threading.CancellationToken.None);
        }

        public async Task<Project[]> ProjectList()
        {
            var surveys = await SurveyList();
            var surveyGroups = await SurveyGroups();
            var vueContext = _vueContextService.GetVueContext();


            var projectIdToUsersLookup = await GetProjectIdToUsersLookup();
            var subProductIds = surveys.Select(s => s.SubProductId)
                .Concat(surveyGroups.Select(g => g.SubProductId))
                .ToArray();
            var sharedProjectIds = await GetSharedProjectIds(subProductIds);
            var authCompany = await _requestContext.GetAuthCompany();
            var summaryPermission = await _permissionService.GetSummaryPermissionProjectAccessAsync([authCompany.Id], System.Threading.CancellationToken.None);

            var projects = surveys.Select(s => GetProjectFromSurvey(s, vueContext, summaryPermission, projectIdToUsersLookup, sharedProjectIds.Contains(s.SubProductId)))
                .Concat(surveyGroups.Select(g => GetProjectFromSurveyGroup(g, vueContext, summaryPermission, projectIdToUsersLookup, sharedProjectIds.Contains(g.SubProductId), authCompany.Id)));
            var accessibleProjects = await FilterToUserAccessibleProjects(projects, summaryPermission);
            return accessibleProjects.DistinctBy(p => p.SubProductId).ToArray();
        }

        private async Task<Survey[]> SurveyList()
        {
            var authCompany = await _requestContext.GetAuthCompany();
            var companyId = authCompany.Id;

            var surveys = await _surveyDbContext.Surveys
                .AsNoTracking()
                .Where(s =>
                    s.AuthCompanyId == companyId ||
                    _surveyDbContext.SurveySharedOwners.Any(so => so.SurveyId == s.Id && so.AuthCompanyId == companyId)
                )
                .ToArrayAsync();
            return surveys;
        }

        private async Task<SurveyGroupInfo[]> SurveyGroups()
        {
            var authCompany = await _requestContext.GetAuthCompany();
            var companyId = authCompany.Id;
            var surveyGroups = _surveyDbContext.SurveyGroups.AsNoTracking()
                .Include(g => g.Surveys).ThenInclude(s => s.Survey)
                .Where(g => g.Type == SurveyGroup.SurveyGroupType.AllVue)
                .Where(g => g.Surveys.Any(sgs =>
                    sgs.Survey.AuthCompanyId == companyId ||
                    _surveyDbContext.SurveySharedOwners.Any(so => so.SurveyId == sgs.SurveyId && so.AuthCompanyId == companyId)
                ))
                .ToArray();
            var groupInfo = surveyGroups.Select(g => GetInfoFromSurveyGroup(g)).ToArray();

            return groupInfo;
        }

        public async Task<Survey> Survey(int surveyId)
        {
            var authCompany = await _requestContext.GetAuthCompany();
            var companyId = authCompany.Id;
            var survey = _surveyDbContext.Surveys
                .Where(s => CanLoadProjectsForOtherCompanies 
                            || s.AuthCompanyId == companyId
                            || _surveyDbContext.SurveySharedOwners.Any(so => so.SurveyId == s.Id && so.AuthCompanyId == companyId)
                            )
                .SingleOrDefault(s => s.Id == surveyId);

            if (survey == null)
            {
                throw new ProjectNotFound($"Survey {surveyId} not found for '{_requestContext.PortalGroup}'.");
            }
            await ValidateAccess(survey.AuthCompanyId, surveyId.ToString());
            return survey;
        }

        public async Task<Project> Project(string subProductId)
        {
            var authCompany = await _requestContext.GetAuthCompany();
            var companyId = authCompany.Id;
            var vueContext = _vueContextService.GetVueContext();
            var surveyGroup = _surveyDbContext.SurveyGroups.AsNoTracking()
                .Include(g => g.Surveys).ThenInclude(s => s.Survey)
                .Where(g => g.Type == SurveyGroup.SurveyGroupType.AllVue)
                .Where(g => CanLoadProjectsForOtherCompanies 
                            || g.Surveys.All(
                                s => s.Survey.AuthCompanyId == companyId
                                     || _surveyDbContext.SurveySharedOwners.Any(so => so.SurveyId == s.SurveyId && so.AuthCompanyId == companyId))

                )
                .SingleOrDefault(g => g.UrlSafeName == subProductId);

            if (surveyGroup != null)
            {
                await ValidateAccess(surveyGroup.Surveys.First().Survey.AuthCompanyId, subProductId);
                return GetProjectFromSurveyGroup(GetInfoFromSurveyGroup(surveyGroup), vueContext, [], EmptyUserProjectsLookup, false, authCompany.Id);
            }

            if (int.TryParse(subProductId, out var surveyId))
            {
                var survey = _surveyDbContext.Surveys
                    .Where(s => CanLoadProjectsForOtherCompanies 
                                || s.AuthCompanyId == companyId
                                || _surveyDbContext.SurveySharedOwners.Any(so => so.SurveyId == s.Id && so.AuthCompanyId == companyId)
                                )
                    .SingleOrDefault(s => s.Id == surveyId);

                if (survey != null)
                {
                    await ValidateAccess(survey.AuthCompanyId, subProductId);
                    return GetProjectFromSurvey(survey, vueContext, [], EmptyUserProjectsLookup, false);
                }
            }

            throw new ProjectNotFound($"Project {subProductId} not found for '{_requestContext.PortalGroup}'.");
        }

        public Survey SurveyForEgnytePathUnrestricted(int surveyId)
        {
            //document downloads are on a separate domain
            //so we don't have access to user claims and can't check user project access

            var survey = _surveyDbContext.Surveys.SingleOrDefault(s => s.Id == surveyId);

            if (survey == null)
            {
                throw new ProjectNotFound($"Survey {surveyId} not found.");
            }
            return survey;
        }

        public Survey SurveyForEgnytePathUnrestricted(Guid downloadGuid, bool isSecureDownload)
        {
            //document downloads are on a separate domain
            //so we don't have access to user claims and can't check user project access

            var survey = _surveyDbContext.Surveys.SingleOrDefault(s => s.FileDownloadGuid == downloadGuid);

            if (survey == null)
            {
                throw new ProjectNotFound($"Survey {downloadGuid} not found.");
            }
            
            if (!isSecureDownload && _allVueProductConfigurationService.GetConfiguration(survey.SubProductId)
                .AllVueDocumentationConfiguration.EnableSecureFileDownload)
            {
                throw new ProjectNotFound($"Survey {downloadGuid} not found.");
            }
            return survey;
        }

        public async Task<SurveyDetails> SurveyDetails(int surveyId)
        {
            var authCompany = await _requestContext.GetAuthCompany();
            var companyId = authCompany.Id;
            var survey = _surveyDbContext.Surveys
                .Where(s => CanLoadProjectsForOtherCompanies
                            || s.AuthCompanyId == companyId
                            || _surveyDbContext.SurveySharedOwners.Any(so => so.SurveyId == s.Id && so.AuthCompanyId == companyId)
                            )
                .Include(s => s.Quota)
                .ThenInclude(q => q.QuotaCells)
                .SingleOrDefault(s => s.Id == surveyId);

            if (survey == null)
            {
                throw new ProjectNotFound($"Survey {surveyId} not found for '{_requestContext.PortalGroup}'.");
            }

            await ValidateAccess(survey.AuthCompanyId, surveyId.ToString());

            survey.Quota = survey.Quota
                .OrderBy(q => q.Order)
                .ThenBy(q => q.Name)
                .ToList();

            var company = await GetCompanyForSurvey(survey);
            return new SurveyDetails(survey, company?.ShortCode);
        }

        public async Task<SurveyGroupDetails> SurveyGroupDetails(string subProductId)
        {
            var authCompany = await _requestContext.GetAuthCompany();
            var companyId = authCompany.Id;
            var surveyGroup = _surveyDbContext.SurveyGroups.AsNoTracking()
                .Include(g => g.Surveys).ThenInclude(s => s.Survey)
                .Where(g => g.Type == SurveyGroup.SurveyGroupType.AllVue)
                .Where(g => CanLoadProjectsForOtherCompanies
                            || g.Surveys.All(sgs =>
                                sgs.Survey.AuthCompanyId == companyId ||
                                _surveyDbContext.SurveySharedOwners.Any(so => so.SurveyId == sgs.SurveyId && so.AuthCompanyId == companyId)
                            ))
                .SingleOrDefault(g => g.UrlSafeName == subProductId);

            if (surveyGroup == null)
            {
                throw new ProjectNotFound($"SurveyGroup {subProductId} not found for '{_requestContext.PortalGroup}'.");
            }

            var firstSurvey = surveyGroup.Surveys.First();
            await ValidateAccess(firstSurvey.Survey.AuthCompanyId, subProductId);
            var company = await GetCompanyForSurvey(firstSurvey.Survey);

            var vueContext = _vueContextService.GetVueContext();
            return new SurveyGroupDetails()
            {
                SurveyGroupId = surveyGroup.SurveyGroupId,
                Name = surveyGroup.Name,
                Type = surveyGroup.Type,
                UrlSafeName = surveyGroup.UrlSafeName,
                ChildSurveys = surveyGroup.Surveys?.Select(s => GetProjectFromSurvey(s.Survey, vueContext, [], EmptyUserProjectsLookup, false)) ?? Enumerable.Empty<Project>(),
                OrganisationShortCode = company?.ShortCode
            };
        }

        private async Task ValidateAccess(string authCompanyId, string subProductId)
        {
            await ValidateSecurityGroupAccess(authCompanyId, subProductId);
            await ValidateUserProjectAccess(subProductId);
        }

        private async Task ValidateSecurityGroupAccess(string authCompanyId, string subProductId)
        {
            var hasSecurityGroupAccess = await _securityGroupService.UserHasSecurityGroupAccessFor(authCompanyId);
            if (!hasSecurityGroupAccess)
            {
                throw new ProjectNotFound($"Project {subProductId} not found for '{_requestContext.PortalGroup}'.");
            }
        }

        private async Task ValidateUserProjectAccess(string subProductId)
        {
            var hasProjectAccess = !ShouldFilterToUserProjects || await HasPermissionsToAccessProject(subProductId);
            if (!hasProjectAccess)
            {
                throw new NoProjectAccess($"User does not have permission to access project {subProductId}.");
            }
        }

        private async Task<bool> HasPermissionsToAccessProject(string subProductId)
        {
            var authCompany = await _requestContext.GetAuthCompany();
            var hasPermissions = (await _permissionService.GetUserDataPermissionForCompanyAndProjectAsync(authCompany.Id, SavantaConstants.AllVueShortCode, subProductId, _userContext.UserId)) != null;

            return hasPermissions || await _authApiClient.CanUserAccessProject(string.Empty, _userContext.UserId, subProductId, System.Threading.CancellationToken.None); ;
        }

        private async Task<IEnumerable<Project>> FilterToUserAccessibleProjects(IEnumerable<Project> projects, IEnumerable<SummaryProjectAccess> summaryPermission)
        {
            if (ShouldFilterToUserProjects)
            {
                var userProjectIds = (await _authApiClient.GetProjectsForUser(_userContext.UserId, System.Threading.CancellationToken.None))
                    .Select(p => p.ProjectId)
                    .ToHashSet();

                var filteredProjectsByAuth = projects.Where(p => p.IsSharedWithAllUsers || userProjectIds.Contains(p.SubProductId)).ToList();

                var projectsUserHasAccessTo =
                    summaryPermission.Where(x => x.IsShared || x.SharedUserIds.Contains(_userContext.UserId)).Select(x => x.ToName());

                var filteredProjectsByPermissions = projects.Where(p => projectsUserHasAccessTo.Contains($"{p.ProjectType}/{p.Id}")).ToList();

                return filteredProjectsByPermissions.Concat(filteredProjectsByAuth);
            }
            return projects;
        }

        private async Task<ILookup<string,string>> GetProjectIdToUsersLookup()
        {
            var authCompany = await _requestContext.GetAuthCompany();
            var allUserProjectsForCurrentCompany = (await _authApiClient.GetAllUserDetailsForCompanyScopeAsync(string.Empty, _userContext.UserId, authCompany.Id, false, System.Threading.CancellationToken.None))
                .Where(user => user.OrganisationId == authCompany.Id)
                .SelectMany(user => user.Projects);
            return allUserProjectsForCurrentCompany.ToLookup(userProject => userProject.ProjectId, userProject => userProject.ApplicationUserId);
        }

        private async Task<HashSet<string>> GetSharedProjectIds(IEnumerable<string> projectIds)
        {
            var sharedProjectIds = await _authApiClient.GetSharedProjects(projectIds, System.Threading.CancellationToken.None);
            return sharedProjectIds.ToHashSet();
        }

        private SurveyGroupInfo GetInfoFromSurveyGroup(SurveyGroup surveyGroup)
        {
            var latestSurvey = surveyGroup.Surveys.LastOrDefault();
            return new SurveyGroupInfo
            {
                Id = surveyGroup.SurveyGroupId,
                Name = surveyGroup.Name,
                UrlSafeName = surveyGroup.UrlSafeName,
                isOpen = surveyGroup.Surveys.Any(s => s.Survey.IsOpen),
                Complete = surveyGroup.Surveys.Sum(s => s.Survey.Complete),
                LaunchDate = latestSurvey?.Survey.LaunchDate,
                ChildSurveysIds = surveyGroup.Surveys.Select(x => x.SurveyId).ToArray(),
            };
        }

        private Project GetProjectFromSurvey(Survey survey, VueContext vueContext, IEnumerable<SummaryProjectAccess> summaryPermissions, ILookup<string, string> projectIdToUsersLookup, bool isSharedWithAllUsers)
        {
            var contextModel = vueContext.GetContextModel(survey.SubProductId);
            var summaryPermission = summaryPermissions.FirstOrDefault(x => x.ProjectType == (int)BrandVue.EntityFramework.MetaData.ProjectType.AllVueSurvey && x.ProjectId == survey.Id);
            return new Project
            {
                Id = survey.Id,
                SubProductId = survey.SubProductId,
                Name = survey.Name ?? "",
                UniqueSurveyId = survey.UniqueSurveyId,
                IsOpen = survey.IsOpen,
                IsPaused = survey.IsPaused,
                IsClosed = survey.isClosed,
                Complete = survey.Complete,
                Target = survey.Target,
                PercentComplete = survey.PercentComplete,
                LaunchDate = survey.LaunchDate,
                CompleteDate = survey.CompleteDate,
                ProjectType = ProjectType.Survey,
                DataPageUrl = contextModel.VueDataPageUrl,
                ReportsPageUrl = contextModel.VueReportsPageUrl,
                NumberOfUsers = (summaryPermission?.SharedUserIds.Count() ?? 0) + projectIdToUsersLookup[survey.SubProductId].Distinct().Count(),
                IsSharedWithAllUsers = summaryPermission?.IsShared ?? false || isSharedWithAllUsers,
                IsDocumentsTabAvailable = contextModel.VueDocumentsEnabled,
                IsQuotaTabAvailable = contextModel.VueQuotaEnabled,
                CustomIntegrations = contextModel.CustomUiWidgets,
                IsHelpIconAvailable = contextModel.VueHelpIconEnabled,
                AllVueDocumentationConfiguration = contextModel.AllVueDocumentationConfiguration,
                CompanyAuthId = survey.AuthCompanyId,
            };
        }

        private Project GetProjectFromSurveyGroup(SurveyGroupInfo surveyGroup, VueContext vueContext, IEnumerable<SummaryProjectAccess> summaryPermissions, ILookup<string, string> projectIdToUsersLookup, bool isSharedWithAllUsers, string authCompanyId)
        {
            var summaryPermission = summaryPermissions.FirstOrDefault(x => x.ProjectType == (int)BrandVue.EntityFramework.MetaData.ProjectType.AllVueSurveyGroup && x.ProjectId == surveyGroup.Id);
            var contextModel = vueContext.GetContextModel(surveyGroup.SubProductId);
            return new Project
            {
                Id = surveyGroup.Id,
                SubProductId = surveyGroup.SubProductId,
                Name = surveyGroup.Name ?? "",
                UniqueSurveyId = null,
                IsOpen = surveyGroup.isOpen,
                IsClosed = !surveyGroup.isOpen,
                IsPaused = false,
                Complete = surveyGroup.Complete,
                Target = 0,
                PercentComplete = 100,
                LaunchDate = surveyGroup.LaunchDate,
                CompleteDate = null,
                ProjectType = ProjectType.SurveyGroup,
                DataPageUrl = contextModel.VueDataPageUrl,
                ReportsPageUrl = contextModel.VueReportsPageUrl,
                ChildSurveysIds = surveyGroup.ChildSurveysIds ?? Array.Empty<int>(),
                NumberOfUsers = (summaryPermission?.SharedUserIds.Count() ?? 0) + projectIdToUsersLookup[surveyGroup.SubProductId].Distinct().Count(),
                IsSharedWithAllUsers = summaryPermission?.IsShared ?? false || isSharedWithAllUsers,
                IsDocumentsTabAvailable = contextModel.VueDocumentsEnabled,
                IsQuotaTabAvailable = contextModel.VueQuotaEnabled,
                CustomIntegrations = contextModel.CustomUiWidgets,
                CompanyAuthId = authCompanyId,
            };
        }

        string GenerateAnonymousUrlToDownloadFile(string fileName, DocumentOwnedBy ownedBy, SurveyDocumentsRequestContext context)
        {
            var baseUri = new Uri($"{Uri.UriSchemeHttps}{Uri.SchemeDelimiter}{context.InsecureDownloadDomain}");
            var location = ownedBy == DocumentOwnedBy.Client ? "DownloadClient" : "Download";
            return GenerateURL(fileName, context, location, baseUri);
        }

        string GenerateAuthenticatedUrlToDownloadFile(string fileName, DocumentOwnedBy ownedBy, SurveyDocumentsRequestContext context)
        {
            var location = ownedBy == DocumentOwnedBy.Client ? "SecureDownloadClient" : "SecureDownload";
            return GenerateURL(fileName, context, location, context.SurveyDownloadUri);
        }

        private static string GenerateURL(string fileName, SurveyDocumentsRequestContext context, string location, Uri baseUri)
        {
            var folderPathWithDelims = "/" + context.FolderPath;
            if (folderPathWithDelims != "/")
            {
                folderPathWithDelims += "/";
            }
            var segments = new List<String>
            {
                context.PathBase.Trim('/'),
                location,
                context.SurveyDownloadGuid.ToString("N"),
                fileName,
            };
            var queryString = $"path={folderPathWithDelims}";
            return new Uri(baseUri, $"{string.Join("/", segments.Where(x => !string.IsNullOrEmpty(x)))}?{queryString}")
                .AbsoluteUri;
        }

        public DocumentUrlProviderSignature DocumentUrlProvider(int surveyId)
        {
            var config = _allVueProductConfigurationService.GetConfiguration(surveyId.ToString());
            if (config.AllVueDocumentationConfiguration.EnableSecureFileDownload)
            {
                return GenerateAuthenticatedUrlToDownloadFile;
            }
            return GenerateAnonymousUrlToDownloadFile;
        }
    }
}
