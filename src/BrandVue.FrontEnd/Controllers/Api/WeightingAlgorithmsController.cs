using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Filters;
using BrandVue.Models;
using BrandVue.PublicApi.Services;
using BrandVue.Services.Exporter;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using BrandVue.SourceData.Weightings;
using BrandVue.SourceData.Weightings.Rim;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;

namespace BrandVue.Controllers.Api
{
    public record TargetWeightsRequestModel(IFormFile RespondentWeights, string[] SubsetIds, bool PostToDatabase)
        : ISubsetIdsProvider<string[]>;

    [SubProductRoutePrefix("api/meta")]
    [CacheControl(NoStore = true)]
    public class WeightingAlgorithmsController : ApiController
    {
        private readonly IProductContext _productContext;
        private readonly IWeightingPlanRepository _weightingPlanRepository;
        private readonly IResponseDataStreamWriter _csvStreamWriter;
        private readonly WeightingAlgorithmService _weightingAlgorithmService;
        private readonly TargetWeightingGenerationService _targetWeightGeneratorService;
        private readonly ResponseWeightingGenerationService _responseWeightGeneratorService;
        IAverageDescriptorRepository _averageDescriptorRepository;
        private readonly IInvalidatableLoaderCache _invalidatableLoaderCache;
        private readonly IResponseWeightingRepository _responseWeightingRepository;
        private IMetricConfigurationRepository _metricConfigRepository;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;


        public WeightingAlgorithmsController(ISubsetRepository subsetRepository,
            IMeasureRepository measureRepository,
            IProductContext productContext,
            IRimWeightingCalculator rimWeightingCalculator, 
            ISampleSizeProvider sampleSizeProvider,
            IWeightingPlanRepository weightingPlanRepository,
            IResponseDataStreamWriter csvStreamWriter, 
            IRespondentRepositorySource respondentRepositorySource,
            IProfileResponseAccessorFactory profileResponseAccessorFactory,
            IInvalidatableLoaderCache invalidatableLoaderCache,
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
            IEntityRepository entityRepository,
            IBaseExpressionGenerator baseExpressionGenerator,
            IAverageDescriptorRepository averageConfigurationRepository,
            ILoggerFactory loggerFactory,
            IResponseWeightingRepository responseWeightingRepository,
            IVariableConfigurationRepository variableConfigurationRepository,
            IMetricConfigurationRepository metricConfigRepository)
        {
            _productContext = productContext;
            _weightingPlanRepository = weightingPlanRepository;
            _csvStreamWriter = csvStreamWriter;
            _invalidatableLoaderCache = invalidatableLoaderCache;

            _weightingAlgorithmService = new WeightingAlgorithmService(subsetRepository, entityRepository, measureRepository, rimWeightingCalculator, sampleSizeProvider, respondentRepositorySource, profileResponseAccessorFactory, baseExpressionGenerator);
            _responseWeightGeneratorService = new ResponseWeightingGenerationService(loggerFactory, subsetRepository, productContext,respondentRepositorySource, profileResponseAccessorFactory, quotaCellReferenceWeightingRepository);
            var targetPlanWeightGeneratorService = new TargetPlanWeightingGenerationService(respondentRepositorySource, profileResponseAccessorFactory, measureRepository);
            _targetWeightGeneratorService = new TargetWeightingGenerationService(subsetRepository, productContext, weightingPlanRepository, targetPlanWeightGeneratorService);
            _averageDescriptorRepository = averageConfigurationRepository;
            _responseWeightingRepository = responseWeightingRepository;
            _metricConfigRepository = metricConfigRepository;
            _variableConfigurationRepository = variableConfigurationRepository;
        }

        [HttpGet]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [Route("rim/totalSampleSize")]
        [SubsetAuthorisation(nameof(subsetId))]
        public Task<double> GetRimTotalSampleSize(string subsetId, CancellationToken cancellationToken,
            string filterMetricName = null, int? filterInstanceId = null)
        {
            AssertGenerated();

            return _weightingAlgorithmService.GetRimTotalSampleSize(subsetId, filterMetricName, filterInstanceId, cancellationToken);
        }

        [HttpGet]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [SubsetAuthorisation(nameof(selectedSubset))]
        [Route("rim/allSampleSizes")]
        public async Task<IEnumerable<SampleSize>> GetSampleSizeByWeightingForTopLevel(string selectedSubset,
            CancellationToken cancellationToken)
        {
            AssertGenerated();

            return await _weightingAlgorithmService.GetSampleSizeByWeightingForTopLevel(selectedSubset, cancellationToken);
        }


