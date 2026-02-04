using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Import;

public class SubsetRepositoryLoader
{
    private readonly IBrandVueDataLoaderSettings _settings;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly IChoiceSetReader _choiceSetReader;
    private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;
    private readonly IDbContextFactory<MetaDataContext> _metaDataContextFactory;
    private readonly IProductContext _productContext;

    public SubsetRepositoryLoader(IBrandVueDataLoaderSettings settings, 
        ILoggerFactory loggerFactory,
        IChoiceSetReader choiceSetReader,
        ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
        IDbContextFactory<MetaDataContext> metaDataContextFactory,
        IProductContext productContext)
    {
        _settings = settings;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<SubsetRepositoryLoader>();
        _choiceSetReader = choiceSetReader;
        _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
        _metaDataContextFactory = metaDataContextFactory;
        _productContext = productContext;
    }

    private void AutoCreateSubsetsForAllVue(SubsetRepository subsetRepository)
    {
        var anySubsetsAlreadyLoaded = subsetRepository.Any(); 
        var isSubsetDisabled = anySubsetsAlreadyLoaded;
        var allSubsetAlreadyExist = subsetRepository.TryGet(BrandVueDataLoader.All, out var _);

        if (!anySubsetsAlreadyLoaded || !allSubsetAlreadyExist)
        {
            var nonMapSurveyIds = _productContext.NonMapFileSurveyIds;
            var allSegments = _choiceSetReader.GetSegments(nonMapSurveyIds);
            var generatedSubsets = GenerateSubsetsFromSegments(nonMapSurveyIds, allSegments);

            foreach (var subset in generatedSubsets)
            {
                if (!subsetRepository.Any(x => x.Id == subset.Id))
                {
                    subset.Disabled = isSubsetDisabled;
                    subsetRepository.Add(subset);
                }
            }
        }
    }

    private SubsetRepository LoadSubsetsFromDatabase()
    {
        var subsetRepository = new SubsetRepository();
        var dbSubsets = new SubsetConfigurationRepositorySql(_metaDataContextFactory, _productContext, _choiceSetReader, null).GetAll();
        if (dbSubsets.Any())
        {
            foreach (var dbSubset in dbSubsets)
            {
                var newSubset = new Subset { Id = dbSubset.Identifier };
                OverrideSubset(newSubset, dbSubset);
                subsetRepository.Add(newSubset);
            }
        }
        return subsetRepository;
    }

    public SubsetRepository LoadSubsetConfiguration(AllVueConfiguration allVueConfiguration)
    {
        var subsetRepository = LoadSubsetsFromDatabase();
        if (_productContext.GenerateFromAnswersTable || _productContext.IsAllVue || !allVueConfiguration.AllowLoadFromMapFile)
        {
            AutoCreateSubsetsForAllVue(subsetRepository);
        }
        else
        {
            AutoloadSubsetsFromMapFile(subsetRepository);
        }
        UpdateSubsetValidity(subsetRepository);
        return subsetRepository;
    }

    void AutoloadSubsetsFromMapFile(SubsetRepository subsetRepository)
    {
        bool MapFileExistsForSubsets()
        {
            return System.IO.File.Exists(_settings.SubsetMetadataFilepath) && System.IO.File.Exists(_settings.SurveysMetadataFilePath);
        }

        if (MapFileExistsForSubsets())
        {
            var mapFileSubsets = new SubsetRepository();
            var mapFileSubsetLoader = new SubsetInformationLoader(mapFileSubsets, _commonMetadataFieldApplicator, _loggerFactory.CreateLogger<SubsetInformationLoader>());
            mapFileSubsetLoader.Load(_settings.SubsetMetadataFilepath, _settings.SurveysMetadataFilePath);

            foreach (var mapFile in mapFileSubsets)
            {
                if (!subsetRepository.TryGet(mapFile.Id, out var subset))
                {
                    var cloneSubset = new Subset()
                    {
                        Id = mapFile.Id,
                        Alias = mapFile.Alias,
                        AllowedSegmentNames = mapFile.AllowedSegmentNames,
                        Description = mapFile.Description,
                        Disabled = mapFile.Disabled,
                        DisplayName = mapFile.DisplayName,
                        DisplayNameShort = mapFile.DisplayNameShort,
                        EnableRawDataApiAccess = mapFile.EnableRawDataApiAccess,
                        Environment = mapFile.Environment,
                        ExternalUrl = mapFile.ExternalUrl,
                        Iso2LetterCountryCode = mapFile.Iso2LetterCountryCode,
                        MinimumDataSpan = mapFile.MinimumDataSpan,
                        Order = mapFile.Order,
                        ProductId = mapFile.ProductId,
                        SegmentIds = mapFile.SegmentIds,
                        SurveyIdToSegmentNames = mapFile.SurveyIdToSegmentNames,
                        OverriddenStartDate = mapFile.OverriddenStartDate,
                        AlwaysShowDataUpToCurrentDate = mapFile.AlwaysShowDataUpToCurrentDate,
                        ParentGroupName = mapFile.ParentGroupName
                    };
                    subsetRepository.Add(cloneSubset);
                }
            }
        }
    }

