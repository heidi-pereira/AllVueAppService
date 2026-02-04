import React from 'react';
import style from "./WeightingWaveValidationControl.module.less"
import { ErrorMessageLevel, ErrorMessageType, WeightingValidationMessage } from '../../../../../BrandVueApi';
import { WaveDescription } from '../WeightingWaveListItem';
import ErrorChip from './ErrorChip';
import WeightingValidationMessageListModal from '../Modals/WeightingValidationMessageListModal';
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from './MaterialSymbol';
import { WeightingPlanValidation, WaveError } from '../WeightingPlanValidation';

interface IWeightingWaveValidationControlProps {
    wave: WaveDescription;
    metricName: string;
    planValidation: WeightingPlanValidation;
    weightingPlanId: number;
}

export interface IWaveErrorMessage {
    isWarning: boolean;
    errorMessage: string;
}

class WaveErrorMessage implements IWaveErrorMessage {
    isWarning: boolean;
    errorMessage: string;
    constructor(data?: IWaveErrorMessage) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property)) {
                    this[property] = data[property];
                }
            }

            if (!data) {
                this.isWarning = false;
                this.errorMessage = "";
            }
        }
    }

    static fromValidationMessage(message: WeightingValidationMessage, planValidation: WeightingPlanValidation, wave: WaveDescription): IWaveErrorMessage {
        return new WaveErrorMessage({
            isWarning: message.errorLevel === ErrorMessageLevel.Warning,
            errorMessage: planValidation.getErrorMessageTextFromValidationMessage(message, [wave.EntityId]),
            
        });
    }
}

const WeightingWaveValidationControl = (props: IWeightingWaveValidationControlProps) => {
    const [errors, setErrors] = React.useState<IWaveErrorMessage[]>([]);
    const [multiMessageModalVisible, setMultiMessageModalVisible] = React.useState(false);
    const [multiMessageModalHeaderText, setMultiMessageModalHeaderText] = React.useState<string>("");

    React.useEffect(() => {
        setErrors(getErrors(props.wave, props.metricName, props.planValidation));
    }, [props.wave, props.planValidation]);

    const getWaveError = (errorType: WaveError, metricName: string, entityInstanceId: number): IWaveErrorMessage => {
        var errorMessage = 'Unknown error';

        switch (errorType) {
            case WaveError.UnweightedNoTarget:
                errorMessage = `New unweighted wave. Add weights to fix this.`;
                break;
            case WaveError.Unweighted:
                errorMessage = `Unweighted wave. Add weights to fix this.`;
                break;
            case WaveError.MissingEntity:
                errorMessage = `Entity with ID=${entityInstanceId} is missing in metric '${metricName}'. Add the entity to the metric.`;
                break;
            case WaveError.MissingMetric:
                errorMessage = `Metric with name '${metricName}' is missing. Add the metric.`;
                break;
            case WaveError.MultipleEntitiesCombination:
                errorMessage = 'Waves based on multiple entities are not supported. Remove the wave or change the metric to only have one entity.';
                break;
        }

        const isWarning = errorType == WaveError.UnweightedNoTarget;

        return new WaveErrorMessage({ isWarning: isWarning, errorMessage });
    }

    const getPlanErrors = (planValidation: WeightingPlanValidation): WeightingValidationMessage[] => {
        const numberOfWaves = Object.keys(planValidation.instanceNames).length;

        const validationMessagesForEntity = planValidation.getValidationMessagesForEntityInstanceId(props.wave.EntityId);
        const questionNotValidErrors = validationMessagesForEntity.find(e => e.errorType == ErrorMessageType.QuestionNotValid);
        const allWavesUnweighted = questionNotValidErrors && questionNotValidErrors.instanceIds.length == numberOfWaves

        if(allWavesUnweighted) {
            return [];
        }
        
        return validationMessagesForEntity;
    }

    const getWaveErrors = (planValidation: WeightingPlanValidation, metricName: string, wave: WaveDescription): WaveError[] => {
        const noWeightingErrors = planValidation.waveErrors.filter(w => w.errorType == WaveError.Unweighted); 
        const instances = Object.keys(planValidation.instanceNames).length;
        const allWavesUnweighted = noWeightingErrors.length == instances;

        if (allWavesUnweighted) {
            return [];
        } else {
            return planValidation.getWaveErrorsForEntityInstanceId(wave.EntityId);
        }
    }

    const getErrors = (wave: WaveDescription, metricName: string, planValidation: WeightingPlanValidation): IWaveErrorMessage[] => {
        const planErrors = getPlanErrors(planValidation);
        const waveErrors = getWaveErrors(planValidation, metricName, wave);

        const unweightedWaveError = waveErrors?.find(e => e === WaveError.Unweighted);
        if (unweightedWaveError) {
            return [getWaveError(unweightedWaveError, metricName, wave.EntityId)];
        }

        const planErrorMessages = planErrors.map(message => WaveErrorMessage.fromValidationMessage(message, planValidation, props.wave));
        const waveErrorMessages = waveErrors.map(error => getWaveError(error, metricName, wave.EntityId));

        planErrorMessages.push(...waveErrorMessages);

        const errors = planErrorMessages.filter((error) => !error.isWarning);
        const warnings = planErrorMessages.filter((error) => error.isWarning);
        errors.push(...warnings);

        return errors;
    }

    const getErrorChip = (error: IWaveErrorMessage) => {
        const id = `${props.weightingPlanId}-${props.wave.EntityId}`;
        return (<ErrorChip id={id} isWarning={error.isWarning} reasonText={error.errorMessage} />);
    }

    const displayMessagesModal = (headerText: string) => {
        setMultiMessageModalHeaderText(headerText.replace('&', 'and'));
        setMultiMessageModalVisible(true);
    }

    const getMessage = (messageCount: number, messageType: string) => {
        if (messageCount == 0) {
            return "";
        }
        const singleOrPlural = messageCount == 1 ? messageType : messageType + "s";

        return `${messageCount} ${singleOrPlural}`;
    }

    const multiButton = () => {
        const errorCount = errors.filter(e => !e.isWarning).length;
        const warningCount = errors.filter(e => e.isWarning).length;

        const errorMessage = getMessage(errorCount, "Error");
        const warningMessage = getMessage(warningCount, "Warning");

        const colorClass = errorCount > 0 ? style.errorColor : style.warningColor;
        const validationIcon = errorCount > 0 ? "error" : "warning";
        const possibleAmpersand = errorCount > 0 && warningCount > 0 ? ' & ' : '';
        const totalMessage = errorMessage + possibleAmpersand + warningMessage;
        return (
            <button className={`${style.validationMessageButton} ${colorClass}`} onClick={() => displayMessagesModal(totalMessage)}>
                <MaterialSymbol symbolType={MaterialSymbolType[validationIcon]} symbolStyle={MaterialSymbolStyle.outlined} noFill={errorCount > 0} className={style.symbol} />
                <span className={style.text}>{totalMessage}</span>
            </button>
        )
    }

    const getErrorsAndWarnings = () => {
        if (errors.length === 0) {
            return <></>;
        }

        if (errors.length === 1) {
            return getErrorChip(errors[0]);
        }

        return multiButton();
    }

    return (
        <div>
            {getErrorsAndWarnings()}
            <WeightingValidationMessageListModal
                isVisible={multiMessageModalVisible}
                toggle={() => setMultiMessageModalVisible(!multiMessageModalVisible)}
                header={multiMessageModalHeaderText}
                messages={errors}
            />
        </div>
    )
};

export default WeightingWaveValidationControl;