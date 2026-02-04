import { PaneType } from "./panes/PaneType";
import React from "react";
import { NavLink } from "react-router-dom";
import { IImmutableSession } from "../dsession";
import AccountOptions from "./AccountOptions";
import { settingsPageUrl, getReportsPage, reportVuePageUrl, allVueWebPageUrl, dataPageUrl } from "./helpers/PagesHelper";
import { getPathByPageName } from "./helpers/UrlHelper";
import { UserContext } from "../GlobalContext";
import { AdditionalProductFeature, IApplicationUser, IntegrationStyle, IntegrationPosition, IntegrationReferenceType, CustomUIIntegration, FeatureCode, PermissionFeaturesOptions } from "../BrandVueApi";
import WarningBanner from "./visualisations/WarningBanner";
import { getResearchPortalUrl } from "./helpers/ResearchPortalHelper";
import { ApplicationConfiguration } from "../ApplicationConfiguration";
import { ProductConfiguration } from "../ProductConfiguration";
import AllVueHomePageLogo from "./AllVueHomePageLogo";
import { IGoogleTagManager } from "../googleTagManager";
import { isFeatureEnabled, showbrandVueHelpLink } from "./helpers/FeaturesHelper";
import { MixPanel } from "./mixpanel/MixPanel";
import FeatureGuard from "./FeatureGuard/FeatureGuard";

interface SurveyVueHeaderInternalProps {
    activePaneType: PaneType;
    activePageName: string;
    session: IImmutableSession;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
    user: IApplicationUser | null;
}

