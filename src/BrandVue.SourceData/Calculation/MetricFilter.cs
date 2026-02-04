using BrandVue.SourceData.Measures;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.LazyLoading;

namespace BrandVue.SourceData.Calculation
{
    public class MetricFilter : IFilter
    {
        private readonly IReadOnlyCollection<EntityType> _implicitEntityTypes;
        private readonly EntityValueCombination _entityValues;
        private readonly int[] _primaryValues;
        private readonly bool _invert;
        private readonly bool _treatPrimaryValuesAsRange;
        private readonly int _primaryRangeMaxValue;
        private readonly int _primaryRangeMinValue;
        private readonly Subset _subset;
        public Measure Metric { get; }
        /// <summary>
        /// For these types, get the value from the context of the request/result.
        /// Note: Don't try to apply a filter with an implicit brand to requested measure without brand as an entity. That doesn't make any sense.
        /// </summary>
        public IReadOnlyCollection<EntityType> ImplicitEntityCombination { get; }
        public int? OnlyValueOrDefault => !_invert && _primaryValues.Distinct().Count() == 1 ? _primaryValues[0] : null;

        public MetricFilter(Subset subset, Measure metric, EntityValueCombination entityValues, int[] primaryValues, bool invert = false, bool treatPrimaryValuesAsRange = false)
        {
            Metric = metric ?? throw new ArgumentNullException(nameof(metric), "Measure cannot be null.");
            _subset = subset;
            _entityValues = entityValues;
            ImplicitEntityCombination = Metric.EntityCombination.Except(entityValues.EntityTypes).ToArray();
            _primaryValues = primaryValues;
            _invert = invert;
            _treatPrimaryValuesAsRange = treatPrimaryValuesAsRange;

            if (_treatPrimaryValuesAsRange)
            {
                _primaryRangeMaxValue = _primaryValues[1] < 0 ? int.MaxValue : _primaryValues[1];
                _primaryRangeMinValue = _primaryValues[0] < 0 ? int.MinValue : _primaryValues[0];
            }

            ThrowIfInvalid();
        }

        public Func<IProfileResponseEntity, bool> CreateForEntityValues(EntityValueCombination targetEntityValues)
        {
            targetEntityValues = _entityValues.With(targetEntityValues.GetRelevantEntityValues(ImplicitEntityCombination).ToArray());
            var relevantBaseEntityValues = new EntityValueCombination(targetEntityValues.GetRelevantEntityValues(Metric.BaseEntityCombination));
            var relevantPrimaryEntityValues = new EntityValueCombination(targetEntityValues.GetRelevantEntityValues(Metric.PrimaryFieldEntityCombination));
            var getBase = Metric.CheckShouldIncludeInBase(relevantBaseEntityValues);
            var getPrimary = Metric.PrimaryFieldValueCalculator(relevantPrimaryEntityValues);
            var getSecondary = Metric.SecondaryFieldValueCalculator(relevantPrimaryEntityValues);
            return p => Apply(p, Metric, getBase, getPrimary, getSecondary);
        }

        private bool Apply(IProfileResponseEntity profileResponse, Measure measure, Func<IProfileResponseEntity, bool> isInBase,
            Func<IProfileResponseEntity, int?> getPrimary, Func<IProfileResponseEntity, int?> getSecondary)
        {
            if (!isInBase(profileResponse))
            {
                return false;
            }
            var primaryFilterFieldValue = getPrimary(profileResponse);
            var secondaryFilterFieldValue = getSecondary(profileResponse);
            return measure.FieldOperation switch
            {
                FieldOperation.Or => FilterIncludesValue(primaryFilterFieldValue) || FilterIncludesValue(secondaryFilterFieldValue),
                _ => FilterIncludesValue(primaryFilterFieldValue)
            };
        }

        public FieldsAndDataTargets
            GetFieldDependenciesAndDataTargets(IReadOnlyCollection<IDataTarget> resultDataTargets)
        {
            // What the user actually specified in the filter dropdown directly
            var explicitFilterTargets = _entityValues.AsReadOnlyCollection().Select(ev => new DataTarget(ev.EntityType, [ev.Value]));
            // When the user said "for the chosen brand", we need to grab the chosen brand from the result data targets
            var implicitTargetsFromUserContext = resultDataTargets.Where(t => ImplicitEntityCombination.Contains(t.EntityType)).ToArray();
            // A measure can be calculated based on other entities the user doesn't see. We still need to use these to get data from the db
            var implicitTargetsFromMeasure = Metric.GetImplicitDataTargets(_subset).SelectMany(x => x.DataTargets);

            return new(
                Metric.GetFieldDependencies(),
                explicitFilterTargets.Concat(implicitTargetsFromUserContext).Concat(implicitTargetsFromMeasure)
            );
        }

        private bool FilterIncludesValue(int? filterMeasureValue)
        {
            if (_primaryValues == null || _primaryValues.Length == 0) return true;

            bool matchesFilter = _treatPrimaryValuesAsRange
                ? filterMeasureValue >= _primaryRangeMinValue && filterMeasureValue <= _primaryRangeMaxValue
                : filterMeasureValue.HasValue && _primaryValues.Contains(filterMeasureValue.Value);

            return matchesFilter ^ _invert;
        }

        private void ThrowIfInvalid()
        {
            if (_treatPrimaryValuesAsRange && (_primaryValues == null || _primaryValues.Length != 2))
            {
                throw new InvalidOperationException(
                    $"Cannot use range measure filter with more or less than two values, or where values is null. You have {_primaryValues?.Length ?? 0} values.");
            }
        }
    }
}
