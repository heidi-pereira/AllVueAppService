import { ModalContent } from "../VariableContentModal";
import { VariableCreationService } from "../Utils/VariableCreationService";
import { getVariableErrorMessage } from "../Utils/VariableValidation";
import { CalculationType, ReportVariableAppendType, VariableDefinition, Factory, MetricModalDataModel } from "../../../../../BrandVueApi";
import { toast } from "react-hot-toast";
import { useContext } from "react";
import { VariableContext } from "../Utils/VariableContext";
import { useSavedReportsContext } from "../../../../../components/visualisations/Reports/SavedReportsContext";
import { useEntityConfigurationStateContext } from "../../../../../entity/EntityConfigurationStateContext";
import { useMetricStateContext } from "../../../../../metrics/MetricStateContext";
import { BaseVariableContext } from "../../BaseVariableContext";
import BaseModalButtons from "./BaseModalButtons";
import { Metric } from "client/metrics/metric";
import { useWriteVueQueryParams } from "../../../../helpers/UrlHelper";
import { useAppDispatch, useAppSelector } from "client/state/store";
import { useLocation, useNavigate } from "react-router-dom";
import { handleError } from "client/components/helpers/SurveyVueUtils";
import { selectCurrentReportOrNull } from "client/state/reportSelectors";

interface IVariableActionButtonsProps {
    content: ModalContent;
    goBackHandler: () => void;
    closeHandler: () => void;
    setIsLoading: (isLoading: boolean) => void;
    canGoBack?: boolean;
    isBase?: boolean;
    variableName: string;
    description?: string;
    variableDefinition: VariableDefinition;
    variableId?: number;
    selectedPart?: string;
    appendType?: ReportVariableAppendType;
    updateLocalMetricBase(variableId: number): void;
    returnCreatedMetricName(metricName: string): void;
    calculationType: CalculationType;
    shouldSetQueryParamOnCreateOverride?: boolean;
    metric?: Metric;
    flattenMultiEntity: boolean;
    isFullyLoaded: boolean;
}

const VariableActionButtons = (props: IVariableActionButtonsProps) => {
    const { googleTagManager, pageHandler, shouldSetQueryParamOnCreate } = useContext(VariableContext)
    const { entityConfigurationDispatch } = useEntityConfigurationStateContext();
    const { metricsDispatch } = useMetricStateContext();
    const { reportsDispatch } = useSavedReportsContext();
    const { baseVariableDispatch } = useContext(BaseVariableContext);
    const dispatch = useAppDispatch();
    const currentReportPage = useAppSelector(selectCurrentReportOrNull);
    
    const createOrUpdate = props.variableId ? "Update" : "Create"
    const cancelOrBack = props.canGoBack ? "Back" : "Cancel"
    const secondaryButtonAction = props.canGoBack ? props.goBackHandler : props.closeHandler
    const baseOrVariable = props.isBase ? "base" : (props.flattenMultiEntity ? "variables" : "variable");
    const configureMetricClient = Factory.ConfigureMetricClient(error => error());
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());

    const createOrUpdateHandler = async () => {
        const variableCreationService = new VariableCreationService(
            googleTagManager,
            pageHandler,
            metricsDispatch,
            dispatch,
            baseVariableDispatch,
            entityConfigurationDispatch,
            reportsDispatch,
            currentReportPage);
        const errorMsg = getVariableErrorMessage(props.variableName, props.flattenMultiEntity, props.variableDefinition);
        if (errorMsg) {
            toast.error(errorMsg);
            return;
        }
        props.setIsLoading(true);
        try {
            const promises: Promise<void>[] = [];
            if(props.flattenMultiEntity) {
                promises.push(variableCreationService.createFlattenedMultiEntity(props.variableName,
                    props.variableDefinition,
                    props.calculationType)
                ); 
            }
            else if (props.variableId) {
                const model = new MetricModalDataModel({
                    metricName: props.metric?.name ?? "",
                    displayName: props.variableName,
                    displayText: props.description ?? "",
                    entityInstanceIdMeanCalculationValueMapping: JSON.stringify(props.metric?.entityInstanceIdMeanCalculationValueMapping)
                })
                if (props.isBase) {
                    promises.push(variableCreationService.updateBase(props.variableId, props.variableName, props.variableDefinition));
                    if (props.metric) {
                        promises.push(configureMetricClient.updateMetricModalData(model));
                    }
                } else {
                    promises.push(variableCreationService.updateVariable(props.variableId, props.variableName, props.variableDefinition, props.calculationType));
                    if (props.metric) {
                        promises.push(configureMetricClient.updateMetricModalData(model));
                    }
                }
            } else {
                if (props.isBase) {
                    const createBasePromise = variableCreationService.createBase(
                        props.variableName,
                        props.variableDefinition,
                        props.selectedPart)
                        .then((variableId) => {
                            props.updateLocalMetricBase(variableId);
                        });
                    promises.push(createBasePromise);
                } else {
                    const createVariablePromise = variableCreationService.createVariable(props.variableName,
                        props.variableDefinition,
                        props.calculationType,
                        setQueryParameter,
                        props.shouldSetQueryParamOnCreateOverride ?? shouldSetQueryParamOnCreate,
                        props.selectedPart,
                        props.appendType)
                        .then((createVariableResult) => {
                            props.returnCreatedMetricName(createVariableResult.urlSafeMetricName);
                            const model = new MetricModalDataModel({
                                metricName: createVariableResult.metric.name,
                                displayName: props.variableName,
                                displayText: props.description ?? "",
                                entityInstanceIdMeanCalculationValueMapping: JSON.stringify(props.metric?.entityInstanceIdMeanCalculationValueMapping)
                            })
                            configureMetricClient.updateMetricModalData(model);
                        });

                    promises.push(createVariablePromise);
                }
            }

            await Promise.all(promises);
            props.closeHandler();
        } catch (error) {
            handleError(error);
        } finally {
            props.setIsLoading(false);
        }
    }

    return (
        <BaseModalButtons primaryButtonName={createOrUpdate + " " + baseOrVariable}
            primaryButtonAction={createOrUpdateHandler}
            secondaryButtonName={cancelOrBack}
            secondaryButtonAction={secondaryButtonAction}
            isShown={props.content != ModalContent.SysAdminOptions}
            primaryDisabledReason={!props.isFullyLoaded && props.variableId ? "Checking references..." : undefined} />
    );
}

export default VariableActionButtons