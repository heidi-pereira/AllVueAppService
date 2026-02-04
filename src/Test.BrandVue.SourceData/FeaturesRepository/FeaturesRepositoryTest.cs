using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.FeatureToggle;
using System.Threading;

namespace Test.BrandVue.SourceData.FeaturesRepository
{
    [TestFixture]
    public class UserFeaturesRepositoryTest
    {
        public UserFeaturesRepositoryTest() { }

        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public async Task EmptyTest()
        {
            var userFeatureRepostitory =
                new global::BrandVue.EntityFramework.MetaData.UserFeaturesRepository(FeatureRepositoryHelper.CreateMetaDbContextForUserFeatures());
            var result = await userFeatureRepostitory.GetEnabledFeaturesForUserAsync("User", CancellationToken.None);
            Assert.That(result.Count(), Is.EqualTo(0), "Got back data when not expecting any");
        }
    }
}
