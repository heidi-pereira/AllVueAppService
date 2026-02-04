using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Dates;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.QuotaCells;
using Newtonsoft.Json;

namespace BrandVue.PublicApi.Models
{
    public class MetricCalculationRequest
    {
        public DateTimeOffset? StartDate { get; }
        public DateTimeOffset? EndDate { get; }
        public Dictionary<string, int[]> ClassInstances { get; }

        [JsonConstructor]
        public MetricCalculationRequest(DateTimeOffset? startDate, DateTimeOffset? endDate,
            Dictionary<string, int[]> classInstances)
        {
            StartDate = startDate;
            EndDate = endDate;
            ClassInstances = classInstances ?? new Dictionary<string, int[]>();
        }
    }

    public class MetricCalculationRequestInternal
    {
        private readonly MetricCalculationRequest _metricCalculationRequest;

        private readonly IEntityRepository _entityRepository;
        private readonly IProfileResponseAccessor _surveySetAccessor;
        private readonly int _resultLimit;
        private readonly string[] _orderedClassIds;

        public MetricDescriptor MetricDescriptor { get; }
        public SurveysetDescriptor Surveyset { get; }
        public AverageDescriptor Average { get; }
        public DateTimeOffset StartDate { get; }
        public DateTimeOffset EndDate { get; }

        public TargetInstances[] RequestedEntityInstances { get; }

        public TargetInstances PrimaryEntityInstances { get; }

        public TargetInstances[] FilterEntityInstancesCollection { get; }

        public bool IsValid { get; }

        public string[] Errors { get; }

        public MetricCalculationRequestInternal(MetricCalculationRequest metricCalculationRequest,
            SurveysetDescriptor surveyset, AverageDescriptor average, MetricDescriptor metric,
            IEntityRepository entityRepository, IProfileResponseAccessor surveySetAccessor, int resultLimit)
        {
            _metricCalculationRequest = metricCalculationRequest;
            _entityRepository = entityRepository;
            _surveySetAccessor = surveySetAccessor;
            _resultLimit = resultLimit;
            _orderedClassIds = metricCalculationRequest.ClassInstances.Keys
                    .OrderBy(s => s)
                    .ToArray();

            Surveyset = surveyset;
            MetricDescriptor = metric;
            (StartDate, EndDate, Average) = AdjustDateRangeAndAverageForCalculation(average);

            IsValid = TryValidate(out var errors);
            Errors = errors;
            if (IsValid)
            {
                RequestedEntityInstances = ToTargetInstancesCollection();
                var sortedTargetInstances = RequestedEntityInstances.OrderByDescending(i => i.OrderedInstances.Length).ToArray();
                PrimaryEntityInstances = sortedTargetInstances.First();
                FilterEntityInstancesCollection = sortedTargetInstances.Skip(1).ToArray();
            }
        }

        private bool TryValidate(out string[] errors)
        {
            var errorList = new List<string>();

            if (MetricDescriptor.Measure.CalculationType == SourceData.Measures.CalculationType.Text)
            {
                // Before we added this block, they returned a result per word, but without the word - useless.
                // If someone requests this and we want to add it, we to fix the performance fist. i.e. avoid it calling GetWeightedLoweredAndTrimmedTextCounts for every single brand requested, or make the query near-instant since it currently pegs the live SQL Server CPU at 100%
                errorList.Add($"Metrics with 'text' calculation type are not currently supported via this API");
            }

            if (EndDateBeforeStartDate())
            {
                errorList.Add($"End Date {EndDate:yyyy-MM-dd} is greater than the Start Date {StartDate:yyyy-MM-dd}");
            }

            if (!ClassIdsMatchMetricDescriptor(out var validKeys))
            {
                string requestKeys = string.Join(", ", _orderedClassIds);
                string validKeyString = string.Join(", ", validKeys);
                errorList.Add(
                    $"{nameof(MetricCalculationRequest.ClassInstances)} key combination: [{requestKeys}] is invalid for '{MetricDescriptor.Name}' metric. Keys should be [{validKeyString}]");
            }

            if (AnyClassInstanceIdArraysEmpty())
            {
                errorList.Add(
                    $"{nameof(MetricCalculationRequest.ClassInstances)} {nameof(ClassInstanceDescriptor.ClassInstanceId)} must be specified.");
            }

            if (AnyClassInstanceIdsNotInSurveySet(out string idString))
            {
                errorList.Add(
                    $"Invalid {nameof(ClassInstanceDescriptor.ClassInstanceId)}s have been found in the request. They are {idString}.");
            }

            if (!WithinResponseLimit(out int totalResultCount))
            {
                errorList.Add($"Request has exceeded the {_resultLimit} result limit. Request would yield {totalResultCount} results.");
            }

            errors = errorList.ToArray();
            return !errors.Any();
        }

