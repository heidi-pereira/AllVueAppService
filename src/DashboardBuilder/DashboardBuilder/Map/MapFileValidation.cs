using System.Linq;
using System.Threading.Tasks;
using Aspose.Cells;
using DashboardMetadataBuilder;
using DashboardMetadataBuilder.MapProcessing;
using Microsoft.Extensions.Logging;

namespace DashboardBuilder.Map
{
    internal static class MapFileValidation
    {

        public static bool IsStaticallyValid(Workbook mapWorkbook, ILogger logger)
        {
            logger.LogInformation($"Validating map file: {mapWorkbook.FileName}");

            var result = StaticAnalysisMapValidator.Validate(mapWorkbook);

            return IsValid(logger, result);
        }

        public static async Task<bool> ValidateMetadataBuild(string mapFilePath, ITempMetadataBuilder tempMetadataBuilder,
            ILogger logger)
        {
            logger.LogInformation($"Validating map file: {mapFilePath}");

            var result = await DynamicMapValidator.GetValidateMetadataBuildResult(mapFilePath, tempMetadataBuilder);

            return IsValid(logger, result);
        }

        private static bool IsValid(ILogger logger, MapValidationResult result)
        {
            foreach (var validationResultWarning in result.Warnings)
            {
                logger.LogInformation($"Warning: {validationResultWarning}");
            }
            if (!result.HasErrors)
            {
                logger.LogInformation("Map file validated, no issues found");
                return true;
            }

            foreach (var resultIssueDescription in result.Errors)
            {
                logger.LogError(resultIssueDescription);
            }

            return false;
        }
    }
}