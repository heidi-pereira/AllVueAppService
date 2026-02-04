using System;
using System.IO;
using System.Threading.Tasks;
using BrandVue.Services.Weighting;
using BrandVue.SourceData.Weightings.ResponseLevel;
using Microsoft.Extensions.FileProviders.Physical;
using NUnit.Framework;
using OfficeOpenXml;
using VerifyNUnit;

namespace Test.BrandVue.FrontEnd.Weightings
{
    public class WeightingFileLoaderTests
    {
        private Stream GenerateStream(string sheetName, string[] columnNames, Action<ExcelWorksheet> injectData)
        {
            var fileName = GenerateFile(sheetName, columnNames, injectData);
            var byteArray = File.ReadAllBytes(fileName);
            var stream = new MemoryStream(byteArray);
            File.Delete(fileName);
            return stream;
        }

        private string GenerateFile(string sheetName, string[] columnNames, Action<ExcelWorksheet> injectData)
        {
            var excelPackage = new ExcelPackage();

            var sheet = excelPackage.Workbook.Worksheets.Add(sheetName);
            for (int index = 0; index < columnNames.Length; index++)
            {
                if (columnNames[index] != null)
                {
                    sheet.Cells[1, index + 1].Value = columnNames[index];
                }
            }

            if (injectData != null)
            {
                injectData(sheet);
            }
            var fileName = Path.GetTempFileName() + ".xlsx";

            using (FileStream fs = File.Create(fileName))
            {
                excelPackage.SaveAs(fs);
                fs.Flush();
                fs.Close();
            }
            return fileName;
        }


        [TestCase("Data", "ResponseId", "Weight")]
        [TestCase("Data1", "Response", "Weight")]
        [TestCase("Intro", "Id", "Weight")]
        [TestCase("Foo", "ResponseId", "Weightings")]
        [TestCase("Bar", "Weightings", "ResponseId")]
        public async Task TestValidExcel(string sheetName, string column1, string column2)
        {
            await TestExcelFileSheetAndColumnNames(nameof(TestValidExcel), sheetName, column1, column2, (sheet) =>
            {
                sheet.Cells[2, 1].Value = 1234;
                sheet.Cells[2, 2].Value = 1.0;
            });
        }

        [TestCase("Data", "ResponseId", "Weight")]
        public async Task TestValidExcelWithValidData(string sheetName, string column1, string column2)
        {
            await TestExcelFileSheetAndColumnNames(nameof(TestValidExcelWithValidData), sheetName, column1, column2, (sheet) =>
            {
                for (int rowId = 2; rowId < 10; rowId++)
                {
                    sheet.Cells[rowId, 1].Value = rowId;
                    sheet.Cells[rowId, 2].Value = 1.0;
                }
            });
        }
        [TestCase("Data", "ResponseId", "Weighting")]
        public async Task TestValidExcelWithDuplicateData(string sheetName, string column1, string column2)
        {
            await TestExcelFileSheetAndColumnNames(nameof(TestValidExcelWithDuplicateData), sheetName, column1, column2, (sheet) =>
            {
                for (int rowId = 2; rowId < 12; rowId++)
                {
                    sheet.Cells[rowId, 1].Value = rowId %2 == 0 ? 2345: rowId;
                    sheet.Cells[rowId, 2].Value = 1.0;
                }
            });
        }
        [TestCase("Data", "ResponseId", "Weighting")]
        public async Task TestValidExcelWithDuplicateDataAndDifferentWeightings(string sheetName, string column1, string column2)
        {
            await TestExcelFileSheetAndColumnNames(nameof(TestValidExcelWithDuplicateDataAndDifferentWeightings), sheetName, column1, column2, (sheet) =>
            {
                for (int rowId = 2; rowId < 12; rowId++)
                {
                    sheet.Cells[rowId, 1].Value = rowId % 2 == 0 ? 2345 : rowId;
                    sheet.Cells[rowId, 2].Value = rowId/10.0;
                }
            });
        }

