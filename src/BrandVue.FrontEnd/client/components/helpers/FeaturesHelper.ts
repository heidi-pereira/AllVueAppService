import { AdditionalProductFeature, FeatureCode, Features, PermissionFeaturesOptions,RunningEnvironment } from "../../BrandVueApi";
import { useUserFeaturesContext } from "../../features/UserFeaturesContext";
import { ProductConfiguration } from "../../ProductConfiguration";
import { doesUserHavePermission } from "client/components/FeatureGuard/FeatureGuard";

const isUserSystemAdmin = (productConfiguration: ProductConfiguration): boolean => productConfiguration.user?.isSystemAdministrator ?? false;
const isUserAdmin = (productConfiguration: ProductConfiguration): boolean => productConfiguration.user?.isAdministrator??false;

const isChartConfigEnabled = (productConfiguration: ProductConfiguration) : boolean => productConfiguration.isFeatureEnabled(Features.ChartConfiguration);

export const isSubsetConfigEnabled = (productConfiguration: ProductConfiguration): boolean => isUserSystemAdmin(productConfiguration);

export const hasAllVuePermissionsOrSystemAdmin = (productConfiguration: ProductConfiguration, permissions: string[]): boolean => {
    if (permissions === undefined || permissions.length === 0) {
        throw new Error("hasAllVuePermissionsOrSystemAdmin called without valid permissions array.");
    }
    const isSystemAdmin = isUserSystemAdmin(productConfiguration);
    return doesUserHavePermission(productConfiguration.user, permissions,
        "any",
        (userContext, isAuthorized) => ((isAuthorized && productConfiguration.isSurveyVue()) || isSystemAdmin));
};


export const isCrosstabAdministrator =
    (productConfiguration: ProductConfiguration): boolean => {
        const permissions = [
                PermissionFeaturesOptions.VariablesEdit, PermissionFeaturesOptions.VariablesCreate,
                PermissionFeaturesOptions.VariablesDelete
            ];

        return hasAllVuePermissionsOrSystemAdmin(productConfiguration, permissions);
    }
export const isColourConfigurationEnabled = (productConfiguration: ProductConfiguration): boolean => isUserSystemAdmin(productConfiguration);

export const isEntityTypeConfigEnabled = (productConfiguration: ProductConfiguration): boolean => isUserSystemAdmin(productConfiguration);

export const isAverageConfigurationEnabled = (productConfiguration: ProductConfiguration): boolean => isUserSystemAdmin(productConfiguration);

export const isQuestionVariableDefinitionConfigurationEnabled = (productConfiguration: ProductConfiguration): boolean => isUserSystemAdmin(productConfiguration);

export const isPagesAndMetricsConfigEnabled = (productConfiguration: ProductConfiguration): boolean => isUserSystemAdmin(productConfiguration) && isChartConfigEnabled(productConfiguration);

export const isWeightingsConfigEnabled = (productConfiguration: ProductConfiguration): boolean => isUserSystemAdmin(productConfiguration);

export const isFeaturesConfigEnabled = (productConfiguration: ProductConfiguration): boolean => isUserSystemAdmin(productConfiguration);

export const isExportDataEnabled = (productConfiguration: ProductConfiguration): boolean => isUserAdmin(productConfiguration) && productConfiguration.user?.doesUserHaveAccessToInternalSavantaSystems;

export const isWeightingsConfigAccessible = (productConfiguration: ProductConfiguration): boolean => (isUserSystemAdmin(productConfiguration) && productConfiguration.isNonMapFileSurveys()) || isWeightingsConfigEnabled(productConfiguration);

export const isWeightingsExportAccessible = (productConfiguration: ProductConfiguration): boolean => (isUserAdmin(productConfiguration) && productConfiguration.isNonMapFileSurveys()) || isWeightingsConfigEnabled(productConfiguration);
export const isForcedReloadAccessible = (productConfiguration: ProductConfiguration): boolean => (isUserAdmin(productConfiguration) && productConfiguration.isNonMapFileSurveys()) || isWeightingsConfigEnabled(productConfiguration);

export const isBrandVueAudiencesConfigEnabled = (productConfiguration: ProductConfiguration): boolean => isUserSystemAdmin(productConfiguration) && !productConfiguration.isSurveyVue();

export const isTableBuilderFeatureEnabled = (productConfiguration: ProductConfiguration): boolean =>
    isFeatureEnabled(FeatureCode.Table_builder) && productConfiguration.isSurveyVue();

export const showbrandVueHelpLink = (productConfiguration: ProductConfiguration): boolean => {
    var showHelpLink = true;
    if (showHelpLink) {
        if (productConfiguration.isSurveyVue()
            && !productConfiguration.isProductFeatureEnabled(AdditionalProductFeature.HelpIconAvailable)
        )
        {
            showHelpLink = false;
        }
    }
    return showHelpLink;
}


export const isDevEnvironment = (productConfiguration: ProductConfiguration): boolean => productConfiguration.runningEnvironment == RunningEnvironment.Development;

export const isBarometer = (productConfiguration: ProductConfiguration): boolean => productConfiguration.productName.toLowerCase() === "barometer";

export const isFeatureEnabled = (featureCode: FeatureCode): boolean => { 
    const userFeaturesContext = useUserFeaturesContext();
    return userFeaturesContext?.features.find(feature => feature.FeatureCode === featureCode) != undefined;
}