using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Respondents
{
    public class MetricBasedRespondentRepositoryFactory : IRespondentRepositoryFactory
    {
        private readonly ILogger _logger;
        private readonly IAverageDescriptorRepository _averageDescriptorRepository;
        private readonly IDataPresenceGuarantor _dataPresenceGuarantor;
        private readonly IReadOnlyDictionary<Subset, WeightingMetrics> _subsetWeightingMeasures;
        private readonly IProductContext _productContext;

        public MetricBasedRespondentRepositoryFactory(
            IAverageDescriptorRepository averageDescriptorRepository,
            IDataPresenceGuarantor dataPresenceGuarantor,
            IReadOnlyDictionary<Subset, WeightingMetrics> subsetWeightingStrategy,
            ILogger<MetricBasedRespondentRepositoryFactory> logger,
            IProductContext productContext)
        {
            _logger = logger;
            _dataPresenceGuarantor = dataPresenceGuarantor;
            _subsetWeightingMeasures = subsetWeightingStrategy;
            _averageDescriptorRepository = averageDescriptorRepository;
            _productContext = productContext;
        }

        public async Task<IRespondentRepository> CreateRespondentRepository(Subset subset, DateTimeOffset? signOffDate,
            CancellationToken cancellationToken)
        {
            if (subset.Disabled)
            {
                _logger.LogWarning("'{Product}' ({Subset}): is disabled and will not be loaded. Full subset information: {@Subset}",
                    _productContext, subset.Id, subset);
                return new RespondentRepository(subset, signOffDate);
            }

            return await LoadProfiles(subset, signOffDate, cancellationToken);
        }

        private async Task<IRespondentRepository> LoadProfiles(Subset subset, DateTimeOffset? signOffDate,
            CancellationToken cancellationToken)
        {
            var profiles = _dataPresenceGuarantor.LoadEmptyProfiles(subset, Array.Empty<ResponseFieldDescriptor>());

            var unweightedRepository = RespondentRepository.CreateUnweightedRepository(profiles, subset, signOffDate);
            if (unweightedRepository.Count == 0)
            {
                _logger.LogCritical("'{Product}' ({Subset}): No responses have been loaded for dataset. Check profile source has not been corrupted.", _productContext, subset.Id);
                return unweightedRepository;
            }

            _logger.LogInformation("'{Product}' ({Subset}): Loaded {RespondentsCount} responses.", _productContext, subset.Id, unweightedRepository.Count);

            if (!_subsetWeightingMeasures.TryGetValue(subset, out var weightingMeasures)) return unweightedRepository;

            var weightedRepository = new RespondentRepository(subset, signOffDate);
            foreach (var respondent in await WithWeightsFrom(unweightedRepository, weightingMeasures, cancellationToken))
            {
                weightedRepository.Add(respondent.ProfileResponseEntity, respondent.QuotaCell);
            }

            return weightedRepository;
        }

        public List<QuotaCellAllocationReason> QuotaCellAllocationReason(Subset subset, IProfileResponseEntity profileResponseEntity)
        {
            if (_subsetWeightingMeasures.TryGetValue(subset, out var weightingMeasures))
            {
                var quotaCellFactory = weightingMeasures.CreateQuotaCellFactory();
                return quotaCellFactory.QuotaCellAllocationReason(profileResponseEntity);
            }
            return new List<QuotaCellAllocationReason> { new QuotaCellAllocationReason(subset.DisplayName, null, $"Subset '{subset.Id}' not available")};
        }

        public async Task<IRespondentRepository> WithWeightsFrom(IRespondentRepository respondents,
            WeightingMetrics weightingMetrics, CancellationToken cancellationToken)
        {
            var subset = respondents.Subset;
            var quotaCellFactory = weightingMetrics.CreateQuotaCellFactory();
            var newRespondentRepository = new RespondentRepository(subset, respondents.LatestResponseDate);
            
            await LoadIntoMemory(respondents, subset, weightingMetrics.AllMeasureDependencies.Select(m => m.Metric).ToArray(), cancellationToken);
            int respondentsWithWeightedQuotaCell = 0, respondentsWithUnweightedQuotaCell = 0;

            foreach (var respondent in respondents.OrderBy(x => x.ProfileResponseEntity.Id))
            {
                var quotaCell = quotaCellFactory.GetQuotaCell(respondent.ProfileResponseEntity);
                if (quotaCell.IsUnweightedCell)
                {
                    respondentsWithUnweightedQuotaCell++;
                }
                else
                {
                    respondentsWithWeightedQuotaCell++;
                }
                newRespondentRepository.Add(respondent.ProfileResponseEntity, quotaCell);
            }
            _logger.LogInformation("'{ProductContext}' ({Subset}): Respondents with weighted quota cell: {respondentsWithWeightedQuotaCell}, unweighted: {respondentsWithUnweightedQuotaCell}", _productContext, subset.Id, respondentsWithWeightedQuotaCell, respondentsWithUnweightedQuotaCell);

            return newRespondentRepository;
        }

        private async Task LoadIntoMemory(IRespondentRepository respondentRepository, Subset subset,
            IEnumerable<Measure> allWeightingMeasures, CancellationToken cancellationToken)
        {
            var customAverage = _averageDescriptorRepository.GetCustom(AverageIds.CustomPeriod);
            var calculationPeriod = new CalculationPeriod(respondentRepository.EarliestResponseDate, respondentRepository.LatestResponseDate);

            await _dataPresenceGuarantor.EnsureDataLoadedIntoMemory(respondentRepository, subset, allWeightingMeasures, calculationPeriod, customAverage, new AlwaysIncludeFilter(), Array.Empty<Break>(), cancellationToken);
        }
    }
}