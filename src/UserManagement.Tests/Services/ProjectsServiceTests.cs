using AuthServer.GeneratedAuthApi;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using Microsoft.Extensions.Logging;
using UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;
using UserManagement.BackEnd.Models;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth;
using Vue.Common.AuthApi;
using Vue.Common.AuthApi.Models;
using Vue.Common.BrandVueApi;
using Vue.Common.BrandVueApi.Models;
using UMDataPermissions = UserManagement.BackEnd.Application.UserDataPermissions.Services;

namespace UserManagement.Tests.Services
{
    [TestFixture]
    public class ProjectsServiceTests
    {
        private readonly string AuthCompanyId = "company1";
        private readonly int ProjectId = 1;

        private string _company = "CompanyGUID";
        private IUserContext _userContext = null!;
        private IAuthApiClient _authApiClient = null!;
        private AnswersDbContext _answersDbContext = null!;
        private IExtendedAuthApiClient _extendedAuthApiClient = null!;
        private IAllVueRuleRepository _allVueRuleRepository = null!;
        private ISurveyGroupService _surveyGroupService = null!;
        private ProjectServiceForTesting _service = null!;
        private readonly ILogger<ProjectsService> _logger = Substitute.For<ILogger<ProjectsService>>();
        private CancellationToken _token;
        private IProductsService _productsService = null!;
        private IBrandVueApiClient _brandVueApiClient = null!;
        private IVariableService _variableService = null!;
        private IQuestionService _questionService = null!;
        private UMDataPermissions.IUserDataPermissionsService _userDataPermissionsService = null!;


        private List<Surveys> CreateSurveyDbSet(int howMany)
        {
            var surveys = new List<Surveys>();
            for (int i = 1; i <= howMany; i++)
            {
                surveys.Add(new Surveys { SurveyId = i, Name = $"Survey {i}", AuthCompanyId = AuthCompanyId });
            }
            return surveys;
        }

        private List<SurveyGroup> CreateSurveyGroupDbSet(int howMany, List<Surveys> existingSurveys)
        {
            var surveyGroups = new List<SurveyGroup>();
            for (int i = 1; i <= howMany; i++)
            {
                var mySurvey = existingSurveys[i-1];
                surveyGroups.Add(new SurveyGroup
                {
                    SurveyGroupId = i,
                    UrlSafeName = $"group{i}",
                    Name = $"Group {i} based only on survey {mySurvey.Name}",
                    Type = SurveyGroupType.AllVue,
                    Surveys = new List<SurveyGroupSurveys>
                    {
                        new SurveyGroupSurveys { Survey = mySurvey }
                    }
                });
            }
            return surveyGroups;
        }

        private List<QuestionWithSurveySets> CreateQuestionList(int howMany)
        {
            var questions = new List<QuestionWithSurveySets>();
            for (int i = 1; i <= howMany; i++)
            {
                questions.Add(new QuestionWithSurveySets
                {
                    QuestionId = $"{i}",
                    QuestionText = $"Question {i}",
                    AnswerSpec = new QuestionAnswer { AnswerType = "Value", MinValue = 0, MaxValue = 100, Choices = new List<QuestionChoice> { new QuestionChoice {Id = "1", Value = "Value 1"}, new QuestionChoice {Id = "2", Value = "Value 2"}},
                    Multiplier = 1.0 },
                });
            }
            return questions;
        }

        private List<MetricConfiguration> CreateMetricConfigurationList(int howMany, int variableIdOffset = 100)
        {
            var metrics = new List<MetricConfiguration>();

            for (int i = 1; i <= howMany; i++)
            {
                metrics.Add(new MetricConfiguration
                {
                    Id = i,
                    Field = $"{i}",
                    VariableConfigurationId = variableIdOffset + i,
                    Name = $"Metric {i}",
                    VarCode = $"VarCode_{i}",
                    VariableConfiguration = new VariableConfiguration { Definition = new QuestionVariableDefinition()}
                });
            }

            return metrics;
        }

