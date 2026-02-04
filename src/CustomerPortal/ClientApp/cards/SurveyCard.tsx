import React from 'react';
import moment from 'moment';
import "@Styles/cards.scss";
import { CustomUIIntegration, IntegrationPosition, PermissionFeaturesOptions, IntegrationStyle, IProject, IUserContext, ProjectType} from '../CustomerPortalApi';
import {NavLinkExt} from '../routes';
import SavantaOnlyLock from "@Cards/SavantaOnlyLock";
import {SAVANTA_SHORTCODE} from "@Utils";
import { useProductConfigurationContext } from '@Store/ProductConfigurationContext';

const SurveyCard = (props: { project: IProject, user: IUserContext }) => {
    const { getSettingsPageUrl, getOpenEndsPageUrl } = useProductConfigurationContext();
    const getDateFormatted = (className: string, date?:Date) => {
        if (date) {
            return <span className={className}>{moment(date).format("DD MMM YYYY")}</span>;
        }
        className += " error";
        return <span className={className}>Not available</span>;
    }

    const getIcon = () => {
        if (props.project.isPaused){
            return  <i className={`material-symbols-outlined no-symbol-fill pause-icon`}>pause_circle</i>
        } else if (props.project.isOpen){
            return <i className={`material-symbols-outlined no-symbol-fill in-progress-icon`}>pending</i>
        }else {
            return <i className={`material-symbols-outlined no-symbol-fill completed-icon`}>check_circle</i>
        }
    }

    const getLinkFromIntegrations = (position: IntegrationPosition): CustomUIIntegration | undefined => {
        const preferedTabs = props.project.customIntegrations.filter(item => item.style == IntegrationStyle.Tab);
        if (preferedTabs.length > 0) {
            const preferredLhTab = preferedTabs.filter(x => x.position == position);
            if (preferredLhTab.length > 0) {
                return preferredLhTab[0];
            }
        }
        return undefined;
    };

    const isFeaturePermissionEnabled = (feature: PermissionFeaturesOptions): boolean => {
        if (props.user.isSystemAdministrator || props.user.isAdministrator) {
            return true;
        }
        if (!props.user.featurePermissions || props.user.featurePermissions.length === 0) {
            return false;
        }
        return props.user.featurePermissions.some(f => f.code === feature);
    };
    const getLink = () => {
        const lhIntegration = getLinkFromIntegrations(IntegrationPosition.Left);
        const rhIntegration = getLinkFromIntegrations(IntegrationPosition.Right);

        if (props.project.projectType === ProjectType.SurveyGroup) {
            const statusPageEnabled = props.user?.isSystemAdministrator ?? false;
            if (statusPageEnabled)
                return `/Survey/Status/${props.project.subProductId}`;
            
            if (lhIntegration != null) {
                return lhIntegration.path;
            }

            if (isFeaturePermissionEnabled(PermissionFeaturesOptions.DataAccess) && props.project.dataPageUrl)
                return props.project.dataPageUrl;

            if (isFeaturePermissionEnabled(PermissionFeaturesOptions.ReportsView) && props.project.reportsPageUrl)
                return props.project.reportsPageUrl;

            if (rhIntegration != null) {
                return rhIntegration.path;
            }

            if (isFeaturePermissionEnabled(PermissionFeaturesOptions.SettingsAccess)) {
                return getSettingsPageUrl(props.project.subProductId);
            }

            return '';
        }

        if (lhIntegration != null) {
            return lhIntegration.path;
        }
        if (isFeaturePermissionEnabled(PermissionFeaturesOptions.QuotasAccess) && props.project.isQuotaTabAvailable) {
            return `/Survey/Quotas/${props.project.subProductId}`;
        }
        if (isFeaturePermissionEnabled(PermissionFeaturesOptions.DocumentsAccess) && props.project.isDocumentsTabAvailable) {
            return `/Survey/Documents/${props.project.subProductId}`;
        }
        if (isFeaturePermissionEnabled(PermissionFeaturesOptions.DataAccess) && props.project.dataPageUrl) {
            return props.project.dataPageUrl;
        }
        if (isFeaturePermissionEnabled(PermissionFeaturesOptions.ReportsView) && props.project.reportsPageUrl) {
            return props.project.reportsPageUrl;
        }
        if (isFeaturePermissionEnabled(PermissionFeaturesOptions.AnalysisAccess) ) {
            return getOpenEndsPageUrl(props.project.subProductId);
        }
        if (rhIntegration != null) {
            return rhIntegration.path;
        }
        if (isFeaturePermissionEnabled(PermissionFeaturesOptions.SettingsAccess)) {
            return getSettingsPageUrl(props.project.subProductId);
        }

        return '';
    }

    return (
        <NavLinkExt to={!props.user.isReportViewer ? getLink() : props.project.reportsPageUrl}>
            <div key={props.project.subProductId} className="card survey-container">
                <div className="left-items">
                    {getIcon()}
                    <div className='survey-details'>
                        <div className='survey-name'>{props.project.name}</div>
                        {props.project.projectType === ProjectType.SurveyGroup &&
                            <div className='survey-info'>
                                <div className="survey-multi-wave">Multi-wave survey</div>
                            </div>
                        }
                        <div className='survey-info'>
                            {props.project.projectType === ProjectType.SurveyGroup &&
                                <div className="survey-text">Latest wave:</div>
                            }
                            <div className={`survey-percentage ${props.project.percentComplete == 100 ? "completed-text" : ""}`}><span className="info-bold">{props.project.percentComplete}%</span> complete</div>
                            <div className="survey-responses"><span className="info-bold">{props.project.complete.toLocaleString()}</span> responses</div>
                            <div className="survey-date launched">
                                Launched {getDateFormatted("launch-date", props.project.launchDate)}
                            </div>
                            {props.project.completeDate && !props.project.isOpen &&
                                <div className="survey-date">
                                    Closed {getDateFormatted("completed-date", props.project.completeDate)}
                                </div>
                            }
                            {props.project.isPaused &&
                                <div className="survey-paused">Paused</div>
                            }
                        </div>
                    </div>
                </div>
                {props.user.authCompany !== SAVANTA_SHORTCODE &&
                    <SavantaOnlyLock project={props.project} />
                }
            </div>
        </NavLinkExt>
    );
}

export default SurveyCard;
