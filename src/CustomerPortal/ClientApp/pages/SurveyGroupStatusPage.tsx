import "@Styles/main.scss";
import "@Styles/surveyDetailsPage.scss";
import React from 'react';
import { useDataLoader } from '@Store/dataLoading';
import SurveyGroupWaveCard from "@Cards/SurveyGroupWaveCard";
import {useNavigate, useParams} from "react-router";
import {useProductConfigurationContext} from "@Store/ProductConfigurationContext";
import { GoogleTagManager } from '../util/googleTagManager';

const SurveyGroupStatusPage = (props: {googleTagManager: GoogleTagManager}) => {
    const { productConfiguration } = useProductConfigurationContext();
    const navigate = useNavigate();
    const params = useParams();
    const subProductId = params.id;
    const selectedSurveyGroup = useDataLoader((c) => c.getSurveyGroupDetails(subProductId));

    React.useEffect(() => {
        if (selectedSurveyGroup?.subProductId) {
            props.googleTagManager.addEvent("surveyGroupStatusView", selectedSurveyGroup?.organisationShortCode, selectedSurveyGroup?.subProductId);
        }
    }, [selectedSurveyGroup]);

    if (!productConfiguration.user?.isSystemAdministrator ?? false) {
        navigate("/");
        return <></>;
    }

    const childSurveys = selectedSurveyGroup?.childSurveys
        .sort((a,b) => b.launchDate.getTime() - a.launchDate.getTime());
    return (selectedSurveyGroup &&
        <div className='survey-group-info-page'>
            <div className="savanta-admin-warning">
                <i className="material-symbols-outlined no-symbol-fill">admin_panel_settings</i>
                This status page is currently only available to Savanta admins
            </div>
            <div className="survey-group-info">
                <div className="label">Fieldwork</div>
                <div className="survey-list">
                    {childSurveys.map(project =>  {
                        return (
                            <SurveyGroupWaveCard key={project.subProductId}
                                project={project}
                                user={productConfiguration.user}
                                surveyGroup={selectedSurveyGroup}
                                googleTagManager={props.googleTagManager} />)
                    })}
                </div>
            </div>
        </div>
    );
}

export default (SurveyGroupStatusPage);