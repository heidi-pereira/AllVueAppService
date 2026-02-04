using NSubstitute;
using Vue.Common.Auth.Permissions;
using Microsoft.Extensions.Logging;
using Vue.Common.Constants.Constants;

namespace Test.Vue.Common.Auth.Permissions
{
    public class PermissionServiceTests
    {
        private const string UserId = "user-123";
        private readonly IUserPermissionHttpClient _userPermissionHttpClient = Substitute.For<IUserPermissionHttpClient>();
        private readonly ILoggerFactory _loggerFactory = Substitute.For<ILoggerFactory>();
        private PermissionService _service;

        [SetUp]
        public void SetUp()
        {
            var returnValue = (IReadOnlyCollection<PermissionFeatureOptionDto> ?) null;
            _userPermissionHttpClient.GetUserFeaturePermissionsAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(returnValue);
            _service = new PermissionService(_userPermissionHttpClient, _loggerFactory);
        }

        [TearDown]
        public void TearDown()
        {
            _userPermissionHttpClient.ClearReceivedCalls();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_loggerFactory is IDisposable disposableLoggerFactory)
            {
                disposableLoggerFactory.Dispose();
            }
        }

        [Test]
        public async Task GetAllUserFeaturePermissionsAsync_ReturnsPermissions()
        {
            // Arrange
            var expectedDtos = new List<PermissionFeatureOptionDto>
            {
                new PermissionFeatureOptionDto(Id: 2, Name: "Feature1"),
                new PermissionFeatureOptionDto(Id: 3, Name: "Feature2")
            };

            _userPermissionHttpClient.GetUserFeaturePermissionsAsync(UserId, Arg.Any<string>())
                .Returns(expectedDtos);

            // Act
            var result = await _service.GetAllUserFeaturePermissionsAsync(UserId, "UserRole");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First().Name, Is.EqualTo("Feature1"));
            Assert.That(result.Last().Name, Is.EqualTo("Feature2"));
            Assert.That(result.First().Id, Is.EqualTo(2));
        }

        [TestCase(Roles.User,8)]
        [TestCase(Roles.SystemAdministrator, 14)]
        [TestCase(Roles.Administrator, 14)]
        [TestCase(Roles.ReportViewer, 1)]
        public async Task GetAllUserFeaturePermissionsAsync_ReturnsPermissionsWhenReturnNULL(string userRole, int expectedNumberOfOptions)
        {
            // Act
            var result = await _service.GetAllUserFeaturePermissionsAsync(UserId, userRole);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(expectedNumberOfOptions));
        }

        [Test]
        public async Task GetAllUserFeaturePermissionsAsync_ReturnsEmptyWhenUserIdIsNull()
        {
            // Act
            var result = await _service.GetAllUserFeaturePermissionsAsync(null!, "UserRole");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
            
            // Verify that HTTP client was not called
            await _userPermissionHttpClient.DidNotReceive().GetUserFeaturePermissionsAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task GetAllUserFeaturePermissionsAsync_ReturnsEmptyWhenUserIdIsEmpty()
        {
            // Act
            var result = await _service.GetAllUserFeaturePermissionsAsync("", "UserRole");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
            
            // Verify that HTTP client were called
            await _userPermissionHttpClient.DidNotReceive().GetUserFeaturePermissionsAsync(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
