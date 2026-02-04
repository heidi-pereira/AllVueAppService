import React from 'react';
import "@Styles/cards.scss";
import { IProject, IUserContext, SurveyGroupDetails } from '../CustomerPortalApi';
import FuzzyDate from '@Components/FuzzyDate';
import { NavLinkExt } from '../routes';
import { GoogleTagManager } from '../util/googleTagManager';

const SurveyGroupWaveCard = (props: { project: IProject, user: IUserContext, surveyGroup: SurveyGroupDetails, googleTagManager: GoogleTagManager }) => {

    const trackClickEvent = () => {
        props.googleTagManager.addEvent("surveyGroupStatusNavigateSubSurvey", props.surveyGroup.organisationShortCode, props.surveyGroup.subProductId);
    }

    const getCardContent = () => {
        return (
            <div key={props.project.subProductId} className='card survey-group-wave-container' onClick={trackClickEvent}>
                <div className="left-items">
                    <div className='survey-name'>
                        <i className='material-symbols-outlined'>waves</i> {props.project.name}
                    </div>
                    <div className='survey-details'>
                        <div className="survey-percentage"><span className="info-bold">{props.project.percentComplete}%</span> complete</div>
                        <div className="survey-responses"><span className="info-bold">{props.project.complete.toLocaleString()}</span> responses</div>
                        {props.project.launchDate ?
                            <div>Launched <FuzzyDate date={props.project.launchDate} lowerCase includePastFuture/></div>
                            :
                            <div>launch date <span className="error">not available</span> </div>
                        }
                        {props.project.isPaused &&
                            <div className="survey-paused">Paused</div>
                        }
                    </div>
                </div>
                <div className="survey-arrow">
                    <i className='material-symbols-outlined survey-arrow-icon'>arrow_forward</i>
                </div>
            </div>
        );
    }

    return (
        <NavLinkExt to={!props.user.isReportViewer ? '/Survey/Quotas/' + props.project.subProductId : props.project.reportsPageUrl}>
            {getCardContent()}
        </NavLinkExt>
    );
}

export default SurveyGroupWaveCard;