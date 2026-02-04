using System.Threading;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Middleware;
using BrandVue.Models;
using BrandVue.PublicApi.Extensions;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging;
using Vue.AuthMiddleware;
using Vue.Common.Auth.Permissions;

namespace BrandVue.Services;

public class ProductConfigurationProvider : IProductConfigurationProvider
{
    private readonly IAllVueConfigurationRepository _allVueConfigurationRepository;
    private readonly ISubProductSecurityRestrictionsProvider _subProductSecurityRestrictionsProvider;
    private readonly IUserContext _userContext;
    private readonly IUserFeaturePermissionsService _userFeaturePermissionsService;
    private readonly InitialWebAppConfig _initialWebAppConfig;
    private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
    private readonly ISubsetRepository _subsetRepository;
    private readonly IProductContext _productContext;
    private readonly RequestScope _requestScope;
    private readonly AppSettings _settings;
    private ILogger<ProductConfigurationProvider> _logger;


    public ProductConfigurationProvider(
        IAllVueConfigurationRepository allVueConfigurationRepository, 
        ISubProductSecurityRestrictionsProvider subProductSecurityRestrictionsProvider,
        IUserContext userContext,
        IUserFeaturePermissionsService userFeaturePermissionsService,
        InitialWebAppConfig initialWebAppConfig,
        IProfileResponseAccessorFactory profileResponseAccessorFactory,
        ISubsetRepository subsetRepository,
        IProductContext productContext,
        RequestScope requestScope,
        AppSettings settings,
        ILogger<ProductConfigurationProvider> logger)
    {
        _allVueConfigurationRepository = allVueConfigurationRepository;
        _subProductSecurityRestrictionsProvider = subProductSecurityRestrictionsProvider;
        _userContext = userContext;
        _userFeaturePermissionsService = userFeaturePermissionsService;
        _initialWebAppConfig = initialWebAppConfig;
        _profileResponseAccessorFactory = profileResponseAccessorFactory;
        _subsetRepository = subsetRepository;
        _productContext = productContext;
        _requestScope = requestScope;
        _settings = settings;
        _logger = logger;
    }

    public async Task<ProductConfigurationResult> GetProductConfiguration(CancellationToken cancellationToken)
    {
        string userOrganisation = _userContext.UserOrganisation;
        var user = new BrandVue.Models.ApplicationUser
        {
            UserId = _userContext.UserId,
            UserName = _userContext.UserName,
            Name = _userContext.FirstName,
            Surname = _userContext.LastName,
            AccountName = userOrganisation,
            Products = _userContext.Products,
            IsAdministrator = _userContext.IsAdministrator,
            IsSystemAdministrator = _userContext.IsSystemAdministrator,
            IsThirdPartyLoginAuth = _userContext.IsThirdPartyLoginAuth,
            IsReportViewer = _userContext.IsReportViewer,
            IsTrialUser = _userContext.IsTrialUser,
            CanEditMetricAbouts = _userContext.CanEditMetricAbouts,
            CanAccessRespondentLevelDownload = _userContext.CanAccessRespondentLevelDownload(),
            RunningEnvironmentDescription = _initialWebAppConfig.RunningEnvironmentDescription,
            RunningEnvironment = _initialWebAppConfig.RunningEnvironment,
            DoesUserHaveAccessToInternalSavantaSystems = _userContext.IsAuthorizedSavantaUser,
            FeaturePermissions = _userFeaturePermissionsService.FeaturePermissions,
        };
        _logger.LogWarning("** Test {Claims}", string.Join(",",_userContext.Claims.Select(c=> $"'{c.Type}' = '{c.Value}'")));

        var featuresEnabled = GetFeatureFlags();

        var additionalProductFeature = AdditionalProductFeature.None;
        var allVueConfiguration = _allVueConfigurationRepository.GetConfigurationDetails();

        if (allVueConfiguration.IsReportsTabAvailable)
        {
            additionalProductFeature |= AdditionalProductFeature.ReportTabAvailable;
        }

        if (allVueConfiguration.IsDataTabAvailable)
        {
            additionalProductFeature |= AdditionalProductFeature.DataTabAvailable;
        }

        if (allVueConfiguration.IsQuotaTabAvailable)
        {
            additionalProductFeature |= AdditionalProductFeature.QuotaTabAvailable;
        }

        if (allVueConfiguration.IsDocumentsTabAvailable)
        {
            additionalProductFeature |= AdditionalProductFeature.DocumentsTabAvailable;
        }

        if (allVueConfiguration.IsHelpIconAvailable)
        {
            additionalProductFeature |= AdditionalProductFeature.HelpIconAvailable;
        }
        var requiredCompanies =
            (await _subProductSecurityRestrictionsProvider.GetSecurityRestrictions(cancellationToken))
            .RequiredCompanyShortcodes;

        string[] gaTags = _initialWebAppConfig.GaTags;
        if (_settings.ExtraGaTagsByThirdPartyOrganisation.TryGetValue(userOrganisation, out var orgSpecificGaTags))
        {
            gaTags = gaTags.Concat(orgSpecificGaTags).ToArray();
        }

        return new ProductConfigurationResult
        {
            GoogleTags = _initialWebAppConfig.GoogleTags,
            GaTags = gaTags,
            FeaturesEnabled = featuresEnabled,
            User = user,
            LowSampleForBrand = _initialWebAppConfig.LowSampleForBrand,
            NoSampleForBrand = _initialWebAppConfig.NoSampleForBrand,
            CustomerPortalQuotaLink = GetCustomerPortalQuotaLink(),
            CustomerPortalDocumentLink = GetCustomerPortalDocumentLink(),
            CustomerPortalStatusLink = GetCustomerPortalStatusLink(),
            SurveyManagementLink = GetSurveyManagementLink(),
            OpenEndsLink = GetOpenEndsLink(),
            SurveyName = _productContext.SurveyName,
            SurveyUid = _productContext.SurveyUid,
            IsSurveyOpen = _productContext.IsSurveyOpen,
            IsSurveyGroup = _productContext.IsSurveyGroup,
            SurveyGroupId = _productContext.SurveyGroupId,
            NonMapFileSurveys = _productContext.NonMapFileSurveys,
            SubdomainOrganisation = _requestScope.Organization,
            ProjectOrganisation = string.Join(", ", requiredCompanies),
            SubProductId = _productContext.SubProductId,
            CdnAssetsEndpoint = _initialWebAppConfig.cdnAssetsEndpoint,
            BrandVueHelpLink = _initialWebAppConfig.BrandVueHelpLink,
            AllVueHelpLink = allVueConfiguration.IsHelpIconAvailable?_initialWebAppConfig.AllVueHelpLink:null,
            AdditionalProductFeature = additionalProductFeature,
            BrandVueMixpanelToken = _initialWebAppConfig.BrandVueToken,
            AllVueMixpanelToken = _initialWebAppConfig.AllVueToken,
            CustomUIIntegration = allVueConfiguration.AdditionalUiWidgets?.ToArray(),
            RunningEnvironment = _initialWebAppConfig.RunningEnvironment,
            RunningEnvironmentDescription = _initialWebAppConfig.RunningEnvironmentDescription,
            AllVueDocumentationConfiguration = allVueConfiguration.AllVueDocumentationConfiguration,
            SurveyType = allVueConfiguration.SurveyType,
            WaveVariableForSubsets = allVueConfiguration.WaveVariableForSubsets,
            KimbleProposalId = user.DoesUserHaveAccessToInternalSavantaSystems ? _productContext.KimbleProposalId :string.Empty,
            SurveyAuthCompanyId = _productContext.SurveyAuthCompanyId,
        };
    }
    
