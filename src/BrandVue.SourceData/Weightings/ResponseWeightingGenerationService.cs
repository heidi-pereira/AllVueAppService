using System.Threading;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.QuotaCells;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Weightings
{
    public record ExportedWeight(string SubsetId, int? WeightingGroupId, string WaveId, int ResponseId, double? Weight, DateTimeOffset ResponseDate, string[] Descriptions);

    public class ResponseWeightingGenerationService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IProductContext _productContext;
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
        private readonly IQuotaCellReferenceWeightingRepository _quotaCellReferenceWeightingRepository;

        public ResponseWeightingGenerationService(ILoggerFactory loggerFactory, ISubsetRepository subsetRepository, IProductContext productContext, IRespondentRepositorySource respondentRepositorySource,
            IProfileResponseAccessorFactory profileResponseAccessorFactory,
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository)
        {
            _loggerFactory = loggerFactory;
            _subsetRepository = subsetRepository;
            _productContext = productContext;
            _respondentRepositorySource = respondentRepositorySource;
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
            _quotaCellReferenceWeightingRepository = quotaCellReferenceWeightingRepository;
        }

        public IEnumerable<ExportedWeight> Export(string subsetId, AverageDescriptor averageDescriptor,
            List<WeightingFilterInstance> weightingFilterInstances, CancellationToken cancellationToken)
        {
            var subset = _subsetRepository.Single(s => s.Id == subsetId);
            return GetSubsetWeightings(subset, averageDescriptor, cancellationToken, weightingFilterInstances);
        }

        public IEnumerable<ExportedWeight> Export(string[] subsetIds, AverageDescriptor averageDescriptor,
            CancellationToken cancellationToken)
        {
            if (subsetIds == null || subsetIds.Length == 0)
            {
                return _subsetRepository.SelectMany( s=>GetSubsetWeightings(s, averageDescriptor, cancellationToken));
            }
            return _subsetRepository.Where(s => subsetIds.Contains(s.Id)).SelectMany(s => GetSubsetWeightings(s, averageDescriptor, cancellationToken));
        }

        private IEnumerable<ExportedWeight> GetSubsetWeightings(Subset s, AverageDescriptor averageDescriptor,
            CancellationToken cancellationToken, List<WeightingFilterInstance> weightingFilterInstances = null)
        {
            var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(s);
            var weightedQuotaCells = _respondentRepositorySource.GetForSubset(s).WeightedCellsGroup;
            var unWeightedQuotaCells = _respondentRepositorySource.GetForSubset(s).UnWeightedCellsGroup;

            var weightedResults = weightedQuotaCells.IndependentlyWeightedGroups.SelectMany(
                        waveCells => GetWeightsForWave(s, waveCells.Value, profileResponseAccessor, averageDescriptor, weightingFilterInstances));
            var unweightedResults = GetUnWeights(s, unWeightedQuotaCells, profileResponseAccessor, averageDescriptor, cancellationToken);
            return unweightedResults.Concat(weightedResults);
        }

        private static DateTimeOffset GetLastDayOfPeriodForAverage(AverageDescriptor average, DateTimeOffset dateSomewhereInPeriod)
        {
            if (average.TotalisationPeriodUnit == TotalisationPeriodUnit.Month)
            {
                return dateSomewhereInPeriod.GetLastDayOfMonthOnOrPreceding().AddMonths(1);
            }
            if (average.TotalisationPeriodUnit == TotalisationPeriodUnit.All)
            {
                return DateTimeOffset.MaxValue;
            }
            return average.MakeUpTo switch
            {
                MakeUpTo.Day => dateSomewhereInPeriod,
                MakeUpTo.WeekEnd => dateSomewhereInPeriod.DayOfWeek == DayOfWeek.Sunday? dateSomewhereInPeriod : dateSomewhereInPeriod.AddDays(7- (int)dateSomewhereInPeriod.DayOfWeek),
                MakeUpTo.MonthEnd => dateSomewhereInPeriod.GetLastDayOfMonthOnOrPreceding().AddMonths(1),
                _ => throw new ArgumentOutOfRangeException(nameof(average.MakeUpTo), average.MakeUpTo, null)
            };
        }
        private string[] GetUnweightedReason(IProfileResponseEntity profileResponseEntity, Subset subset,
            CancellationToken cancellationToken)
        {
            var reasons = new List<string>()
            {
                "Unweighted"
            };
            var items = _respondentRepositorySource.QuotaCellAllocationReason(subset, profileResponseEntity, cancellationToken);

            reasons.AddRange(items.Select(x =>
            {
                var standardBlock = $"[{x.Dimension}";
                if (x.AnswerValue.HasValue)
                {
                    standardBlock += $",{x.AnswerValue}";
                }
                standardBlock +="]";
                if (string.IsNullOrEmpty(x.Reason))
                {
                    return standardBlock;
                }
                return standardBlock + " "+ x.Reason;
            }
            ));

            return reasons.ToArray();
        }
        private IEnumerable<ExportedWeight> GetUnWeights(Subset subset, IGroupedQuotaCells waveCells,
            IProfileResponseAccessor profileResponseAccessor, AverageDescriptor averageDescriptor,
            CancellationToken cancellationToken)
        {
            var waveResponses = profileResponseAccessor.GetResponses(waveCells).ToArray();

            var vals = waveResponses.SelectMany(populatedWaveCell => populatedWaveCell.Profiles.ToArray().Select(response =>
                            new ExportedWeight(SubsetId: subset.Id, 
                            WeightingGroupId: (int?)null, 
                            string.Empty, 
                            ResponseId: response.Id, 
                            Weight: 0.0, 
                            ResponseDate: response.Timestamp,
                            Descriptions: GetUnweightedReason(response, subset, cancellationToken) ) ) );
            return vals;
        }
        private string WaveId(int? weightingGroupId, QuotaCell cell)
        {
            if ((weightingGroupId != null) && (weightingGroupId.HasValue))
            {
                return cell.FieldGroupToKeyPart.First().Value;
            }
            return string.Empty;
        }
        private string[] QuotaDescriptions(QuotaCell cell, string warning = null)
        {
            if (cell.IsUnweightedCell)
            {
                return new[] {"Unweighted" }; 
            }
            var result = new List<string>
            {
                "Weighted"
            };
            result.AddRange(cell.FieldGroupToKeyPart.Select((k, v) => $"{k}"));
            if (warning != null)
            {
                result.Add(warning);
            }
            return result.ToArray();
        }
        private IEnumerable<ExportedWeight> GetWeightsForWave(Subset subset, IGroupedQuotaCells waveCells, IProfileResponseAccessor profileResponseAccessor, AverageDescriptor averageDescriptor, List<WeightingFilterInstance> weightingFilterInstances)
        {
            var firstCell = waveCells.Cells.First();
            var weightingGroupId = firstCell.WeightingGroupId;
            var populatedQuotaCells = profileResponseAccessor.GetResponses(waveCells).ToArray();

            Dictionary<QuotaCell, double> cellWeightsLookup;
            if (averageDescriptor == null)
            {
                var result = WeightGeneratorForRequestedPeriod
                    .GenerateCellWeights(subset,
                            profileResponseAccessor,
                            _quotaCellReferenceWeightingRepository,
                            waveCells,
                            DateTimeOffset.MinValue,
                            DateTimeOffset.MaxValue);
                cellWeightsLookup = result.ToDictionary(c => c.QuotaCell, c => c.WeightingForQuotaCellForDay);

                return populatedQuotaCells.Where ( cell=>IsCellIncludedByFilter(weightingFilterInstances, cell))
                .Select(populatedWaveCell => (Weight: cellWeightsLookup[populatedWaveCell.QuotaCell], populatedWaveCell.Profiles, QuotaCell: populatedWaveCell.QuotaCell))
                .SelectMany(p => p.Profiles.ToArray(), (x, p) => new ExportedWeight(
                            SubsetId: subset.Id, 
                            WeightingGroupId: weightingGroupId,
                            WaveId: WaveId(weightingGroupId, x.QuotaCell),
                            ResponseId: p.Id, 
                            x.Weight,
                            ResponseDate: p.Timestamp,
                            Descriptions: QuotaDescriptions(x.QuotaCell)
                            ));

            }
            else
            {
                var result = new List<ExportedWeight>();
                var lookup = new Dictionary<DateTimeOffset, Dictionary<QuotaCell, double>>();
                foreach (var populatedQuotaCell in populatedQuotaCells)
                {
                    if (IsCellIncludedByFilter(weightingFilterInstances, populatedQuotaCell))
                    {
                        foreach (var profile in populatedQuotaCell.Profiles.ToArray())
                        {
                            var endDate = GetLastDayOfPeriodForAverage(averageDescriptor, profile.Timestamp);

                            if (!lookup.ContainsKey(endDate))
                            {
                                lookup.Add(endDate, WeightGeneratorForRequestedPeriod.Generate(subset,
                                        profileResponseAccessor,
                                        _quotaCellReferenceWeightingRepository,
                                        averageDescriptor,
                                        waveCells,
                                        endDate));

                            }
                            var weightLookup = lookup[endDate];
                            if (!weightLookup.TryGetValue(populatedQuotaCell.QuotaCell, out var weight))
                            {
                                result.Add(new ExportedWeight(
                                    SubsetId: subset.Id,
                                    WeightingGroupId: weightingGroupId,
                                    WaveId: WaveId(weightingGroupId, populatedQuotaCell.QuotaCell),
                                    ResponseId: profile.Id,
                                    Weight: 0.0,
                                    ResponseDate: profile.Timestamp,
                                    Descriptions: QuotaDescriptions(populatedQuotaCell.QuotaCell, "!No lookup found.")));
                            }
                            else
                            {
                                result.Add(new ExportedWeight(
                                    SubsetId: subset.Id,
                                    WeightingGroupId: weightingGroupId,
                                    WaveId: WaveId(weightingGroupId, populatedQuotaCell.QuotaCell),
                                    ResponseId: profile.Id,
                                    Weight: weight,
                                    ResponseDate: profile.Timestamp,
                                    Descriptions: QuotaDescriptions(populatedQuotaCell.QuotaCell)));
                            }
                        }
                    }
                }
                return result;
            }
        }

        private  Boolean IsCellIncludedByFilter(List<WeightingFilterInstance> weightingFilterInstances, PopulatedQuotaCell wave)
        {
            if (weightingFilterInstances != null)
            {
                var fieldParts = wave.QuotaCell.FieldGroupToKeyPart;
                foreach (var weightingFilterInstance in weightingFilterInstances)
                {
                    if (fieldParts.ContainsKey(weightingFilterInstance.FilterMetricName))
                    {
                        if (fieldParts[weightingFilterInstance.FilterMetricName] != weightingFilterInstance.FilterInstanceId.ToString())
                        {
                            return false;
                        }
                    }
                    else
                    {
                        var logger = _loggerFactory.CreateLogger<ResponseWeightingGenerationService>();
                        logger.LogWarning("{ProductContext}: Ignoring filter by {FilterMetric}", _productContext, weightingFilterInstance.FilterMetricName);
                    }
                }
            }
            return true;
        }
    }
}
