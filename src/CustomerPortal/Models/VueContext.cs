using BrandVue.EntityFramework.MetaData;
using CustomerPortal.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomerPortal.Models
{
    public class VueContext
    {
        private readonly IAllVueProductConfigurationService _allVueProductConfigurationService;
        public VueContext(string vueUrl, bool vueQuotaEnabled, bool vueDocumentsEnabled, bool vueDataEnabled, bool vueReportsEnabled, bool vueSettingsEnabled, bool vueHelpIconEnabled, IAllVueProductConfigurationService allVueProductConfigurationService)
        {
            VueUrl = vueUrl;
            SurveyMangementUrl = vueUrl.Replace("/survey", "/surveymanagement");
            VueQuotaEnabled = vueQuotaEnabled;
            VueDocumentsEnabled = vueDocumentsEnabled;
            VueDataEnabled = vueDataEnabled;
            VueReportsEnabled = vueReportsEnabled;
            VueSettingsEnabled = vueSettingsEnabled;
            VueHelpIconEnabled = vueHelpIconEnabled;
            _allVueProductConfigurationService = allVueProductConfigurationService;
        }

        public string VueUrl { get; }
        [Obsolete]
        public string SurveyMangementUrl { get; }
        public bool VueQuotaEnabled { get; }
        public bool VueDocumentsEnabled { get; }
        public bool VueDataEnabled { get; }
        public bool VueReportsEnabled { get; }
        public bool VueSettingsEnabled { get; }
        public bool VueHelpIconEnabled { get; }

        public VueContextModel GetContextModel(string subProductId)
        {
            var configuration = _allVueProductConfigurationService.GetConfiguration(subProductId);
            return new VueContextModel(_allVueProductConfigurationService, VueUrl, 
                VueQuotaEnabled && configuration.IsQuotaTabAvailable, 
                VueDocumentsEnabled && configuration.IsDocumentsTabAvailable, 
                VueDataEnabled, 
                VueReportsEnabled, VueSettingsEnabled,
                VueHelpIconEnabled && configuration.IsHelpIconAvailable,
                GetDataPageUrl(subProductId), 
                GetReportsPageUrl(subProductId), GetSettingsPageUrl(subProductId),
                configuration,
                subProductId);
        }

        protected string GetPage(string subProductId, string url) => $"{VueUrl}{subProductId}/ui/{url}";
        private string GetDataPageUrl(string subProductId) => GetPage(subProductId, "crosstabbing");

        private string GetReportsPageUrl(string subProductId) => GetPage(subProductId, "reports");

        private string GetSettingsPageUrl(string subProductId) => GetPage(subProductId, "settings");
    }

    public class VueContextModel : VueContext
    {
        public VueContextModel(
            IAllVueProductConfigurationService allVueProductConfigurationService,
            string vueUrl,
            bool vueQuotaEnabled,
            bool vueDocumentsEnabled,
            bool vueDataEnabled,
            bool vueReportsEnabled,
            bool vueSettingsEnabled,
            bool vueHelpIconEnabled,
            string vueDataUrl,
            string vueReportsUrl,
            string vueSettingsUrl,
            AllVueConfigurationDetails allVueConfigurationDetails,
            string subProductId


            ) : base(vueUrl, vueQuotaEnabled, vueDocumentsEnabled, vueDataEnabled, vueReportsEnabled, vueSettingsEnabled, vueHelpIconEnabled, allVueProductConfigurationService)
        {
            CustomUiWidgets = allVueConfigurationDetails.AdditionalUiWidgets.Select( integration=>
            {
                var res = new CustomUIIntegration(integration);
                if (res.Style == CustomUIIntegration.IntegrationStyle.Tab)
                {
                    switch (res.ReferenceType)
                    {
                        
                        case CustomUIIntegration.IntegrationReferenceType.Page:
                        case CustomUIIntegration.IntegrationReferenceType.ReportVue:
                        default:
                            res.Path = GetPage(subProductId, integration.Path);
                            break;

                        case CustomUIIntegration.IntegrationReferenceType.WebLink:
                            break;

                        case CustomUIIntegration.IntegrationReferenceType.SurveyManagement:
                            //Legacy configuration - remove later
                            //https://app.shortcut.com/mig-global/story/84351/
                            if (string.IsNullOrEmpty(integration.Path))
                            {
                                res.Path = SurveyMangementUrl;
                            }
                            break;
                    }
                }
                return res;
            }).ToList();
            VueDataPageUrl = vueDataEnabled && allVueConfigurationDetails.IsDataTabAvailable ? vueDataUrl: string.Empty;
            VueReportsPageUrl = vueReportsEnabled && allVueConfigurationDetails.IsReportsTabAvailable ? vueReportsUrl : string.Empty;
            VueSettingsPageUrl = vueSettingsUrl;
            AllVueDocumentationConfiguration = allVueConfigurationDetails.AllVueDocumentationConfiguration;
        }

        public string VueDataPageUrl { get; }
        public string VueReportsPageUrl { get; }
        public string VueSettingsPageUrl { get; }
        public List<CustomUIIntegration> CustomUiWidgets { get; }
        public AllVueDocumentationConfiguration AllVueDocumentationConfiguration { get; }

    }
}