using System.Security.Claims;
using Vue.Common.Constants;
using Vue.Common.Constants.Constants;
using Vue.Common.Auth;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Test.Vue.Common.Auth
{
    [TestFixture]
    public partial class UserContextTest
    {
        private ClaimsPrincipal _user;
        private UserContext _userContext;

        private const string TestProductName = "testProduct";
        private const string TestCompanyName = "testCompany";
        private const string UserId = "testUserId";
        private const string UserName = "testUser";
        private const string FirstName = "Test";
        private const string LastName = "User";
        private const string CompanyShortCode = "testCompanyShortCode";

        [SetUp]
        public void SetUp()
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.UserId, UserId),
                new(RequiredClaims.IdentityProvider, AuthConstants.AuthServerIdentityProvider),
                new(RequiredClaims.Role, Roles.SystemAdministrator),
                new(RequiredClaims.Username, UserName),
                new(RequiredClaims.CurrentCompanyShortCode, TestCompanyName),
                new(RequiredClaims.Products, JsonSerializer.Serialize(new[] { TestProductName })),
                new(RequiredClaims.FirstName, FirstName),
                new(RequiredClaims.LastName, LastName),
            }));
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = _user });
            _userContext = new UserContext(httpContextAccessor);
        }

        private static IEnumerable<TestCaseData> UserContextTestCases()
        {
            yield return new TestCaseData(nameof(UserContext.UserId), UserId);
            yield return new TestCaseData(nameof(UserContext.IsThirdPartyLoginAuth), false);
            yield return new TestCaseData(nameof(UserContext.IsAdministrator), true);
            yield return new TestCaseData(nameof(UserContext.IsSystemAdministrator), true);
            yield return new TestCaseData(nameof(UserContext.IsReportViewer), false);
            yield return new TestCaseData(nameof(UserContext.IsTrialUser), false);
            yield return new TestCaseData(nameof(UserContext.CanEditMetricAbouts), false);
            yield return new TestCaseData(nameof(UserContext.TrialEndDate), null);
            yield return new TestCaseData(nameof(UserContext.UserName), UserName);
            yield return new TestCaseData(nameof(UserContext.Role), Roles.SystemAdministrator);
            yield return new TestCaseData(nameof(UserContext.UserOrganisation), TestCompanyName);
            yield return new TestCaseData(nameof(UserContext.SecurityGroups), Enumerable.Empty<object>());
            yield return new TestCaseData(nameof(UserContext.AuthCompany), TestCompanyName);
            yield return new TestCaseData(nameof(UserContext.Products), new[] { TestProductName });
            yield return new TestCaseData(nameof(UserContext.FirstName), FirstName);
            yield return new TestCaseData(nameof(UserContext.LastName), LastName);
        }

        [Test]
        [TestCaseSource(nameof(UserContextTestCases))]
        public void UserContext_ShouldReturnExpectedValue(string propertyName, object expectedValue)
        {
            var property = typeof(UserContext).GetProperty(propertyName);
            Assert.NotNull(property, $"Property '{propertyName}' does not exist on UserContext.");
            var actualValue = property.GetValue(_userContext);
            Assert.That(actualValue, Is.EqualTo(expectedValue));
        }

        [Test]
        public void AccountName_ShouldReturnCorrectValue_WhenCompanyIsNotInOrgsAllowingExternalCompanyClaim()
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.CurrentCompanyShortCode, TestCompanyName)
            }));
            Assert.That(_userContext.AccountName, Is.EqualTo(TestCompanyName));
        }

        [Test]
        public void SecurityGroups_ShouldReturnArrayOfValues_WhenUserPartOfMultipleSecurityGroups()
        {
            string expectedGroups = JsonSerializer.Serialize(new[] { "Group1", "Group2" });
            _user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(OptionalClaims.Groups, expectedGroups)
            }));
            _userContext = InitializeUserContext(_user);
        
            Assert.That(JsonSerializer.Serialize(_userContext.SecurityGroups), Is.EqualTo(expectedGroups));
        }

        [Test]
        public void AccountName_ShouldReturnCorrectValue_WhenCompanyIsInOrgsAllowingExternalCompanyClaim_AndExternalCompanyIsNotNullOrEmpty()
        {
            // Arrange
            var externalCompany = "externalCompany";
            _user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.CurrentCompanyShortCode, externalCompany),
                new(OptionalClaims.ExternalCompany, externalCompany)
            }));
            _userContext = InitializeUserContext(_user);

            // Assert
            Assert.That(_userContext.AccountName, Is.EqualTo(externalCompany));
        }

        [Test]
        public void AccountName_ShouldReturnCorrectValue_WhenCompanyIsInOrgsAllowingExternalCompanyClaim_AndExternalCompanyIsNullOrEmpty()
        {
            const string companyShortCode = "wgsn";
            _user = new ClaimsPrincipal(new ClaimsIdentity(
            new List<Claim>
            {
                new(RequiredClaims.CurrentCompanyShortCode, companyShortCode),
                new(OptionalClaims.ExternalCompany, string.Empty)
            }));
            _userContext = InitializeUserContext(_user);

            Assert.That(_userContext.AccountName, Is.EqualTo(companyShortCode));
        }

        [Test]
        public void IsInSavantaRequestScope_ShouldReturnTrue_WhenAuthCompanyEqualsSavantaCompany()
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(RequiredClaims.CurrentCompanyShortCode, AuthConstants.SavantaCompany)
            }));
            _userContext = InitializeUserContext(_user);

            Assert.That(_userContext.IsInSavantaRequestScope, Is.True);
        }

        [Test]
        public void IsInSavantaRequestScope_ShouldReturnFalse_WhenAuthCompanyDoesNotEqualSavantaCompany()
        {  
            Assert.That(_userContext.IsInSavantaRequestScope, Is.False);
        }

        [Test]
        public void UserCompanyShortCode_ShouldReturnCorrectValue()
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(OptionalClaims.UserCompanyShortCode, CompanyShortCode)
            }));
           _userContext = InitializeUserContext(_user);

            Assert.That(_userContext.UserCompanyShortCode, Is.EqualTo(CompanyShortCode));
        }

        [Test]
        public void IsAuthorizedSavantaUser_ShouldReturnTrue_WhenUserCompanyShortCodeEqualsSavantaCompany()
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new(OptionalClaims.UserCompanyShortCode, AuthConstants.SavantaCompany)
            }));
            _userContext = InitializeUserContext(_user);

            Assert.That(_userContext.IsAuthorizedSavantaUser, Is.True);
        }

        [Test]
        public void IsAuthorizedSavantaUser_ShouldReturnFalse_WhenUserCompanyShortCodeDoesNotEqualSavantaCompany()
        {
            Assert.That(_userContext.IsAuthorizedSavantaUser, Is.False);
        }

        private UserContext InitializeUserContext(ClaimsPrincipal user)
        {
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = user });
            return new UserContext(httpContextAccessor);
        }
    }
}