using AuthServer.GeneratedAuthApi;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;
using UserManagement.BackEnd.Domain.UserDataPermissions.Entities;
using UserManagement.BackEnd.Models;
using Vue.Common.Auth;
using Vue.Common.AuthApi;
using Vue.Common.AuthApi.Models;
using Vue.Common.Constants;
using UMDataPermissions = UserManagement.BackEnd.Application.UserDataPermissions.Services;

namespace UserManagement.BackEnd.Services
{
    public class ProjectsService : IProjectsService
    {
        private readonly IUserContext _userContext;
        private readonly IAuthApiClient _authApiClient;
        private readonly AnswersDbContext _answersDbContext;
        private readonly IExtendedAuthApiClient _extendedAuthApiClient;
        private readonly IAllVueRuleRepository _allVueRuleRepository;
        private readonly ISurveyGroupService _surveyGroupService;
        private readonly ILogger<ProjectsService> _logger;
        private readonly IProductsService _productsService;
        private readonly UMDataPermissions.IUserDataPermissionsService _userDataPermissionsService;
        private readonly IQuestionService _questionService;

        public ProjectsService(
            IUserContext userContext,
            IAuthApiClient authApiClient,
            AnswersDbContext answersDbContext,
            IExtendedAuthApiClient extendedAuthApiClient,
            IAllVueRuleRepository allVueRuleRepository,
            ISurveyGroupService surveyGroupService,
            IProductsService productsService,
            IQuestionService questionService,
            UMDataPermissions.IUserDataPermissionsService userDataPermissionsService,
            ILogger<ProjectsService> logger)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _authApiClient = authApiClient ?? throw new ArgumentNullException(nameof(authApiClient));
            _answersDbContext = answersDbContext;
            _extendedAuthApiClient =
                extendedAuthApiClient ?? throw new ArgumentNullException(nameof(extendedAuthApiClient));
            _allVueRuleRepository = allVueRuleRepository ?? throw new ArgumentNullException(nameof(allVueRuleRepository));
            _surveyGroupService = surveyGroupService ?? throw new ArgumentNullException(nameof(surveyGroupService));
            _productsService = productsService ?? throw new ArgumentNullException(nameof(productsService));
            _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userDataPermissionsService = userDataPermissionsService ?? throw new ArgumentNullException(nameof(userDataPermissionsService));
        }

        private async Task<string> GetCompanyShortCode(string? companyId, CancellationToken token)
        {
            if (string.IsNullOrEmpty(companyId))
            {
                return _userContext.AuthCompany;
            }
            var company = await _authApiClient.GetCompanyById(companyId, token);
            if (!CurrentUserHasSecurityGroupAccess(company.SecurityGroup))
            {
                throw new KeyNotFoundException($"Company with id {companyId} not found");
            }
            return company.ShortCode;
        }

        private bool CurrentUserHasSecurityGroupAccess(string securityGroup)
        {
            return _userContext.IsAuthorizedWithinThisCompanyScope(securityGroup);
        }

