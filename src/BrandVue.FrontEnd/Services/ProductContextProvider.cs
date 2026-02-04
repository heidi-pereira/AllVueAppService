using BrandVue.EntityFramework.Answers.Model;
using BrandVue.Middleware;
using BrandVue.SourceData.Import;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using BrandVue.EntityFramework;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Utils;
using Microsoft.Extensions.Logging;

namespace BrandVue.Services
{
    public class ProductContextProvider
    {
        private readonly AppSettings _appSettings;
        private static readonly int _nonExistentSurveyId = -1;

        private static IReadOnlyDictionary<(string ProductShortCode, string SubProductId), SubProductSettings>
            _configuredSettings;

        private readonly IAnswerDbContextFactory _answerDbContextFactory;
        private readonly ILogger<ProductContextProvider> _logger;

        public ProductContextProvider(AppSettings appSettings, IAnswerDbContextFactory answerDbContextFactory, ILogger<ProductContextProvider> logger)
        {
            _appSettings = appSettings;
            _answerDbContextFactory = answerDbContextFactory;
            _logger = logger;
        }


        private IReadOnlyDictionary<(string ProductShortCode, string SubProductId), SubProductSettings> ConfiguredSettings
        {
            get
            {
                if (_configuredSettings == null)
                {
                    var configuredSettings =
                        new Dictionary<(string ProductShortCode, string SubProductId), SubProductSettings>(
                            TupleEqualityComparer.Create(StringComparer.OrdinalIgnoreCase,
                                StringComparer.OrdinalIgnoreCase)
                        );
                    foreach (var kvp in _appSettings.ProductSettings)
                    {
                        string[] keys = kvp.Key.Split("|");
                        string productShortCode = keys[0];
                        string subProductId = keys.Length > 1 ? keys[1] : null;

                        configuredSettings.Add((productShortCode, subProductId), kvp.Value);
                    }
                    _configuredSettings = configuredSettings;
                }
                return _configuredSettings;
            }
        }

        private static SubProductSettings CreateSubProductSettings(SurveyGroupType surveyGroupType)
        {
            return new SubProductSettings(
                HasSingleClient: surveyGroupType == SurveyGroupType.AllVue,
                KeepInMemory: surveyGroupType != SurveyGroupType.AllVue,
                AllowPartialDays: surveyGroupType == SurveyGroupType.AllVue,
                DisableAutoMetricFiltering: surveyGroupType != SurveyGroupType.AllVue,
                GenerateFromAnswersTable: surveyGroupType == SurveyGroupType.AllVue,
                OverrideAverageIds: new string[] { });
        }

        public IProductContext ProvideProductContext(RequestScope requestScope) => ProvideProductContext(requestScope.ProductName, requestScope.SubProduct);

        public IProductContext ProvideProductContext(string productName, string subProductId)
        {
            var subProductSettings = ConfiguredSettings.TryGetValue((productName, subProductId), out var sps) ? sps : null;
            var surveyGroupType = BrandVueDataLoader.IsSurveyVue(productName) ? SurveyGroupType.AllVue : SurveyGroupType.BrandVue;
            var productToLookFor = surveyGroupType== SurveyGroupType.AllVue ? subProductId : productName;


            using var dbContext = _answerDbContextFactory.CreateDbContext();
            if ( (surveyGroupType == SurveyGroupType.AllVue) && (int.TryParse(subProductId, out int surveyId)) )
            {
                var survey = dbContext.Surveys.FirstOrDefault(s => s.SurveyId == surveyId);
                var isSurveyOpen = survey?.IsOpen ?? true;

                subProductSettings ??= CreateSubProductSettings(surveyGroupType);

                return new ProductContext(productName, subProductId, survey?.DisplayName, survey?.UniqueSurveyId,
                    survey?.AuthCompanyId,
                    isSurveyOpen, surveyGroupType == SurveyGroupType.AllVue, subProductSettings)
                {
                    KimbleProposalId = survey?.KimbleProposalId,
                };
            }

            var surveyGroup = dbContext.SurveyGroups
                .Include(g => g.Surveys).ThenInclude(s => s.Survey)
                .FirstOrDefault(g => g.Type == surveyGroupType && g.UrlSafeName == productToLookFor);
            if (surveyGroup != null)
            {
                var surveys = surveyGroup.Surveys.Select(s => s.Survey).ToArray();

                if (surveys.Any(s => s == null))
                {
                    throw new KeyNotFoundException(
                        $"Could not find surveys for survey group: (Id: '{surveyGroup.SurveyGroupId}', Name: '{surveyGroup.Name}')");
                }

                var authCompanyId = surveys.FirstOrDefault()?.AuthCompanyId;
                if (surveys.Any(s => s.AuthCompanyId != authCompanyId))
                {
                    throw new ConfigurationErrorsException(
                        $"Surveys are not all from the same company for survey group: (Id: '{surveyGroup.SurveyGroupId}', Name: '{surveyGroup.Name}')");
                }

                var surveyRecords = surveys.Select(s => new SurveyRecord
                {
                    SurveyId = s.SurveyId,
                    SurveyName = s.DisplayName,
                }).ToArray();
                var mostPopularKimbleProposalId = surveys.GroupBy(x => x.KimbleProposalId).Where(x => !string.IsNullOrEmpty(x.Key))
                    .OrderByDescending(x => x.Count()).FirstOrDefault()?.Key;

                subProductSettings ??= CreateSubProductSettings(surveyGroupType);

                return new ProductContext(productName, subProductId, surveyGroup.Name, 
                    surveyUid: "", authCompanyId, isSurveyOpen:true, 
                    surveyGroupType == SurveyGroupType.AllVue, subProductSettings)
                {
                    NonMapFileSurveys = surveyRecords, 
                    IsSurveyGroup = true,
                    SurveyGroupId = surveyGroup.SurveyGroupId,
                    KimbleProposalId = mostPopularKimbleProposalId,
                };
            }

            _logger.LogError("{product}, {subProductId} was not correctly configured", productName, subProductId);

            return new ProductContext(productName, subProductId, subProductId, surveyUid:null, 
                surveyAuthCompanyId:null, isSurveyOpen:false, isSurveyVue: surveyGroupType == SurveyGroupType.AllVue, subProductSettings)
            {
                NonMapFileSurveys = new[]{new SurveyRecord(){SurveyId = _nonExistentSurveyId, SurveyName = subProductId } },
            };
        }

        internal IEnumerable<string> GetSubProductsToEagerlyLoad(string productToLoadDataFor)
        {
            if (!BrandVueDataLoader.IsSurveyVue(productToLoadDataFor)) return new string[] { null };
            if (!_appSettings.AllowEagerlyLoadingSubProducts) return Array.Empty<string>();

            return ConfiguredSettings.Where(s => s.Key.ProductShortCode == productToLoadDataFor && s.Value.KeepInMemory)
                .Select(s => s.Key.SubProductId);
        }
    }
}
