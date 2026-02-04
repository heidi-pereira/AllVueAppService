import React from "react";
import { ProductConfiguration } from "../ProductConfiguration";
import DesktopLogo from "./DesktopLogo";
import AccountOptions from "./AccountOptions";
import { isBarometer, isDevEnvironment, showbrandVueHelpLink } from "./helpers/FeaturesHelper";
import { AdditionalProductFeature, IntegrationReferenceType } from "../BrandVueApi";
import EnvironmentBannerBar from "./helpers/EnvironmentBannerBar";

interface IConfigurationTopNavProps {
    productConfiguration: ProductConfiguration;
}

const ConfigurationTopNav = (props: IConfigurationTopNavProps) => {

    const getSurveyVueStartURL = () => {
        if (props.productConfiguration.isProductFeatureEnabled(AdditionalProductFeature.DataTabAvailable))
            return "/crosstabbing/competition";
        else if (props.productConfiguration.isProductFeatureEnabled(AdditionalProductFeature.ReportTabAvailable)) {
            return "/reports";
        }
        const reportVueLinks = props.productConfiguration.additionalUiWidgets.filter(x => x.referenceType == IntegrationReferenceType.ReportVue);
        if (reportVueLinks.length > 0) {
            return "/" + reportVueLinks[0].path;
        }
        return "/";
    }

    /*
    * Get relevant home url from product config provider when available
    */
    const getSurveyPath = () => {
        if (props.productConfiguration.isSurveyVue()) {
            const baseUrl = getSurveyVueStartURL();
            return baseUrl.endsWith('/') ? `${baseUrl}home/` : `${baseUrl}/home/`;
        }
        return '/home/';
    };
      
    const startURL = getSurveyPath();
    const showAccountMenu = !isBarometer(props.productConfiguration)
        || isDevEnvironment(props.productConfiguration)
        || props.productConfiguration.user.isSystemAdministrator;

    return (
        <div className="sticky-header not-exported">
            <EnvironmentBannerBar productConfiguration={props.productConfiguration} />
            <div className={`configuration-top-nav ${props.productConfiguration.isSurveyVue() ? 'surveyvue' : ''}`}>
                <div className="desktop-nav">
                    <div className="nav-container">
                        <div className="flex-container">
                            <DesktopLogo productConfiguration={props.productConfiguration} startPageUrl={startURL} />
                        </div>
                        {showAccountMenu && <div className="menu-icons-container">
                            {showbrandVueHelpLink(props.productConfiguration) &&
                                <a className="circular-nav-button" target="_blank" href={props.productConfiguration.getHelpLink()} title="Help">
                                    <div className="circle">
                                        <i className="material-symbols-outlined">help</i>
                                    </div>
                                    <span className="text">Help</span>
                                </a>}
                            <div className="button-container">
                                <AccountOptions productConfiguration={props.productConfiguration} />
                            </div>
                        </div>}
                    </div>
                </div>
            </div>
        </div>
    );
}

export default ConfigurationTopNav;