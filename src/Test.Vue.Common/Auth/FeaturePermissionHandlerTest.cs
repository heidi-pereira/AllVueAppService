using BrandVue.EntityFramework;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vue.Common.Auth;
using Vue.Common.Auth.Permissions;
using Vue.Common.Constants.Constants;

namespace Test.Vue.Common.Auth
{
    [TestFixture]
    public class FeaturePermissionHandlerTests
    {
        private ILogger<FeaturePermissionHandler> _logger;

        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger<FeaturePermissionHandler>>();
        }
        private static AuthorizationHandlerContext GetAuthContext(FeaturePermissionRequirement requirement, ClaimsPrincipal? user = null)
        {
            return new AuthorizationHandlerContext(
                new[] { requirement },
                user ?? new ClaimsPrincipal(new ClaimsIdentity()),
                null);
        }

        private IHttpContextAccessor MockHttpContextAccessor(bool hasSingleClient=true, IUserContext? userContext = null, IUserFeaturePermissionsService? userPermissionsService = null)
        {
            var productContext = Substitute.For<IProductContext>();
            productContext.HasSingleClient.Returns(hasSingleClient); // Set up any properties/methods as needed

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IProductContext)).Returns(productContext);
            serviceProvider.GetService(typeof(IUserContext)).Returns(userContext);
            serviceProvider.GetService(typeof(IUserFeaturePermissionsService)).Returns(userPermissionsService);

            var httpContext = Substitute.For<HttpContext>();
            httpContext.RequestServices.Returns(serviceProvider);

            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(httpContext);

            return httpContextAccessor;
        }


        [TestCase(PermissionFeaturesOptions.VariablesCreate)]
        [TestCase(PermissionFeaturesOptions.VariablesEdit)]
        [TestCase(PermissionFeaturesOptions.VariablesDelete)]
        [TestCase(PermissionFeaturesOptions.AnalysisAccess)]
        [TestCase(PermissionFeaturesOptions.DocumentsAccess)]
        [TestCase(PermissionFeaturesOptions.QuotasAccess)]
        [TestCase(PermissionFeaturesOptions.SettingsAccess)]
        [TestCase(PermissionFeaturesOptions.DataAccess)]
        [TestCase(PermissionFeaturesOptions.BreaksAdd)]
        [TestCase(PermissionFeaturesOptions.BreaksEdit)]
        [TestCase(PermissionFeaturesOptions.BreaksDelete)]
        [TestCase(PermissionFeaturesOptions.ReportsAddEdit)]
        [TestCase(PermissionFeaturesOptions.ReportsView)]
        [TestCase(PermissionFeaturesOptions.ReportsDelete)]
        public async Task CheckForSystemAdminPermissions(PermissionFeaturesOptions permissionFeatures)
        {
            await CheckForPermissions(permissionFeatures, Roles.SystemAdministrator, true);
        }

        [TestCase(PermissionFeaturesOptions.VariablesCreate, true)]
        [TestCase(PermissionFeaturesOptions.VariablesEdit, true)]
        [TestCase(PermissionFeaturesOptions.VariablesDelete, true)]
        [TestCase(PermissionFeaturesOptions.AnalysisAccess, true)]
        [TestCase(PermissionFeaturesOptions.DocumentsAccess, true)]
        [TestCase(PermissionFeaturesOptions.QuotasAccess, true)]
        [TestCase(PermissionFeaturesOptions.SettingsAccess, true)]
        [TestCase(PermissionFeaturesOptions.DataAccess, true)]
        [TestCase(PermissionFeaturesOptions.BreaksAdd, true)]
        [TestCase(PermissionFeaturesOptions.BreaksEdit, true)]
        [TestCase(PermissionFeaturesOptions.BreaksDelete, true)]
        [TestCase(PermissionFeaturesOptions.ReportsAddEdit, true)]
        [TestCase(PermissionFeaturesOptions.ReportsView, true)]
        [TestCase(PermissionFeaturesOptions.ReportsDelete, true)]
        public async Task CheckForAdminPermissions(PermissionFeaturesOptions permissionFeatures, bool succeeded)
        {
            await CheckForPermissions(permissionFeatures, Roles.Administrator, succeeded);
        }

        [TestCase(PermissionFeaturesOptions.VariablesCreate, false)]
        [TestCase(PermissionFeaturesOptions.VariablesEdit, false)]
        [TestCase(PermissionFeaturesOptions.VariablesDelete, false)]
        [TestCase(PermissionFeaturesOptions.AnalysisAccess, true)]
        [TestCase(PermissionFeaturesOptions.DocumentsAccess, true)]
        [TestCase(PermissionFeaturesOptions.QuotasAccess, true)]
        [TestCase(PermissionFeaturesOptions.SettingsAccess, false)]
        [TestCase(PermissionFeaturesOptions.DataAccess, true)]
        [TestCase(PermissionFeaturesOptions.BreaksAdd, true)]
        [TestCase(PermissionFeaturesOptions.BreaksEdit, true)]
        [TestCase(PermissionFeaturesOptions.BreaksDelete, true)]
        [TestCase(PermissionFeaturesOptions.ReportsAddEdit, false)]
        [TestCase(PermissionFeaturesOptions.ReportsView, true)]
        [TestCase(PermissionFeaturesOptions.ReportsDelete, false)]
        public async Task CheckForUserPermissions(PermissionFeaturesOptions permissionFeatures, bool succeeded)
        {
            await CheckForPermissions(permissionFeatures, Roles.User, succeeded);
        }

        [TestCase(PermissionFeaturesOptions.VariablesCreate, false)]
        [TestCase(PermissionFeaturesOptions.VariablesEdit, false)]
        [TestCase(PermissionFeaturesOptions.VariablesDelete, false)]
        [TestCase(PermissionFeaturesOptions.AnalysisAccess, false)]
        [TestCase(PermissionFeaturesOptions.DocumentsAccess, false)]
        [TestCase(PermissionFeaturesOptions.QuotasAccess, false)]
        [TestCase(PermissionFeaturesOptions.SettingsAccess, false)]
        [TestCase(PermissionFeaturesOptions.DataAccess, false)]
        [TestCase(PermissionFeaturesOptions.BreaksAdd, false)]
        [TestCase(PermissionFeaturesOptions.BreaksEdit, false)]
        [TestCase(PermissionFeaturesOptions.BreaksDelete, false)]
        [TestCase(PermissionFeaturesOptions.ReportsAddEdit, false)]
        [TestCase(PermissionFeaturesOptions.ReportsView, true)]
        [TestCase(PermissionFeaturesOptions.ReportsDelete, false)]
        public async Task CheckForReportUserPermissions(PermissionFeaturesOptions permissionFeatures, bool succeeded)
        {
            await CheckForPermissions(permissionFeatures, Roles.ReportViewer, succeeded);
        }

        private async Task CheckForPermissions(PermissionFeaturesOptions permissionFeature, string userRole,
            bool succeeded)
        {
            // Arrange
            var requirement = new FeaturePermissionRequirement(permissionFeature);

            ClaimsPrincipal user = Substitute.For<ClaimsPrincipal>();
            user.IsInRole(userRole).Returns(true);
            
            var context = GetAuthContext(requirement, user);

            var userContext = Substitute.For<IUserContext>();
            userContext.UserId.Returns("UserId");
            userContext.Role.Returns(userRole);
            var permissionFeatureOptionDtos = new List<PermissionFeatureOptionDto>();
            if (succeeded)
            {
                permissionFeatureOptionDtos.Add(new PermissionFeatureOptionDto((int)permissionFeature, permissionFeature.ToString()));
            }
            var userPermissionHttpClient = Substitute.For<IUserPermissionHttpClient>();
            userPermissionHttpClient.GetUserFeaturePermissionsAsync(Arg.Any<string>(), userRole)
                .Returns(Task.FromResult<IReadOnlyCollection<PermissionFeatureOptionDto>?>(permissionFeatureOptionDtos));
            var userPermissionsService = new PermissionService(userPermissionHttpClient,Substitute.For<ILoggerFactory>());
            var userFeaturePermissionsService = new UserFeaturePermissionsService(userContext, userPermissionsService, Substitute.For<ILogger<UserFeaturePermissionsService>>());
            var handler = new FeaturePermissionHandler(MockHttpContextAccessor(true, userContext, userFeaturePermissionsService), _logger);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.EqualTo(succeeded), $"{userRole}, {permissionFeature} Expected Access: {(succeeded ? "Yes": "No")}");
        }

        [TestCase(Roles.SystemAdministrator, true)]
        [TestCase(Roles.Administrator, false)]
        [TestCase(Roles.User, false)]
        [TestCase(Roles.ReportViewer, false)]
        [TestCase(Roles.TrialUser, false)]

        public async Task When_FeatureFlag_DisabledForBrandVue(string userRole, bool succeeded)
        {
            // Arrange
            var requirement = new FeaturePermissionRequirement( PermissionFeaturesOptions.VariablesEdit);

            ClaimsPrincipal user = Substitute.For<ClaimsPrincipal>();
            user.IsInRole(userRole).Returns(true);

            var context = GetAuthContext(requirement, user);

            var handler = new FeaturePermissionHandler(MockHttpContextAccessor(false), _logger);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.EqualTo(succeeded));
        }


        [Test]
        public async Task Succeeds_When_FeatureFlagEnabled_And_UserHasRequiredPermission()
        {
            // Arrange
            var requirement = new FeaturePermissionRequirement(PermissionFeaturesOptions.VariablesEdit);
            var context = GetAuthContext(requirement);

            var userPermissionsService = ArrangeUserPermissionsServiceWithFeaturesPermission([PermissionFeaturesOptions.VariablesEdit]);

            var handler = new FeaturePermissionHandler(MockHttpContextAccessor(userPermissionsService: userPermissionsService), _logger);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.True);
        }

        private static IUserFeaturePermissionsService ArrangeUserPermissionsServiceWithFeaturesPermission(IEnumerable<PermissionFeaturesOptions> permissionFeaturesOptions)
        {
            var userPermissionsService = Substitute.For<IUserFeaturePermissionsService>();
            var id = 1;
            var permissionFeatureOptionWithCodes = permissionFeaturesOptions
                .Select(x => new PermissionFeatureOptionWithCode(id++, Guid.NewGuid().ToString("D"), x)).ToList();

            userPermissionsService.GetFeaturePermissionsAsync().Returns(Task.FromResult<IReadOnlyCollection<PermissionFeatureOptionWithCode>>(permissionFeatureOptionWithCodes));
            userPermissionsService.FeaturePermissions.Returns(permissionFeatureOptionWithCodes);
            return userPermissionsService;
        }

        [Test]
        public async Task Fails_When_FeatureFlagEnabled_And_UserLacksRequiredPermission()
        {
            // Arrange
            var requirement = new FeaturePermissionRequirement(PermissionFeaturesOptions.BreaksAdd);
            var context = GetAuthContext(requirement);

            var userPermissionsService = ArrangeUserPermissionsServiceWithFeaturesPermission([PermissionFeaturesOptions.VariablesEdit]);

            var handler = new FeaturePermissionHandler(MockHttpContextAccessor(userPermissionsService: userPermissionsService), _logger);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.False);
            Assert.That(context.HasFailed, Is.True);
        }

        [Test]
        public async Task Succeeds_When_FeatureFlagEnabled_And_UserHasAnyRequiredPermission()
        {
            // Arrange
            var requirement = new FeaturePermissionRequirement(PermissionFeaturesOptions.VariablesEdit, PermissionFeaturesOptions.VariablesCreate);
            var context = GetAuthContext(requirement);
            var userPermissionsService = ArrangeUserPermissionsServiceWithFeaturesPermission([PermissionFeaturesOptions.VariablesEdit]);

            var handler = new FeaturePermissionHandler(MockHttpContextAccessor(userPermissionsService: userPermissionsService), _logger);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.True);
        }

        [Test]
        public async Task Fails_When_FeatureFlagEnabled_And_UserContextIsNull()
        {
            // Arrange
            var requirement = new FeaturePermissionRequirement(PermissionFeaturesOptions.VariablesEdit);
            var context = GetAuthContext(requirement);

            var handler = new FeaturePermissionHandler(MockHttpContextAccessor(), _logger);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.False);
            Assert.That(context.HasFailed, Is.True);
        }

        [Test]
        public async Task Fails_When_FeatureFlagEnabled_And_FeaturePermissionsIsNull()
        {
            // Arrange
            var requirement = new FeaturePermissionRequirement(PermissionFeaturesOptions.VariablesEdit);
            var context = GetAuthContext(requirement);

            var userPermissionsService = Substitute.For<IUserFeaturePermissionsService>();
            var permissionFeatureOptionWithCodes = (IReadOnlyCollection<PermissionFeatureOptionWithCode>?)null;
            userPermissionsService.FeaturePermissions.Returns(permissionFeatureOptionWithCodes);
            userPermissionsService.GetFeaturePermissionsAsync().Returns(Task.FromResult<IReadOnlyCollection<PermissionFeatureOptionWithCode>>(permissionFeatureOptionWithCodes));

            var handler = new FeaturePermissionHandler(MockHttpContextAccessor(userPermissionsService: userPermissionsService), _logger);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.False);
            Assert.That(context.HasFailed, Is.True);
        }
    }
}
