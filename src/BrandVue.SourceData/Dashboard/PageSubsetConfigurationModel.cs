using JetBrains.Annotations;

namespace BrandVue.SourceData.Dashboard
{
    public class PageSubsetConfigurationModel
    {
        public PageSubsetConfigurationModel(string subset, bool enabled, string helpText = null)
        {
            Subset = subset;
            Enabled = enabled;
            HelpText = helpText;
        }
        
        public string Subset { get; set; }
        [CanBeNull] public string HelpText { get; set; }
        public bool Enabled { get; set; }
    }
}