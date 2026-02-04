import React from "react";
import { NavLink, useLocation, useNavigate } from "react-router-dom";
import { exportsPageUrl, getActivePage, getUrlForPageName, productConfigurationPageUrl, surveyConfigurationPageUrl, usersPageUrl, weightingPageUrl } from "../../../components/helpers/PagesHelper";
import UsersSettingsPage from "./Users/UsersSettingsPage";
import { CatchReportAndDisplayErrors } from "../../../components/CatchReportAndDisplayErrors";
import styles from "./SettingsPage.module.less";
import WeightingSettingsPage from "./Weighting/WeightingSettingsPage";
import WeightingSettingsPageV3 from "./Weighting/WeightingSettingsPageV3";
import { IGoogleTagManager } from "../../../googleTagManager";
import { IEntityConfiguration } from "../../../entity/EntityConfiguration";
import { MetricSet } from "../../../metrics/metricSet";
import { IAverageDescriptor, Features, FeatureCode, PermissionFeaturesOptions } from "../../../BrandVueApi";
import { ProductConfiguration } from "../../../ProductConfiguration";
import { ApplicationConfiguration } from "../../../ApplicationConfiguration";
import { PageHandler } from "../../PageHandler";
import ConfigurationSettingsPage from "./ConfigurationSettingsPage";
import ExportsPage from "./ExportsPage";
import SurveyConfigurationPage from "./SurveyConfiguration/SurveyConfigurationPage";
import { useReadVueQueryParams } from "../../helpers/UrlHelper";
import { isFeatureEnabled } from "../../helpers/FeaturesHelper";
import {FeatureGuard, doesUserHavePermission} from "client/components/FeatureGuard/FeatureGuard";

interface ISettingsPageProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    entityConfiguration: IEntityConfiguration
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
    metrics: MetricSet;
    averages: IAverageDescriptor[];
    canAccessRespondentLevelDownload: boolean;
}