    private void UpdateSubsetValidity(ISubsetRepository subsetRepository)
    {
        foreach (var subset in subsetRepository.Where( subset=>!subset.Disabled) )
        {
            subset.SegmentIds ??= _choiceSetReader.GetSegmentIds(subset);
        }
    }

    private static void OverrideSubset(Subset subsetToOverride, SubsetConfiguration dbSubset)
    {
        subsetToOverride.Description = dbSubset.Description;
        subsetToOverride.Disabled = dbSubset.Disabled;
        subsetToOverride.DisplayName = dbSubset.DisplayName;
        subsetToOverride.DisplayNameShort = dbSubset.DisplayNameShort;
        subsetToOverride.EnableRawDataApiAccess = dbSubset.EnableRawDataApiAccess;
        subsetToOverride.Iso2LetterCountryCode = dbSubset.Iso2LetterCountryCode;
        subsetToOverride.Order = dbSubset.Order;
        subsetToOverride.SurveyIdToSegmentNames = dbSubset.SurveyIdToAllowedSegmentNames;
        subsetToOverride.Alias = dbSubset.Alias ?? dbSubset.Identifier;
        subsetToOverride.OverriddenStartDate = dbSubset.OverriddenStartDate;
        subsetToOverride.AlwaysShowDataUpToCurrentDate = dbSubset.AlwaysShowDataUpToCurrentDate;
        subsetToOverride.ParentGroupName = dbSubset.ParentGroupName;
    }

    private IEnumerable<Subset> GenerateSubsetsFromSegments(IReadOnlyList<int> surveyIds, IEnumerable<SurveySegment> allSegments)
    {
        var surveyIdsWithEmptySegmentNames =
            surveyIds.ToDictionary(id => id, _ => (IReadOnlyCollection<string>)Array.Empty<string>());
        var allSubset = CreateSubset(BrandVueDataLoader.All, surveyIdsWithEmptySegmentNames, 0);
        var subsets = new List<Subset> { allSubset };
        var segmentNameToSurveyIds = allSegments.ToLookup(x => x.SegmentName);
        var autoGenerateSubsets = segmentNameToSurveyIds.Count > 1;

        var results = _choiceSetReader.GetAnswerStats(surveyIds, allSegments.Select(s => s.SurveySegmentId).ToArray());

        foreach (var answerStat in results.ToList())
        {
            var surveySegment = allSegments.Single(x => x.SurveySegmentId == answerStat.SegmentId);
            if (autoGenerateSubsets)
            {
                var surveySegmentName = allSubset.Id == surveySegment.SegmentName ? $"Auto {surveySegment.SegmentName}" : surveySegment.SegmentName;

                var previouslyCreatedSubset = subsets.SingleOrDefault(x => String.Compare(x.Id, surveySegmentName, StringComparison.InvariantCultureIgnoreCase) == 0);

                if (previouslyCreatedSubset == null)
                {
                    var surveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                    {
                        { surveySegment.SurveyId, (IReadOnlyCollection<string>)new[] { surveySegment.SegmentName } }
                    };
                    subsets.Add(CreateSubset(surveySegmentName, surveyIdToSegmentNames));
                }
                else
                {
                    var surveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>(previouslyCreatedSubset.SurveyIdToSegmentNames)
                    {
                        { surveySegment.SurveyId, (IReadOnlyCollection<string>)new[] { surveySegment.SegmentName } }
                    };
                    previouslyCreatedSubset.SurveyIdToSegmentNames = surveyIdToSegmentNames;
                }
            }
        }

        var segmentsWithNoResponses = allSegments.Where(x => !results.Any(result => result.SegmentId == x.SurveySegmentId));
        foreach (var missingSegment in segmentsWithNoResponses)
        {
            _logger.LogInformation(
                "{Product} Survey {SurveyId} not generating subset for segment {SegmentName} since there are no responses for it",
                _productContext, missingSegment.SurveyId, missingSegment.SegmentName);
        }

        if (!results.Any())
        {
            _logger.LogWarning($@"{_productContext} Surveys {string.Join(", ", surveyIds)} have no data in any segments {string.Join(", ", segmentNameToSurveyIds.Select(x => x.Key))}.
                                   Please check survey has published correctly");
        }

        return subsets;
    }

    private static Subset CreateSubset(string subsetName, IReadOnlyDictionary<int, IReadOnlyCollection<string>> surveyIdToSegmentNames, int order = 100)
    {
        return new Subset
        {
            Id = subsetName,
            Disabled = true,
            DisplayName = subsetName,
            DisplayNameShort = subsetName,
            EnableRawDataApiAccess = true,
            Alias = subsetName,
            Iso2LetterCountryCode = "GB",
            Environment = Array.Empty<string>(),
            SurveyIdToSegmentNames = surveyIdToSegmentNames,
            Order = order,
        };
    }
}
