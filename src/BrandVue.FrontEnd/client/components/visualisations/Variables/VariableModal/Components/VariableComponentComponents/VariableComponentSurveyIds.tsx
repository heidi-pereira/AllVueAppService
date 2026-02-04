import React from 'react';
import {SurveyIdVariableComponent, SurveyRecord} from '../../../../../../BrandVueApi';

interface IVariableComponentSurveyIdsProps {
    component: SurveyIdVariableComponent;
    availableSurveys: SurveyRecord[];
    setComponentForGroup(component: SurveyIdVariableComponent): void;
}

const VariableComponentSurveyIds = (props: IVariableComponentSurveyIdsProps) => {
    const selectedSurveyIds = [...props.component.surveyIds];

    const toggleSurvey = (surveyId: number) => {
        const indexOfId = selectedSurveyIds.indexOf(surveyId);
        if (indexOfId > -1) {
            //Already contains survey id so remove
            selectedSurveyIds.splice(indexOfId, 1);
        } else {
            //Add survey id
            selectedSurveyIds.push(surveyId);
        }

        const newComponent: SurveyIdVariableComponent = new SurveyIdVariableComponent({
            ...props.component,
            surveyIds: selectedSurveyIds,
        });

        props.setComponentForGroup(newComponent);
    }

    const updateAll = (select: boolean) => {
        const newIds = select ? props.availableSurveys.map(s => s.surveyId) : [];
        const newComponent: SurveyIdVariableComponent = new SurveyIdVariableComponent({
            ...props.component,
            surveyIds: newIds,
        });
        props.setComponentForGroup(newComponent);
    }

    return (
        <div className="instance-selector">
            <div className="instance-buttons">
                <button className="instance-button secondary-button" onClick={() => updateAll(true)}>Select all</button>
                <button className="instance-button secondary-button" onClick={() => updateAll(false)}>Clear all</button>
            </div>
            {props.availableSurveys.map((survey, index) => {
                const name = `${survey.surveyId} - ${survey.surveyName}`;
                return (
                    <div className="instance-checkbox" key={`${survey.surveyId}-${index}`}>
                        <input type="checkbox" className="checkbox"
                            checked={selectedSurveyIds.includes(survey.surveyId)}
                            onChange={() => toggleSurvey(survey.surveyId)} />
                        <label className="instance-checkbox-label" title={name} onClick={() => toggleSurvey(survey.surveyId)}>
                            <span>{name}</span>
                        </label>
                    </div>
                );
            })}
        </div>
    );
}

export default VariableComponentSurveyIds;