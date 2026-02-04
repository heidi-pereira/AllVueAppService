using Newtonsoft.Json;
using NUnit.Framework;
using VueReporting.Models;

namespace VueReportingTests
{
    [TestFixture]
    class SerialisationTests
    {
        [Test]
        public void TestRequired()
        {
            var incompleteBrandset = new {Name = "Test"};

            var json = JsonConvert.SerializeObject(incompleteBrandset);

            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<EntitySet>(json));
        }
    }
}
