using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace OpenEnds.BackEnd.Model
{
    public class AllVueConfiguration
    {
        [Key]
        public int Id { get; set; }
        public string? ProductShortCode { get; set; }
        public string? SubProductId { get; set; }
        public bool IsReportsTabAvailable { get; set; }
        public bool IsDataTabAvailable { get; set; }
        public bool IsDocumentsTabAvailable { get; set; }
        public bool IsQuotaTabAvailable { get; set; }
        public string? AdditionalUiWidgets { get; set; }

        public NavigationTab[] GetNavigationTabs()
        {
            List<NavigationTab> tabs = new List<NavigationTab>();

            if (IsQuotaTabAvailable)
            {
                tabs.Add(NavigationTab.Quota);
            }
            if (IsDocumentsTabAvailable)
            {
                tabs.Add(NavigationTab.Documents);
            }
            if (IsDataTabAvailable)
            {
                tabs.Add(NavigationTab.Data);
            }
            if (IsReportsTabAvailable)
            {
                tabs.Add(NavigationTab.Reports);
            }

            return tabs.ToArray();
        }

        public CustomUIIntegration[] GetCustomUIIntegrations()
        {
            if (string.IsNullOrEmpty(AdditionalUiWidgets))
            {
                return Array.Empty<CustomUIIntegration>();
            }

            try
            {
                return JsonSerializer.Deserialize<CustomUIIntegration[]>(AdditionalUiWidgets) ?? Array.Empty<CustomUIIntegration>();
            }
            catch (JsonException)
            {
                return Array.Empty<CustomUIIntegration>();
            }
        }
    }
}
