using System.Linq;

namespace BrandVue.EntityFramework.MetaData
{
    public class AllVueConfigurationDetails
    {
        public record WaveVariableForSubset(string SubsetIdentifier, string VariableIdentifier);
        public bool IsDataTabAvailable { get; set; }
        public bool IsReportsTabAvailable { get; set; }
        public bool IsQuotaTabAvailable { get; set; }
        public bool IsDocumentsTabAvailable { get; set; }
        public bool IsHelpIconAvailable { get; set; }
        public AllVueDocumentationConfiguration AllVueDocumentationConfiguration { get; set; }

        public bool CheckOrphanedMetricsForCanonicalVariables { get; set; }
        public List<CustomUIIntegration> AdditionalUiWidgets { get; set; }
        /// <summary>
        /// Doesn't force loading from map file, just allows it if the file is present
        /// </summary>
        public bool AllowLoadingFromMapFile { get; set; }
        public SurveyType SurveyType { get; set; }
        public List<WaveVariableForSubset> WaveVariableForSubsets { get; set; }

        public AllVueConfigurationDetails()
        {
            IsDataTabAvailable = true;
            IsReportsTabAvailable = true;
            IsDocumentsTabAvailable = true;
            IsQuotaTabAvailable = true;
            CheckOrphanedMetricsForCanonicalVariables = true;
            IsHelpIconAvailable = true;
            AdditionalUiWidgets = new List<CustomUIIntegration>();
            AllVueDocumentationConfiguration = new AllVueDocumentationConfiguration();
            WaveVariableForSubsets = new List<WaveVariableForSubset>();
        }

        public AllVueConfigurationDetails(AllVueConfiguration databaseConfiguration) : this()
        {
            if (databaseConfiguration != null)
            {
                IsDataTabAvailable = databaseConfiguration.IsDataTabAvailable;
                IsReportsTabAvailable = databaseConfiguration.IsReportsTabAvailable;
                IsDocumentsTabAvailable = databaseConfiguration.IsDocumentsTabAvailable;
                IsQuotaTabAvailable = databaseConfiguration.IsQuotaTabAvailable;
                CheckOrphanedMetricsForCanonicalVariables = databaseConfiguration.CheckOrphanedMetricsForCanonicalVariables;
                AdditionalUiWidgets = databaseConfiguration.AdditionalUiWidgets ?? new List<CustomUIIntegration>();
                AllowLoadingFromMapFile = databaseConfiguration.AllowLoadFromMapFile;
                IsHelpIconAvailable = databaseConfiguration.IsHelpIconAvailable;
                AllVueDocumentationConfiguration = databaseConfiguration.AllVueDocumentationConfiguration ??
                                                   new AllVueDocumentationConfiguration();
                SurveyType = databaseConfiguration.SurveyType;
                WaveVariableForSubsets = databaseConfiguration.WaveVariableForSubsets
                    .Select(c => new WaveVariableForSubset(c.SubsetIdentifier, c.VariableIdentifier)).ToList();
            }
        }
    }
}