        [TestCase("Data", "ResponseId", "Weighting")]
        public async Task TestValidExcelWithDuplicateDataAndNullData(string sheetName, string column1, string column2)
        {
            await TestExcelFileSheetAndColumnNames(nameof(TestValidExcelWithDuplicateDataAndNullData), sheetName, column1, column2, (sheet) =>
            {
                var rowId = 2;
                sheet.Cells[rowId, 1].Value = 1234;
                sheet.Cells[rowId++, 2].Value = 0.5;

                sheet.Cells[rowId, 1].Value = "1234";
                sheet.Cells[rowId++, 2].Value = "0.5";

                sheet.Cells[rowId++, 2].Value = 0.5;

                sheet.Cells[rowId++, 1].Value = 1234;

                sheet.Cells[rowId, 1].Value = 5678;
                sheet.Cells[rowId++, 2].Value = 0.9;

            });
        }
        [TestCase("Data", "Bad", "Weighting")]
        [TestCase("Data", "ResponseId", "Bad")]
        [TestCase("Data", "Bad", "Bad")]
        [TestCase("Data", null, "Bad")]
        [TestCase("Data", "Bad", null)]
        [TestCase("Data", null, null)]
        public async Task TestInValidExcel(string sheetName, string column1, string column2)
        {
            await TestExcelFileSheetAndColumnNames(nameof(TestInValidExcel), sheetName, column1, column2, null);
        }

        private async Task TestExcelFileSheetAndColumnNames(string testName, string sheetName, string column1, string column2, Action<ExcelWorksheet> injectData)
        {
            var excelFile = string.Empty;
            try
            {
                excelFile = GenerateFile(sheetName, new[] { column1, column2 }, injectData);
                var loader = new WeightingFileLoader();
                var stats = new ResponseLevelAlgorithmService.ValidationStatistics("All", null);
                var data = loader.LoadResponseWeights(new PhysicalFileInfo(new FileInfo(excelFile)), stats);
                await Verifier.Verify(stats);
                if (data is { Count: > 0 })
                {
                    await Verifier.Verify(data).UseMethodName($"{testName}.data");
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(excelFile))
                {
                    File.Delete(excelFile);
                }
            }
        }

        [TestCase("Data", "ResponseId", "Weight")]
        public void TestExcelStreamWithNoData(string sheetName, string column1, string column2)
        {
            var stream = GenerateStream(sheetName, new[] { column1, column2 }, null);
            var loader = new WeightingFileLoader();
            var result = loader.Validation(stream);
            Assert.That(result, Is.EqualTo(ResponseLevelAlgorithmService.ValidationMessageType.ExcelMissingData));
        }

        [Test]
        public void TestExcelStreamWithInvalidData()
        {
            var data = new byte[] { 1,2,3,4};
            var stream = new MemoryStream(data);
            var loader = new WeightingFileLoader();
            var result = loader.Validation(stream);
            Assert.That(result, Is.EqualTo(ResponseLevelAlgorithmService.ValidationMessageType.ExcelInvalidFile));
        }

        [Test]
        public void TestExcelStreamWithEmptyStream()
        {
            var data = new byte[] { };
            var stream = new MemoryStream(data);
            var loader = new WeightingFileLoader();
            var result = loader.Validation(stream);
            Assert.That(result, Is.EqualTo(ResponseLevelAlgorithmService.ValidationMessageType.ExcelMissingSheet));
        }

        [TestCase("Data", "ResponseId", "Weighting")]
        [TestCase("Data1", "Response", "Weighting")]
        [TestCase("Intro", "Id", "Weighting")]
        [TestCase("Foo", "ResponseId", "Weightings")]
        [TestCase("Bar", "Weightings", "ResponseId")]
        public void TestExcelStreamWithValid(string sheetName, string column1, string column2)
        {
            var stream = GenerateStream(sheetName, new [] {column1, column2}, (sheet) =>
            {
                sheet.Cells[2, 1].Value = 1234;
                sheet.Cells[2, 2].Value = 1.0;
            });
            var loader = new WeightingFileLoader();
            var result = loader.Validation(stream);
            Assert.That(result, Is.Null);
        }
    }
}
