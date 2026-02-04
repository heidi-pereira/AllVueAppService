import React from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { Helmet } from 'react-helmet';
import ProgressBar from "../components/shared/ProgressBar";
import { useDataLoader } from '@Store/dataLoading';
import { useSelectedProjectContext, SelectedProjectProvider } from '@Store/SelectedProjectProvider';
import '@Styles/surveyDetailsContainerPage.scss';
import { IntegrationPosition, IntegrationStyle, IProject, ProjectType, FeatureCode, PermissionFeaturesOptions, Feature } from '../CustomerPortalApi';
import { useEffect } from "react";
import Loader from "@Components/shared/Loader";
import { useProductConfigurationContext } from '@Store/ProductConfigurationContext';
import { useFeatureContext } from '@Store/FeatureContext';
import { getResearchPortalUrl } from '@Utils';
import { useParams } from "react-router";
import TopMenu from '../components/shared/TopMenu';
import FeatureGuard from '../components/FeatureGuard/FeatureGuard';

const ProjectDetailsHeader = (props: { subProductId: string }) => {

    const { state } = useSelectedProjectContext();
    const { productConfiguration, getDataPageUrl, getReportsPageUrl, getSettingsPageUrl, getOpenEndsPageUrl } = useProductConfigurationContext();
    const { isFeatureEnabled } = useFeatureContext();

    const selectedProject = state.selectedProject;

    const generateTestSurveyLink = () => {
        return `${getResearchPortalUrl(window)}/api/redirect/testlink?uniqueSurveyId=${selectedProject.uniqueSurveyId}`;
    }

    const getTestSurveyLink = () => {
        if (selectedProject.projectType === ProjectType.Survey) {
            return (
                <a href={generateTestSurveyLink()} className="open-survey-link" target="_blank">
                    <span className="link-text">Open test survey</span>
                    <i className='material-symbols-outlined'>open_in_new</i>
                </a>
            );
        }
        return null;
    }

    const getTabs = () => {
        if (selectedProject.projectType === ProjectType.Survey) {
            return (
                <>
                    {selectedProject.isQuotaTabAvailable &&
                        <FeatureGuard permissions={[PermissionFeaturesOptions.QuotasAccess]}>
                            <NavLink to={"/Survey/Quotas/" + selectedProject.subProductId} className={({ isActive }) => isActive ? "tab-link active" : "tab-link"}>
                                <i className='material-symbols-outlined'>donut_large</i>
                                <span>Quotas</span>
                            </NavLink>
                        </FeatureGuard>
                    }
                    {selectedProject.isDocumentsTabAvailable &&
                        <FeatureGuard permissions={[PermissionFeaturesOptions.DocumentsAccess]}>
                            <NavLink to={"/Survey/Documents/" + selectedProject.subProductId} className={({ isActive }) => isActive ? "tab-link active" : "tab-link"}>
                                <i className='material-symbols-outlined'>folder_open</i>
                                <span>Documents</span>
                            </NavLink>
                        </FeatureGuard>
                    }
                </>
            );
        }

        if (selectedProject.projectType === ProjectType.SurveyGroup && productConfiguration.user?.isSystemAdministrator) {
            return (
                <NavLink to={"/Survey/Status/" + selectedProject.subProductId} className={({ isActive }) => isActive ? "tab-link active" : "tab-link"}>
                    <i className='material-symbols-outlined no-symbol-fill'>info</i>
                    <span>Status</span>
                </NavLink>
            );
        }

        return null;
    }

    const getCompletionPercentage = () => {
        if (selectedProject.target && selectedProject.target > 0) {
            return Math.floor((selectedProject.complete / selectedProject.target) * 100);
        } else {
            return null;
        }
    }

    const getCustomTabs = (position: IntegrationPosition) => {
        const relevantTabs = state.selectedProject.customIntegrations.filter(x => x.style == IntegrationStyle.Tab && x.position == position);
        return <>
            {relevantTabs.map((x, index) => {

                return (<a href={x.path} className="tab-link" key={`${position}${index}`}><i className='material-symbols-outlined'>{x.icon}</i><span>{x.name}</span></a>
                )
            })}</>
    }

    return selectedProject &&
        <>
            <Helmet>
                <title>{selectedProject.name}</title>
            </Helmet>
            <TopMenu selectedProject={selectedProject} />
            {selectedProject.projectType === ProjectType.Survey && <ProgressBar value={getCompletionPercentage()} />}
            <div className="tabs">
                <div className="tab-link-container">
                    {getCustomTabs(IntegrationPosition.Left)}
                    {getTabs()}
                    {productConfiguration.vueContext.vueDataEnabled && state.selectedProject.dataPageUrl != '' &&
                        <FeatureGuard permissions={[PermissionFeaturesOptions.DataAccess]}>
                            <a href={getDataPageUrl(selectedProject.subProductId)} className="tab-link data-tab" key="1"><i className='material-symbols-outlined'>grid_on</i><span>Data</span></a>
                        </FeatureGuard>
                    }
                    <FeatureGuard
                        permissions={[PermissionFeaturesOptions.ReportsView]}
                        customCheck={(_, isAuthorized) =>
                            isAuthorized &&
                            productConfiguration.vueContext.vueReportsEnabled &&
                            state.selectedProject.reportsPageUrl !== ''
                        }
                    >
                            <a href={getReportsPageUrl(selectedProject.subProductId)} className="tab-link" key="2"><i className='material-symbols-outlined'>insights</i><span>Reports</span></a>
                        </FeatureGuard>
                    {isFeatureEnabled(FeatureCode.Open_ends) &&
                    <FeatureGuard permissions={[PermissionFeaturesOptions.AnalysisAccess]}>
                            <a className="tab-link" href={getOpenEndsPageUrl(selectedProject.subProductId)} title="Analysis (Beta) - This feature is in testing and may change." aria-label="Analysis (Beta) - This feature is in testing and may change.">
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
                    {getCustomTabs(IntegrationPosition.Right)}
                    {productConfiguration.vueContext.vueSettingsEnabled &&
                        <FeatureGuard permissions={[PermissionFeaturesOptions.SettingsAccess]}>
                            <a href={getSettingsPageUrl(selectedProject.subProductId)} className="tab-link" key="3"><i className='material-symbols-outlined'>settings</i><span>Settings</span></a>
                        </FeatureGuard>
                    }
                </div>
                {getTestSurveyLink()}
            </div>
        </>;
}

const ProjectDetailsContainerPage = () => {
    const params = useParams();

    return <SelectedProjectProvider>
        <ProjectDetailsContainer id={params.id} />
    </SelectedProjectProvider>;
}

const ProjectDetailsContainer = ({ id }) => {
    const { state, dispatch } = useSelectedProjectContext();
    const subProductId = id;
    const [dataFetchId, setDataFetchId] = React.useState<number>(1);
    const selectedProject = state.selectedProject;

    useDataLoader((c) => c.getProject(subProductId), null, (s: IProject) => { dispatch({ type: "SET_SELECTEDSEARCH", data: s }) }, dataFetchId);
    useEffect(() => setDataFetchId(-1 * dataFetchId), [id])

    const getContent = () => {
        if (!selectedProject) {
            return <Loader show={true} />;
        } else {
            return <Outlet />;
        }
    }

    return (
        <>
            <ProjectDetailsHeader subProductId={id} />
            <div className='survey-details-root-container'>
                {getContent()}
            </div>
        </>
    );
}

export default ProjectDetailsContainerPage;

