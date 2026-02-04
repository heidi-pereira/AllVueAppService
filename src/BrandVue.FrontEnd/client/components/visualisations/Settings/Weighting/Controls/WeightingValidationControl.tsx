import React, {useEffect} from 'react';
import * as BrandVueApi from '../../../../../BrandVueApi';
import {ErrorMessageLevel, Message, WeightingValidationMessage} from '../../../../../BrandVueApi';
import style from "./WeightingValidationControl.module.less"
import ButtonThrobber from '../../../../throbber/ButtonThrobber';
import MaterialSymbol, {MaterialSymbolStyle, MaterialSymbolType} from './MaterialSymbol';
import PopoverTooltip, {PopoverType} from '../PopoverTooltip';
import {WeightingPlanValidation} from '../WeightingPlanValidation';

interface IWeightingValidationControl {
    isSubsetValid: boolean;
    subsetId: string;
    onErrorMessage: (message: string) => void;
    planValidation: WeightingPlanValidation;
}

const WeightingValidationControl = (props: IWeightingValidationControl) => {
    const [planValidation, setPlanValidation] = React.useState<WeightingPlanValidation>(new WeightingPlanValidation());
    const [isLoading, setIsLoading] = React.useState(true);
    const [popoverOpen, setPopoverOpen] = React.useState(false);

    const convertValidationMessageToMessage = (validationMessage: WeightingValidationMessage) => {
        return new Message({
            errorLevel: validationMessage.errorLevel,
            path: validationMessage.path,
            messageText: planValidation.getErrorMessageTextFromValidationMessage(validationMessage, validationMessage.instanceIds),
        });
    }

    useEffect(() => {
        if (props.planValidation) {
            setPlanValidation(props.planValidation);
            setIsLoading(props.planValidation.isLoading);
        }
    }, [props.planValidation.isLoading, props.planValidation.messages]);

    const validationSummary = (errors: Message[], warnings: Message[]) => {
        const allMessages = errors.concat(warnings);

        return (
            <div>
                {allMessages[0].messageText}
            </div>
        );
    }

    if (isLoading) {
        return <ButtonThrobber />
    }

    const errors: Message[] = [];
    const warnings: Message[] = [];

    if (!planValidation) {
        errors.push(new Message({ errorLevel: ErrorMessageLevel.Error, messageText: "No validation", path: "" }));
    } else {
        if (!props.isSubsetValid) {
            const newMessage = new Message();
            newMessage.messageText = `Survey segment ${props.subsetId} has been disabled or deleted`;
            errors.push(newMessage);
        }

        errors.push(...planValidation.messages
            .filter(x => x.errorLevel == BrandVueApi.ErrorMessageLevel.Error)
            .map(convertValidationMessageToMessage)
        );

        warnings.push(...planValidation.messages
            .filter(x => x.errorLevel == BrandVueApi.ErrorMessageLevel.Warning)
            .map(convertValidationMessageToMessage)
        );

        const groupedWaveMessages = planValidation.getGroupedWaveErrorMessages();
        warnings.push(...groupedWaveMessages.filter(message => message.errorLevel == ErrorMessageLevel.Warning));
        errors.push(...groupedWaveMessages.filter(message => message.errorLevel == ErrorMessageLevel.Error));
    }

    if (errors.length > 0 || warnings.length > 0) {
        const colorClass = errors.length > 0 ? style.errorColor : style.warningColor
        const warningOrErrorSymbol = errors.length > 0 ? MaterialSymbolType.error : MaterialSymbolType.warning;
        const validationPopoverId = `pop-validation-${props.subsetId.replaceAll(" ", "-") }`;
        return (
            <>
                <div
                    id={validationPopoverId}
                    className={`${colorClass} ${style.symbolContainer}`}
                    onMouseEnter={() => setPopoverOpen(true)}
                    onMouseLeave={() => setPopoverOpen(false)}
                >
                    <MaterialSymbol symbolType={warningOrErrorSymbol} symbolStyle={MaterialSymbolStyle.outlined} noFill={errors.length > 0} />
                </div>
                <PopoverTooltip
                    type={errors.length > 0 ? PopoverType.Error : PopoverType.Warning}
                    popoverContent={validationSummary(errors, warnings)}
                    id={validationPopoverId}
                    isOpen={popoverOpen}
                    includeHeader={true}
                    limitWidth={true}
                />
            </>
        );
    }
    return <MaterialSymbol symbolType={MaterialSymbolType.done} symbolStyle={MaterialSymbolStyle.outlined} className={style.goodColor} />
};

export default WeightingValidationControl;