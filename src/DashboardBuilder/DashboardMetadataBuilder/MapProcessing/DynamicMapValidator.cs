using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace DashboardMetadataBuilder.MapProcessing
{
    public static class DynamicMapValidator
    {
        public static async Task<MapValidationResult> GetValidateMetadataBuildResult(string mapFilePath,
            ITempMetadataBuilder tempMetadataBuilder)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), nameof(DynamicMapValidator), Path.GetRandomFileName());
            var tempLocalDirectory = new DirectoryInfo(tempDir);
            try
            {
                var validators = new Func<string, Task<MapValidationResult>>[]
                {
                    async m => StaticAnalysisMapValidator.Validate(ExcelWorkbookExtensions.LoadWorkbook(m)),
                    m => AsValidationResult("building metadata", () => tempMetadataBuilder.BuildToTempMetadataFolder(m, tempLocalDirectory)),
                    m => tempMetadataBuilder.IsBrandVue(m) ? ValidateResultCanBeLoadedByBrandVue(tempLocalDirectory) : Success()
                };

                foreach (var validate in validators)
                {
                    var result = await validate(mapFilePath);
                    if (result.HasErrors) return result;
                }
                return await Success();
            }
            finally
            {
                if (tempLocalDirectory.Exists) tempLocalDirectory.Delete(true);
            }
        }

        private static async Task<MapValidationResult> AsValidationResult(string taskBeingPerformed, Func<Task> buildMetadata)
        {
            try
            {
                await buildMetadata();
                return await Success();
            }
            catch (Exception e)
            {
                return MapValidationResult.ThrewException(taskBeingPerformed, e);
            }
        }

        private static async Task<MapValidationResult> ValidateResultCanBeLoadedByBrandVue(DirectoryInfo tempLocalDirectory)
        {
            var validator = new BrandVueMetadataLoadValidator(NullLoggerFactory.Instance);
            try
            {
                validator.LoadMetadata(tempLocalDirectory.FullName, tempLocalDirectory.GetDirectories().First().Name);
                return await Success();
            }
            catch (Exception e)
            {
                return MapValidationResult.ThrewException("simulating BrandVue metadata load", e);
            }
        }

        private static async Task<MapValidationResult> Success()
        {
            return new MapValidationResult(new string[0], new string[0]);
        }
    }
}