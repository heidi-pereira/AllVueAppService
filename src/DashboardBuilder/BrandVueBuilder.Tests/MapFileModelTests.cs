using System.Collections.Generic;
using System.Linq;
using Aspose.Cells;
using DashboardBuilder.AsposeHelper;
using NUnit.Framework;

namespace BrandVueBuilder.Tests
{
    public class MapFileModelTests
    {
        [Test]
        public void ShouldReadEntitiesFromWorkbook()
        {
            var workbook = AsposeCellsHelper.StartWorkbook();
            workbook.Worksheets.Add("SomethingElse");
            
            var brandWorksheet = workbook.Worksheets.Add("BrandEntity");
            var brandEntityData = new[,]
            {
                {"Id", "Name", "Aliases"},
                {"1", "3 Store", ""},
                {"10", "Argos", ""},
                {"193", "Zara Home", ""},
            };
            brandWorksheet.Cells.ImportArray(brandEntityData, 0, 0);

            var productWorksheet = workbook.Worksheets.Add("ProductEntity");
            var productEntityData = new[,]
            {
                {"Id", "Name", "Aliases"},
                {"101", "Food / groceries", ""},
                {"202", "Beds and / or mattresses", ""},
                {"303", "Carpets / flooring", ""},
            };
            productWorksheet.Cells.ImportArray(productEntityData, 0, 0);
            
            var mapFileModel = new MapFileModel(workbook);

            var entities = mapFileModel.Entities;
            
            Assert.That(entities.Count, Is.EqualTo(2), "Should have exactly 2 entities");
            AssertEntityIsCorrect(entities, "Brand", new[] {1, 10, 193});
            AssertEntityIsCorrect(entities, "Product", new[] {101, 202, 303});
        }
        
        [Test]
        public void ShouldReadFieldsFromWorkbook()
        {
            var workbook = AsposeCellsHelper.StartWorkbook();
            
            var fieldWorksheet = workbook.Worksheets.Add("Fields");
            var fieldData = new[,]
            {
                {"Name", "VarCode", "CH1", "CH2", "optValue", "Text", "ScaleFactor", "RelatedField", "StartDate", "Subset", "RoundingType"},
                {"FieldOne", "Field_One", "{Brand}", "1", "{value}", "TextOne", "0.01", "", "", "C|Subset|A", "Floor"},
                {"FieldTwo", "Field_Two", "{Brand}", "1", "{value}", "TextOne", "0.01", "", "", "A|B|C", "Floor"},
            };
            fieldWorksheet.Cells.ImportArray(fieldData, 0, 0);
           
            var mapFileModel = new MapFileModel(workbook);

            var fields = mapFileModel.FieldsForSubset("Subset");
            
            Assert.That(fields.Count, Is.EqualTo(1), "Should only have 1 field");
            var field = fields.Single();
            Assert.That(field.Name, Is.EqualTo("FieldOne"));
            Assert.That(field.varCode, Is.EqualTo("Field_One"));
            Assert.That(field.CH1, Is.EqualTo("{Brand}"));
            Assert.That(field.CH2, Is.EqualTo("1"));
            Assert.That(field.optValue, Is.EqualTo("{value}"));
            Assert.That(field.Text, Is.EqualTo("TextOne"));
            Assert.That(field.ScaleFactor, Is.EqualTo("0.01"));
            Assert.That(field.RoundingType, Is.EqualTo("Floor"));
        }

        [Test]
        public void ShouldReadAllLookups()
        {
            var workbook = AsposeCellsHelper.StartWorkbook();
            
            var postcodeLookupSheet = workbook.Worksheets.Add("PostcodeLookup");
            var postcodeData = new[,]
            {
                {"ID", "Lookup"},
                {"1", "AB"},
                {"2", "AL"},
            };
            postcodeLookupSheet.Cells.ImportArray(postcodeData, 0, 0);
            
            var testLookupSheet = workbook.Worksheets.Add("TestLookup");
            var testLookupData = new[,]
            {
                {"ID", "Lookup"},
                {"1", "This|Or this"},
            };
            testLookupSheet.Cells.ImportArray(testLookupData, 0, 0);
           
            var mapFileModel = new MapFileModel(workbook);

            Assert.That(mapFileModel.Lookups.Count, Is.EqualTo(2), "Should have exactly 2 lookups");
        }
        
        [Test]
        public void ShouldReadLookupData()
        {
            var workbook = AsposeCellsHelper.StartWorkbook();
            
            var postcodeLookupSheet = workbook.Worksheets.Add("PostcodeLookup");
            var postcodeData = new[,]
            {
                {"ID", "Lookup"},
                {"1", "AB"},
                {"2", "AL"},
            };
            postcodeLookupSheet.Cells.ImportArray(postcodeData, 0, 0);
            
            var mapFileModel = new MapFileModel(workbook);

            var lookups = mapFileModel.Lookups;
            
            var postcodeLookup = lookups.SingleOrDefault(l => l.Name == "Postcode");
            Assert.NotNull(postcodeLookup, $"Could not find Postcode lookup");
            Assert.That(postcodeLookup.Data.Count, Is.EqualTo(2), "Incorrect number of data rows");
            Assert.That(postcodeLookup.Data.First().Id, Is.EqualTo(1), "Incorrect ID");
            Assert.That(postcodeLookup.Data.First().LookupValues, Is.EquivalentTo(new[] {"AB"}), "Incorrect lookup values");
        }
        
        [Test]
        public void ShouldReadMultipleLookupData()
        {
            var workbook = AsposeCellsHelper.StartWorkbook();
            
            var testLookupSheet = workbook.Worksheets.Add("TestLookup");
            var testLookupData = new[,]
            {
                {"ID", "Lookup"},
                {"1", "This|Or this"},
            };
            testLookupSheet.Cells.ImportArray(testLookupData, 0, 0);
           
            var mapFileModel = new MapFileModel(workbook);

            var lookups = mapFileModel.Lookups;

            var testLookup = lookups.SingleOrDefault(l => l.Name == "Test");
            Assert.NotNull(testLookup, $"Could not find lookup");
            var firstDataRow = testLookup.Data.SingleOrDefault();
            Assert.NotNull(firstDataRow, "No lookup data found");
            Assert.That(firstDataRow.Id, Is.EqualTo(1), "Incorrect Id");
            Assert.That(firstDataRow.LookupValues, Is.EquivalentTo(new[] {"This", "Or this"}), "Incorrect lookup values");
        }

        private static void AssertEntityIsCorrect(IEnumerable<Entity> entities, string entityType, IEnumerable<int> expectedIds)
        {
            var brandEntity = entities.SingleOrDefault(e => e.Type == entityType);
            Assert.NotNull(brandEntity, $"The {entityType} entity is missing");
            var brandyEntityInstanceIds = brandEntity.Instances.Select(i => i.Id);
            Assert.That(brandyEntityInstanceIds, Is.EquivalentTo(expectedIds), $"Unexpected {entityType} IDs");
        }
    }
}