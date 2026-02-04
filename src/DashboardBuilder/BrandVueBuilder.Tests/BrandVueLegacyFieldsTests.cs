using System;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;
using DashboardMetadataBuilder.MapProcessing.Typed;
using NUnit.Framework;

namespace BrandVueBuilder.Tests
{
    public class BrandVueLegacyFieldsTests
    {
        [Test]
        public void ModelShouldProvideProfileAndBrandTableDefinitions()
        {
            //Setup
            var entityTypes = new[] {"Brand"};
            var fieldsWorksheet = WorksheetBuilder.CreateFieldWorksheet(entityTypes);
            fieldsWorksheet.Cells.ImportArray(new [,]{{"Gender", "Gender", "{value}"}}, 1, 0);
            fieldsWorksheet.Cells.ImportArray(new [,]{{"Age", "Age", "", "", "{value}"}}, 2, 0);
            fieldsWorksheet.Cells.ImportArray(new [,]{{"PositiveBuzz", "Positive_buzz", "{Brand}", "", "{value}"}}, 3, 0);
            fieldsWorksheet.Cells.ImportArray(new [,]{{"NegativeBuzz", "Negative_buzz", "{Brand}", "", "{value}"}}, 4, 0);

            var categoriesWorksheet = WorksheetBuilder.CreateCategoriesWorksheet(fieldsWorksheet.Workbook);
            categoriesWorksheet.Cells.ImportArray(new[,] { { "Gender", "0:Female|1:Male|2:Other", "All"} }, 1, 0);

            //Run the code
            var legacy = new LegacyFieldConverter(fieldsWorksheet.Workbook);
            legacy.ConvertFieldsToBrandAndProfileFields();

            //Validate that it's correct
            var profileFields = new TypedWorksheet<ProfilingFields>(fieldsWorksheet.Workbook).Rows.ToArray();
            Assert.That(profileFields.Count, Is.EqualTo(2), "Wrong number of rows");

            Assert.That(profileFields[0].Name, Is.EqualTo("Gender"));
            Assert.That(profileFields[1].Name, Is.EqualTo("Age"));

            Assert.That(profileFields[0].Categories, Is.EqualTo("0:Female|1:Male|2:Other"));
            Assert.That(profileFields[1].Categories, Is.EqualTo(""));

        }
    }
}