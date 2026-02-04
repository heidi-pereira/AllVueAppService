using System.Collections.Generic;
using Aspose.Cells;
using DashboardBuilder;
using DashboardBuilder.AsposeHelper;
using DashboardBuilder.Core;
using NSubstitute;
using NUnit.Framework;

namespace BrandVueBuilder.Tests
{
    public class BrandVueProductSettingsTests
    {
        [Test]
        public void ShouldDelegateToMapSettings()
        {
            const string expectedShortCode = "TechTest";
            IReadOnlyList<string> expectedSubsetIds = new[] {"UK", "US"};
            
            var mapSettings = Substitute.For<IMapSettings>();
            var mapFile = CreateTestMapFile(expectedSubsetIds);
            mapSettings.Map.Returns(mapFile);
            
            mapSettings.ShortCode.Returns(expectedShortCode);

            var productSettings = BrandVueProductSettings.FromMapSettings(mapSettings);
            
            Assert.That(productSettings.Map, Is.EqualTo(mapFile));
            Assert.That(productSettings.ShortCode, Is.EqualTo(expectedShortCode));
            Assert.That(productSettings.SubsetIds, Is.EquivalentTo(expectedSubsetIds));
        }

        private static Workbook CreateTestMapFile(IReadOnlyList<string> subsetIds)
        {
            var mapFile = AsposeCellsHelper.StartWorkbook();
            
            var subsetWorksheet = mapFile.Worksheets.Add("SubsetsIdOnly");
            subsetWorksheet.Cells.ImportArray(new[,] {{"Id"}}, 0, 0);
            for (var i = 0; i < subsetIds.Count; i++)
            {
                subsetWorksheet.Cells.ImportArray(new[,] { { subsetIds[i] } }, i + 1, 0);
            }

            return mapFile;
        }

    }
}