        [HttpGet]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [Route("rim/dimensionSampleSizes")]
        [SubsetAuthorisation(nameof(selectedSubset))]
        public async Task<IEnumerable<InstanceResultSize>> GetRimDimensionSampleSizes(string selectedSubset,
            string metricName, CancellationToken cancellationToken, string filterMetricName = null,
            int? filterInstanceId = null)
        {
            AssertGenerated();

            return (await _weightingAlgorithmService.GetRimDimensionSampleSizes(selectedSubset, metricName, filterMetricName, filterInstanceId, cancellationToken))
                .Select(r => new InstanceResultSize { EntityInstance = r.Key, Result = r.Value });
        }


        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [Route("rim/totalSampleSizeWithFilters")]
        [SubsetAuthorisation(nameof(subsetId))]
        public async Task<double> GetTotalSampleSizeWithFilters(string subsetId,
            [Required] [FromBody] List<WeightingFilterInstance> filters, CancellationToken cancellationToken)
        {
            AssertGenerated();

            return await _weightingAlgorithmService.GetRimTotalSampleSize(subsetId, filters, cancellationToken);
        }

        public class RimDimensionSampleSizeParameters
        {
            public string MetricName { get; set; }
            public List<WeightingFilterInstance> Instances { get; set; } = new List<WeightingFilterInstance>();
            
        }
        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [Route("rim/dimensionSampleSizesWithFilters")]
        [SubsetAuthorisation(nameof(selectedSubset))]
        public async Task<IEnumerable<InstanceResultSize>> GetRimDimensionSampleSizesWithFilters(
            [Required] [FromBody] RimDimensionSampleSizeParameters parameters, string selectedSubset,
            CancellationToken cancellationToken)
        {
            AssertGenerated();

            return (await _weightingAlgorithmService.GetRimDimensionSampleSizes(selectedSubset, parameters.MetricName, parameters.Instances, cancellationToken))
                .Select(r => new InstanceResultSize { EntityInstance = r.Key, Result = r.Value });
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [Route("rim/validatePartialPlan")]
        [SubsetAuthorisation]
        public async Task<RimWeightingCalculationResult> ValidateRimWeightingPartialRoot(
            [Required] [FromBody] UiWeightingConfigurationRoot partialWeightingConfiguration,
            CancellationToken cancellationToken, List<WeightingFilterInstance> weightingFilterInstances)
        {
            AssertGenerated();
            var weightingPlans = _weightingPlanRepository.GetWeightingPlansForSubset(_productContext.ShortCode, _productContext.SubProductId, partialWeightingConfiguration.SubsetId);
            if (weightingPlans is null) throw new NotFoundException("Weighting not found for this subset");

            return await _weightingAlgorithmService.ValidateRimWeightingScheme(partialWeightingConfiguration.SubsetId, 
                partialWeightingConfiguration.ToWeightingPlanConfiguration(_productContext.ShortCode, _productContext.SubProductId).ToAppModel().ToList(), 
                weightingFilterInstances,
                cancellationToken);
        }


        [HttpGet]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [Route("getAllResponsesWithCell")]
        public IActionResult ResponsesWithCell()
        {
            AssertGenerated();
            var (headers, rows) = _weightingAlgorithmService.ResponsesWithCell();

            return _csvStreamWriter.StreamSurveyAnswersetsToHttpResponseMessage(headers, rows);
        }

        public record ExportRespondentWeightsRequest(string[] SubsetIds, string AverageId)
            : ISubsetIdsProvider<string[]>;

        [Route("exportRespondentWeights")]
        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [SubsetAuthorisation]
        public IActionResult ExportRespondentWeights([FromBody] ExportRespondentWeightsRequest request, CancellationToken cancellationToken)
        {
            var averageToUse = _averageDescriptorRepository.FirstOrDefault(a => a.AverageId == request.AverageId);
            string separator = "";
            const string dateFormat = "yyyy-M-d";

            string subsetIdHeader = "SubsetId", 
                waveIdHeader="WaveId",
                responseIdHeader = "ResponseId", 
                weightHeader = "Weight",
                separatorHeader = separator,
                dateTime =$"Completed date ({dateFormat})",
                descriptionHeader ="Information on Quota cell assignment";

            var rows = _responseWeightGeneratorService.Export(request.SubsetIds, averageToUse, cancellationToken);

            string[] headers = {subsetIdHeader, waveIdHeader, responseIdHeader, weightHeader, separatorHeader, dateTime, descriptionHeader };
            var rowsWithSameColumnOrder = rows.Select(r => (r.SubsetId, r.WaveId,r.ResponseId, r.Weight, separator, r.ResponseDate.UtcDateTime.ToString(dateFormat), r.Descriptions));
            var csvWriter = new CSVWeightingExport(_csvStreamWriter);
            return csvWriter.StreamSurveyAnswersetsToHttpResponseMessage(headers, rowsWithSameColumnOrder);
        }


        [Route("exportRespondentWeightsForSubset")]
        [HttpGet]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [SubsetAuthorisation(nameof(subsetId))]
        public IActionResult ExportRespondentWeightsForSubset(string subsetId, string averageId,
            CancellationToken cancellationToken, List<WeightingFilterInstance> weightingFilterInstances)
        {
            var averageToUse = _averageDescriptorRepository.FirstOrDefault(a => a.AverageId == averageId);
            string separator = "";

            string subsetIdHeader = "SubsetId",
                waveIdHeader = "WaveId",
                responseIdHeader = "ResponseId",
                weightHeader = "Weight",
                separatorHeader = separator,
                descriptionHeader = "Information on Quota cell assignment";

            var rows = _responseWeightGeneratorService.Export(subsetId, averageToUse, weightingFilterInstances, cancellationToken);

            string[] headers = { subsetIdHeader, waveIdHeader, responseIdHeader, weightHeader, separatorHeader, descriptionHeader };
            var rowsWithSameColumnOrder = rows.Select(r => (r.SubsetId, r.WaveId, r.ResponseId, r.Weight, separator, r.Descriptions));
            var csvWriter = new CSVWeightingExport(_csvStreamWriter);
            return csvWriter.StreamSurveyAnswersetsToHttpResponseMessage(headers, rowsWithSameColumnOrder);
        }

        [Route("weightingTypeAndStyle")]
        [HttpGet]
        [SubsetAuthorisation(nameof(subsetId))]
        public WeightingTypeStyle WeightingTypeAndStyle(string subsetId)
        {
            var service = new WeightingHelperService(_productContext, _responseWeightingRepository, _weightingPlanRepository, _metricConfigRepository, _variableConfigurationRepository);
            return service.WeightingTypeAndStyle(subsetId);
        }

        [Route("respondentWeightsReport")]
        [HttpGet]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [SubsetAuthorisation(nameof(subsetId))]
        public async Task<IActionResult> RespondentWeightsReport(string subsetId, string[] metricNames,
            CancellationToken cancellationToken)
        {
            string[] headers = { "MetricName", "InstanceName", "Id", "Sample size", "Sample size by Quota", "Description"};
            var rowsInOrder = new List<(string metricName, string name, int?instanceId, double? rawSampleSize, double? sampleSizeByWeighting, string)>();
            var total = await _weightingAlgorithmService.GetRimTotalSampleSize(subsetId, new List<WeightingFilterInstance>(), cancellationToken);

            foreach (var metricName in metricNames)
            {
                var rows = await _weightingAlgorithmService.GetReport(subsetId, metricName, cancellationToken);
                rowsInOrder.AddRange(rows.Select(r => (r.metricName, r.name, new int?(r.instanceId), r.rawSampleSize, r.sampleSizeByWeighting, GetDescription(r.rawSampleSize, r.sampleSizeByWeighting))));

                if (rows.Count() > 1)
                {
                    var sum1 = rows.Sum(r => r.rawSampleSize);
                    var sum2 = rows.Sum(r => r.sampleSizeByWeighting);
                    rowsInOrder.Add(new(metricName, "Total", null, sum1, sum2, GetTotalDescription(total, sum1, sum2)));
                    rowsInOrder.Add(new("", "", null, null, null,null));
                }
            }
            rowsInOrder.Add(("Total respondents", "", null, total, null, null));
            var csvWriter = new CSVWeightingExport(_csvStreamWriter);
            return csvWriter.StreamDataToHttpResponseMessage(headers, rowsInOrder);
        }

        private string GetDescription(double? rawSampleSize, double? sampleSizeByWeighting)
        {
            if (rawSampleSize.HasValue && sampleSizeByWeighting.HasValue)
            {
                if (rawSampleSize.Value != sampleSizeByWeighting.Value)
                {
                    if (sampleSizeByWeighting.Value == 0)
                    {
                        return $"!{rawSampleSize.Value} -  Missing quota cell";
                    }
                    return $"!{rawSampleSize.Value - sampleSizeByWeighting.Value}";
                }
            }
            return null;
        }
        private string GetTotalDescription(double total, double? rawSampleSize, double? sampleSizeByWeighting)
        {
            if (rawSampleSize.GetValueOrDefault() != total)
            {
                return "!!";
            }
            return GetDescription(rawSampleSize, sampleSizeByWeighting);
        }

        [HttpPost]
        [Authorize(Policy = FeatureRolePolicy.Weighting)]
        [Route("generateTargetWeights")]
        [SubsetAuthorisation]
        public GenerateTargetWeightsResponse GenerateTargetWeights([FromForm] TargetWeightsRequestModel targetWeightsRequestModel)
        {
            ControllerHelper.VerifySubsetsPermissions(HttpContext, targetWeightsRequestModel.SubsetIds);
            AssertGenerated();

            var MaxFileSizeInMb = 50 * (1 << 20);
            if (targetWeightsRequestModel.RespondentWeights.Length > MaxFileSizeInMb)
            {
                throw new BadHttpRequestException(
                    $"Error: File is too large.  Maximum upload file size is {MaxFileSizeInMb:N0}MB");
            }
            string fileExtension = Path.GetExtension(targetWeightsRequestModel.RespondentWeights.FileName) ?? "";
            var acceptedFileTypes = new[] { ".csv" };
            if (!acceptedFileTypes.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
            {
                throw new BadHttpRequestException(
                    $"Error: This type of file can't be uploaded. Please use a supported file type ({string.Join(", ", acceptedFileTypes)}).");
            }


            using var fileStream = targetWeightsRequestModel.RespondentWeights.OpenReadStream();
            var result= _targetWeightGeneratorService.ReverseScaleFactors(fileStream, targetWeightsRequestModel.SubsetIds);


            var plans = result.SubsetToPlans.SelectMany(x =>
            {
                var plans = x.SelectMany(x => x).ToList();
                var planConfigurations = plans.FromAppModel(_productContext.ShortCode, _productContext.SubProductId, x.Key.Id).ToList();
                return UiWeightingConfigurationRoot.ToUIWeightingRoots(planConfigurations);
            }).ToList();
            var warnings = result.Warnings.ToList();
            var errors = result.Errors.ToList();
            if (targetWeightsRequestModel.PostToDatabase)
            {
                if (errors.Any())
                {
                    errors.Add($"Not directly updating the database as there are {errors.Count} error(s).");
                }
                else
                {
                    foreach (var subset in targetWeightsRequestModel.SubsetIds)
                    {
                        try
                        {
                            var myResult = result.SubsetToPlans.Single(x => x.Key.Id == subset);
                            _weightingPlanRepository.DeleteWeightingPlanForSubset(_productContext.ShortCode, _productContext.SubProductId, subset);
                            _responseWeightingRepository.DeleteResponseWeights(subset);

                            var myPlans = myResult.SelectMany(x => x).ToList();
                            var planConfigurations = myPlans.FromAppModel(_productContext.ShortCode, _productContext.SubProductId, subset).ToList();
                            _weightingPlanRepository.UpdateWeightingPlanForSubset(_productContext.ShortCode, _productContext.SubProductId, subset, planConfigurations);
                            warnings.Add($"Updated weights for survey segment '{subset}'");
                            _invalidatableLoaderCache.InvalidateCacheEntry(_productContext.ShortCode, _productContext.SubProductId);
                        }
                        catch (Exception ex) {
                            errors.Add($"FATAL(Adding to database) for survey segment'{subset}' {ex.Message} {(ex.InnerException != null ? ex.InnerException.Message : "")}");
                        }
                    }
                }
            }

            return new GenerateTargetWeightsResponse(plans, warnings, errors);

        }

        public record GenerateTargetWeightsResponse (IEnumerable<UiWeightingConfigurationRoot> Plans, IReadOnlyCollection<string> Warnings, IReadOnlyCollection<string> Errors);


        private void AssertGenerated()
        {
            if (!_productContext.GenerateFromSurveyIds)
                throw new NotImplementedException("Custom weightings are only supported for non-map-file Vues");
        }
    }
}