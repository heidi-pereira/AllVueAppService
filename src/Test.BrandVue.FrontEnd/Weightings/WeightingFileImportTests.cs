using System.Collections.Generic;
using BrandVue.Controllers.Api;
using BrandVue.SourceData.Weightings;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd.Weightings
{
    internal class WeightingFileImportTests
    {
        [TestCase("All1", WeightingStyle.Interlocked)]
        [TestCase("1All", WeightingStyle.RIM)]
        [TestCase("All", WeightingStyle.ResponseWeighting)]
        [TestCase("All", WeightingStyle.Unknown)]
        [TestCase("All.MyWorld", WeightingStyle.Unknown, Ignore = "Currently not supported")]
        public void SerializeDeserializeEmptyContext(string subset, WeightingStyle style)
        {
            var item = new WeightingImportFile(subset, new List<WeightingFilterInstance>(), style);

            Assert.That(WeightingImportFile.TryParse(item.ToString(), out var checkedItem), Is.True);
            Assert.That(checkedItem.WeightingStyle, Is.EqualTo(style));
            Assert.That(checkedItem.SubsetId, Is.EqualTo(subset));
        }

        [TestCase("Var", 1)]
        [TestCase("Var", null)]
        [TestCase("Var.1", null, Ignore = "Currently not supported")]
        [TestCase("Var+1", null, Ignore = "Currently not supported")]
        [TestCase("Var,1", null, Ignore = "Currently not supported")]
        public void SerializeDeserializeWithContext(string variableName, int?value)
        {
            var context = new List<WeightingFilterInstance>
            {
                new (variableName, value)
            };
            var item = new WeightingImportFile("All", context, WeightingStyle.ResponseWeighting);

            Assert.That(WeightingImportFile.TryParse(item.ToString(), out var checkedItem), Is.True);
            Assert.That(checkedItem.WeightingStyle, Is.EqualTo(item.WeightingStyle));
            Assert.That(checkedItem.SubsetId, Is.EqualTo(item.SubsetId));
            Assert.That(checkedItem.Context, Is.EqualTo(item.Context));
        }

    }
}