        protected virtual async Task<IList<Surveys>> GetSurveysForCompanies(List<string> companyIds, CancellationToken token)
        {
            var companyIdsTable = new DataTable();
            companyIdsTable.Columns.Add("Id", typeof(string));
            foreach (var id in companyIds)
                companyIdsTable.Rows.Add(id);

            var param = new SqlParameter("@CompanyIds", companyIdsTable)
            {
                TypeName = "dbo.AuthCompanyIdTableType",
                SqlDbType = SqlDbType.Structured
            };

            return await _answersDbContext.Surveys
                .FromSqlRaw(@"SELECT surveyId, name, portalName, portalVisible, uniqueSurveyId, status, authCompanyId, kimbleProposalId FROM dbo.surveys s
                  INNER JOIN @CompanyIds c ON s.authCompanyId = c.Id
                  WHERE s.name IS NOT NULL AND s.name <> ''", param).ToListAsync(token);
        }

        private async Task<IEnumerable<Project>> UpdateProjectsAccessStatus(
            IList<Project> projects,
            string[] companyIds,
            CancellationToken token)
        {
            var allVueRules = await _allVueRuleRepository.GetByCompaniesAsync(companyIds, token);
            var updatedProjects = new List<Project>();
            var lookup = _surveyGroupService.GetLookupOfSurveyGroupIdToSafeUrl();

            var sharedProjects = new HashSet<string>(await _authApiClient.GetSharedProjects(projects.Select(p => p.ProjectId.ToLegacyAuthName(lookup)), token));
            var rulesLookup = allVueRules.ToLookup(rule => new ProjectOrProduct(rule.ProjectType, rule.ProjectOrProductId), rule => rule);
            foreach (var project in projects)
            {
                var rulesForProject = rulesLookup[project.ProjectId.ToProjectOrProduct()].ToList();
                var isLegacyShared = sharedProjects.Contains(project.ProjectId.ToLegacyAuthName(lookup));
                var sharedToAllViaRule = rulesForProject.GetSharedToAll();
                var isProjectShared = sharedToAllViaRule != null;
                var dataGroupCountIgnoringSharedAll = rulesForProject.Count - (sharedToAllViaRule != null ? 1 : 0);

                updatedProjects.Add(project with { UserAccess = GetAccessStatusFromSharedAndGroupCount(isLegacyShared || isProjectShared, dataGroupCountIgnoringSharedAll), DataGroupCount = rulesForProject.Count, IsShared = isLegacyShared });
            }
            return updatedProjects;
        }

        private IEnumerable<string> FlattenCompanyTree(CompanyNode company)
        {
            var children = company.Children;
            var companies = new List<string>();
            foreach (var child in children)
            {
                companies.Add(child.Id);
                companies.AddRange(FlattenCompanyTree(child));
            }
            return companies;
        }

        private List<ProjectIdentifier> GetProjectIdentifiersForSurveys(List<string> companyIds, 
            IList<Surveys> allSurveysList, 
            HashSet<int> sharedSurveyProjects)
        {
            return allSurveysList
                .Where(s => companyIds.Contains(s.AuthCompanyId) && sharedSurveyProjects.Contains(s.SurveyId))
                .Select(s => new ProjectIdentifier(ProjectType.AllVueSurvey, s.SurveyId))
                .ToList();
        }

        private List<Project> GetProjectsForCompanySurveys(CompanyNode company,
            IList<Surveys> allSurveysList,
            HashSet<int> sharedProjects)
        {
            return allSurveysList
                .Where(s => (company.Id == s.AuthCompanyId))
                .Select(s => 
                    new Project(
                    new ProjectIdentifier(ProjectType.AllVueSurvey, s.SurveyId),
                    s.Name,
                    AccessStatus.AllUsers,
                    company.Id,
                    company.DisplayName,
                    0,
                    sharedProjects.Contains(s.SurveyId),
                    $"{company.Url}/survey/{s.SurveyId}"
                    ))
                .ToList();
        }

        private List<Project> GetProjectsForCompanySurveyGroups(CompanyNode company,
            IEnumerable<SurveyGroup> surveyGroups,
            HashSet<string> sharedProjects)
        {
            var result = new List<Project>();
            foreach (var group in surveyGroups)
            {
                if (group.Surveys.All(s => company.Id == s.Survey.AuthCompanyId) && group.Surveys.Any())
                {
                    result.Add(new Project(
                        new ProjectIdentifier(ProjectType.AllVueSurveyGroup, group.SurveyGroupId),
                        group.Name,
                        AccessStatus.AllUsers,
                        company.Id,
                        company.DisplayName,
                        0,
                        sharedProjects.Contains(group.UrlSafeName),
                        $"{company.Url}/survey/{group.UrlSafeName}"
                    ));
                }
            }
            return result;
        }

        public async Task<List<Project>> GetProjectsByCompanyShortCode(bool includeSavanta, string companyShortCode, CancellationToken token)
        {
            var companyList = await GetListOfCompaniesAndChildrenForCompanyShortCode(companyShortCode, token);
            if (!includeSavanta)
            {
                companyList = companyList.Where(c => c.ShortCode != AuthConstants.SavantaCompany).ToList();
            }
            var companyIds = companyList.Select(company=>company.Id).ToList();
            var surveys = await GetSurveysForCompanies(companyIds, token);
            var sharedSurveyProjects = await SharedSurveyProjects(surveys, token);

            var surveyGroups = _surveyGroupService.GetSurveyGroupsForCompanies(companyIds).ToList();

            var sharedGroupProjects =
                (await _authApiClient.GetSharedProjects(surveyGroups.Select(x => x.UrlSafeName), token))
                .ToHashSet();
            var result = companyList.SelectMany(c =>
            {
                var projects = GetProjectsForCompanySurveys(c, surveys, sharedSurveyProjects)
                    .Concat(GetProjectsForCompanySurveyGroups(c, surveyGroups, sharedGroupProjects));
                return projects;
            }).OrderBy(x => x.CompanyName).ToList();

            return result;
        }

        private async Task<HashSet<int>> SharedSurveyProjects(IList<Surveys> surveys, CancellationToken token)
        {
            var sharedSurveyProjects =
                (await _authApiClient.GetSharedProjects(surveys.Select(x => x.SurveyId.ToString()), token));
            return sharedSurveyProjects
                .Select(consideredToBeSurveyId => 
                {
                    if (int.TryParse(consideredToBeSurveyId, out var projectId))
                    {
                        return projectId;
                    }
                    _logger.LogError("{method} ignoring shared project {projectId} as it's not an int", nameof(SharedSurveyProjects), consideredToBeSurveyId);
                    return (int?)null;
                })
                .Where(projectId => projectId.HasValue)
                .Select(projectId => projectId.Value)
                .ToHashSet();
        }

        public async Task<List<CompanyWithProductsAndProjects>> GetCompanyAndChildCompanyWithProjectIdentifier(string companyShortCode, CancellationToken token)
        {
            var companyList = await GetListOfCompaniesAndChildrenForCompanyShortCode(companyShortCode, token);

            List<CompanyNode> FlattenOrdered(CompanyNode node)
            {
                if (!CurrentUserHasSecurityGroupAccess(node.SecurityGroup))
                {
                    return new List<CompanyNode>();
                }
                var result = new List<CompanyNode> { node };
                var orderedChildren = node.Children?.OrderBy(c => c.DisplayName) ?? Enumerable.Empty<CompanyNode>();
                foreach (var child in orderedChildren)
                {
                    result.AddRange(FlattenOrdered(child));
                }
                return result;
            }

            var orderedCompanies = new List<CompanyNode>();
            var selectedCompany = companyList.Single(c => c.ShortCode == companyShortCode);
            orderedCompanies.AddRange(FlattenOrdered(selectedCompany));

            var companyIds = orderedCompanies.Select(company => company.Id).ToList();
            var surveys = await GetSurveysForCompanies(companyIds, token);
            var sharedSurveyProjects = await SharedSurveyProjects(surveys, token);
            var surveyGroups = _surveyGroupService.GetSurveyGroupsForCompanies(companyIds);
            var sharedSurveyGroupProjects =
                (await _authApiClient.GetSharedProjects(surveyGroups.Select(x => x.UrlSafeName), token))
                .ToHashSet();

            var result = orderedCompanies.Select(c =>
            {
                var childCompanyIds = FlattenCompanyTree(c).ToList();
                var allRelatedCompanyIds = new List<string>(childCompanyIds) { c.Id };
                var projects = GetProjectIdentifiersForSurveys(allRelatedCompanyIds, surveys, sharedSurveyProjects)
                    .Concat(GetProjectIdentifiersForSurveyGroups(surveyGroups, allRelatedCompanyIds, sharedSurveyGroupProjects));

                return new CompanyWithProductsAndProjects(c.Id,
                    c.ShortCode,
                    c.DisplayName,
                    c.Url,
                    c.HasExternalSSOProvider,
                    childCompanyIds,
                    projects.ToList(),
                    _productsService.ToProducts(c.ProductShortCodes),
                    c.ProductShortCodes.Contains(ProductsService.AuthProductIdFor_SurveyVueEditor),
                    c.ProductShortCodes.Contains(ProductsService.AuthProductIdFor_SurveyVueFeedback));
            })
            .ToList();

            return result;
        }
        private List<ProjectIdentifier> GetProjectIdentifiersForSurveyGroups(IEnumerable<SurveyGroup> surveyGroups,
            List<string> allRelatedCompanyIds,
            HashSet<string> sharedSurveyGroupProjects)
        {
            var result = new List<ProjectIdentifier>();
            foreach (var group in surveyGroups.Where(group => sharedSurveyGroupProjects.Contains(group.UrlSafeName)))
            {
                if (group.Surveys.All(s => allRelatedCompanyIds.Contains(s.Survey.AuthCompanyId)) && group.Surveys.Any())
                {
                    result.Add(new ProjectIdentifier(ProjectType.AllVueSurveyGroup, group.SurveyGroupId));
                }
            }
            return result;
        }

        private AccessStatus GetAccessStatusFromSharedAndGroupCount(bool isShared, int dataGroupCount)
        {
            if (isShared)
            {
                return dataGroupCount > 0 ? AccessStatus.Mixed : AccessStatus.AllUsers;
            }
            return dataGroupCount > 0 ? AccessStatus.Restricted : AccessStatus.None;
        }

        private async Task<(IList<BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue.AllVueRule>, bool)> GetGroupsAndSharedStatusForProjectId(string company, ProjectOrProduct projectId, CancellationToken token)
        {
            var dataGroups = (await _allVueRuleRepository.GetByCompanyAndProjectId(company, projectId, token)).ToList();
            var projectIdString = projectId.ToLegacyAuthName(_surveyGroupService.GetLookupOfSurveyGroupIdToSafeUrl());

            var sharedProjects = await _authApiClient.GetSharedProjects([projectIdString], token);
            return (dataGroups, sharedProjects.Contains(projectIdString));
        }

        public async Task<IEnumerable<Project>> GetProjectsByCompanyId(string? companyId, CancellationToken token)
        {
            var companyShortCode = await GetCompanyShortCode(companyId, token);
            var companiesFiltered = await GetListOfCompaniesAndChildrenForCompanyShortCode(companyShortCode, token);
            var companyIds = companiesFiltered.Select(c => c.Id).ToList();
            var companyNameLookup = companiesFiltered.ToLookup(company => company.Id, company => company.DisplayName);
            var (surveys, surveyGroups) = await GetSurveyAndSurveyGroups(companyId, token, companyIds, companiesFiltered, companyShortCode);
            var projects = surveys.Select(survey => new Project(new ProjectIdentifier(ProjectType.AllVueSurvey, survey.SurveyId),
                survey.Name,
                AccessStatus.None,
                survey.AuthCompanyId,
                companyNameLookup[survey.AuthCompanyId].FirstOrDefault(),
                0,
                true, 
                $"{companiesFiltered.Single(c => c.Id == survey.AuthCompanyId).Url}/survey/{survey.SurveyId.ToString()}")).ToList();
            projects.AddRange(surveyGroups.Select(group => new Project(new ProjectIdentifier(ProjectType.AllVueSurveyGroup, group.SurveyGroupId), 
                group.Name, 
                AccessStatus.None,
                group.Surveys.First().Survey.AuthCompanyId,
                companyNameLookup[group.Surveys.First().Survey.AuthCompanyId].FirstOrDefault(),
                0,
                true, $"{companiesFiltered.Single(c => c.Id == group.Surveys.First().Survey.AuthCompanyId).Url}/survey/{group.UrlSafeName}")));
            if (!_userContext.IsAuthorizedSavantaUser)
            {
                projects = await RemoveProjectsThatTheUserDoesNotHaveAccessTo(projects, token);
            }
            return await UpdateProjectsAccessStatus(projects, companyIds.ToArray(), token);
        }

        private async Task<List<Project>> RemoveProjectsThatTheUserDoesNotHaveAccessTo(List<Project> projects, CancellationToken token)
        {
            var companies = projects.Select(p => p.CompanyId).Distinct().ToArray();
            var projectsUserHasAccessTo = await _userDataPermissionsService.GetSharedProjectsByUserId(_userContext.UserId, companies, token);
            var accessibleProjects = projectsUserHasAccessTo
                .Select(x => (x.Company, x.ProjectType, x.ProjectId))
                .ToHashSet();
            var projectListUserHasAccessTo = projects.Where(
                project => accessibleProjects.Contains((project.CompanyId, project.ProjectId.Type, project.ProjectId.Id)));
            return projectListUserHasAccessTo.ToList();
        }

        private async Task<(IList<Surveys> surveys, IEnumerable<SurveyGroup> surveyGroups)> GetSurveyAndSurveyGroups(string? companyId, CancellationToken token, List<string> companyIds,
            List<CompanyNode> companiesFiltered, string companyShortCode)
        {
            var surveys = await GetSurveysForCompanies(companyIds, token);
            var surveyGroups = _surveyGroupService.GetSurveyGroupsForCompanies(companyIds);

            (surveys, surveyGroups) = FilteredSurveysList(companyId, companiesFiltered, companyShortCode, surveys, surveyGroups);

            return (surveys, surveyGroups);
        }

        private static (IList<Surveys> surveys, IEnumerable<SurveyGroup> surveyGroups) FilteredSurveysList(string? companyId, List<CompanyNode> companiesFiltered,
            string companyShortCode, IList<Surveys> surveys, IEnumerable<SurveyGroup> surveyGroups)
        {
            var savanta = companiesFiltered.FirstOrDefault(x => x.ShortCode == AuthConstants.SavantaCompany);

            if (savanta != null)
            {
                bool includeSavantaOwnedProjects = companyShortCode == AuthConstants.SavantaCompany && companyId == savanta.Id;
                if (!includeSavantaOwnedProjects)
                {
                    var companyIdForSavanta = savanta.Id;
                    surveys = surveys.Where(s => s.AuthCompanyId != companyIdForSavanta).ToList();
                    surveyGroups = surveyGroups
                        .Where(g => g.Surveys.All(s => s.Survey.AuthCompanyId != companyIdForSavanta)).ToList();
                }
            }

            return (surveys, surveyGroups);
        }

        private async Task<List<CompanyNode>> GetListOfCompaniesAndChildrenForCompanyShortCode(string companyShortCode, CancellationToken token)
        {
            var companies = await _extendedAuthApiClient.GetCompanyAndChildrenList(companyShortCode, token);
            return companies.Where(c => CurrentUserHasSecurityGroupAccess(c.SecurityGroup)).ToList();
        }

        private async Task<Project> GetProjectByIdAndCompanyId(ProjectOrProduct projectId, string projectName, string authCompanyId, CancellationToken token)
        {
            var company = await _authApiClient.GetCompanyById(authCompanyId, token);
            if (!CurrentUserHasSecurityGroupAccess(company.SecurityGroup))
            {
                throw new KeyNotFoundException($"Company with id {authCompanyId} not found");
            }

            var (dataGroups, isShared) = await GetGroupsAndSharedStatusForProjectId(authCompanyId, projectId, token);

            var sharedToAllRule = dataGroups.GetSharedToAll();
            var shareToAllViaDataGroups = sharedToAllRule != null;
            var dataGroupCount = dataGroups.Count - (sharedToAllRule != null ? 1 : 0);

            return new Project(new ProjectIdentifier(projectId.ProjectType, projectId.ProjectId),
                projectName,
                GetAccessStatusFromSharedAndGroupCount(isShared || shareToAllViaDataGroups, dataGroupCount),
                company.Id,
                company.DisplayName,
                dataGroups.Count(),
                isShared,
                $@"{company.Url}/survey/{projectId.ToLegacyAuthName(_surveyGroupService.GetLookupOfSurveyGroupIdToSafeUrl())}");
        }

        public async Task<Project?> GetProjectById(string companyAuthId, ProjectIdentifier projectId, CancellationToken token)
        {
            switch (projectId.Type)
            {
                case ProjectType.AllVueSurvey:
                {
                    var survey = _answersDbContext.Surveys.AsNoTracking()
                        .SingleOrDefault(s => s.SurveyId == projectId.Id);
                    if (survey != null && (companyAuthId == survey.AuthCompanyId || 
                                           await IsProjectedShared(projectId, companyAuthId) ) )
                    {
                        return await GetProjectByIdAndCompanyId(
                            projectId.ToProjectOrProduct(),
                            survey.Name,
                            companyAuthId,
                            token);
                    }
                }
                    break;

                case ProjectType.AllVueSurveyGroup:
                case ProjectType.BrandVue:
                {
                    var surveyGroup = await _surveyGroupService.GetSurveyGroupByIdAsync(projectId.Id, token);
                    if (surveyGroup != null && surveyGroup.Surveys.Any())
                    {
                        var surveySharedOwners = await _surveyGroupService.GetSharedSurveysByIds(surveyGroup.Surveys.Select(s => s.SurveyId).ToArray());
                        bool hasAccess = surveyGroup.Surveys.All(surveyGroupSurvey => surveyGroupSurvey.Survey.AuthCompanyId == companyAuthId || surveySharedOwners.Any(x =>
                            x.SurveyId == surveyGroupSurvey.SurveyId && x.AuthCompanyId == companyAuthId));

                        if (hasAccess)
                        {
                            return await GetProjectByIdAndCompanyId(
                                projectId.ToProjectOrProduct(),
                                surveyGroup.Name,
                                companyAuthId,
                                token);
                        }
                    }
                } break;
            }
            return null;
        }

        private async Task<bool> IsProjectedShared(ProjectIdentifier projectId, string authCompanyId)
        {
            return (await _surveyGroupService
                .GetSharedSurveysByIds([projectId.Id]))
                .Any(x=> x.AuthCompanyId == authCompanyId);
        }

        private async Task<bool> ProjectExists(ProjectIdentifier projectId, CancellationToken token)
        {
            switch (projectId.Type)
            {
                case ProjectType.AllVueSurvey:
                    return await _answersDbContext.Surveys.AsNoTracking().AnyAsync(s => s.SurveyId == projectId.Id, token);
                case ProjectType.AllVueSurveyGroup:
                case ProjectType.BrandVue:
                    return await _surveyGroupService.GetSurveyGroupByIdAsync(projectId.Id, token) != null;
            }
            return false;
        }

        public async Task SetProjectSharedStatus(string company, ProjectIdentifier projectId, bool isShared, CancellationToken token)
        {
            if (await ProjectExists(projectId, token))
            {
                var lookup = _surveyGroupService.GetLookupOfSurveyGroupIdToSafeUrl();
                var legacyProjectId = projectId.ToLegacyAuthName(lookup);

                var rules = await _userDataPermissionsService.GetAllVueRulesByProjectAsync(company, new ProjectOrProduct(projectId.Type, projectId.Id), token);

                var existingSharedAllRule = rules.SingleOrDefault(x => x.AllCompanyUsersCanAccessProject);
                if (!isShared)
                {
                    await _authApiClient.SetProjectShared(legacyProjectId, false, _userContext.UserId, token);
                    if (existingSharedAllRule != null)
                    {
                        await _userDataPermissionsService.DeleteAllVueRuleAsync(existingSharedAllRule.Id, token);
                    }
                }
                else
                {
                    if (existingSharedAllRule == null)
                    {
                        await _userDataPermissionsService.AddAllVueRuleAsync(_userContext.UserId,
                            rules.CreateDefaultRole("All",company, projectId, true),
                            token);
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Project {projectId.Type} with id {projectId.Id} not found", nameof(projectId));
            }
        }


        public async Task<IList<string>> GetLegacySharedUsers(ProjectIdentifier projectId, string shortCode, string currentUserId, CancellationToken token)
        {
            if (await ProjectExists(projectId, token))
            {
                (string _, var legacyEmailUsers) = await GetLegacyUsersForThisProject(projectId, shortCode, currentUserId, token);
                return legacyEmailUsers.Select(userProjectModel =>  userProjectModel.Email).ToList();
            }
            throw new ArgumentException($"Project {projectId.Type} with id {projectId.Id} not found", nameof(projectId));
        }

        public async Task MigrateLegacySharedUsers(string company, ProjectIdentifier projectId, string shortCode, string currentUserId, CancellationToken token)
        {
            if (await ProjectExists(projectId, token))
            {
                var rules = await _userDataPermissionsService.GetAllVueRulesByProjectAsync(company, new ProjectOrProduct(projectId.Type, projectId.Id), token);
                (string legacyProjectId, var legacyEmailUsers) = await GetLegacyUsersForThisProject(projectId, shortCode, currentUserId, token);
                await MigrateSharedToAll(company, projectId, token, legacyProjectId, rules);
                await MigrateSharedUsers(company, projectId, shortCode, token, legacyEmailUsers, rules, legacyProjectId);
            }
            else
            {
                throw new ArgumentException($"Project {projectId.Type} with id {projectId.Id} not found", nameof(projectId));
            }
        }

        private async Task MigrateSharedUsers(string company, ProjectIdentifier projectId, string shortCode,
            CancellationToken token, IList<UserProjectsModel> legacyEmailUsers, IList<AllVueRule> rules, string legacyProjectId)
        {
            if (legacyEmailUsers.Any())
            {
                var newRuleId = await _userDataPermissionsService.AddAllVueRuleAsync(_userContext.UserId,
                    rules.CreateDefaultRole("All-User", company, projectId, false),
                    token);

                await _userDataPermissionsService.AssignAllVueRuleToUserDataPermissionAsync(
                    legacyEmailUsers.Select(x => x.ApplicationUserId).ToArray(),
                    newRuleId, _userContext.UserId, token);

                foreach (var userProjectsModel in legacyEmailUsers)
                {
                    var project = userProjectsModel.Projects.Single(x => x.ProjectId == legacyProjectId);
                    await _authApiClient.RemoveUserFromProject(shortCode, project.Id,
                        userProjectsModel.ApplicationUserId, token);
                }
            }
        }

        private async Task MigrateSharedToAll(string company, ProjectIdentifier projectId, CancellationToken token,
            string legacyProjectId, IList<AllVueRule> rules)
        {
            if (await _authApiClient.IsProjectShared(legacyProjectId, token))
            {
                var existingSharedWithAll = rules.SingleOrDefault(x => x.AllCompanyUsersCanAccessProject);
                if (existingSharedWithAll == null)
                {
                    await _userDataPermissionsService.AddAllVueRuleAsync(_userContext.UserId,
                        rules.CreateDefaultRole("All", company, projectId, true),
                        token);
                }
                await _authApiClient.SetProjectShared(legacyProjectId, false, _userContext.UserId, token);
            }
        }

        private async Task<(string LegacyProjectId, IList<UserProjectsModel> LegacyEmailUsers)> GetLegacyUsersForThisProject(ProjectIdentifier projectId, string shortCode, string currentUserId,
            CancellationToken token)
        {
            CompanyModel company = await _authApiClient.GetCompanyByShortcode(shortCode, token);
            if (!CurrentUserHasSecurityGroupAccess(company.SecurityGroup))
            {
                throw new KeyNotFoundException($"Company with short code {shortCode} not found");
            }
            var lookup = _surveyGroupService.GetLookupOfSurveyGroupIdToSafeUrl();
            var legacyProjectId = projectId.ToLegacyAuthName(lookup);

            var users = await _authApiClient.GetAllUserDetailsForCompanyScopeAsync(shortCode, currentUserId, company.Id,
                false, token);
            var legacyEmailUsers = users.Where(x => x.Projects.Any(x => x.ProjectId == legacyProjectId)).ToList();
            return (legacyProjectId, legacyEmailUsers);
        }

        public async Task<int> GetProjectResponseCountFromFilter(string companyId, ProjectIdentifier projectId,
            List<AllVueFilter> filters, CancellationToken token)
        {
            if (await ProjectExists(projectId, token))
            {
                return await _questionService.GetProjectResponseCountFromFilter(companyId, projectId, filters, token);
            }
            throw new ArgumentException($"Project with ID {projectId} does not exist.", nameof(projectId));
        }

        public async Task<VariablesAvailable> GetProjectVariablesAvailable(string companyId, ProjectIdentifier projectId, CancellationToken token)
        {
            if (await ProjectExists(projectId, token))
            {
                var allQuestionsAvailable = await _questionService.GetProjectQuestionsAvailable(_userContext.AuthCompany, projectId, token);
                
                if (_userContext.IsSystemAdministrator)
                {
                    return allQuestionsAvailable;
                }

                if (_userContext.IsAuthorizedSavantaUser)
                {
                    return allQuestionsAvailable;
                }
                var myPermissions = await _userDataPermissionsService.GetByUserIdByCompanyAndProjectAsync(_userContext.UserId, companyId, projectId.ToProjectOrProduct(), token);
                if (myPermissions == null)
                {
                    return new VariablesAvailable(allQuestionsAvailable.SurveySegments, []);
                }
                if (myPermissions.AvailableVariableIds.Any())
                {
                    var includedQuestions = allQuestionsAvailable.UnionOfQuestions
                        .Where(q => myPermissions.AvailableVariableIds.Contains(q.Id))
                        .ToList();
                    return new VariablesAvailable(allQuestionsAvailable.SurveySegments, includedQuestions);
                }
                return allQuestionsAvailable;
            }
            throw new ArgumentException($"Project with ID {projectId} does not exist.", nameof(projectId));
        }


        public ProjectIdentifier AuthProjectToProjectIdentifier(string projectName)
        {
            if (_surveyGroupService.TryParse(projectName, out var surveyGroupId))
            {
                return new ProjectIdentifier(ProjectType.AllVueSurveyGroup, surveyGroupId);
            }
            if (int.TryParse(projectName, out var projectId))
            {
                return new ProjectIdentifier(ProjectType.AllVueSurvey, projectId);
            }
            return new ProjectIdentifier(ProjectType.Unknown, 0);
        }

        public ProjectIdentifier AuthShortCodeAndProjectToProjectIdentifier(string productShortCode, string projectName)
        {
            if (productShortCode == SavantaConstants.AllVueShortCode)
            {
                return AuthProjectToProjectIdentifier(projectName);
            }
            if (int.TryParse(projectName, out var projectId))
            {
                return new ProjectIdentifier(ProjectType.BrandVue, projectId);
            }
            return new ProjectIdentifier(ProjectType.Unknown, 0);
        }
    }
}
