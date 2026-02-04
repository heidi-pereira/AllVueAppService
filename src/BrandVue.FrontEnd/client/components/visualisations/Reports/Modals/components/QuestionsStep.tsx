import Throbber from '../../../../throbber/Throbber';
import { Metric } from '../../../../../metrics/metric';
import MetricSelectList from '../../Components/MetricSelectList';

export interface IQuestionsStepProps {
    numberOfPages: number;
    isCreatingReport: boolean;
    metricsForReports: Metric[];
    selectedMetrics: Metric[];
    setSelectedMetrics: (m: Metric[]) => void;
    canPickWaves: boolean;
    onBack: () => void;
    onNext: () => void;
    onCreate: () => void;
};

const QuestionsStep = (props: IQuestionsStepProps) => {
    const questionsPageValid = props.selectedMetrics.length > 0;

    return (
    <>
        <div className="details">2 of {props.numberOfPages}: Questions / variables to include</div>
        <div className="content-and-buttons">
            {!props.isCreatingReport && (
                <>
                    <div className="charts-to-include">
                        <MetricSelectList availableMetrics={props.metricsForReports} selectedMetrics={props.selectedMetrics} updateSelectedMetrics={props.setSelectedMetrics} />
                    </div>
                    <div className="modal-buttons">
                        <button className="modal-button secondary-button" onClick={props.onBack}>
                            Back
                        </button>
                        {props.canPickWaves && (
                            <button className="modal-button primary-button" onClick={props.onNext} disabled={!questionsPageValid}>
                                Next
                            </button>
                        )}
                        {!props.canPickWaves && (
                            <button className="modal-button primary-button" onClick={props.onCreate} disabled={!questionsPageValid}>
                                Create report
                            </button>
                        )}
                    </div>
                </>
            )}
            {props.isCreatingReport && (
                <div className="throbber-container-fixed">
                    <Throbber />
                </div>
            )}
        </div>
    </>
    )
};

export default QuestionsStep;