const SurveyVueEntryPageHeaderInternal = (props: SurveyVueHeaderInternalProps) => {

    const productConfiguration = props.productConfiguration;
    const googleTagManager = props.googleTagManager;
    const hasSurveyUid = productConfiguration.surveyUid != null && productConfiguration.surveyUid.trim() != '';
    const reportsPage = getReportsPage();
    const isQuotaTabEnabled = productConfiguration.isProductFeatureEnabled(AdditionalProductFeature.QuotaTabAvailable);
    const isDocumentsTabEnabled = productConfiguration.isProductFeatureEnabled(AdditionalProductFeature.DocumentsTabAvailable);
    const isDataTabEnabled = productConfiguration.isProductFeatureEnabled(AdditionalProductFeature.DataTabAvailable);
    const isReportTabEnabled = productConfiguration.isProductFeatureEnabled(AdditionalProductFeature.ReportTabAvailable);
    const isAnalysisTabEnabled = isFeatureEnabled(FeatureCode.Open_ends);
    const isSystemAdministrator = props.user?.isSystemAdministrator ?? false;
    const statusPageEnabled = isSystemAdministrator && productConfiguration.isSurveyGroup;
    const generateTestSurveyLink = () => {
        return `${getResearchPortalUrl(window)}/api/redirect/testlink?uniqueSurveyId=${productConfiguration.surveyUid}`;
    }

    const generateFieldVueLink = () => {
        return `${getResearchPortalUrl(window)}/SurveyLive?uid=${productConfiguration.surveyUid}`;
    }
    const generateKimbleLink = () => {
        return `https://savanta.lightning.force.com/lightning/r/KimbleOne__Proposal__c/${productConfiguration.kimbleProposalId}`;
    }

    const logTestSurveyEvent = () => {
        googleTagManager.addEvent("surveyVueTestSurvey", props.session.pageHandler, { value: productConfiguration.surveyUid });
        MixPanel.track("testSurveyOpened");
    }
    const logFieldVueEvent = () => {
        MixPanel.track("fieldVueLinkOpened");
    }
    const logKimbleEvent = () => {
        MixPanel.track("kimbleLinkOpened");
    }

    const displayTitleBar = () => {
        if (productConfiguration.user.isReportViewer) {
            return false;
        }

        let numberOftabs = productConfiguration.additionalUiWidgets.filter(x => x.style == IntegrationStyle.Tab).length;
        if (!productConfiguration.isSurveyGroup) {
            if (isQuotaTabEnabled) {
                numberOftabs++;
            }
            if (isDocumentsTabEnabled) {
                numberOftabs++;
            }
        }
        if (isDataTabEnabled) {
            numberOftabs++;
        }
        if (statusPageEnabled) {
            numberOftabs++;
        }
        if (isReportTabEnabled && reportsPage) {
            numberOftabs++;
        }
        if (props.user?.isAdministrator) {
            numberOftabs++;
        }
        return numberOftabs > 1;
    }

    const renderCustomTab = (integration: CustomUIIntegration, index: number) => {
        if (integration.referenceType == IntegrationReferenceType.Page) {
            return (<NavLink
                to={allVueWebPageUrl(integration.path)}
                className={({ isActive }) =>
                    (isActive && props.activePaneType === PaneType.allVueWebPage && props.activePageName === integration.name)
                        ? "tab-link active"
                        : "tab-link"
                }
                title={integration.altText}
                key={index}
            >
                <i className="material-symbols-outlined">{integration.icon}</i>
                <span>{integration.name}</span>
            </NavLink>);
        }
        if (integration.referenceType == IntegrationReferenceType.ReportVue) {
            return (<NavLink
                to={reportVuePageUrl(integration.path)}
                className={({ isActive }) =>
                    (isActive && props.activePaneType === PaneType.reportVuePage && props.activePageName === integration.name)
                        ? "tab-link active"
                        : "tab-link"
                }
                title={integration.altText}
                key={index}
            >
                <i className="material-symbols-outlined">{integration.icon}</i>
                <span>{integration.name}</span>
            </NavLink>);
        }
        else if (integration.referenceType == IntegrationReferenceType.WebLink ||
            integration.referenceType == IntegrationReferenceType.SurveyManagement) {
            return (<a href={integration.path} className="tab-link" key={index} title={integration.altText}>
                <i className="material-symbols-outlined">{integration.icon}</i>
                <span>{integration.name}</span>
            </a>);
        }
    }

    const renderCustomHelpButton = (integration: CustomUIIntegration, index: number) => {
        return (<a className="circular-nav-button"
            target="_blank"
            key={index}
            href={integration.path}
            title={integration.altText}
            onClick={() => MixPanel.track("helpOpened", { Custom: true, Path: integration.path })}
        >
            <div className="circle">
                <i className="material-symbols-outlined">{integration.icon}</i>
            </div>
            <div className="text">{integration.name}</div>
        </a>);
    }

    const listOfHelpButtons = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
        return additionalUIWidgets.filter(x => x.style == IntegrationStyle.Help);
    }

    const listOfTabs = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
        return additionalUIWidgets.filter(x => x.style == IntegrationStyle.Tab);
    }

    const lhsWidgets = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
        return additionalUIWidgets.filter(x => x.position == IntegrationPosition.Left);
    }
    const rhsWidgets = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
        return additionalUIWidgets.filter(x => x.position == IntegrationPosition.Right);
    }


    return (
        <div className="survey-vue-page-header" id="survey-vue-page-header">
            <nav className="topmenu">
                <div className="page-title-container">
                    <AllVueHomePageLogo user={props.user} />
                    <span className="page-title">{productConfiguration.surveyName}</span>
                </div>
                <div className="menu-icons-container">
                    {lhsWidgets(listOfHelpButtons(productConfiguration.additionalUiWidgets)).
                        map((item, index) => renderCustomHelpButton(item, index))}

                    {showbrandVueHelpLink(props.productConfiguration) &&
                        <a className="circular-nav-button"
                            target="_blank" href={props.productConfiguration.getHelpLink()}
                            title="Help"
                            onClick={() => MixPanel.track("helpOpened")}
                        >
                            <div className="circle">
                                <i className="material-symbols-outlined">help</i>
                            </div>
                            <span className="text">Help</span>
                        </a>}
                    <AccountOptions productConfiguration={props.productConfiguration} />
                    {rhsWidgets(listOfHelpButtons(productConfiguration.additionalUiWidgets)).
                        map((item, index) => renderCustomHelpButton(item, index))}
                </div>
            </nav>
            {displayTitleBar() &&
                <div className="tabs">
                    <div className="tab-link-container">
                        {lhsWidgets(listOfTabs(productConfiguration.additionalUiWidgets)).map((item, index) => renderCustomTab(item, index))}
                        {!productConfiguration.isSurveyGroup &&
                            <>
                            {isQuotaTabEnabled &&
                                <FeatureGuard permissions={[PermissionFeaturesOptions.QuotasAccess]}>
                                    <a className="tab-link" href={productConfiguration.customerPortalQuotaLink}>
                                        <i className='material-symbols-outlined'>donut_large</i>
                                        <span>Quotas</span>
                                    </a>
                                </FeatureGuard>
                                }
                                {isDocumentsTabEnabled && 
                                    <FeatureGuard permissions={[PermissionFeaturesOptions.DocumentsAccess]}>
                                        <a className="tab-link" href={productConfiguration.customerPortalDocumentLink}>
                                            <i className='material-symbols-outlined'>folder_open</i>
                                            <span>Documents</span>
                                        </a>
                                    </FeatureGuard>
                                }
                            </>
                        }
                        {statusPageEnabled &&
                            <a className="tab-link" href={productConfiguration.customerPortalStatusLink}>
                                <i className='material-symbols-outlined no-symbol-fill'>info</i>
                                <span>Status</span>
                            </a>
                        }
                        {isDataTabEnabled && 
                            <FeatureGuard permissions={[PermissionFeaturesOptions.DataAccess]}>
                                <NavLink
                                    to={dataPageUrl()}
                                    className={({ isActive }) =>
                                        (isActive && props.activePaneType === PaneType.crossTabPage)
                                            ? "tab-link active"
                                            : "tab-link"
                                    }
                                >
                                    <i className="material-symbols-outlined">grid_on</i>
                                    <span>Data</span>
                                </NavLink>
                            </FeatureGuard>
                        }
                        {reportsPage != null && isReportTabEnabled &&
                            <FeatureGuard permissions={[PermissionFeaturesOptions.ReportsView]}>
                                <NavLink
                                    to={getPathByPageName(reportsPage.name)}
                                    className={({ isActive }) =>
                                        (isActive && (props.activePaneType === PaneType.reportsPage || props.activePaneType === PaneType.reportSubPage))
                                            ? "tab-link active"
                                            : "tab-link"
                                    }
                                >
                                    <i className="material-symbols-outlined">insights</i>
                                    <span>{reportsPage.displayName}</span>
                                </NavLink>
                            </FeatureGuard>
                        }
                        {isAnalysisTabEnabled && 
                            <FeatureGuard permissions={[PermissionFeaturesOptions.AnalysisAccess]}>
                                <a className="tab-link" href={productConfiguration.openEndsLink} title="Analysis (Beta) - This feature is in testing and may change." aria-label="Analysis (Beta) - This feature is in testing and may change.">
                                    <i className='material-symbols-outlined'>find_replace</i>
                                    <span>
                                        Analysis <span
                                            style={{
                                                color: '#fff',
                                                backgroundColor: '#f39c12',
                                                fontSize: '0.7em',
                                                padding: '2px 4px',
                                                borderRadius: '4px',
                                                marginLeft: '4px',
                                            }}>Beta</span>
                                    </span>
                                </a>
                            </FeatureGuard>
                        }
                        {rhsWidgets(listOfTabs(productConfiguration.additionalUiWidgets)).map((item, index) => renderCustomTab(item, index))}
                        <FeatureGuard permissions={[PermissionFeaturesOptions.SettingsAccess]}>
                            <NavLink
                                to={settingsPageUrl()}
                                className={({ isActive }) =>
                                    (isActive && props.activePaneType === PaneType.settingsPage)
                                        ? "tab-link active"
                                        : "tab-link"
                                }
                            >
                                <i className="material-symbols-outlined">settings</i>
                                <span>Settings</span>
                            </NavLink>
                        </FeatureGuard>
                    </div>
                    <div className="links_to_savanta">
                        {props.user?.doesUserHaveAccessToInternalSavantaSystems &&
                            props.productConfiguration.kimbleProposalId && <a href={generateKimbleLink()} className="open-survey-link" target="_blank" rel="noopener noreferrer" onClick={logKimbleEvent}>
                                <span className="link-text">Kimble</span>
                                <i className='material-symbols-outlined'>open_in_new</i>
                            </a>
                        }
                        {props.user?.doesUserHaveAccessToInternalSavantaSystems &&
                            hasSurveyUid && <a href={generateFieldVueLink()} className="open-survey-link" target="_blank" rel="noopener noreferrer" onClick={logFieldVueEvent}>
                                <span className="link-text">FieldVue</span>
                                <i className='material-symbols-outlined'>open_in_new</i>
                            </a>
                        }

                        {
                            isQuotaTabEnabled && hasSurveyUid && <a href={generateTestSurveyLink()} className="open-survey-link" target="_blank" onClick={logTestSurveyEvent}>
                                <span className="link-text">Open test survey</span>
                                <i className='material-symbols-outlined'>open_in_new</i>
                            </a>
                        }
                    </div>
                </div>
            }
            {productConfiguration.isSurveyOpen &&
                <WarningBanner message="This survey is in progress so data may change" materialIconName="timelapse" isClosable={true} />
            }
        </div>

    );
};

interface SurveyVueHeaderProps {
    activePaneType: PaneType;
    activePageName: string;
    session: IImmutableSession;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
}

const SurveyVueEntryPageHeader = (props: SurveyVueHeaderProps) => {
    return (
        <UserContext.Consumer>
            {(user) =>
                <SurveyVueEntryPageHeaderInternal activePaneType={props.activePaneType} activePageName={props.activePageName}
                    session={props.session} googleTagManager={props.googleTagManager} applicationConfiguration={props.applicationConfiguration} productConfiguration={props.productConfiguration}
                    user={user} />
            }
        </UserContext.Consumer>
    );
}

export default SurveyVueEntryPageHeader;