const SettingsPage = (props: ISettingsPageProps) => {

    const currentPage = getActivePage();
    const navigate = useNavigate();
    const location = useLocation();
    const readVueQueryParams = useReadVueQueryParams();
    const isUserSettingsSubPage = () => {
        return currentPage.name == "Users";
    }

    const isWeightingSettingsSubPage = () => {
        return currentPage.name == "Weighting";
    }

    const isExportsSubPage = () => {
        return currentPage.name == "Exports";
    }

    const isProductConfigurationSubPage = () => {
        return currentPage.name == "Configuration";
    }
    const isSurveyConfigurationSubPage = () => {
        return currentPage.name == "SurveyConfiguration";
    }

    const isParentSettingsPage = () => {
        return currentPage.name == "Settings";
    }

    React.useEffect(() => {
        if (isParentSettingsPage() && currentPage.childPages.length > 0) {
            navigate(getUrlForPageName(currentPage.childPages[0].name, location, readVueQueryParams));
        }
    });

    const getPageContent = () => {
        if (isSurveyConfigurationSubPage()) {
            return (<SurveyConfigurationPage productConfiguration={props.productConfiguration} metrics={props.metrics} applicationConfiguration={props.applicationConfiguration} />);
        }
        else if (isUserSettingsSubPage()) {
            return (
                <CatchReportAndDisplayErrors applicationConfiguration={props.applicationConfiguration} childInfo={{ "Component": "UsersSettingsPage" }}>
                    <UsersSettingsPage productConfiguration={props.productConfiguration} googleTagManager={props.googleTagManager} pageHandler={props.pageHandler} />
                </CatchReportAndDisplayErrors>
            );
        }
        if (isProductConfigurationSubPage()) {
            return (<ConfigurationSettingsPage googleTagManager={props.googleTagManager}
                pageHandler={props.pageHandler} />
            );

        }
        else if (isWeightingSettingsSubPage()) {
            if (props.productConfiguration.isFeatureEnabled(Features.FeatureFlagNewWeightingUIAvailable)) {
                return (
                    <WeightingSettingsPageV3
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        entityConfiguration={props.entityConfiguration}
                        metrics={props.metrics}
                        averages={props.averages} />
                );
            }
            else {
                return (
                    <WeightingSettingsPage googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        entityConfiguration={props.entityConfiguration}
                        metrics={props.metrics}
                        averages={props.averages} />
                );
            }
        }
        else if (isExportsSubPage()) {
            return (
                <ExportsPage googleTagManager={props.googleTagManager} pageHandler={props.pageHandler}/>
            );
        }
        return <></>;
    }
    const accessToSurveyTab = () => props.productConfiguration.user.isAdministrator ||
        props.productConfiguration.user.doesUserHaveAccessToInternalSavantaSystems;
    const accessToConfigurationTab = () => props.productConfiguration.user.isSystemAdministrator;
    const accessToUsersTab = () => !isFeatureEnabled(FeatureCode.User_management);
    const accessToExportTab = () => !props.productConfiguration.isSurveyGroup && props.canAccessRespondentLevelDownload;
    const accessToWeightingTab = () => doesUserHavePermission( props.productConfiguration.user, [PermissionFeaturesOptions.SettingsAccess]);

    return (
        <FeatureGuard permissions={[PermissionFeaturesOptions.SettingsAccess]} 
            fallback={<div className={styles.noAccessMessage}>You do not have permission to view this page.</div>}>
            <div className="settings-page">
                <aside className="settings-side-panel">
                    {accessToConfigurationTab() &&
                        <NavLink
                            key={productConfigurationPageUrl(location, readVueQueryParams)}
                            to={productConfigurationPageUrl(location, readVueQueryParams)}
                            className={({ isActive }) =>
                                (isActive && isProductConfigurationSubPage())
                                    ? "side-panel-link active"
                                    : "side-panel-link"
                            }
                        >
                            <i className="material-symbols-outlined no-symbol-fill">Settings</i>
                            <span>Configuration</span>
                        </NavLink>
                    }

                    {accessToSurveyTab() &&
                        <NavLink
                            to={{ pathname: surveyConfigurationPageUrl(location, readVueQueryParams) }}
                            className={({ isActive }) =>
                                (isActive && isSurveyConfigurationSubPage())
                                    ? "side-panel-link active"
                                    : "side-panel-link"
                            }
                        >
                            <i className="material-symbols-outlined no-symbol-fill">Settings</i>
                            <span>Survey</span>
                        </NavLink>
                    }
                    {accessToUsersTab() &&
                        <NavLink
                            key={usersPageUrl(location, readVueQueryParams)}
                            to={usersPageUrl(location, readVueQueryParams)}
                            className={({ isActive }) =>
                                (isActive && isUserSettingsSubPage())
                                    ? "side-panel-link active"
                                    : "side-panel-link"
                            }
                        >
                            <i className="material-symbols-outlined no-symbol-fill">admin_panel_settings</i>
                            <span>Users</span>
                        </NavLink>
                    }
                    {accessToWeightingTab() && 
                        <NavLink
                            key={weightingPageUrl(location, readVueQueryParams)}
                            to={weightingPageUrl(location, readVueQueryParams)}
                            className={({ isActive }) =>
                                (isActive && isWeightingSettingsSubPage())
                                    ? "side-panel-link active"
                                    : "side-panel-link"
                            }
                        >
                            <i className="material-symbols-outlined no-symbol-fill">weight</i>
                            <span>Weightings</span>
                        </NavLink>
                    }
                    {accessToExportTab()
                        && <NavLink
                            key={exportsPageUrl(location, readVueQueryParams)}
                            to={exportsPageUrl(location, readVueQueryParams)}
                            className={({ isActive }) =>
                                (isActive && isExportsSubPage())
                                    ? "side-panel-link active"
                                    : "side-panel-link"
                            }
                        >
                            <i className="material-symbols-outlined no-symbol-fill">downloading</i>
                            <span>Exports</span>
                        </NavLink>
                    }
                </aside>
                {getPageContent()}
            </div>
        </FeatureGuard>
    );
}

export default SettingsPage;