        //
        // Caution:
        // The database is in-memory and will be reset for each test run.
        // Ensure that the tests are independent and do not rely on previous state.
        // And the test code assumes the values in the database increment from 1
        //
        [SetUp]
        public void SetUp()
        {
            _userContext = Substitute.For<IUserContext>();
            _authApiClient = Substitute.For<IAuthApiClient>();

            var uniqueDbName = $"TestDB_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<AnswersDbContext>()
                .UseInMemoryDatabase(databaseName: uniqueDbName)
                .Options;

            _answersDbContext = new AnswersDbContext(options);
            _extendedAuthApiClient = Substitute.For<IExtendedAuthApiClient>();
            _allVueRuleRepository = Substitute.For<IAllVueRuleRepository>();
            _brandVueApiClient = Substitute.For<IBrandVueApiClient>();
            _variableService = Substitute.For<IVariableService>();
            _surveyGroupService = new SurveyGroupService(_answersDbContext);
            _productsService = new ProductsService(_surveyGroupService);
            _userDataPermissionsService = Substitute.For<UMDataPermissions.IUserDataPermissionsService>();
            _questionService = new QuestionService(_surveyGroupService, 
                new QuestionRepository(_answersDbContext), 
                _variableService, 
                _brandVueApiClient);
            var blankRule = new UserManagement.BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(1, "BlankRule", false, AuthCompanyId, ProjectType.AllVueSurvey, 1, [], [], "User", DateTime.MinValue);
            var simpleRule = new UserManagement.BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(2, "Simple", false, AuthCompanyId, ProjectType.AllVueSurvey, 2, [101], [], "User", DateTime.MinValue);
            _service = new ProjectServiceForTesting(_userContext, 
                _authApiClient, 
                _answersDbContext, 
                _extendedAuthApiClient, 
                _allVueRuleRepository, 
                _surveyGroupService, 
                _productsService, 
                _questionService, 
                _userDataPermissionsService, 
                _logger);
            _userDataPermissionsService.GetByUserIdByCompanyAndProjectAsync(Arg.Any<string>(),
                    Arg.Any<string>(), new ProjectOrProduct(ProjectType.AllVueSurvey, 1), CancellationToken.None)
                .Returns(Task.FromResult<UserManagement.BackEnd.Domain.UserDataPermissions.Entities.AllVueRule>(blankRule));
            _userDataPermissionsService.GetByUserIdByCompanyAndProjectAsync(Arg.Any<string>(),
                    Arg.Any<string>(), new ProjectOrProduct(ProjectType.AllVueSurvey, 2), CancellationToken.None)
                .Returns(Task.FromResult<UserManagement.BackEnd.Domain.UserDataPermissions.Entities.AllVueRule>(simpleRule));
            _token = CancellationToken.None;
        }

        [TearDown]
        public void TearDown()
        {
            _answersDbContext.Database.EnsureDeleted();
            _answersDbContext.Database.EnsureCreated();
        }

        [Test]
        public async Task GetProjects_ReturnsProjects_ForValidCompanyId()
        {
            // Arrange
            _userContext.IsAuthorizedSavantaUser.Returns(true);
            int surveyGroupId = ArrangeProjectsThatAreNotSharedWithUsers();

            // Act
            var result = (await _service.GetProjectsByCompanyId(AuthCompanyId, _token)).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Any(p => p.Name == "Survey 1"));
            Assert.That(result.Any(p => p.Name == "Group 1"));
            Assert.That(result.First(p => p.ProjectId.Id == surveyGroupId).UserAccess,
                Is.EqualTo(AccessStatus.AllUsers));
        }

        [Test]
        public async Task GetProjects_ReturnsProjects_ForValidCompanyIdForNonSavantaUser()
        {
            // Arrange
            _userContext.IsAuthorizedSavantaUser.Returns(false);
            int surveyGroupId = ArrangeProjectsThatAreNotSharedWithUsers();

            // Act
            var result = (await _service.GetProjectsByCompanyId(AuthCompanyId, _token)).ToList();

            Assert.That(result, Is.Empty);
        }

