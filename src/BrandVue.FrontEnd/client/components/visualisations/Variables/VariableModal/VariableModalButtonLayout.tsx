import {ReactNode, useContext} from "react";
import VariableModalHeader from "./Components/VariableModalHeader";
import VariableActionButtons from "./Components/VariableActionButtons";
import {ModalContent} from "./VariableContentModal";
import {CalculationType, ReportVariableAppendType, VariableDefinition} from "../../../../BrandVueApi";
import {VariableContext} from "./Utils/VariableContext";
import { Metric } from 'client/metrics/metric';

interface IVariableModalButtonLayoutProps{
    children?: ReactNode
    title: string
    content: ModalContent
    setContent: (content: ModalContent) => void;
    variableToView?: number
    setIsLoading: (isLoading: boolean) => void
    setIsOpen: (isOpen: boolean) => void
    variableName: string;
    variableDefinition: VariableDefinition;
    isBase?: boolean;
    resetSetUp: () => void;
    isCopiedFromExisting?: boolean;
    isWaveCreation?: boolean;
    selectedPart?: string;
    appendType?: ReportVariableAppendType;
    updateLocalMetricBase(variableId: number): void;
    returnCreatedMetricName(metricName: string): void;
    calculationType: CalculationType;
    shouldSetQueryParamOnCreateOverride?: boolean;
    isDeleteButtonHidden?: boolean;
    metric?: Metric | undefined;
    description?: string;
    isFullyLoaded: boolean;
    flattenMultiEntity: boolean;
}

const VariableModalButtonLayout = (props: IVariableModalButtonLayoutProps) => {
    const { user } = useContext(VariableContext)
    const canGoBack = user?.isSystemAdministrator && !props.variableToView && !props.isCopiedFromExisting && props.content != ModalContent.SysAdminOptions && !props.isWaveCreation

    const goBackHandler = () => {
        props.setContent(ModalContent.SysAdminOptions)
    }

    const closeHandler = () => {
        props.resetSetUp()
        props.setIsOpen(false)
    }

    return (
        <>
            <VariableModalHeader
                title={props.title}
                content={props.content}
                flattenMultiEntity={props.flattenMultiEntity}
                goBackHandler={goBackHandler}
                closeHandler={closeHandler}
                variableId={props.variableToView}
                canGoBack={canGoBack}
                isBase={props.isBase}
                isDeleteButtonHidden={props.isDeleteButtonHidden}
            />
            {props.children}
            <VariableActionButtons
                content={props.content}
                goBackHandler={goBackHandler}
                closeHandler={closeHandler}
                setIsLoading={props.setIsLoading}
                variableId={props.variableToView}
                variableName={props.variableName}
                variableDefinition={props.variableDefinition}
                canGoBack={canGoBack}
                isBase={props.isBase}
                selectedPart={props.selectedPart}
                appendType={props.appendType}
                updateLocalMetricBase={(variableId: number) => props.updateLocalMetricBase(variableId)}
                returnCreatedMetricName={(metricName: string) => props.returnCreatedMetricName(metricName)}
                calculationType={props.calculationType}
                shouldSetQueryParamOnCreateOverride={props.shouldSetQueryParamOnCreateOverride}
                metric={props.metric}
                description={props.description}
                isFullyLoaded={props.isFullyLoaded}
                flattenMultiEntity={props.flattenMultiEntity}
            />
        </>
    );
}

export default VariableModalButtonLayout