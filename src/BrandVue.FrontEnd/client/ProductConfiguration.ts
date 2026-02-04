import * as BrandVueApi from "./BrandVueApi";
import { AllVueDocumentationConfiguration,FeatureCode } from "./BrandVueApi";
import LowSample from "./components/visualisations/BrandVueOnlyLowSampleHelper";
import ApplicationUser = BrandVueApi.ApplicationUser;
import Feature = BrandVueApi.Features;
import { isFeatureEnabled } from "./components/helpers/FeaturesHelper";

export class ProductConfiguration {
    appBasePath: string = (window as any).appBasePath;
    productName: string = (window as any).productName;
    googleTags: string[];//remove when gaTags is correctly configured
    gaTags: string[];
    user: ApplicationUser;
    featureEnabled: Feature;
    surveyName: string;
    surveyUid: string;
    customerPortalQuotaLink: string;
    customerPortalDocumentLink: string;
    customerPortalStatusLink: string;
    surveyManagementLink: string;
    openEndsLink: string;
    isSurveyOpen: boolean;
    isSurveyGroup: boolean;
    surveyGroupId: number;
    nonMapFileSurveys: BrandVueApi.SurveyRecord[];
    subdomainOrganisation: string;
    projectOrganisation: string;
    subProductId: string;
    cdnAssetsEndpoint: string;
    brandVueHelpLink: string;
    allVueHelpLink: string;
    productFeature: BrandVueApi.AdditionalProductFeature;
    additionalUiWidgets: BrandVueApi.CustomUIIntegration[];
    brandVueMixpanelToken: string;
    allVueMixpanelToken: string;
    runningEnvironment: BrandVueApi.RunningEnvironment;
    runningEnvironmentDescription: string;
    allVueDocumentationConfiguration: AllVueDocumentationConfiguration;
    kimbleProposalId: string;
    surveyAuthCompanyId: string;
    lowSampleForBrand: number;
    noSampleForBrand: number;

    static getAsync(): Promise<ProductConfiguration> {
        return BrandVueApi.Factory.ConfigClient(throwErr => throwErr()).getProductConfiguration().then(r => {
            const productConfiguration = new ProductConfiguration();
            productConfiguration.googleTags = r.googleTags;
            productConfiguration.gaTags = r.gaTags;
            productConfiguration.user = r.user;
            productConfiguration.featureEnabled = r.featuresEnabled;
            productConfiguration.surveyName = r.surveyName;
            productConfiguration.surveyUid = r.surveyUid;
            productConfiguration.customerPortalQuotaLink = r.customerPortalQuotaLink;
            productConfiguration.customerPortalDocumentLink = r.customerPortalDocumentLink;
            productConfiguration.customerPortalStatusLink = r.customerPortalStatusLink;
            productConfiguration.surveyManagementLink = r.surveyManagementLink;
            productConfiguration.openEndsLink = r.openEndsLink;
            productConfiguration.isSurveyOpen = r.isSurveyOpen;
            productConfiguration.isSurveyGroup = r.isSurveyGroup;
            productConfiguration.surveyGroupId = r.surveyGroupId;
            productConfiguration.nonMapFileSurveys = r.nonMapFileSurveys;
            productConfiguration.subdomainOrganisation = r.subdomainOrganisation;
            productConfiguration.projectOrganisation = r.projectOrganisation;
            productConfiguration.subProductId = r.subProductId;
            productConfiguration.lowSampleForBrand = r.lowSampleForBrand;
            productConfiguration.noSampleForBrand = r.noSampleForBrand;
            LowSample.initialiseThresholds(r.lowSampleForBrand, r.noSampleForBrand);
            productConfiguration.cdnAssetsEndpoint = r.cdnAssetsEndpoint;
            productConfiguration.brandVueHelpLink = r.brandVueHelpLink;
            productConfiguration.allVueHelpLink = r.allVueHelpLink;
            productConfiguration.productFeature = r.additionalProductFeature;
            productConfiguration.brandVueMixpanelToken = r.brandVueMixpanelToken;
            productConfiguration.allVueMixpanelToken = r.allVueMixpanelToken;
            productConfiguration.additionalUiWidgets = r.customUIIntegration;
            productConfiguration.runningEnvironment= r.runningEnvironment;
            productConfiguration.runningEnvironmentDescription = r.runningEnvironmentDescription;
            productConfiguration.allVueDocumentationConfiguration = r.allVueDocumentationConfiguration;
            productConfiguration.kimbleProposalId = r.kimbleProposalId;
            productConfiguration.surveyAuthCompanyId = r.surveyAuthCompanyId;
            return productConfiguration;
        });
    }

    isFeatureEnabled(feature: Feature): boolean {
        if (!this.featureEnabled)
            return false;
        if (this.featureEnabled.toString().includes(feature.toString()))
            return true;
        return false;
    }

    isProductFeatureEnabled(feature: BrandVueApi.AdditionalProductFeature): boolean {
        if (!this.featureEnabled)
            return false;
        if (this.productFeature.toString().includes(feature.toString()))
            return true;
        return false;
    }

    isNonMapFileSurveys(): boolean {
        if (!this.nonMapFileSurveys)
            return false;
        return this.nonMapFileSurveys.length != 0;
    }

    getManageUsersUrl(shortCode?: string): string {
        let url = `${this.appBasePath}/account/manageusers`;
        if (isFeatureEnabled(FeatureCode.User_management)) {
            url = `/usermanagement`;
        }
        
        if (shortCode) {
            url += `?shortCode=${shortCode}`;
        }
        return url;
    }
    getManageProjectForUsersUrl(): string {
        const projectType = this.isSurveyGroup ? "allvuesurveygroup" : "allvuesurvey";
        const projectId = this.isSurveyGroup ? this.surveyGroupId : this.subProductId;
        return `/usermanagement/projects/${this.surveyAuthCompanyId}/${projectType}/${projectId}`;
    }

    isSurveyVue(): boolean {
        return this.productName.toLowerCase() === "survey";
    }

    capitalisedProductName(): string {
        const name = this.productName;
        return name === "eatingout"
            ? "Eating Out"
            : name.replace(name.charAt(0), name.charAt(0).toLocaleUpperCase());
    }

    calculateCdnPath(filePath: string): string {
        return `${this.cdnAssetsEndpoint}/${this.productName.toLowerCase()}${filePath}`;
    }

    getHelpLink = (): string => {
        return this.isSurveyVue() ? this.allVueHelpLink : this.brandVueHelpLink;
    }
}