        private int ArrangeProjectsThatAreNotSharedWithUsers()
        {
            var surveyGroupId = 1;
            var companyShortCode = "short1";
            var companies = new List<CompanyNode>
            {
                new CompanyNode { Id = AuthCompanyId, DisplayName = "Company 1" }
            };
            var surveys = new List<Surveys>
            {
                new Surveys { SurveyId = 1, Name = "Survey 1", AuthCompanyId = AuthCompanyId }
            };
            var surveyGroups = new List<SurveyGroup>
            {
                new SurveyGroup
                {
                    SurveyGroupId = surveyGroupId,
                    UrlSafeName = "group1",
                    Name = "Group 1",
                    Type = SurveyGroupType.AllVue,
                    Surveys = new List<SurveyGroupSurveys>
                    {
                        new SurveyGroupSurveys
                        {
                            Survey = surveys[0]
                        }
                    }
                }
            };
            var sharedProjects = new List<string> { "1" };

            _authApiClient.GetCompanyById(Arg.Any<string>(), _token)
                .Returns(new CompanyModel { ShortCode = companyShortCode });
            _extendedAuthApiClient.GetCompanyAndChildrenList(companyShortCode, _token)
                .Returns(companies);
            _answersDbContext.Surveys.AddRange(surveys);
            _answersDbContext.SurveyGroups.AddRange(surveyGroups);
            _answersDbContext.SaveChanges();
            _authApiClient.GetSharedProjects(Arg.Any<IEnumerable<string>>(), _token)
                .Returns(sharedProjects);
            return surveyGroupId;
        }

