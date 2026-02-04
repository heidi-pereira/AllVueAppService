using System.Security.Claims;
using Newtonsoft.Json;
using NSubstitute;
using Vue.Common.Extensions;

namespace Test.Vue.Common.Extensions
{
    [TestFixture]
    public class ClaimsExtensionsTest
    {
        private const string TestClaimType = "testClaim";
        private const string ProductKey = "product1";
        private IEnumerable<Claim> _claims;

        [SetUp]
        public void SetUp()
        {
            _claims = Substitute.For<IEnumerable<Claim>>();
        }

        [Test]
        public void GetClaimValue_ShouldReturnClaimValue_WhenClaimExists()
        {
            var claimType = TestClaimType;
            var claimValue = "testValue";
            var claimsList = new List<Claim> { new Claim(claimType, claimValue) };
            _claims = claimsList;

            var result = _claims.GetClaimValue(claimType);

            Assert.That(result, Is.EqualTo(claimValue));
        }

        [Test]
        public void GetClaimValue_ShouldReturnEmptyString_WhenClaimDoesNotExist()
        {
            var claimType = TestClaimType;

            var result = _claims.GetClaimValue(claimType);

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetNullableClaimValue_ShouldReturnClaimValue_WhenClaimExists()
        {
            var claimType = TestClaimType;
            var claimValue = "123";
            var claimsList = new List<Claim> { new Claim(claimType, claimValue) };
            _claims = claimsList;

            var result = _claims.GetNullableClaimValue<int>(claimType);

            Assert.That(result, Is.EqualTo(123));
        }

        [Test]
        public void GetNullableClaimValue_ShouldReturnNull_WhenClaimDoesNotExist()
        {
            var claimType = TestClaimType;

            var result = _claims.GetNullableClaimValue<int>(claimType);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetClaimValue_Generic_ShouldReturnClaimValue_WhenClaimExists()
        {
            var claimType = TestClaimType;
            const string name = "test";
            var claimValue = JsonConvert.SerializeObject(new { Name = name });
            var claimsList = new List<Claim> { new(claimType, claimValue) };
            _claims = claimsList;

            var result = _claims.GetClaimValue<dynamic>(claimType);

            Assert.That(name, Is.EqualTo(result.Name.ToString()));
        }

        [Test]
        public void GetClaimValue_Generic_ShouldThrowInvalidOperationException_WhenClaimDoesNotExist()
        {
            var claimType = TestClaimType;

            Assert.Throws<InvalidOperationException>(() => _claims.GetClaimValue<dynamic>(claimType));
        }
    }
}