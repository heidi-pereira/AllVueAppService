using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.Import
{
    public class MapFileQuotaCellDescriptionProvider : IQuotaCellDescriptionProvider
    {
        public static class DefaultHumanNames
        {
            public const string Gender = "gender";
            public const string Region = "region";
            public const string Age = "ageGroup";
            public const string Seg = "socioEconomicGroupIndicator";
        }

        private readonly IDictionary<Subset, Lazy<IQuotaFieldMapper>> _genderMappers;
        private readonly IDictionary<Subset, Lazy<IQuotaFieldMapper>> _regionMappers;
        private readonly IDictionary<Subset, Lazy<IQuotaFieldMapper>> _socialGroupMappers;
        private readonly IDictionary<Subset, Lazy<IQuotaFieldMapper>> _ageGroupMappers;
        internal static readonly IReadOnlyDictionary<string, string> FieldToHumanName = 
            new Dictionary<string, string>
            {
                { DefaultQuotaFieldGroups.Age, DefaultHumanNames.Age}, 
                { DefaultQuotaFieldGroups.Region, DefaultHumanNames.Region}, 
                { DefaultQuotaFieldGroups.Seg, DefaultHumanNames.Seg }, 
                { DefaultQuotaFieldGroups.Gender, DefaultHumanNames.Gender}
            };

        public MapFileQuotaCellDescriptionProvider(CategoryMappingFactory categoryMappingFactory, ISubsetRepository subsets)
        {
            _ageGroupMappers = new Dictionary<Subset, Lazy<IQuotaFieldMapper>>();
            _regionMappers = new Dictionary<Subset, Lazy<IQuotaFieldMapper>>();
            _socialGroupMappers = new Dictionary<Subset, Lazy<IQuotaFieldMapper>>();
            _genderMappers = new Dictionary<Subset, Lazy<IQuotaFieldMapper>>();

            foreach (var subset in subsets)
            {
                _genderMappers.Add(subset, new Lazy<IQuotaFieldMapper>(() => new MapFileQuotaFieldMapper(DefaultQuotaFieldGroups.Gender, categoryMappingFactory.CreateMapperParameters(RespondentFields.Gender, subset))));
                _ageGroupMappers.Add(subset, new Lazy<IQuotaFieldMapper>(() => new MapFileQuotaFieldMapper(DefaultQuotaFieldGroups.Age, categoryMappingFactory.CreateMapperParameters(RespondentFields.Age, subset))));
                _regionMappers.Add(subset, new Lazy<IQuotaFieldMapper>(() => new MapFileQuotaFieldMapper(DefaultQuotaFieldGroups.Region, categoryMappingFactory.CreateMapperParameters(RespondentFields.Region, subset))));

                _socialGroupMappers.Add(
                        subset,
                        new Lazy<IQuotaFieldMapper>(() =>
                            new MapFileQuotaFieldMapper(DefaultQuotaFieldGroups.Seg, RespondentFields.GetSegFieldsForCountryCode(subset.Iso2LetterCountryCode).Select(
                                profileField =>
                                    categoryMappingFactory.CreateMapperParameters(
                                        profileField, subset,
                                        "Seg")).ToArray())));
            }
        }

        public string GetDescriptionForQuotaCellKey(Subset subset, string questionIdentifier, string quotaCellKey)
        {
            var mapper = GetOrCreateMapperFor(subset, questionIdentifier);
            return mapper?.GetDescriptionForQuotaCellKey(quotaCellKey);
        }

        public IReadOnlyDictionary<string, string> GetIdentifiersToKeyPartDescriptions(QuotaCell quotaCell)
        {
            if (quotaCell.IsUnweightedCell)
            {
                return null;
            }
            return quotaCell.FieldGroupToKeyPart.ToDictionary(f => FieldToHumanName[f.Key],
                f => GetDescriptionForQuotaCellKey(quotaCell.Subset, f.Key, f.Value));
        }

        internal static IReadOnlyCollection<string> GetFieldsForSubset(Subset subset) => RespondentFields.CommonFieldsForCountryCode(subset.Iso2LetterCountryCode);

        public IQuotaFieldMapper GetOrCreateMapperFor(Subset subset, string fieldName)
        {
            return fieldName switch
            {
                DefaultQuotaFieldGroups.Region => RegionGroupMapper(subset),
                DefaultQuotaFieldGroups.Age => AgeGroupMapper(subset),
                DefaultQuotaFieldGroups.Gender => GenderMapper(subset),
                DefaultQuotaFieldGroups.Seg => SocialGroupMapper(subset),
                _ => throw new ArgumentOutOfRangeException($"No mapper exists for {fieldName}")
            };
        }

        public IReadOnlyCollection<IQuotaFieldMapper> GetMappersForSubset(Subset subset, int? schemeId)
        {
            return new[]
            {
                GetOrCreateMapperFor(subset, DefaultQuotaFieldGroups.Region),
                GetOrCreateMapperFor(subset, DefaultQuotaFieldGroups.Gender),
                GetOrCreateMapperFor(subset, DefaultQuotaFieldGroups.Age),
                GetOrCreateMapperFor(subset, DefaultQuotaFieldGroups.Seg)
            };
        }

        private IQuotaFieldMapper AgeGroupMapper(Subset subset)
        {
            return _ageGroupMappers.TryGetValue(subset, out var mapper)
                ? mapper.Value
                : throw new ArgumentOutOfRangeException(
                    nameof(subset),
                    subset,
                    $"Data subset {subset} is not supported for age group mapping.");
        }

        private IQuotaFieldMapper GenderMapper(Subset subset)
        {
            return _genderMappers.TryGetValue(subset, out var mapper)
                ? mapper.Value
                : throw new ArgumentOutOfRangeException(
                    nameof(subset),
                    subset,
                    $"Data subset {subset} is not supported for gender mapping.");
        }

        private IQuotaFieldMapper RegionGroupMapper(Subset subset)
        {
            return _regionMappers.TryGetValue(subset, out var mapper)
                ? mapper.Value
                : throw new ArgumentOutOfRangeException(
                    nameof(subset),
                    subset,
                    $"Data subset {subset} is not supported for region mapping.");
        }

        private IQuotaFieldMapper SocialGroupMapper(Subset subset)
        {
            return _socialGroupMappers.TryGetValue(subset, out var mapper)
                ? mapper.Value
                : null;
        }
    }
}
