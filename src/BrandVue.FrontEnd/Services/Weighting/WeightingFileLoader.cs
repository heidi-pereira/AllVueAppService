using Microsoft.Extensions.FileProviders;
using OfficeOpenXml;
using static BrandVue.SourceData.Weightings.ResponseLevel.ResponseLevelAlgorithmService;
using System.IO;
using BrandVue.SourceData.Weightings.ResponseLevel;

namespace BrandVue.Services.Weighting
{
    public class WeightingFileLoader
    {
        public const string WeightingColumnName = "Weight"; 
        public const string ResponseIdColumnName = "ResponseId";

        private bool Matches(object value, string matcher)
        {
            var text = "";
            if (value != null)
            {
                if (value is string text1)
                {
                    text = text1;
                }
                else
                {
                    text = value.ToString();
                }
            }

            text = text?.ToLowerInvariant()??string.Empty;

            return text.StartsWith(matcher);
        }

        private int? GetColumnId(ExcelWorksheet sheet, string [] matchingColumnNames)
        {
            foreach (var matchingColumnName in matchingColumnNames)
            {
                for (int col = 1, maxCol = sheet.Cells.Columns; col < maxCol; col++)
                {
                    if (Matches(sheet.Cells[1, col].Value, matchingColumnName))
                    {
                        return col;
                    }
                }
            }

            return null;
        }

        private (ExcelWorksheet sheet, int? colResponse, int ?colWeighting) FindMatchingData(ExcelWorkbook workbook)
        {
            var weightingColumnName = WeightingColumnName.ToLowerInvariant();
            var responseIdColumnName = ResponseIdColumnName.ToLowerInvariant();

            foreach (var sheet in workbook.Worksheets)
            {
                var responseIdColumnId = GetColumnId(sheet, new[] { responseIdColumnName, "response", "id" });
                var weightColumnId = GetColumnId(sheet, new[] { weightingColumnName });
                if (responseIdColumnId.HasValue && weightColumnId.HasValue)
                {
                    return (sheet, responseIdColumnId, weightColumnId);
                }
            }

            return (null, null, null);
        }

        public ValidationMessageType? Validation(Stream excelStream)
        {
            using var excelPackage = new ExcelPackage();
            try
            {
                excelPackage.Load(excelStream);
            }
            catch (InvalidDataException e)
            {
                return ValidationMessageType.ExcelInvalidFile;
            }
            var result = FindMatchingData(excelPackage.Workbook);
            if (result.sheet == null)
            {
                return ValidationMessageType.ExcelMissingSheet;
            }

            if (result.sheet.Dimension.Rows < 2)
            {
                return ValidationMessageType.ExcelMissingData;
            }

            return null;
        }

        public BasicExcelFileInformation BasicDetails(IFileInfo file, BasicExcelFileInformation details)
        {
            var fileInfo = new FileInfo(file.PhysicalPath);
            using var excelPackage = new ExcelPackage(fileInfo);
            details.NumberOfBytes = fileInfo.Length;
            details.DateTimeCreatedUtc = fileInfo.CreationTimeUtc;
            var result = FindMatchingData(excelPackage.Workbook);
            if (result.sheet != null)
            {
                details.IsValid = true;
                details.NumberOfRows = result.sheet.Dimension?.Rows ?? 0;
            }
            return details;
        }

        public List<ResponseWeight> LoadResponseWeights(IFileInfo file, ValidationStatistics statistics)
        {
            var fileInfo = new FileInfo(file.PhysicalPath);
            using var excelPackage = new ExcelPackage(fileInfo);
            statistics.NumberOfBytes = fileInfo.Length;
            statistics.DateTimeCreatedUtc = fileInfo.CreationTimeUtc;
            var result = FindMatchingData(excelPackage.Workbook);
            if (result.sheet != null)
            {
                if (result.sheet.Dimension.Rows > 1)
                {
                    return LoadWeightsFromSheet(result.sheet,
                        result.colResponse.Value,
                        result.colWeighting.Value, statistics);
                }
                statistics.Messages.Add(new ValidationMessage(ValidationMessageType.ExcelMissingData,
                    $"Failed to find any response weight rows"));
                return null;
            }
            statistics.Messages.Add(new ValidationMessage(ValidationMessageType.ExcelMissingSheet,
                $"Failed to find a sheet with columns named {ResponseIdColumnName} and {WeightingColumnName}"));
            return null;
        }

        private List<ResponseWeight> LoadWeightsFromSheet(ExcelWorksheet sheet, int responseIdCol, int weightCol, ValidationStatistics statistics)
        {
            var weights = new List<ResponseWeight>();
            var maxRows = sheet.Cells.Rows;
            statistics.NumberOfRows = maxRows;
            for (int rowId = 2; rowId < maxRows; rowId++)
            {
                int? responseId = null;
                double? weighting = null;
                var responseIdAsObject = sheet.Cells[rowId, responseIdCol].Value;
                var weightAsObject = sheet.Cells[rowId, weightCol].Value;
                if (responseIdAsObject == null && weightAsObject == null)
                {
                    continue;
                }
                if (responseIdAsObject is double responseIdAsDouble)
                {
                    responseId = (int)responseIdAsDouble;
                }
                else if (responseIdAsObject is int responseIdAsInt)
                {
                    responseId = responseIdAsInt;
                }
                else if (responseIdAsObject is string responseIdAsString)
                {
                    if (int.TryParse(responseIdAsString, out var response))
                    {
                        responseId = response;
                    }
                }

                if (weightAsObject is double responseWeightAsDouble)
                {
                    weighting = responseWeightAsDouble;
                }
                else if (weightAsObject is string responseWeightAsString)
                {
                    if (double.TryParse(responseWeightAsString, out var responseWeight))
                    {
                        weighting = responseWeight;
                    }
                }

                if (responseId.HasValue && weighting.HasValue)
                {
                    var existing = weights.SingleOrDefault(x => x.ResponseId == responseId.Value);
                    if (existing != null)
                    {
                        statistics.NumberOfRowsInExcelIgnored++;
                        if (Math.Abs(existing.Weight - weighting.Value) > 0.000001)
                        {
                            statistics.Messages.Add(new ValidationMessage(ValidationMessageType.ExcelIgnoringRow, $"Ignoring weight for row {rowId} as duplicate ResponseId: '{responseId}' Weight:'{weighting }', using weight {existing.Weight}"));
                        }
                    }
                    else
                    {
                        weights.Add(new ResponseWeight(responseId.Value, weighting.Value));
                        statistics.NumberOfValidRowsInExcel++;
                    }
                }
                else
                {
                    statistics.NumberOfRowsInExcelIgnored++;
                    statistics.Messages.Add(new ValidationMessage(ValidationMessageType.ExcelIgnoringRow, $"Ignoring row {rowId} as data is not valid. ResponseId: '{responseIdAsObject ?? "(null)"}' Weight:'{weightAsObject ?? "(null)"}'"));
                }
            }
            return weights;
        }
    }
}
