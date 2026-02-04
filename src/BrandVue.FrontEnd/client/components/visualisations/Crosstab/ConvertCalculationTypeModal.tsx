import React from 'react';
import * as BrandVueApi from "../../../BrandVueApi";
import { Modal, ModalBody } from "reactstrap";
import { Metric } from '../../../metrics/metric';
import IApplicationUser = BrandVueApi.IApplicationUser;
import { IGoogleTagManager } from '../../../googleTagManager';
import Throbber from '../../throbber/Throbber';
import { handleError } from 'client/components/helpers/SurveyVueUtils';
import { PageHandler } from '../../PageHandler';
import { CalculationType } from '../../../BrandVueApi';
import { selectSubsetId } from 'client/state/subsetSlice';
import { useMetricStateContext } from 'client/metrics/MetricStateContext';
import { useAppSelector } from 'client/state/store';

interface IConvertCalculationTypeModal {
    isVisible: boolean;
    setIsModalVisible(isVisible: boolean): void;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    selectedMetric: Metric;
    user: IApplicationUser | null;
}

const ConvertCalculationTypeModal = (props: IConvertCalculationTypeModal) => {
    const [isLoading, setIsLoading] = React.useState<boolean>(false);
    const typeToConvetTo = props.selectedMetric.calcType == BrandVueApi.CalculationType.Average ? BrandVueApi.CalculationType.NetPromoterScore : BrandVueApi.CalculationType.Average
    const subsetId = useAppSelector(selectSubsetId);
    const { metricsDispatch } = useMetricStateContext();
    
    const updateCalculationType = (metricName: string, newCalculationType: CalculationType): Promise<void> => {
        return metricsDispatch({
            type: "UPDATE_CALCULATION_TYPE",
            data: { metricName: metricName, calculationType: newCalculationType, subsetId: subsetId }
        });
    }

    const doConversion = async () => {
        setIsLoading(true);
        try {
            props.googleTagManager.addEvent("calculationTypeConverted", props.pageHandler);
            await updateCalculationType(props.selectedMetric.name, typeToConvetTo);
            props.setIsModalVisible(false);
        } catch (error) {
            handleError(error);
        } finally {
            setIsLoading(false);
        }
    }


    if (isLoading) {
        return (
            <div className="throbber-container">
                <Throbber />
            </div>
        );
    }

    return (
        <Modal isOpen={props.isVisible} centered={true} className="convert-calculation-type-modal" keyboard={false} autoFocus={false}>
            <h3>Convert calculation type?</h3>
            <ModalBody>
            <p className="text">Are you sure you want to change the calculation type for question {props.selectedMetric.displayName}?</p>
                    <p className="text warning">
                        This will affect all users of this project and will update any reports that reference this question
                    </p>
                <div className="button-container">
                    <button onClick={() => props.setIsModalVisible(false)} className="secondary-button" autoFocus={true}>Cancel</button>
                    <button onClick={doConversion} className={`primary-button`}>Convert to {typeToConvetTo == BrandVueApi.CalculationType.Average ? "average" : "net promoter score"}</button>
                </div>
            </ModalBody>
        </Modal>
    );
};

export default ConvertCalculationTypeModal;