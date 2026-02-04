using System.Linq;
using JetBrains.Annotations;

namespace BrandVue.EntityFramework
{
    public record SubProductSettings(bool HasSingleClient, bool KeepInMemory, bool AllowPartialDays, bool DisableAutoMetricFiltering, bool GenerateFromAnswersTable, string[] OverrideAverageIds = null)
    {
        [UsedImplicitly]
        public SubProductSettings() : this(false, false, false, false, false)
        {

        }
    }

    public class ProductContext : IProductContext
    {
        private readonly SubProductSettings _subProductSettings;
        public string ShortCode { get; }
        public string SubProductId { get; }
        public bool IsAllVue { get; }
        public IReadOnlyList<SurveyRecord> NonMapFileSurveys { get; init; }
        public IReadOnlyList<int> NonMapFileSurveyIds => NonMapFileSurveys.Select(s => s.SurveyId).ToArray();
        public string SurveyName { get; init; }
        public string SurveyUid { get; }
        public string SurveyAuthCompanyId { get; }
        public bool IsSurveyOpen { get; }
        public bool GenerateFromAnswersTable => _subProductSettings.GenerateFromAnswersTable;

        public bool AllowPartialDays => _subProductSettings.AllowPartialDays;

        public bool DayIsFromResponseEndTime { get; init; } = true;
        public bool GenerateFromSurveyIds => NonMapFileSurveys.Any();
        public bool IsContinuous { get; init; }
        public bool KeepInMemory => _subProductSettings.KeepInMemory;

        public bool HasSingleClient => _subProductSettings.HasSingleClient;
        public bool DisableAutoMetricFiltering => _subProductSettings.DisableAutoMetricFiltering;

        public bool IsSurveyGroup { get; init; }
        public int SurveyGroupId { get; init; }
        public string[] DefaultAveragesToInclude => _subProductSettings.OverrideAverageIds;
        public bool IncludeAllDefaultAverages => DefaultAveragesToInclude == null;
        public string KimbleProposalId { get; init; }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(SubProductId))
            {
                return ShortCode;
            }
            return $"{ShortCode}/{SubProductId}";
        }

        public ProductContext(string shortCode, string subProductId, bool isSurveyVue, string surveyName, string surveyAuthCompanyId = null, SubProductSettings subProductSettings = null)
        {
            ShortCode = shortCode;
            SubProductId = subProductId;
            SurveyName = surveyName;
            SurveyAuthCompanyId = surveyAuthCompanyId;
            NonMapFileSurveys = DefaultSurveyIdsForSubProduct(subProductId, surveyName);
            IsAllVue = isSurveyVue;
            _subProductSettings = subProductSettings ?? new SubProductSettings(HasSingleClient: isSurveyVue, KeepInMemory: !isSurveyVue, AllowPartialDays: isSurveyVue, 
                DisableAutoMetricFiltering: !isSurveyVue, GenerateFromAnswersTable: isSurveyVue, OverrideAverageIds: isSurveyVue ? new string[] { } : null);
        }

        public ProductContext(string shortCode, SubProductSettings subProductSettings = null)
            : this(shortCode, null, false, null, null, subProductSettings: subProductSettings)
        {
        }


        public ProductContext(string requestScopeProductName,
            string subProductId,
            string surveyName,
            string surveyUid,
            string surveyAuthCompanyId,
            bool isSurveyOpen,
            bool isSurveyVue,
            SubProductSettings subProductSettings)
            : this(requestScopeProductName, subProductId, isSurveyVue, surveyName, surveyAuthCompanyId, subProductSettings: subProductSettings)
        {
            SurveyUid = surveyUid;
            IsSurveyOpen = isSurveyOpen;
        }

        public string ShortCodeAndSubproduct()
        {
            if (string.IsNullOrEmpty(SubProductId))
            {
                return ShortCode;
            }
            return $"{ShortCode} {SubProductId}";
        }

        private static SurveyRecord[] DefaultSurveyIdsForSubProduct(string subProductId, string surveyName) =>
            subProductId != null && int.TryParse(subProductId, out var surveyId) ?
                new[] { new SurveyRecord { SurveyId = surveyId, SurveyName = surveyName } } :
                Array.Empty<SurveyRecord>();
    }
}
