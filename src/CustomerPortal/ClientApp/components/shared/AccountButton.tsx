import React from "react";
import '@Styles/topmenu.scss';
import { IUserContext, FeatureCode,ProjectType } from "../../CustomerPortalApi";
import gravatar from "gravatar";
import { useEffect, useState } from "react";
import { SAVANTA_SHORTCODE } from "../../utils";
import {FeatureProvider,useFeatureContext} from "@Store/FeatureContext";

const AccountButton = (props: { subProductId?: string, user?: IUserContext, projectType?:ProjectType, projectId?: number, authCompanyId: string}) => {

    const [isGravatarImageValid, setGravatarImageValid] = useState<boolean>(true);

    const isAdministrator = props.user != null && (props.user.isAdministrator || props.user.isSystemAdministrator);
    const isSystemAdministrator = props.user != null && props.user.isSystemAdministrator;
    const isThirdPartyLogin = props.user != null && props.user.isThirdPartyLoginAuth;
    const isAuthorizedSavantaUser = props.user != null && props.user.isAuthorizedSavantaUser;
    const username = props.user?.userName;
    const { features, isFeatureEnabled } = useFeatureContext();

    useEffect(() => {
        setGravatarImageValid(username != null);
    }, [props.user]);

    const renderSingleManagedUserLink = (url: string) => {
        return (
            <li>
                <a href={url} title="Manage users available to this company">
                    <span><i className="material-symbols-outlined">lock</i></span>
                    <span className="text">Manage users</span>
                </a>
            </li>
        );
    }

    const getManageUsersLink = () => {
        if (isAdministrator) {
            if (isFeatureEnabled(FeatureCode.User_management)) {
                const baseUrl = `${window.location.protocol}//${window.location.host}/usermanagement`;
                const projectType = props.projectType === ProjectType.SurveyGroup ? "allvuesurveygroup" : "allvuesurvey";
                const manageProjectUrl = props.subProductId ?
                    baseUrl + `/projects/${props.authCompanyId}/${projectType}/${props.projectId}` :
                    baseUrl + `/projects`
                    ;
                return (
                    <li className="node">
                    <a><span><i className="material-symbols-outlined">lock</i></span><span className="text">Manage</span></a>
                        <ul>
                        <li><a href={baseUrl} title="Manage users available to this company">Users</a></li>
                            <li><a href={manageProjectUrl}
                               title="Manage project access"
                            >Project access</a></li>
                        </ul>
                    </li>
                    );
            } else {
                const url = (window as any).appBasePath + '/account/manageusers';
                return renderSingleManagedUserLink(url);
            }
        }
        return null;
    }

    const getEditThemeLink = () => {
        const isSavantaOrganisation = props.user?.authCompany === SAVANTA_SHORTCODE;
        if (isAdministrator && !isSavantaOrganisation && !isFeatureEnabled(FeatureCode.User_management)) {
            return (
                <li><a href={(window as any).appBasePath + '/account/edittheme'}>
                    <span><i className="material-symbols-outlined">lock</i></span>
                    <span className="text">Edit organisation theme</span>
                </a></li>
            );
        }
        return null;
    }

    const getSurveySpecificLinks = () => {
        if (props.subProductId != null && isSystemAdministrator) {
            const baseUrl = `${window.location.protocol}//${window.location.host}`;
            const weightingEnabled = (window as any).weightingConfigurationEnabled;
            return (
                <li className="node">
                    <a><span><i className="material-symbols-outlined">lock</i></span><span className="text">Configure </span></a>
                    <ul>
                        <li><a href={baseUrl + `/survey/${props.subProductId}/ui/subset-configuration`}>Subsets</a></li>
                        <li><a href={baseUrl + `/survey/${props.subProductId}/ui/colour-configuration`}>Colours</a></li>
                        <li><a href={baseUrl + `/survey/${props.subProductId}/ui/entity-type-configuration`}>Entity types</a></li>
                        <li><a href={baseUrl + `/survey/${props.subProductId}/ui/page-configuration`}>Pages</a></li>
                        <li><a href={baseUrl + `/survey/${props.subProductId}/ui/metric-configuration`}>Metrics</a></li>
                        <li><a href={baseUrl + `/survey/${props.subProductId}/ui/average-configuration`}>Averages</a></li>
                        <li><a href={baseUrl + `/survey/${props.subProductId}/ui/question-variable-definition-configuration`}>Question variables</a></li>
                        {weightingEnabled && <li><a href={baseUrl + `/survey/${props.subProductId}/ui/weightings-configuration`}>Target weightings</a></li>}
                        <li><a href={baseUrl + `/survey/${props.subProductId}/ui/features-configuration`}>Features</a></li>
                    </ul>
                </li>
            );
        }
        return null;
    }
        
    const getExportDataLink = () => {
        if (props.subProductId != null && isAdministrator && isAuthorizedSavantaUser) {
            const baseUrl = `${window.location.protocol}//${window.location.host}`;
            return (
                <li><a href={baseUrl + `/exportdata?product=survey&subProduct=${props.subProductId}&subset=`}>Export data</a></li>
            );
        }
        return null;
    }

    const getChangePasswordLink = () => {
        if(!isThirdPartyLogin) {
            return (<li><a href={(window as any).appBasePath + '/account/changepassword'}>Change password</a></li>);
        }
        return null;
    }

    const handleImage404Error = ()=> {
        setGravatarImageValid(false);
    };

    return <div className="account-options" title="Manage your account">
        <div className="dropdown">
            <div>
                <button className="nav-button" type="button" id="accountMenuButton" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                    <div className="circular-nav-button">
                        <div className="circle">
                            {!isGravatarImageValid &&
                                <i className='material-symbols-outlined'>account_circle</i>
                            }
                            {isGravatarImageValid &&
                                <img src={gravatar.url(props.user?.userName, { s: '32', r: 'g', d: '404' }, true)} onError={() => {
                                    handleImage404Error();
                                }} />
                            }
                        </div>
                        <div className="text">My Account</div>
                    </div>
                </button>
                <div>
                    <ul className="dropdown-menu account-menu" aria-labelledby="accountMenuButton">
                        <li><a className="dropdown-item readonly" title={props.user?.userName}>{props.user?.userName}</a></li>
                        <div className="dropdown-item separator"></div>
                        {getManageUsersLink()}
                        {getEditThemeLink()}
                        {getSurveySpecificLinks()}
                        <li><a href={(window as any).productPage} title="View projects that are available to you">Your projects</a></li>
                        {getExportDataLink()}
                        {getChangePasswordLink()}
                        <li><a href={(window as any).appBasePath + '/account/logout'} title="Logout">Logout</a></li>
                    </ul>
                </div>
            </div>
        </div>
    </div>;
}

export default AccountButton;