        private bool EndDateBeforeStartDate() =>
            EndDate < StartDate;

        private bool AnyClassInstanceIdArraysEmpty() =>
            _metricCalculationRequest.ClassInstances.Values.Any(ids => !ids.Any());

        private bool ClassIdsMatchMetricDescriptor(out string[] validKeys)
        {
            validKeys = MetricDescriptor.Measure.EntityCombination
                .Select(e => e.Identifier)
                .OrderBy(s => s)
                .ToArray();

            return validKeys.SequenceEqual(_orderedClassIds);
        }

        private bool AnyClassInstanceIdsNotInSurveySet(out string idString)
        {
            idString = "";
            var invalidIdsPerEntity = _metricCalculationRequest.ClassInstances
                .Select(kvp => (kvp.Key,
                    InvalidIds: kvp.Value.Except(_entityRepository.GetInstancesOf(kvp.Key, Surveyset)
                        .Select(e => e.Id))))
                .Where(ids => ids.InvalidIds.Any())
                .ToArray();

            if (!invalidIdsPerEntity.Any()) return false;

            var messagePerEntity = invalidIdsPerEntity.Select(i => $"{i.Key}: [{string.Join(", ", i.InvalidIds)}]");
            idString = string.Join(", ", messagePerEntity);
            return true;
        }

        private (DateTimeOffset calculationStartDate, DateTimeOffset calculationEndDate, AverageDescriptor overridenAverage) AdjustDateRangeAndAverageForCalculation(AverageDescriptor average)
        {
            if (average.Average.TotalisationPeriodUnit == TotalisationPeriodUnit.All)
            {
                return (_surveySetAccessor.StartDate, _surveySetAccessor.EndDate, average);
            }

            var calculationEndDate = _metricCalculationRequest.EndDate ?? _surveySetAccessor.EndDate;
            var calculationStartDate = _metricCalculationRequest.StartDate ?? calculationEndDate;

            if (average.Average.MakeUpTo != MakeUpTo.Day)
            {
                calculationEndDate = ResultDateCalculator.GetLast(calculationEndDate, average.Average);
                calculationStartDate = ResultDateCalculator.GetFirstDayOfPeriodForAverage(average.Average,
                    _metricCalculationRequest.StartDate.HasValue ? calculationStartDate : calculationEndDate);
            }

            // ToDateInstance to eliminate the offset in the same way as the DateModelBinder
            return (calculationStartDate.ToDateInstance(), calculationEndDate.ToDateInstance(), average);
        }

        private TargetInstances[] ToTargetInstancesCollection()
        {
            var requestedTargetInstances = _metricCalculationRequest.ClassInstances.Select(rec =>
            {
                var classInstances =
                    _entityRepository.GetInstancesOf(rec.Key, Surveyset).Where(ei => rec.Value.Contains(ei.Id));
                return new TargetInstances(new EntityType {Identifier = rec.Key}, classInstances);
            }).ToArray();

            if (!requestedTargetInstances.Any())
            {
                requestedTargetInstances = new[]
                    {new TargetInstances(EntityType.ProfileType, Array.Empty<EntityInstance>())};
            }

            return requestedTargetInstances;
        }

        private bool WithinResponseLimit(out int resultCount)
        {
            int numberOfDatesRequested = Average.Average.TotalisationPeriodUnit switch
            {
                TotalisationPeriodUnit.All => 1,
                TotalisationPeriodUnit.Month => (EndDate.Year - StartDate.Year) * 12 + EndDate.Month - StartDate.Month + 1,
                _ when Average.Average.MakeUpTo == MakeUpTo.WeekEnd => ((EndDate - StartDate).Days + 1)/7,
                _ => (EndDate - StartDate).Days + 1
            };

            int numberOfCartesianCombinations = _metricCalculationRequest.ClassInstances
                .Select(kvp => kvp.Value.Length)
                .Aggregate(1, (current, next) => current * next);

            resultCount = numberOfDatesRequested * numberOfCartesianCombinations;
            return resultCount <= _resultLimit;
        }
    }

}
