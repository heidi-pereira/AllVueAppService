import 'material-symbols';
import './topmenu.scss';
import './surveyPage.scss';
import './allVueHomePageLogo.scss';
import './surveyDetailsPage.scss';
import { useEffect, useState, useRef } from "react";
import gravatar from "gravatar";
import { IUserContext } from './IUserContext';
import { NavigationTab } from '../../Model/Model';
import { lhsWidgets, listOfTabs, renderCustomTab, rhsWidgets } from './CustomUiIntegrationHelper';
import { ISurveyContext } from './ISurveyContext';
import { FeatureGuard } from '@/components/FeatureGuard';
import { PermissionFeaturesOptions } from '../../orval/api/models/permissionFeaturesOptions';


const AllVueHomePageLogo = () => <a className={"logo-link-container"} href={window.location.origin + '/auth'}>
    <img style={{ content: 'var(--header-logo)' }} alt="Logo" className="page-logo" />
</a>

function useOutsideClick(ref: HTMLDivElement | null, onClickOut: () => void) {
    useEffect(() => {
        const onClick = ({ target }: any) => !ref?.contains(target) && onClickOut?.()
        document.addEventListener("click", onClick);
        return () => document.removeEventListener("click", onClick);
    }, [onClickOut, ref]);
}

const AccountButton = (props: { subProductId?: string, user?: IUserContext }) => {

    const [isGravatarImageValid, setGravatarImageValid] = useState<boolean>(true);

    const isAdministrator = props.user != null && (props.user.isAdministrator || props.user.isSystemAdministrator);

    useEffect(() => {
        setGravatarImageValid(props.user?.userName != null);
    }, [props.user?.userName]);


    const getManageUsersLink = () => {
        if (isAdministrator) {
            return (
                <li><a href={'/auth/userspage'} title="Manage Organisation Users">
                    <span><i className="material-symbols-outlined">lock</i></span>
                    <span className="text">Manage users</span>
                </a></li>
            );
        }
        return null;
    }

    const handleImage404Error = () => {
        setGravatarImageValid(false);
    };

    const [dropdownOpen, setDropdownOpen] = useState(false);

    const ref = useRef<HTMLDivElement>(null);

    useOutsideClick(ref.current, () => {
        setDropdownOpen(false);
    });

    return <div ref={ref} className="account-options" title="Manage your account">
        <div className="dropdown">
            <div>
                <button className="nav-button" type="button" id="accountMenuButton" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" onClick={() => setDropdownOpen(!dropdownOpen)}>
                    <div className="circular-nav-button">
                        <div className="circle">
                            {!isGravatarImageValid &&
                                <i className='material-symbols-outlined'>account_circle</i>
                            }
                            {isGravatarImageValid &&
                                <img src={gravatar.url(props.user?.userName ?? '', { s: '32', r: 'g', d: '404' }, true)} onError={() => {
                                    handleImage404Error();
                                }} />
                            }
                        </div>
                        <div className="text">My Account</div>
                    </div>
                </button>
                {dropdownOpen && <div className='dropdownContainer'>
                    <ul className="dropdown-menu account-menu" aria-labelledby="accountMenuButton">
                        <li><a className="dropdown-item readonly" title={props.user?.userName}>{props.user?.userName}</a></li>
                        <div className="dropdown-item separator"></div>
                        {getManageUsersLink()}
                        <li><a href={'/auth'} title="View projects that are available to you">Your projects</a></li>
                        <li><a href={'/auth/Account/Logout'} title="Logout">Logout</a></li>
                        {
                            props.user?.isAdministrator && props.user.isAuthorizedSavantaUser && <>
                                <li><a href='?viewas=ClientUser'>View as Client User</a></li>
                                <li><a href='?viewas=ClientAdmin'>View as Client Admin</a></li>
                                <li><a href='?viewas=SavantaUser'>View as Savanta User</a></li>
                            </>
                        }
                        {
                            !(props.user?.isAdministrator && props.user.isAuthorizedSavantaUser) && (location.search.indexOf('viewas') >= 0) &&
                            <li><a href='.'>Reset view</a></li>
                        }
                    </ul>
                </div>
                }
            </div>
        </div>
    </div>;
}

