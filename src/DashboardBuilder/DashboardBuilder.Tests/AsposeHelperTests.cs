using System.Linq;
using DashboardBuilder.AsposeHelper;
using NUnit.Framework;

namespace DashboardBuilder.Tests
{
    [TestFixture]
    public class AsposeHelperTests
    {
        [Test]
        public void IdMapIsFromSelectedColumnToId()
        {
            var sheetToTest = "SecondSheet";
            var keyColumn = "ThirdHeader";
            var workbook = AsposeCellsHelper.StartWorkbook($"FirstSheet|{sheetToTest}");

            var key = "3";
            var idValue = "1";
            var secondSheet = workbook.Worksheets[sheetToTest];
            secondSheet.PopulateRow(0, "FirstHeader", "SecondHeader", keyColumn);
            secondSheet.PopulateRow(1, idValue, "2", key);

            var dictionary = AsposeCellsHelper.GetIdMap(workbook, sheetToTest, keyColumn);
            Assert.That(dictionary.Select(kvp => kvp.Key + ", " + kvp.Value), Is.EquivalentTo(new[] {$"{key}, {idValue}"}));
        }

        [TestCase("A path to a file you want to make smaller by deleting blank rows", Explicit = true)]
        public void FixFile(string path)
        {
            var workbook = AsposeCellsHelper.OpenWorkbook(path);
            foreach (var sheet in workbook.Worksheets)
            {
                sheet.Cells.DeleteBlankColumns();
                sheet.Cells.DeleteBlankRows();
            }
            workbook.Save(path);
        }
    }
}