    public ApplicationConfigurationResult GetApplicationConfiguration(string subsetId)
    {
        Subset subset = null;
        if (subsetId != null)
        {
            _subsetRepository.TryGet(subsetId, out subset);
        }

        bool hasLoadedData = subset != null;
        var subsetData = hasLoadedData ? _profileResponseAccessorFactory.GetOrCreate(subset) : null;
        var dateOfFirstDataPoint = hasLoadedData 
            ? subset.OverriddenStartDate ?? subsetData.StartDate 
            : DateTimeOffset.Parse("2017-02-01");
        var dateOfLastDataPoint = hasLoadedData
            ? _userContext.IsTrialUser
                ? _userContext.GetTrialDataRestrictedDate(subsetData.EndDate)
                : subsetData.EndDate
            : DateTimeOffset.Now;
            
        return new ApplicationConfigurationResult
        {
            DateOfFirstDataPoint = dateOfFirstDataPoint,
            DateOfLastDataPoint = dateOfLastDataPoint,
            HasLoadedData = hasLoadedData
        };
    }

    private Features GetFeatureFlags()
    {
        var featuresEnabled = Features.None;
        if (_initialWebAppConfig.ChartConfigurationEnabled)
        {
            featuresEnabled |= Features.ChartConfiguration;
        }

        if (_initialWebAppConfig.FeatureFlagBrandVueLoadWeightingFromDatabase)
        {
            featuresEnabled |= Features.FeatureFlagAllowReadingOfWeightsViaWeightingPlan;
        }

        if (_initialWebAppConfig.FeatureFlagNewWeightingUIAvailable)
        {
            featuresEnabled |= Features.FeatureFlagNewWeightingUIAvailable;
        }

        if (_initialWebAppConfig.FeatureFlagAilaTextSummarisation)
        {
            featuresEnabled |= Features.FeatureFlagAilaTextSummarisation;
        }

        return featuresEnabled;
    }
    
    private string GetCustomerPortalQuotaLink() =>
        $"https://{GetCompanyOrEmpty()}{_initialWebAppConfig.CustomerPortalQuotaLink}{_productContext.SubProductId}";

    private string GetCustomerPortalDocumentLink() =>
        $"https://{GetCompanyOrEmpty()}{_initialWebAppConfig.CustomerPortalDocumentLink}{_productContext.SubProductId}";

    private string GetCustomerPortalStatusLink() =>
        $"https://{GetCompanyOrEmpty()}{_initialWebAppConfig.CustomerPortalStatusLink}{_productContext.SubProductId}";
    
    private string GetCompanyOrEmpty() =>
        _initialWebAppConfig.CustomerPortalDocumentLink.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ? "" : $"{_userContext.AuthCompany}.";

    private string GetSurveyManagementLink() =>
        $"https://{GetCompanyOrEmpty()}{_initialWebAppConfig.SurveyManagementLink}";

    private string GetOpenEndsLink() =>
        $"https://{GetCompanyOrEmpty()}{_initialWebAppConfig.OpenEndsLink}{_productContext.SubProductId}";
}