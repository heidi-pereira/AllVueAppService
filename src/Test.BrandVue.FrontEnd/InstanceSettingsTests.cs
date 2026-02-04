using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Settings;
using NUnit.Framework;
using System;

namespace Test.BrandVue.FrontEnd
{
    public class InstanceSettingsTests
    {
        [Test]
        public void SettingsRepositoryWithNoSettings()
        {
            var instanceSettings = new InstanceSettings(null, false);
            Assert.That(instanceSettings.ForceBrandTypeAsDefault, Is.EqualTo(true));
        }


        [Test]
        public void SettingsRepositoryWithLastSignOffDate()
        {
            var instanceSettings = new InstanceSettings(new DateTimeOffset(2019,12,31,0,0,0, new TimeSpan()), true);

            Assert.That(instanceSettings.LastSignOffDate.HasValue, "the date stored in LastSignOffDate is null, with input \"31/12/2019\"");
            Assert.That(instanceSettings.LastSignOffDate.Value, Is.EqualTo(DateTimeOffset.Parse("31/12/2019")));
        }

    }
}
