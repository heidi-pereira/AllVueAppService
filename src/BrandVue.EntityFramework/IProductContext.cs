namespace BrandVue.EntityFramework
{
    public interface IProductContext
    {
        string ShortCode { get; }
        string SubProductId { get; }
        bool IsAllVue { get; }
        bool AllowPartialDays { get; }
        bool DayIsFromResponseEndTime { get; }
        bool GenerateFromSurveyIds { get; }
        /// <summary>
        /// For single client products, it's ok for client admins to change some aspects of the dashboard that aren't restricted by client
        /// </summary>
        bool HasSingleClient { get; }
        bool IsContinuous { get; }
        bool KeepInMemory { get; }
        /// <summary>
        /// false for legacy brandvues, which use the start time, and thus change history when people complete at a later date
        /// </summary>
        IReadOnlyList<SurveyRecord> NonMapFileSurveys { get; }
        IReadOnlyList<int> NonMapFileSurveyIds { get; }
        string SurveyName { get; }
        string SurveyUid { get; }
        string SurveyAuthCompanyId { get; }
        bool IsSurveyOpen { get; }
        bool IsSurveyGroup { get; }
        int SurveyGroupId { get; }
        string[] DefaultAveragesToInclude { get; }
        public bool IncludeAllDefaultAverages { get; }
        bool DisableAutoMetricFiltering { get; }
        public bool GenerateFromAnswersTable { get; }
        public string ShortCodeAndSubproduct();
        public string KimbleProposalId { get; }
    }
}
