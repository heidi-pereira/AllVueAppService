using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;

namespace BrandVue.Models;

public class ProductConfigurationResult
{
    [Obsolete("Use GaTags, but remember to prefix any existing configured ones with GTM- which was automatically added here previously")]
    public string[] GoogleTags { get; set; }
    public string[] GaTags { get; set; }
    public ApplicationUser User { get; set; }
    public Features FeaturesEnabled { get; set; }
    public int LowSampleForBrand { get; set; }
    public int NoSampleForBrand { get; set; }
    public string CustomerPortalQuotaLink { get; set; }
    public string CustomerPortalDocumentLink { get; set; }
    public string CustomerPortalStatusLink { get; set; }
    public string SurveyManagementLink { get; set; }
    public string OpenEndsLink { get; set; }
    public string SurveyName { get; set; }
    public string SurveyUid { get; set; }
    public bool IsSurveyOpen { get; set; }
    public bool IsSurveyGroup { get; set; }
    public int SurveyGroupId { get; set; }
    public IReadOnlyList<SurveyRecord> NonMapFileSurveys { get; set; }
    public string SubdomainOrganisation { get; set; }
    public string ProjectOrganisation { get; set; }
    public string SubProductId { get; set; }
    public string CdnAssetsEndpoint { get; set; }
    public string BrandVueHelpLink { get; set; }
    public string AllVueHelpLink { get; set; }
    public AdditionalProductFeature AdditionalProductFeature { get; set; }
    public string BrandVueMixpanelToken { get; set; }
    public string AllVueMixpanelToken { get; set; }
    public CustomUIIntegration[] CustomUIIntegration { get; set; }
    public RunningEnvironment RunningEnvironment { get; set; }
    public string RunningEnvironmentDescription { get; set; }
    public AllVueDocumentationConfiguration AllVueDocumentationConfiguration { get; set; }
    public SurveyType SurveyType { get; set; }
    public List<AllVueConfigurationDetails.WaveVariableForSubset> WaveVariableForSubsets { get; set; }
    public string KimbleProposalId { get; set; }
    public string SurveyAuthCompanyId { get; set; }
}
