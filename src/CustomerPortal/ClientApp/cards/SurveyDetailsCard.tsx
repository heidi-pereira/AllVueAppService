import React from 'react';
import "@Styles/cards.scss";
import ProgressBar from '../components/shared/ProgressBar';

interface IProps {
    name: string;
    details: IDetailRow[];
}

export interface IDetailRow {
    name: string;
    complete: number;
    target: number;
}

export const SurveyDetailsCard = (props: IProps) => {

    const getCompletionPercentage = (detail: IDetailRow) => {
        if (detail.target && detail.target > 0) {
            return Math.floor((detail.complete / detail.target) * 100);
        } else {
            return null;
        }
    }

    const renderprogressPercentageValue = (progress: number | null) => {
        if (progress != null) {
            if (progress < 100) {
                return <span>{progress}%</span>
            } else {
                return <span className="survey-quotacell-progress-100">{progress}%</span>
            }
        } else {
            return <span className="survey-quotacell-progress-notarget">No target</span>
        }
    }

    const renderDetail = (detail: IDetailRow) => {
        const progress = getCompletionPercentage(detail);
        return (
            <div key={detail.name} className="survey-quotacell">
                <div className="survey-quota-row">
                    <span className="quota-name" title={detail.name}>{detail.name}</span>
                    <span className="response-header">{detail.complete.toLocaleString()}</span>
                    {renderprogressPercentageValue(progress)}
                </div>
                <ProgressBar value={progress} />
            </div>
        );
    }

    return (
        <div className='survey-quota'>
            <div className='survey-quota-header'>
                <span className="quota-name header" title={props.name}>{props.name}</span>
                <span className="response-header">Responses</span>
                <span>Progress</span>
            </div>
            <div className="survey-quota-cell-container">
                {props.details.map(detail => renderDetail(detail))}
            </div>
        </div>
    );
};