const TopMenu = (props: { user?: IUserContext, survey?: ISurveyContext }) => {

    return <nav className="topmenu">
        <span className="navbar-brand">
            <AllVueHomePageLogo />
            <div className="page-title-container">
                <span className="page-title">{props.survey?.name}</span>
            </div>

            <div className="menu-icons-container">
                <AccountButton subProductId={"todo"} user={props.user} />
            </div>
        </span>
    </nav>;
}

const ProjectDetailsHeader = (props: { user?: IUserContext, associatedSurvey: ISurveyContext }) => {
    const tabEnabled = (tab: NavigationTab) => props.associatedSurvey?.availableTabs?.includes(tab);

    return <div className="savanta-header">
        <TopMenu user={props.user} survey={props.associatedSurvey} />
        <div className="tabs">
            <div className="tab-link-container">
                {props.associatedSurvey?.customUiIntegrations && lhsWidgets(listOfTabs(props.associatedSurvey?.customUiIntegrations)).map((item, index) => renderCustomTab(item, index))}
                {tabEnabled(NavigationTab.Quota) &&
                    <FeatureGuard permissions={[PermissionFeaturesOptions.QuotasAccess]}>
                        <a className="tab-link" href={`${window.location.protocol}//${window.location.host}/projects/survey/quotas/${props.associatedSurvey.id}`}>
                            <i className='material-symbols-outlined'>donut_large</i>
                            <span>Quotas</span>
                        </a>
                    </FeatureGuard>
                }
                {tabEnabled(NavigationTab.Documents) &&
                    <FeatureGuard permissions={[PermissionFeaturesOptions.DocumentsAccess]}>
                        <a className="tab-link" href={`${window.location.protocol}//${window.location.host}/projects/survey/documents/${props.associatedSurvey.id}`}>
                            <i className='material-symbols-outlined'>folder_open</i>
                            <span>Documents</span>
                        </a>
                    </FeatureGuard>
                }
                {tabEnabled(NavigationTab.Data) &&
                    <FeatureGuard permissions={[PermissionFeaturesOptions.DataAccess]}>
                        <a className="tab-link" href={`${window.location.protocol}//${window.location.host}/survey/${props.associatedSurvey.id}/ui/crosstabbing`}>
                            <i className='material-symbols-outlined'>grid_on</i>
                            <span>Data</span>
                        </a>
                    </FeatureGuard>
                }
                {tabEnabled(NavigationTab.Reports) &&
                    <FeatureGuard permissions={[PermissionFeaturesOptions.ReportsView]}>
                        <a className="tab-link" href={`${window.location.protocol}//${window.location.host}/survey/${props.associatedSurvey.id}/ui/reports`}>
                            <i className='material-symbols-outlined'>folder_open</i>
                            <span>Reports</span>
                        </a>
                    </FeatureGuard>
                }
                <FeatureGuard permissions={[PermissionFeaturesOptions.AnalysisAccess]}>
                    <a href={`${window.location.protocol}//${window.location.host}/openends/survey/${props.associatedSurvey.id}`} className="tab-link active" title="Analysis (Beta) - This feature is in testing and may change." aria-label="Analysis (Beta) - This feature is in testing and may change.">
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
                {props.associatedSurvey?.customUiIntegrations && rhsWidgets(listOfTabs(props.associatedSurvey?.customUiIntegrations)).map((item, index) => renderCustomTab(item, index))}
                {props.user?.isAuthorizedSavantaUser &&
                    <FeatureGuard permissions={[PermissionFeaturesOptions.SettingsAccess]}>
                        <a href={`${window.location.protocol}//${window.location.host}/survey/${props.associatedSurvey.id}/ui/settings`} className="tab-link"><i className='material-symbols-outlined'>settings</i><span>Settings</span></a>
                    </FeatureGuard>
                }
            </div>
        </div>
    </div>;
}

export default ProjectDetailsHeader;

