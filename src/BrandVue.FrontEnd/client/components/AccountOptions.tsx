import React, {useState} from 'react'
import gravatar from "gravatar";
import {
    isColourConfigurationEnabled,
    isEntityTypeConfigEnabled,
    isPagesAndMetricsConfigEnabled,
    isSubsetConfigEnabled,
    isWeightingsConfigEnabled,
} from "./helpers/FeaturesHelper";
import {ProductConfiguration} from '../ProductConfiguration';
import Separator from './helpers/Separator';
import {MixPanel} from './mixpanel/MixPanel';
import * as configOptions from './AccountOptionsHelper';
import {Factory,FeatureCode} from '../BrandVueApi';
import { isFeatureEnabled } from "./helpers/FeaturesHelper";
interface IAccountOptionsProps {
    productConfiguration: ProductConfiguration;
}
const MY_PRODUCTS = "My products";
const YOUR_PROJECTS = "Your projects";
const SavantaCompanyShortcode = 'savanta';

const AccountOptions = (props: IAccountOptionsProps) => {
    const [isGravatarImageValid, setIsGravatarImageValid] = useState(true);

    const accountPath = `${props.productConfiguration.appBasePath}/account`;
    const user = props.productConfiguration.user;

    const systemDetails = () => {
        if (props.productConfiguration.user.isSystemAdministrator) {
            Factory.MetaDataClient(() => { }).getSystemDetails().then(details => {
                alert(`Version ${details.version}\nData: ${details.dataDBase}\nMeta: ${details.metaDataDBase}`);
            })
        }
    }
    const trackProjects = () => {

        if (getMyProjectsLinkText() === MY_PRODUCTS)
            MixPanel.track("myProductSelected")
    }
    const handleImage404Error = () => {
        setIsGravatarImageValid(false);
    };
    const getMyProjectsLinkText = () => {
        return props.productConfiguration.isSurveyVue() ? YOUR_PROJECTS : MY_PRODUCTS;
    }
    const getMyProjectsDescription = () => {
        return props.productConfiguration.isSurveyVue() ? "projects" : "products";
    }
    const isConfigureToShow = isSubsetConfigEnabled(props.productConfiguration)
        || isColourConfigurationEnabled(props.productConfiguration)
        || isEntityTypeConfigEnabled(props.productConfiguration)
        || isPagesAndMetricsConfigEnabled(props.productConfiguration)
        || isWeightingsConfigEnabled(props.productConfiguration);
    const isOrganisationThemeToShow = user.isAdministrator
        && props.productConfiguration.isSurveyVue()
        && props.productConfiguration.subdomainOrganisation !== SavantaCompanyShortcode
        && !isFeatureEnabled(FeatureCode.User_management);

    const renderUserOptions = () => {
        if (user.isAdministrator) {
            if (isFeatureEnabled(FeatureCode.User_management)) {
                return (
                    <li className="node">
                        <a>
                            <span><i className="material-symbols-outlined">lock</i></span>
                            <span className="text">Manage</span>
                        </a>
                        <ul>
                            <li>
                                <a  className="dropdown-item"
                                    href={props.productConfiguration.getManageUsersUrl()}
                                    title="Manage users available to this company"
                                    onClick={() => MixPanel.track("manageUsersSelected")}>
                                    Users
                                </a>
                            </li>
                            <li>
                                <a className="dropdown-item"
                                    href={props.productConfiguration.getManageProjectForUsersUrl()}
                                    title="Manage project access"
                                    onClick={() => MixPanel.track("manageUsersSelected")}>
                                    Project access
                                </a>
                            </li>
                        </ul>
                    </li>
                    );
            } else {
                return (<li>
                            <a
                                className="dropdown-item"
                                href={props.productConfiguration.getManageUsersUrl()}
                                title="Manage users available to this company"
                                onClick={() => MixPanel.track("manageUsersSelected")}>
                                <span><i className="material-symbols-outlined">lock</i></span>
                                <span className="text">Manage users</span>
                            </a>
                        </li>);
            }
        }
        return (<></>);
    }

    return (
        <div className="account-options" >
            <div className="dropdown">
                <div>
                    <button
                        className="nav-button"
                        type="button"
                        id="accountMenuButton"
                        data-bs-toggle="dropdown"
                        aria-haspopup="true"
                        aria-expanded="false"
                        title="My Account"
                    >
                        <div className="circular-nav-button">
                            <div className="circle">
                                {!isGravatarImageValid &&
                                    <i className='material-symbols-outlined'>account_circle</i>
                                }
                                {isGravatarImageValid &&
                                    <img
                                        src={gravatar.url(user.userName, { s: '32', r: 'g', d: '404' }, true)}
                                        onError={() => {
                                            handleImage404Error();
                                        }}
                                    />
                                }
                            </div>
                            <div className="text">My Account</div>
                        </div>
                    </button>
                    <div>
                        <ul className="dropdown-menu account-menu" aria-labelledby="accountMenuButton">
                            <li>
                                <a className="dropdown-item readonly" title={user.userName} onClick={() => systemDetails()} >
                                    {user.userName}
                                </a>
                            </li>
                            <Separator />
                            {renderUserOptions()}
                            {isOrganisationThemeToShow &&
                                <li>
                                    <a
                                        className="dropdown-item"
                                        href={`${accountPath}/edittheme`}
                                        title="Edit Organisation Theme"
                                    >
                                        <span><i className="material-symbols-outlined">lock</i></span>
                                        <span className="text">Edit organisation theme</span>
                                    </a>
                                </li>
                            }
                            {isConfigureToShow &&
                                <li className="node">
                                    <a>
                                        <span><i className="material-symbols-outlined">lock</i></span>
                                        <span className="text">Configure </span>
                                    </a>
                                    <ul>
                                        {configOptions.configureSubsets(props)}
                                        {configOptions.configureColours(props)}
                                        {configOptions.configureEntityTypes(props)}
                                        {configOptions.configurePages(props)}
                                        {configOptions.configureMetrics(props)}
                                        {configOptions.configureAverages(props)}
                                        {configOptions.configureQuestionVariableDefinitions(props)}
                                        {configOptions.configureAudiences(props)}
                                        {configOptions.configureWeightings(props)}
                                        {configOptions.configureFeatures(props)}
                                    </ul>
                                </li>
                            }
                            <li>
                                <a
                                    className="dropdown-item"
                                    href={`${accountPath}/products`}
                                    title={`View ${getMyProjectsDescription()} that are available to you`}
                                    onClick={trackProjects}
                                >
                                    {getMyProjectsLinkText()}
                                </a>
                            </li>
                            {!user.isThirdPartyLoginAuth &&
                                <li>
                                    <a
                                        className="dropdown-item"
                                        href={`${accountPath}/changepassword`}
                                        title="Change Password"
                                    >
                                        Change password
                                    </a>
                                </li>
                            }
                            {configOptions.configureExportData(props)}
                            {configOptions.tableBuilderDropdownItem(props)}
                            <li>
                                <a
                                    className="dropdown-item"
                                    href={`${accountPath}/logout`}
                                    title="Logout"
                                    onClick={() => MixPanel.logout()}
                                >
                                    Logout
                                </a>
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    );
}
export default AccountOptions;