        [Test]
        public async Task GetProjects_UsesUserContextAuthCompany_WhenCompanyIdIsNull()
        {
            // Arrange
            _userContext.AuthCompany.Returns("userShortCode");
            var companies = new List<CompanyNode>
            {
                new CompanyNode { Id = AuthCompanyId, DisplayName = "Company 1" }
            };
            var surveys = new List<Surveys>();
            var surveyGroups = new List<SurveyGroup>();
            _extendedAuthApiClient.GetCompanyAndChildrenList("userShortCode", _token)
                .Returns(companies);
            _answersDbContext.Surveys.AddRange(surveys);
            _answersDbContext.SurveyGroups.AddRange(surveyGroups);
            _answersDbContext.SaveChanges();
            _authApiClient.GetSharedProjects(Arg.Any<IEnumerable<string>>(), _token)
                .Returns(new List<string>());

            // Act
            var result = await _service.GetProjectsByCompanyId(null, _token);

            // Assert
            await _extendedAuthApiClient.Received(1).GetCompanyAndChildrenList("userShortCode", _token);
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ProjectsService(null, _authApiClient, _answersDbContext, _extendedAuthApiClient, _allVueRuleRepository, _surveyGroupService, _productsService, _questionService, _userDataPermissionsService, _logger));
            Assert.Throws<ArgumentNullException>(() =>
                new ProjectsService(_userContext, null, _answersDbContext, _extendedAuthApiClient, _allVueRuleRepository, _surveyGroupService, _productsService, _questionService, _userDataPermissionsService, _logger));
            Assert.Throws<ArgumentNullException>(() =>
                new ProjectsService(_userContext, _authApiClient, _answersDbContext, null, _allVueRuleRepository, _surveyGroupService, _productsService, _questionService, _userDataPermissionsService, _logger));
            Assert.Throws<ArgumentNullException>(() =>
                new ProjectsService(_userContext, _authApiClient, _answersDbContext, _extendedAuthApiClient, null, _surveyGroupService, _productsService, _questionService, _userDataPermissionsService, _logger));
        }

        [Test]
        public async Task GetProjects_ReturnsEmpty_WhenNoSurveysOrGroups()
        {
            // Arrange
            var companyShortCode = "short1";
            var companies = new List<CompanyNode>
            {
                new CompanyNode { Id = AuthCompanyId, DisplayName = "Company 1" }
            };
            var surveys = new List<Surveys>();
            var surveyGroups = new List<SurveyGroup>();
            _authApiClient.GetCompanyById(Arg.Any<string>(), _token)
                .Returns(new CompanyModel { ShortCode = companyShortCode });
            _extendedAuthApiClient.GetCompanyAndChildrenList(companyShortCode, _token)
                .Returns(companies);
            _answersDbContext.Surveys.AddRange(surveys);
            _answersDbContext.SurveyGroups.AddRange(surveyGroups);
            _answersDbContext.SaveChanges();
            _authApiClient.GetSharedProjects(Arg.Any<IEnumerable<string>>(), _token)
                .Returns(new List<string>());

            // Act
            var result = await _service.GetProjectsByCompanyId(AuthCompanyId, _token);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [TestCase(false, false, 0, AccessStatus.None)]
        [TestCase(true, false, 0, AccessStatus.AllUsers)]
        [TestCase(false, true, 1, AccessStatus.AllUsers)]
        [TestCase(false, false, 3, AccessStatus.Restricted)]
        [TestCase(true, false, 3, AccessStatus.Mixed)]
        [TestCase(false, true, 3, AccessStatus.Mixed)]
        public async Task GetProjects_ReturnsListOfProjectsWithCorrectUserAccessAndDataGroupCount_WhenProjectsAreShared(bool projectsAreShared, bool sharedViaDataGroups, int dataGroupCount, AccessStatus expectedUserStatus) 
        {
            // Arrange
            var (surveys, surveyGroups) = SetUpDataForTests();
            var projectIds = new List<ProjectOrProduct>();
            projectIds.AddRange(surveys.Select(survey => new ProjectOrProduct(ProjectType.AllVueSurvey, survey.SurveyId)).ToList());
            projectIds.AddRange(surveyGroups.Select(surveyGroup => new ProjectOrProduct(ProjectType.AllVueSurveyGroup, surveyGroup.SurveyGroupId)).ToList());
            var projectIdsAsString = new List<string>();
            _userContext.IsAuthorizedSavantaUser.Returns(true);
            if (projectsAreShared)
            {
                projectIdsAsString.AddRange(surveys.Select(survey => survey.SurveyId.ToString()).ToList());
                projectIdsAsString.AddRange(surveyGroups.Select(surveyGroup => surveyGroup.UrlSafeName).ToList());

            }

            var allVueRules = new List<AllVueRule>();
            foreach (var sharedProjectId in projectIds)
            {
                allVueRules.AddRange(CreateAllVueRules(dataGroupCount, sharedViaDataGroups, sharedProjectId));
            }

            _allVueRuleRepository.GetByCompaniesAsync(Arg.Any<string[]>(), _token).Returns(allVueRules);
            _authApiClient.GetSharedProjects(Arg.Any<IEnumerable<string>>(), _token)
                .Returns(projectIdsAsString);
            // Act
            var result = (await _service.GetProjectsByCompanyId("company1", _token)).ToList();
            // Assert
            Assert.That(result, Has.Count.EqualTo(5));
            Assert.That(result.All(project => project.DataGroupCount == dataGroupCount), Is.True, $"Incorrect data group counts: {string.Join(", ", result.Select(project => project.DataGroupCount.ToString()))}");
            Assert.That(result.All(project => project.UserAccess == expectedUserStatus), Is.True, $"Incorrect user access statuses: {string.Join(", ", result.Select(project => project.UserAccess.ToString()))}");
            Assert.That(result.All(project => project.IsShared == projectsAreShared), Is.True, $"Incorrect isShared values: {string.Join(", ", result.Select(project => project.IsShared.ToString()))}");
        }

        private IEnumerable<AllVueRule> CreateAllVueRules(int howMany, bool doesDataGroupHaveShareToAll, ProjectOrProduct? projectId = null)
        {
            var rules = new List<AllVueRule>();
            for (int i = 1; i <= howMany; i++)
            {
                rules.Add(new AllVueRule
                {
                    Id = i,
                    RuleName = $"Rule {i}",
                    AllUserAccessForSubProduct = doesDataGroupHaveShareToAll && i == 1,
                    Organisation = AuthCompanyId,
                    ProjectType = projectId?.ProjectType ?? ProjectType.AllVueSurveyGroup,
                    ProjectOrProductId = projectId?.ProjectId ?? ProjectId,
                    AvailableVariableIds = new List<int> { 1, 2, 3 },
                    Filters = new List<AllVueFilter>
                    {
                        new AllVueFilter { Id = i, VariableConfigurationId = i, EntitySetId = i, EntityIds = new[] { 1, 2 } }
                    },
                    UpdatedByUserId = "user1",
                    UpdatedDate = DateTime.UtcNow
                });
            }
            return rules;
        }

        private (IEnumerable<Surveys>, IEnumerable<SurveyGroup>) SetUpDataForTests()
        {
            var companyShortCode = "short1";
            var companies = new List<CompanyNode>
            {
                new CompanyNode { Id = AuthCompanyId, DisplayName = "Company 1" }
            };

            var surveys = CreateSurveyDbSet(3);
            var surveyGroups = CreateSurveyGroupDbSet(2, surveys);
            _authApiClient.GetCompanyById(Arg.Any<string>(), _token)
                .Returns(new CompanyModel { ShortCode = companyShortCode });
            _extendedAuthApiClient.GetCompanyAndChildrenList(companyShortCode, _token)
                .Returns(companies);
            _answersDbContext.Surveys.AddRange(surveys);
            _answersDbContext.SurveyGroups.AddRange(surveyGroups);
            _answersDbContext.SaveChanges();
            _authApiClient.GetSharedProjects(Arg.Any<IEnumerable<string>>(), _token)
                .Returns(new List<string> { "1", "3", "group2" });
            _allVueRuleRepository.GetByCompanyAndProjectId(Arg.Any<string>(), Arg.Any<ProjectOrProduct>(), _token).Returns(Enumerable.Empty<AllVueRule>());
            return (surveys.ToList(), surveyGroups.ToList());
        }

        [Test]
        public async Task GetProjectById_ReturnsNull_WhenNullProjectIdSupplied()
        {
            SetUpDataForTests();
            // Act
            var result = await _service.GetProjectById(AuthCompanyId, new ProjectIdentifier(ProjectType.Unknown, 1), _token);
            // Assert
            Assert.That(result, Is.Null, "Expected null result when null projectId is supplied.");
        }

        [Test]
        public async Task GetProjectById_ReturnsNull_WhenEmptyProjectIdSupplied()
        {
            SetUpDataForTests();
            // Act
            var result = await _service.GetProjectById(AuthCompanyId, new ProjectIdentifier(ProjectType.AllVueSurvey, 10001), _token);
            // Assert
            Assert.That(result, Is.Null, "Expected null result when null projectId is supplied.");
        }

        [Test]
        public async Task GetProjectById_ReturnsNull_WhenNonExistentSurveyGroupIdSupplied()
        {
            SetUpDataForTests();
            // Act
            var result = await _service.GetProjectById(AuthCompanyId, new ProjectIdentifier(ProjectType.AllVueSurveyGroup, 10001), _token);
            // Assert
            Assert.That(result, Is.Null, "Expected null result when null projectId is supplied.");
        }

        [Test]
        public async Task GetProjectById_ReturnsNull_WhenNonExistentSurveyIdSupplied()
        {
            SetUpDataForTests();
            // Act
            var result = await _service.GetProjectById(AuthCompanyId, new ProjectIdentifier(ProjectType.BrandVue, 10001), _token);
            // Assert
            Assert.That(result, Is.Null, "Expected null result when null projectId is supplied.");
        }

        [Test]
        public async Task GetProjectById_ReturnsProject_WhenValidSurveyIdSupplied()
        {
            var projectId = 1;
            SetUpDataForTests();
            // Act
            var projectIdentifier = new ProjectIdentifier(ProjectType.AllVueSurveyGroup, projectId);
            var result = await _service.GetProjectById(AuthCompanyId, projectIdentifier, _token);
            // Assert
            Assert.That(result, Is.Not.Null, "Expected a project to be returned for valid surveyId.");
            Assert.That(result.ProjectId, Is.EqualTo(projectIdentifier), "Expected project ID to match the supplied surveyId.");
        }

        [Test]
        public async Task GetProjectById_ReturnsProject_WhenValidSurveyGroupIdSupplied()
        {
            var projectId = 2;
            SetUpDataForTests();
            // Act
            var projectIdentifier = new ProjectIdentifier(ProjectType.AllVueSurveyGroup, projectId);
            var result = await _service.GetProjectById(AuthCompanyId, projectIdentifier, _token);
            // Assert
            Assert.That(result, Is.Not.Null, "Expected a project to be returned for valid surveyGroupId.");
            Assert.That(result.ProjectId, Is.EqualTo(projectIdentifier), "Expected project ID to match the supplied surveyGroupId.");
        }

        [TestCase("", false,0, AccessStatus.None)]
        [TestCase("1", false, 0, AccessStatus.AllUsers)]
        [TestCase("1", false, 3, AccessStatus.Mixed)]
        [TestCase("", false, 3, AccessStatus.Restricted)]
        [TestCase("", true, 1, AccessStatus.AllUsers)]
        [TestCase("", true, 3, AccessStatus.Mixed)]
        public async Task GetProjectById_ReturnsProjectWithCorrectProperties_GivenSharedProjectsAndDataGroups(string sharedProjects, bool sharedViaDataGroup, int dataGroupCount, AccessStatus expectedAccessStatus)
        {
            var isShared = sharedProjects.Length > 0;
            // Arrange
            SetUpDataForTests();
            _allVueRuleRepository.GetByCompanyAndProjectId(Arg.Any<string>(), Arg.Any<ProjectOrProduct>(), _token).Returns(CreateAllVueRules(dataGroupCount, sharedViaDataGroup));
            _authApiClient.GetSharedProjects(Arg.Any<IEnumerable<string>>(), _token)
                .Returns(sharedProjects.Split(","));
            // Act
            var result = await _service.GetProjectById(AuthCompanyId, new ProjectIdentifier(ProjectType.AllVueSurvey, ProjectId), _token);
            // Assert
            Assert.That(result, Is.Not.Null, "Expected a project to be returned for valid projectId.");
            Assert.That(result.DataGroupCount, Is.EqualTo(dataGroupCount), "Expected DataGroupCount to equal the number of data groups.");
            Assert.That(result.UserAccess, Is.EqualTo(expectedAccessStatus), $"Expected access status to be {expectedAccessStatus}");
            Assert.That(result.IsShared, Is.EqualTo(isShared), $"Expected IsShared to be {isShared}");
        }

        [TestCase(ProjectType.AllVueSurvey, 1, true, false)]
        [TestCase(ProjectType.AllVueSurvey, 1, false, true)]
        [TestCase(ProjectType.AllVueSurveyGroup, 1, true, false)]
        [TestCase(ProjectType.AllVueSurveyGroup, 1, false, true)]
        public async Task SetProjectSharedStatus_SetsSharedStatus_OnlyCallsAuthOnNotSharing(ProjectType projectType, int projectId, bool isShared, bool expectedResult)
        {
            // Arrange
            SetUpDataForTests();

            bool? sharedSetting = null;

            // Capture the value when SetProjectShared is called
            _authApiClient
                .When(x => x.SetProjectShared(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
                .Do(callInfo => {
                    sharedSetting = callInfo.Arg<bool>();
                });

            // Act
            await _service.SetProjectSharedStatus(_company, new ProjectIdentifier(projectType, projectId), isShared, _token);

            // Assert
            Assert.That(sharedSetting.HasValue, Is.EqualTo(expectedResult), $"Expected result to be {expectedResult} for projectId {projectId} and isShared {isShared}.");
        }

        [TestCase(ProjectType.AllVueSurvey, -1)]
        [TestCase(ProjectType.AllVueSurveyGroup, -1)]
        [TestCase(ProjectType.Unknown, 1)]
        public async Task SetProjectSharedStatus_ThrowsError_WhenCalledWithInvalidValues(ProjectType projectType, int projectId)
        {
            SetUpDataForTests();
            bool? sharedSetting = null;

            // Capture the value when SetProjectShared is called
            _authApiClient
                .When(x => x.SetProjectShared(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
                .Do(callInfo => {
                    sharedSetting = callInfo.Arg<bool>();
                });

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.SetProjectSharedStatus(_company, new ProjectIdentifier(projectType, projectId), true, _token)
            );
        }

        [Test]
        public async Task GetProjectVariables_ThrowsError_WhenCalledWithInvalidProjectId()
        {
            SetUpDataForTests();
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.GetProjectVariablesAvailable("companyId", new ProjectIdentifier(ProjectType.Unknown, 1), _token);
            }, "Expected an ArgumentException when called with an invalid projectId.");
        }

        [TestCase(1)]
        [TestCase(0)]
        public async Task GetProjectVariables_ReturnsEmptyList_WhenApiResponseOrMetricListIsEmpty(int howManyApiQuestions)
        {
            SetUpDataForTests();
            var projectId = new ProjectIdentifier(ProjectType.AllVueSurvey, 1);
            var questions = CreateQuestionList(howManyApiQuestions);
            _brandVueApiClient
                .GetProjectQuestionsAvailableAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<CancellationToken>()).Returns(new QuestionsAvailable(new List<SurveySet>(), questions));
            _variableService.GetMetricsForProject(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new List<MetricConfiguration>());
            var result = await _service.GetProjectVariablesAvailable("companyId", projectId, _token);
            Assert.That(result.UnionOfQuestions, Is.Empty, "Expected an empty list when no variables are found for the project.");
        }

        [TestCase(2, 2, 2)]
        [TestCase(1, 2, 1)]
        [TestCase(4, 2, 2)]
        public async Task GetProjectVariables_ReturnsCorrectNumberOfVariables_WhenApiResponseAndMetricListAreNotEmpty(int howManyApiQuestions, int howManyMetrics, int expectedListSize)
        {
            SetUpDataForTests();
            var projectId = new ProjectIdentifier(ProjectType.AllVueSurvey, 1);
            var apiQuestions = CreateQuestionList(howManyApiQuestions);
            var metrics = CreateMetricConfigurationList(howManyMetrics);
            _brandVueApiClient
                .GetProjectQuestionsAvailableAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<CancellationToken>()).Returns(new QuestionsAvailable(new List<SurveySet>(), apiQuestions));
            _variableService.GetMetricsForProject(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(metrics);
            var result = await _service.GetProjectVariablesAvailable("companyId", projectId, _token);
            Assert.That(result.UnionOfQuestions, Has.Count.EqualTo(expectedListSize), $"Expected the result length to be {expectedListSize}");
        }

        [TestCase(3, 1)]
        public async Task GetProjectVariables_ReturnsCorrectNumberOfVariables_WhenFiltered(int howManyQuestions, int expectedListSize)
        {
            var (s,s1) = SetUpDataForTests();
            var projectId = new ProjectIdentifier(ProjectType.AllVueSurvey, 2);
            var apiQuestions = CreateQuestionList(howManyQuestions);
            var metrics = CreateMetricConfigurationList(howManyQuestions);
            _brandVueApiClient
                .GetProjectQuestionsAvailableAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<CancellationToken>()).Returns(new QuestionsAvailable(new List<SurveySet>(), apiQuestions));
            _variableService.GetMetricsForProject(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(metrics);

            var result = await _service.GetProjectVariablesAvailable("companyId", projectId, _token);

            Assert.That(result.UnionOfQuestions, Has.Count.EqualTo(expectedListSize), $"Expected the result length to be {expectedListSize}");
        }
        [TestCase(2, 2, 100, 101)]
        [TestCase(2, 2, 200, 201)]
        public async Task GetProjectVariables_ReturnsVariablesWithCorrectIds_WhenApiResponseAndMetricListAreNotEmpty(int howManyApiQuestions, int howManyMetrics, int variableIdOffset, int expectedFirstId)
        {
            SetUpDataForTests();
            var projectId = new ProjectIdentifier(ProjectType.AllVueSurvey, 1);
            var apiQuestions = CreateQuestionList(howManyApiQuestions);
            var metrics = CreateMetricConfigurationList(howManyMetrics, variableIdOffset);
            _brandVueApiClient
                .GetProjectQuestionsAvailableAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<CancellationToken>()).Returns(new QuestionsAvailable(new List<SurveySet>(), apiQuestions));
            _variableService.GetMetricsForProject(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(metrics);
            var result = await _service.GetProjectVariablesAvailable("companyId", projectId, _token);
            Assert.That(result.UnionOfQuestions[0].Id, Is.EqualTo(expectedFirstId), $"Expected the first variable ID to be {expectedFirstId}");
        }

        [TestCase("1", ProjectType.AllVueSurvey, 1)]
        [TestCase("group1", ProjectType.AllVueSurveyGroup, 1)]
        [TestCase("invalid", ProjectType.Unknown, 0)]
        public void AuthProjectToProjectIdentifier_ReturnsCorrectProjectIdentifier(string projectName, ProjectType expectedType, int expectedId)
        {
            var surveys = CreateSurveyDbSet(3);
            var surveyGroups = CreateSurveyGroupDbSet(2, surveys);
            _answersDbContext.Surveys.AddRange(surveys);
            _answersDbContext.SurveyGroups.AddRange(surveyGroups);
            _answersDbContext.SaveChanges();

            var result = _service.AuthProjectToProjectIdentifier(projectName);
            Assert.That(result.Type, Is.EqualTo(expectedType), $"Expected project type to be {expectedType}");
            Assert.That(result.Id, Is.EqualTo(expectedId), $"Expected project ID to be {expectedId}");
        }

        [TestCase("invalid", "", ProjectType.Unknown, 0)]
        [TestCase("survey", "invalid", ProjectType.Unknown, 0)]
        [TestCase("brandvue", "1", ProjectType.BrandVue, 1)]
        [TestCase("survey", "1", ProjectType.AllVueSurvey, 1)]
        [TestCase("survey", "group1", ProjectType.AllVueSurveyGroup, 1)]
        public void AuthShortCodeAndProjectToProjectIdentifier_ReturnsCorrectProjectIdentifier(string shortCode, string projectName, ProjectType expectedType, int expectedId)
        {
            var surveys = CreateSurveyDbSet(3);
            var surveyGroups = CreateSurveyGroupDbSet(2, surveys);
            _answersDbContext.Surveys.AddRange(surveys);
            _answersDbContext.SurveyGroups.AddRange(surveyGroups);
            _answersDbContext.SaveChanges();

            var result = _service.AuthShortCodeAndProjectToProjectIdentifier(shortCode, projectName);
            Assert.That(result.Type, Is.EqualTo(expectedType), $"Expected project type to be {expectedType}");
            Assert.That(result.Id, Is.EqualTo(expectedId), $"Expected project ID to be {expectedId}");
        }
    }
}