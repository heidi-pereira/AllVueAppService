using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vue.Common.Auth.Permissions;

namespace Test.Vue.Common.Auth.Permissions
{
    public class UserPermissionHttpClientTests
    {
        private const string TestToken = "test-token";
        private const string ApiBaseUrl = "https://test-api.com";

        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly IApiBaseUrlResolver _apiBaseUrlResolver = Substitute.For<IApiBaseUrlResolver>();
        private readonly ILoggerFactory _loggerFactory = Substitute.For<ILoggerFactory>();
        private readonly HttpMessageHandler _httpMessageHandler = Substitute.For<HttpMessageHandler>();
        private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();

        [SetUp]
        public void SetUp()
        {
            _configuration["UserManagement:Token"].Returns(TestToken);
            _apiBaseUrlResolver.ApiBaseUrl.Returns(ApiBaseUrl);
            _apiBaseUrlResolver.RequireToken.Returns(true);
            _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(_httpMessageHandler));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_loggerFactory is IDisposable disposableLoggerFactory)
            {
                disposableLoggerFactory.Dispose();
            }

            if (_httpMessageHandler is IDisposable disposableHttpMessageHandler)
            {
                disposableHttpMessageHandler.Dispose();
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Calling_ThrowsArgumentNullException_WhenTokenIsRequiredButEmptyOrNull(string? token)
        {
            // Arrange
            _apiBaseUrlResolver.RequireToken.Returns(true);
            _configuration["UserManagement:Token"].Returns(token);
            var userPermissionHttpClient = new UserPermissionHttpClient(_httpClientFactory, _configuration,
                _apiBaseUrlResolver,
                _loggerFactory);
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                userPermissionHttpClient.GetUserFeaturePermissionsAsync("userId", "User"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Constructor_DoesNotThrow_WhenTokenIsNotRequired(string? token)
        {
            // Arrange
            _apiBaseUrlResolver.RequireToken.Returns(false);
            _configuration["UserManagement:Token"].Returns((string?)null);
            // Act & Assert
            Assert.DoesNotThrow(() =>
                new UserPermissionHttpClient(_httpClientFactory, _configuration, _apiBaseUrlResolver, _loggerFactory));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenApiBaseUrlResolverIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new UserPermissionHttpClient(_httpClientFactory, _configuration, null!, _loggerFactory));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerFactoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new UserPermissionHttpClient(_httpClientFactory, _configuration, _apiBaseUrlResolver, null!));
        }

        [TestCase(false, "token", "user1", 1)]
        public async Task GetUserFeaturePermissionsAsync_ReturnsExpectList_WhenGivenSettings(bool fallBackIfTokenEmpty,
            string token, string userId, int expectedPermissionCount)
        {
            //Arrange
            _apiBaseUrlResolver.RequireToken.Returns(false);
            _configuration["UserManagement:Token"].Returns(token);

            var httpMessageHandler = new FakeHttpMessageHandler(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("[{\"id\":1,\"name\":\"Permission1\",\"code\":\"FeatureA\"}]")
            });
            _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(httpMessageHandler));

            var client = new UserPermissionHttpClient(_httpClientFactory, _configuration, _apiBaseUrlResolver,
                _loggerFactory);

            //Act
            var results = await client.GetUserFeaturePermissionsAsync(userId, "User");

            //Assert
            Assert.That(results.Count, Is.EqualTo(expectedPermissionCount));
        }

        [TestCase( "", "user1")]
        [TestCase(null, "user1")]
        public async Task GetUserFeaturePermissionsAsync_ReturnsNull(string? token, string userId)
        {
            //Arrange
            _apiBaseUrlResolver.RequireToken.Returns(false);
            _configuration["UserManagement:Token"].Returns(token);

            var httpMessageHandler = new FakeHttpMessageHandler(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("[{\"id\":1,\"name\":\"Permission1\",\"code\":\"FeatureA\"}]")
            });
            _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(httpMessageHandler));

            var client = new UserPermissionHttpClient(_httpClientFactory, _configuration, _apiBaseUrlResolver, _loggerFactory);

            //Act
            var results = await client.GetUserFeaturePermissionsAsync(userId, "User");

            //Assert
            Assert.That(results, Is.Null);
        }

        [TestCase(System.Net.HttpStatusCode.NotFound)]
        [TestCase(System.Net.HttpStatusCode.InternalServerError)]
        public void GetUserFeaturePermissionsAsync_ThrowsAnHttpRequestException_WhenTheResponseCodeIsNotOK(System.Net.HttpStatusCode statusCode)
        {
            //Arrange
            _apiBaseUrlResolver.RequireToken.Returns(false);
            _configuration["UserManagement:Token"].Returns("token");

            var httpMessageHandler = new FakeHttpMessageHandler(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("")
            });
            _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(httpMessageHandler));

            var client = new UserPermissionHttpClient(_httpClientFactory, _configuration, _apiBaseUrlResolver,
                _loggerFactory);

            //Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                var result = await client.GetUserFeaturePermissionsAsync("userId", "User");
            });
        }

    }
}
