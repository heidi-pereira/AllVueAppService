using System.Security.Claims;
using Vue.Common.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vue.Common.Auth.Permissions;
using Vue.Common.Constants.Constants;

namespace Test.Vue.Common.Auth
{
    public class UserFeaturePermissionsServiceTests
    {
        private readonly ILogger<UserFeaturePermissionsService> _logger = Substitute.For<ILogger<UserFeaturePermissionsService>>();
        private UserContext InitializeUserContext(ClaimsPrincipal user)
        {
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = user });
            return new UserContext(httpContextAccessor);
        }

        [Test]
        public void FeaturePermissions_ShouldReturnEmptyList_WhenUserIdIsNull()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity()); // No UserId claim
            var userContext = InitializeUserContext(user);
            var userPermissionsService = new UserFeaturePermissionsService(userContext, null, _logger);

            // Act
            var permissions = userPermissionsService.FeaturePermissions;

            // Assert
            Assert.That(permissions, Is.Empty);
        }

        [Test]
        public void FeaturePermissions_ShouldReturnEmptyList_WhenUserIdIsEmpty()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.UserId, string.Empty)
            }));
            var userContext = InitializeUserContext(user);
            var userPermissionsService = new UserFeaturePermissionsService(userContext, null, _logger);

            // Act
            var permissions = userPermissionsService.FeaturePermissions;

            // Assert
            Assert.That(permissions, Is.Empty);
        }

        [Test]
        public void FeaturePermissions_ShouldReturnEmptyList_WhenPermissionServiceIsNull()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.UserId, "testUser")
            }));
            var httpContext = new DefaultHttpContext { User = user };
            // ServiceProvider returns null for IPermissionService
            httpContext.RequestServices = Substitute.For<IServiceProvider>();

            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(httpContext);
            
            var userContext = new UserContext(httpContextAccessor);
            var userPermissionsService = new UserFeaturePermissionsService(userContext, null, _logger);

            // Act
            var permissions = userPermissionsService.FeaturePermissions;

            // Assert
            Assert.That(permissions, Is.Empty);
        }

        [Test]
        public void FeaturePermissions_ShouldReturnEmptyList_WhenPermissionServiceThrowsException()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.UserId, "testUser")
            }));

            var permissionService = Substitute.For<IPermissionService>();
            permissionService.GetAllUserFeaturePermissionsAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromException<IReadOnlyCollection<IPermissionFeatureOptionWithCode>>(
                    new InvalidOperationException("Service unavailable")));

            var httpContext = new DefaultHttpContext { User = user };
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(httpContext);
            
            var userContext = new UserContext(httpContextAccessor);
            var userPermissionsService = new UserFeaturePermissionsService(userContext, permissionService, _logger);

            // Act
            var permissions = userPermissionsService.FeaturePermissions;

            // Assert
            Assert.That(permissions, Is.Empty);
        }

        [Test]
        public void FeaturePermissions_ShouldReturnMappedPermissions_WhenPermissionServiceReturnsData()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.UserId, "testUser")
            }));

            var servicePermissions = new List<IPermissionFeatureOptionWithCode>
            {
                new PermissionFeatureOptionWithCode(3, "Variables Edit", PermissionFeaturesOptions.VariablesEdit),
                new PermissionFeatureOptionWithCode(2, "Variables Create", PermissionFeaturesOptions.VariablesCreate)
            };

            var permissionService = Substitute.For<IPermissionService>();
            permissionService.GetAllUserFeaturePermissionsAsync("testUser", Arg.Any<string>())
                .Returns(Task.FromResult<IReadOnlyCollection<IPermissionFeatureOptionWithCode>>(servicePermissions));

            var httpContext = new DefaultHttpContext { User = user };
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(httpContext);
            
            var userContext = new UserContext(httpContextAccessor);
            var userPermissionsService = new UserFeaturePermissionsService(userContext, permissionService, _logger);

            // Act
            var permissions = userPermissionsService.FeaturePermissions;

            // Assert
            Assert.That(permissions, Has.Count.EqualTo(2));
            Assert.That(permissions.First().Id, Is.EqualTo(3));
            Assert.That(permissions.First().Name, Is.EqualTo("Variables Edit"));
            Assert.That(permissions.First().Code, Is.EqualTo(PermissionFeaturesOptions.VariablesEdit));
        }

        [Test]
        public void FeaturePermissions_ShouldReturnEmptyList_WhenPermissionServiceReturnsNull()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.UserId, "testUser")
            }));

            var permissionService = Substitute.For<IPermissionService>();
            permissionService.GetAllUserFeaturePermissionsAsync("testUser", "UserRole")
                .Returns(Task.FromResult<IReadOnlyCollection<IPermissionFeatureOptionWithCode>>(null!));

            var httpContext = new DefaultHttpContext { User = user };
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(httpContext);
            
            var userContext = new UserContext(httpContextAccessor);
            var userPermissionsService = new UserFeaturePermissionsService(userContext, permissionService, _logger);

            // Act
            var permissions = userPermissionsService.FeaturePermissions;

            // Assert
            Assert.That(permissions, Is.Empty);
        }

        [Test]
        public void FeaturePermissions_ShouldCacheResults_WhenCalledMultipleTimes()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.UserId, "testUser"),
                new(RequiredClaims.Role, "UserRole")
            }));

            var servicePermissions = new List<IPermissionFeatureOptionWithCode>
            {
                new PermissionFeatureOptionWithCode(3, "Variables Edit", PermissionFeaturesOptions.VariablesEdit)
            };

            var permissionService = Substitute.For<IPermissionService>();
            permissionService.GetAllUserFeaturePermissionsAsync("testUser", Arg.Any<string>())
                .Returns(Task.FromResult<IReadOnlyCollection<IPermissionFeatureOptionWithCode>>(servicePermissions));

            var httpContext = new DefaultHttpContext { User = user };
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(httpContext);
            
            var userContext = new UserContext(httpContextAccessor);
            var userPermissionsService = new UserFeaturePermissionsService(userContext, permissionService, _logger);

            // Act
            var permissions1 = userPermissionsService.FeaturePermissions;
            var permissions2 = userPermissionsService.FeaturePermissions;

            // Assert
            Assert.That(permissions1, Is.SameAs(permissions2));
            // Verify the service was called only once
            permissionService.Received(1).GetAllUserFeaturePermissionsAsync("testUser", "UserRole");
        }

        [Test]
        public void FeaturePermissions_ShouldHandleConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.UserId, "testUser"),
                new(RequiredClaims.Role, "UserRole")
            }));

            var servicePermissions = new List<IPermissionFeatureOptionWithCode>
            {
                new PermissionFeatureOptionWithCode(3, "Variables Edit", PermissionFeaturesOptions.VariablesEdit)
            };

            var permissionService = Substitute.For<IPermissionService>();
            permissionService.GetAllUserFeaturePermissionsAsync("testUser", Arg.Any<string>())
                .Returns(Task.FromResult<IReadOnlyCollection<IPermissionFeatureOptionWithCode>>(servicePermissions));

            var httpContext = new DefaultHttpContext { User = user };
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(httpContext);
            
            var userContext = new UserContext(httpContextAccessor);
            var userPermissionsService = new UserFeaturePermissionsService(userContext, permissionService, _logger);

            // Act - Simulate concurrent access
            var tasks = new List<Task<IReadOnlyCollection<PermissionFeatureOptionWithCode>>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() => userPermissionsService.FeaturePermissions));
            }

            var results = Task.WhenAll(tasks).Result;

            // Assert
            Assert.That(results.Length, Is.EqualTo(10));
            // All results should be the same instance due to caching
            Assert.That(results.All(r => ReferenceEquals(r, results[0])), Is.True);
            // Verify the service was called only once despite concurrent access
            permissionService.Received(1).GetAllUserFeaturePermissionsAsync("testUser", "UserRole");
        }
    }
}
