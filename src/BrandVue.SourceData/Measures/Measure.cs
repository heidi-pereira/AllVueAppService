using System.Diagnostics;
using System.Runtime.Serialization;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Models;
using BrandVue.SourceData.Models.Filters;
using BrandVue.SourceData.Utils;
using BrandVue.SourceData.Variable;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace BrandVue.SourceData.Measures
{

    /// <summary>
    /// This should be renamed to "Metric".
    /// It's the app model which can actually calculate the value of a measure for given entity values.
    ///
    /// Most relevant properties:
    /// CalcType - Operation to perform when aggregating across respondents
    /// PrimaryVariable - the value to use for each respondent
    /// TrueVals - Optional convenience filter on the output of PrimaryVariable to avoid declaring a whole new variable
    /// BaseVariable - only include respondents where this returns true
    /// EntityCombination - The shape of the data's context.
    ///
    /// Ignoring weighting for a moment, here's 
    /// 
    /// e.g. CalcType: YesNo (i.e. percentage)
    /// inBase = respondents.Where(r => BaseVariable(r))
    /// result = inBase.Count(r => Variable(r) is not null && TrueVals?.Contains(Variable(r)) != false) / inBase.Count()
    ///
    /// e.g. CalcType: Average
    /// inBase = respondents.Where(r => BaseVariable(r))
    /// result = inBase.Where(r => Variable(r) is not null && TrueVals?.Contains(Variable(r)) != false).Sum(r => Variable(r)) / inBase.Count()
    /// 
    /// </summary>
    [DebuggerDisplay("Name: {Name}, Field: {Field}, Field2: {Field2}, CalculationType: {CalculationType}")]
    public class Measure : IEnvironmentConfigurable, ISubsetConfigurable, IDisableable
    {
        public void Initialize()
        {
            NumberFormatString = MetricNumberFormatter.ParseNumberFormat(NumberFormat);
            UrlSafeName ??= Name.SanitizeUrlSegment();
        }

        public const string UseEqualWeightingMeasureId = "UseEqualWeighting";

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string VarCode { get; set; }
        public string PrimaryVariableIdentifier { get; set; }
        private IVariable<int?> _primaryVariable;
        private AllowedValues _legacyBaseValues = new();
        private AllowedValues _legacyPrimaryTrueValues = new();
        private AllowedValues _legacySecondaryTrueValues = new();

        /// <summary>
        /// This is used in the public api for requesting metric data.
        /// It is safer for third party integrations to refer to
        /// our metrics by a url safe name to avoid having to allow
        /// potentially dangerous characters in the url.
        /// </summary>
        public string UrlSafeName { get; set; }

        /// <summary>
        /// Type of response entity on which measure operates:
        /// e.g., profile, brand.
        /// </summary>
        public IReadOnlyCollection<EntityType> PrimaryFieldEntityCombination => PrimaryVariable?.UserEntityCombination ?? Field?.EntityCombination ?? Array.Empty<EntityType>();

        /// <summary>
        /// Type of response entity on which measure field 2 operates:
        /// e.g., profile, brand.
        /// </summary>
        [JsonIgnore]
        internal IReadOnlyCollection<EntityType> SecondaryFieldEntityCombination => Field2?.EntityCombination ?? Array.Empty<EntityType>();

        /// <summary>
        /// Type of response entity on which eligible measure
        /// responses are based. E.g., might look at dress size
        /// for respondents who are customers of Next, in which
        /// case Type is profile (because dress size is a profile
        /// attribute) and BaseResponseType is brand (because
        /// whether somebody is a customer of a brand is a brand
        /// property).
        /// </summary>
        [JsonIgnore]
        public IReadOnlyCollection<EntityType> BaseEntityCombination => BaseExpression?.UserEntityCombination ?? BaseField?.EntityCombination ?? PrimaryFieldEntityCombination;

        public ResponseFieldDescriptor Field { get; set; }
        public ResponseFieldDescriptor Field2 { get; set; }

        [JsonIgnore]
        public IVariable<int?> PrimaryVariable
        {
            get => _primaryVariable;
            set
            {
                HasCustomFieldExpression = value != null;
                _primaryVariable = value;
            }
        }

        [JsonIgnore]
        public FilterInfo PrimaryDisplayInfo { get; set; }
        
        [JsonIgnore]
        public FilterInfo BaseDisplayInfo { get; set; }
        
        [JsonIgnore]
        public IVariable<bool> BaseExpression { get; set; }
        /// <summary>
        /// For display purposes. Do not parse this, if you need the parsed version, get the stored version from FieldExpressionParser.
        /// </summary>
        public string BaseExpressionString { get; set; }
        public string SubsetSpecificBaseDescription { get; set; } = "";
        public bool HasCustomBase { get; set; } = false;
        public FieldOperation FieldOperation { get; set; }
        public CalculationType CalculationType { get; set; }
        public double? ScaleFactor { get; set; }

        /// <summary>
        /// HasCustomFieldExpression - is set to true when a none null variable expression (user's custom python) is set.
        /// Is used to control YesNo and Average calc types to treat 0 values as null.
        /// See YesNoTotalTransformer and AverageTotalTransformer for implementation details.
        /// </summary>
        public bool HasCustomFieldExpression { get; set; }

        public bool IsNumericVariable => VariableConfigurationId.HasValue && NumericVariableField != null;
        public ResponseFieldDescriptor NumericVariableField { get; set; }

        internal Measure ShallowCopy()
        {
            return (Measure) MemberwiseClone();
        }

        /// <summary>
        /// TODO: PERF: Create this once after all fields initialized
        /// </summary>
        public IReadOnlyCollection<ResponseFieldDescriptor> GetFieldDependencies()
        {
            var baseFieldDependencies = BaseFieldDependencies;
            var fieldDependencies = PrimaryFieldDependencies;
            return fieldDependencies.Concat(Field2.YieldNonNull()).Concat(baseFieldDependencies).Distinct().ToArray();
        }

        public IEnumerable<ResponseFieldDescriptor> BaseFieldDependencies => BaseExpression?.FieldDependencies ?? BaseField?.Yield() ?? Enumerable.Empty<ResponseFieldDescriptor>();
        public IEnumerable<ResponseFieldDescriptor> PrimaryFieldDependencies => PrimaryVariable?.FieldDependencies ?? Field?.Yield() ?? Enumerable.Empty<ResponseFieldDescriptor>();

        public bool IsUsingFieldExpressions => PrimaryVariable is {} || BaseExpression is {};

        //
        // Investgate if this is truely needed
        // https://app.shortcut.com/mig-global/story/82025/allvue-possible-problems-when-creating-date-based-wave-variables
        //
        [DataMember]
        public bool IsWaveMeasure => (PrimaryVariable is DataWaveVariable ||
                                      (PrimaryVariable is FilteredVariable { WrappedVariable: DataWaveVariable }));
        [DataMember]
        public bool IsSurveyIdMeasure => (PrimaryVariable is SurveyIdVariable ||
                                      (PrimaryVariable is FilteredVariable { WrappedVariable: SurveyIdVariable }));
        public ResponseFieldDescriptor BaseField { get; set; }

        [JsonIgnore]
        public bool HasDistinctBaseField =>
            BaseField != null && !BaseField.Equals(Field);

        public bool HasBaseExpression => BaseExpression != null;
        public string Description { get; set; }
        public string HelpText { get; set; }
        public string NumberFormatString { get; set; }
        public string NumberFormat { get; set; }
        [JsonProperty("min")]
        public int? Minimum { get; set; }
        [JsonProperty("max")]
        public int? Maximum { get; set; }
        public DateTimeOffset? StartDate { get; set; }

        public string FilterValueMapping { get; set; }
        [JsonIgnore]
        public IVariable<int?> FilterValueMappingVariable { get; set; }
        [JsonIgnore]
        public VariableConfiguration FilterValueMappingVariableConfiguration { get; set; }
        public string FilterGroup { get; set; }
        public bool FilterMulti { get; set; }
        public bool DownIsGood { get; set; }
        public int? PreNormalisationMinimum { get; set; }
        public int? PreNormalisationMaximum { get; set; }
        /// <summary>
        /// Null/empty means all
        /// </summary>
        public Subset[] Subset { get; private set; }

        public bool DisableMeasure { get; set; }
        public bool DisableFilter { get; set; }
        public bool EligibleForMetricComparison { get; set; }
        public string[] Environment { get; set; }
        public bool Disabled { get; set; }
        public bool HasData { get; set; }
        public bool EligibleForCrosstabOrAllVue { get; set; }
        public int? VariableConfigurationId { get; set; }
        public int? BaseVariableConfigurationId { get; set; }
        public bool QuestionShownInSurvey { get; set; }
        public EntityMeanMap EntityInstanceIdMeanCalculationValueMapping { get; set; }

        public IEnumerable<EntityType> EntityCombination => PrimaryFieldEntityCombination.Union(Field2?.EntityCombination ?? Enumerable.Empty<EntityType>()).Union(BaseEntityCombination).Distinct();

        public string DefaultSplitByEntityTypeName { get; set; }

        internal Func<IProfileResponseEntity, int?> PrimaryFieldValueCalculator(EntityValueCombination primaryFieldEntityValues)
        {
            return PrimaryVariable is { } primaryExpression
                ? primaryExpression.CreateForEntityValues(primaryFieldEntityValues)
                : p => p.GetIntegerFieldValue(Field, primaryFieldEntityValues);
        }

        internal Func<IProfileResponseEntity, int?> SecondaryFieldValueCalculator(EntityValueCombination secondaryFieldEntityValues)
        {
            return p => p.GetIntegerFieldValue(Field2, secondaryFieldEntityValues);
        }

        internal Func<IProfileResponseEntity, bool> CheckShouldIncludeInBase(EntityValueCombination baseFieldEntityValues)
        {
            return BaseExpression is {} baseExpression
                ? baseExpression.CreateForEntityValues(baseFieldEntityValues)
                : p => LegacyBaseValues.Contains(GetBaseFieldValue(p, baseFieldEntityValues));
        }

        private int? GetBaseFieldValue(IProfileResponseEntity profileResponse,
            EntityValueCombination baseFieldEntityValues)
        {
            var baseField = HasDistinctBaseField ? BaseField : Field;
            return profileResponse.GetIntegerFieldValue(baseField, baseFieldEntityValues);
        }

        public string[] ExcludeList { get; set; }
        public string MarketAverageBaseMeasure { get; set; } = UseEqualWeightingMeasureId;

        [DataMember]
        public bool IsBasedOnCustomVariable => GenerationType == AutoGenerationType.Original && VariableConfigurationId.HasValue;

        public string OriginalMetricName { get; internal set; }

        public AutoGenerationType GenerationType { get; internal set; }
        
        /// <summary>
        /// Aim to define metrics/measures with a BaseExpression rather than this and BaseField
        /// </summary>
        public AllowedValues LegacyBaseValues
        {
            [NotNull]
            get => _legacyBaseValues;
            set => _legacyBaseValues = value ?? new();
        }

        /// <summary>
        /// Aim to define metrics/measures with a PrimaryVariable rather than this and Field
        /// </summary>
        public AllowedValues LegacyPrimaryTrueValues
        {
            [NotNull]
            get => _legacyPrimaryTrueValues;
            set => _legacyPrimaryTrueValues = value ?? new();
        }

        /// <summary>
        /// Aim to define metrics/measures with a PrimaryVariable rather than this and SecondaryField
        /// </summary>
        public AllowedValues LegacySecondaryTrueValues
        {
            [NotNull]
            get => _legacySecondaryTrueValues;
            set => _legacySecondaryTrueValues = value ?? new();
        }

        /// <returns>The data targets which are in the question model, and used to query the db, but the user doesn't see e.g. because they're hardcoded within a field</returns>
        internal IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetImplicitDataTargets(Subset subset)
        {
            var fieldsDataTargets = PrimaryVariable?.GetDatabaseOnlyDataTargets(subset) ?? CreateEmptyDataTargets(Field.YieldNonNull());
            var fieldsDataTargetsWithSecondary = fieldsDataTargets.Concat(CreateEmptyDataTargets(Field2.YieldNonNull()));

            var baseFieldsDataTargets = BaseExpression?.GetDatabaseOnlyDataTargets(subset) ?? CreateEmptyDataTargets(BaseField.YieldNonNull());

            return fieldsDataTargetsWithSecondary
                .Concat(baseFieldsDataTargets)
                .GroupBy(t => t.Field)
                .Select(g => (g.Key, g.SelectMany(t => t.DataTargets).ToArray()));
        }

        private static IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> CreateEmptyDataTargets(IEnumerable<ResponseFieldDescriptor> responseFieldDescriptors) =>
            responseFieldDescriptors.Select(f => (f, Array.Empty<IDataTarget>()));

        /// <summary>
        /// Only call this for respondents in the metric's base
        /// Though if the returned func returns null, it means they've been filtered out by an additional check and should not be included in the base
        /// </summary>
        public Func<IProfileResponseEntity, int?> MetricValueCalculator(EntityValueCombination primaryFieldEntityValues, EntityValueCombination secondaryFieldEntityValues)
        {
            var getPrimaryFieldValue = PrimaryFieldValueCalculator(primaryFieldEntityValues);
            var calcTypeTransformer = TotalCalculatorFactory.Create(this);
            return p => GetValue(getPrimaryFieldValue, p, secondaryFieldEntityValues, calcTypeTransformer);
        }

        private int? GetValue(Func<IProfileResponseEntity, int?> getPrimaryFieldValue, IProfileResponseEntity p, EntityValueCombination secondaryFieldEntityValues, ICalcTypeResponseValueTransformer calcTypeResponseValueTransformer)
        {
            var primaryFieldValue = getPrimaryFieldValue(p);
            if (primaryFieldValue == SpecialResponseFieldValues.OriginalMeaningLostToHistoryButDoNotUse) return null;

            var secondaryFieldValue = p.GetIntegerFieldValue(Field2, secondaryFieldEntityValues);
            if (FieldOperation == FieldOperation.Filter &&
                !LegacySecondaryTrueValues.Contains(secondaryFieldValue)) return null;
            return calcTypeResponseValueTransformer.Transform(primaryFieldValue, secondaryFieldValue);
        }

        public bool IsValidPrimaryValue(int? i) => Field is null ? i.HasValue : LegacyPrimaryTrueValues.Contains(i);
        public IReadOnlyList<string> GetSubsets()
        {
            return Subset.Select(x => x.Id).ToList();
        }

        public void SetSubsets(IEnumerable<string> subsets, ISubsetRepository repository)
        {
            Subset = subsets.Where(repository.HasSubset).Select(repository.Get).ToArray();
        }
    }
}
