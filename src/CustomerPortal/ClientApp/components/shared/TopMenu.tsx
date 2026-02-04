import React from "react";
import '@Styles/topmenu.scss';
import AccountButton from "./AccountButton";
import { useProductConfigurationContext } from "../../store/ProductConfigurationContext";
import AllVueHomePageLogo from "@Components/shared/AllVueHomePageLogo";
import { CustomUIIntegration, IntegrationPosition, IntegrationStyle, IProject, RunningEnvironment, ProjectType } from "../../CustomerPortalApi";
import { useSelectedProjectContext } from "../../store/SelectedProjectProvider";
import { lhsWidgets, listOfHelpButtons, rhsWidgets } from "../../util/UiControlsHelper";

interface IProps {
    selectedProject?: IProject
}

const TopMenu = (props: IProps = null) => {
    const { productConfiguration } = useProductConfigurationContext();
    const { state } = useSelectedProjectContext();
    const sessionKey = `banner_bar`;

    const isVisible = () => {
        return sessionStorage.getItem(sessionKey) == null;;
    }
    const [isBannerVisible, setIsBannerVisible] = React.useState<boolean>(isVisible());
    

    const setBanner = (flag: boolean) => {
        sessionStorage.setItem(sessionKey, flag.toString());
        setIsBannerVisible(flag);
    }

    const renderCustomHelpButton = (integration: CustomUIIntegration, index: number) => {
        return (<a className="circular-nav-button" target="_blank" href={integration.path} title={integration.altText}>
            <div className="circle">
                <i className="material-symbols-outlined">{integration.icon}</i>
            </div>
            <span className="text">{integration.name}</span>
        </a>);
    }
    return <nav className="topmenu">
        {productConfiguration.runningEnvironment != RunningEnvironment.Live && isBannerVisible &&
            <span className="env-banner" onClick={() => setBanner(false) }>{productConfiguration.runningEnvironmentDescription}</span>
        }
        <span className="navbar-brand">
            <AllVueHomePageLogo user={productConfiguration.user}/>
            <div className="page-title-container">
                <span className="page-title">{props.selectedProject?.name}</span>
            </div>
            <div className="menu-icons-container">
                {state.selectedProject && lhsWidgets(listOfHelpButtons(state.selectedProject.customIntegrations)).
                    map((item, index) => renderCustomHelpButton(item, index))}
                {(!state.selectedProject || state.selectedProject.isHelpIconAvailable) &&
                    <a className="circular-nav-button" target="_blank" href={productConfiguration.helpLink} title="Help">
                        <div className="circle">
                            <i className="material-symbols-outlined">help</i>
                        </div>
                        <span className="text">Help</span>
                    </a>
                }
                <AccountButton subProductId={props.selectedProject?.subProductId}
                    user={productConfiguration.user}
                    authCompanyId={props.selectedProject?.companyAuthId}
                    projectType={props.selectedProject?.projectType}
                    projectId={props.selectedProject?.id}/>
                {state.selectedProject && rhsWidgets(listOfHelpButtons(state.selectedProject.customIntegrations)).
                    map((item, index) => renderCustomHelpButton(item, index))}
            </div>
        </span>
    </nav>;
}

export default TopMenu;