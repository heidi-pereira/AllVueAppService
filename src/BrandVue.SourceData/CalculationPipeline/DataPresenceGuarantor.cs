using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.CalculationPipeline
{
    public class DataPresenceGuarantor : IDataPresenceGuarantor
    {
        private readonly ILazyDataLoader _lazyDataLoader;
        private readonly IEntityRepository _entityRepository;
        private readonly IDataLimiter _dataLimiter;
        private readonly IRespondentDataLoader _respondentDataLoader;

        public DataPresenceGuarantor(ILazyDataLoader lazyDataLoader, IEntityRepository entityRepository, IRespondentDataLoader respondentDataLoader)
        {
            _lazyDataLoader = lazyDataLoader;
            _entityRepository = entityRepository;
            _dataLimiter = lazyDataLoader.DataLimiter;
            _respondentDataLoader = respondentDataLoader;
        }

        public IEnumerable<ProfileResponseEntity> LoadEmptyProfiles(Subset subset,
            ResponseFieldDescriptor[] responseFieldDescriptors)
        {
            foreach (var responseFieldData in _lazyDataLoader.GetResponses(subset, responseFieldDescriptors))
            {
                yield return new ProfileResponseEntity(responseFieldData.ResponseId,
                    responseFieldData.Timestamp.ToDateInstance(), responseFieldData.SurveyId);
            }
        }

        public async Task EnsureDataLoadedIntoMemory(IRespondentRepository respondentRepository, Subset subset,
            IEnumerable<Measure> measures,
            CalculationPeriod calculationPeriod, AverageDescriptor average, IFilter filter, Break[] breaks,
            CancellationToken cancellationToken)
        {
            foreach (var measure in measures)
            {
                var targetInstances = _entityRepository.CreateTargetInstances(subset, measure).Yield();
                await EnsureDataIsLoaded(respondentRepository, subset, measure, calculationPeriod, average, filter,
                    targetInstances.ToArray(), breaks, cancellationToken);
            }
        }

        public async Task EnsureDataIsLoaded<TOut>(IRespondentRepository respondentRepository, Subset subset,
            IVariable<TOut> variable,
            IReadOnlyCollection<IDataTarget> targetInstances, DateTimeOffset start, DateTimeOffset end,
            CancellationToken cancellationToken)
        {
            var allDataTargets = WithImplicitDataTargets(subset, variable, targetInstances);
            await _respondentDataLoader.PossiblyLoadMeasures(respondentRepository, subset, new(variable.FieldDependencies, allDataTargets), start.Ticks, end.Ticks, cancellationToken);
        }

        private static IDataTarget[] WithImplicitDataTargets(Subset subset, IVariable variable,
            IReadOnlyCollection<IDataTarget> targetInstances)
        {
            var dbDataTargets = variable.GetDatabaseOnlyDataTargets(subset).SelectMany(t => t.DataTargets);
            var allDataTargets = dbDataTargets.Concat(targetInstances).ToArray();
            return allDataTargets;
        }

        public async Task EnsureDataIsLoaded(IRespondentRepository respondentRepository, Subset subset, Measure measure,
            CalculationPeriod calculationPeriod, AverageDescriptor average, IFilter filter,
            IReadOnlyCollection<IDataTarget> originalTargetInstances, Break[] breaks,
            CancellationToken cancellationToken)
        {
            var measureDataTargets = measure.GetImplicitDataTargets(subset).SelectMany(t => t.DataTargets);
            var allFieldsAndDataTargets = new FieldsAndDataTargets(measure.GetFieldDependencies().ToList(),
                originalTargetInstances.Concat(measureDataTargets).ToList());
            allFieldsAndDataTargets = allFieldsAndDataTargets.UnionWith(GetTargetsForBreaks(subset, breaks));
            allFieldsAndDataTargets = allFieldsAndDataTargets.UnionWith(filter.GetFieldDependenciesAndDataTargets(originalTargetInstances));
            await EnsureDataIsLoadedForTargets(respondentRepository, subset, calculationPeriod, average, allFieldsAndDataTargets, cancellationToken);
        }

        private static FieldsAndDataTargets GetTargetsForBreaks(Subset subset, Break[] breaks)
        {
            if (breaks is not null)
            {
                var variablesWithInstances = breaks.SelectMany(b => b.FollowMany(x => x.ChildBreak))
                    .SelectMany(b => new (IVariable Variable, int[] Instances)[]
                        { (b.Variable, b.Instances), (b.BaseVariable, b.BaseInstances) })
                    .ToArray();
                var targetInstances = variablesWithInstances
                    .SelectMany(v => WithImplicitDataTargets(subset, v.Variable, v.Variable.UserEntityCombination.Select(e => new DataTarget(e, v.Instances)).ToArray()));

                return new(variablesWithInstances.SelectMany(v => v.Variable.FieldDependencies).ToArray(), targetInstances.ToArray());
            }

            return new(Array.Empty<ResponseFieldDescriptor>(), Array.Empty<IDataTarget>());
        }

        private async Task EnsureDataIsLoadedForTargets(IRespondentRepository respondentRepository, Subset subset, CalculationPeriod calculationPeriod, AverageDescriptor average, FieldsAndDataTargets allFieldsAndDataTargets, CancellationToken cancellationToken)
        {

            foreach (var period in calculationPeriod.Periods)
            {
                var additionalDaysToLoad = average.NumberOfPeriodsInAverage *
                                           (average.TotalisationPeriodUnit == TotalisationPeriodUnit.Day ? 1 : 31);
                var startDate = period.StartDate.Subtract(TimeSpan.FromDays(additionalDaysToLoad));

                // Only ask for data we know is available, otherwise we'll think we've cached something we haven't
                var endDate = DateTimeOffsetExtensions.Min(period.EndDate.EndOfDay(), _dataLimiter.LatestDateToRequest);

                // Ideally we should have called ToDateInstance higher up the stack in the creation of CalculationPeriod to guard against timezone issues from requesters
                // However, currently, we never intentionally request a startDate that doesn't align with the start of a day, so enforce it here to make reasoning about SQL lower down easier
                var startTicks = DateTimeOffsetExtensions.Min(startDate, endDate).ToDateInstance().Ticks;
                var endTicks = endDate.Ticks;

                await _respondentDataLoader.PossiblyLoadMeasures(respondentRepository, subset, allFieldsAndDataTargets, startTicks, endTicks, cancellationToken);
            }
        }
    }
}