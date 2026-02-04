import React from 'react';
import { Metric } from "../../../../metrics/metric";
import { Modal, ModalBody } from "reactstrap";
import MetricSelectList from '../Components/MetricSelectList';
import VariableContentModal from "../../Variables/VariableModal/VariableContentModal";
import { PermissionFeaturesOptions, ReportVariableAppendType } from '../../../../BrandVueApi';
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from 'client/state/subsetSlice';
import FeatureGuard from 'client/components/FeatureGuard/FeatureGuard';

export const QuestionModalOpenSessionStorageKey = "QuestionModalOpen";

interface IAddMetricsModal {
    isOpen: boolean;
    metrics: Metric[];
    getPrimaryButtonText(metrics: Metric[]): string;
    modalHeaderText: string;
    onMetricsSubmitted(metrics: Metric[]): void;
    setAddChartModalVisibility(isVisible: boolean): void;
    preSelectedMetrics?: Metric[];
    disableSelectAll?: boolean;
    onMetricAdded?(): void;
    saveOpenStateToSessionOnVariableCreation?: boolean;
};

export const AddMetricsModal = (props: IAddMetricsModal) => {
    const [selectedMetrics, setSelectedMetrics] = React.useState<Metric[]>([]);
    const [isVariableModalOpen, setIsVariableModalOpen] = React.useState<boolean>(false)
    const [newlyAddedMetricName, setNewlyAddedMetricName] = React.useState<string | undefined>(undefined)
    const subsetId = useAppSelector(selectSubsetId);

    const toggle = () => {
        setSelectedMetrics([]);
        props.setAddChartModalVisibility(false);
        setNewlyAddedMetricName(undefined);
    };

    const onMetricsSubmitted = () => {
        props.onMetricsSubmitted(selectedMetrics);
        toggle();
    }

    const getPrimaryButton = () => {
        const isDisabled = selectedMetrics.length === 0;
        return (
            <button className="primary-button button" disabled={isDisabled} onClick={() => onMetricsSubmitted()}>
                {props.getPrimaryButtonText(selectedMetrics)}
            </button>
        );
    }

    const createVariableButton = (
        <FeatureGuard permissions={[PermissionFeaturesOptions.VariablesCreate]}>
            <button className="hollow-button create-variable-button" onClick={() => setIsVariableModalOpen(true)}>
                <i className="material-symbols-outlined">add</i>
                <div className="new-variable-button-text">New variable</div>
            </button>
        </FeatureGuard>
    );

    const saveQuestionModalOpenFlagToSessionStorage = () => {
        window.sessionStorage.setItem(QuestionModalOpenSessionStorageKey, "1");
    }

    const processNewMetricName = (metricName: string) => {
        if (props.saveOpenStateToSessionOnVariableCreation) {
            saveQuestionModalOpenFlagToSessionStorage();
        }
        setNewlyAddedMetricName(metricName);
    }

    //this effect is needed to handle the race condition between the validMetrics and the newMetric
    React.useEffect(() => {
        if (newlyAddedMetricName) {
            const newMetric = props.metrics.find(m => m.urlSafeName == newlyAddedMetricName);
            if (newMetric) {
                const clonedSelectedMetrics = [...selectedMetrics];
                if (!clonedSelectedMetrics.some(c => c.urlSafeName == newlyAddedMetricName)) {
                    clonedSelectedMetrics.push(newMetric);
                    setSelectedMetrics(clonedSelectedMetrics);
                }
            }
            else {
                if (props.onMetricAdded != undefined) {
                    props.onMetricAdded()
                }
            }
        }

    }, [props.metrics, newlyAddedMetricName])

    return (
        <>
            <Modal isOpen={props.isOpen} centered={true} className='add-chart-modal' toggle={toggle}>
                <ModalBody>
                    <div className="top-buttons">
                        <button onClick={toggle} className="modal-close-button">
                            <i className="material-symbols-outlined">close</i>
                        </button>
                    </div>
                    <div className="header">
                        <div>{props.modalHeaderText}</div>
                    </div>
                    <MetricSelectList availableMetrics={props.metrics}
                        selectedMetrics={selectedMetrics}
                        updateSelectedMetrics={setSelectedMetrics}
                        additionalButton={createVariableButton}
                        disableSelectAll={props.disableSelectAll}
                    />
                    <div className="buttons confirmation">
                        <button className="secondary-button button" onClick={toggle}>Cancel</button>
                        {getPrimaryButton()}
                    </div>
                </ModalBody>
            </Modal>
            <VariableContentModal
                isOpen={isVariableModalOpen}
                subsetId={subsetId}
                setIsOpen={setIsVariableModalOpen}
                reportAppendType={ReportVariableAppendType.Part}
                returnCreatedMetricName={(metricName: string) => { processNewMetricName(metricName) }}
            />
        </>
    );
};

export default AddMetricsModal;