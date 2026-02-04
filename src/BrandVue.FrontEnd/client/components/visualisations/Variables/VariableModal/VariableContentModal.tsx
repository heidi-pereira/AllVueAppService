import React from "react";
import { Modal } from "reactstrap";
import { ModalBody } from "react-bootstrap";
import {
    BaseFieldExpressionVariableDefinition, BaseGroupedVariableDefinition,
    FieldExpressionVariableDefinition, GroupedVariableDefinition,
    VariableDefinition, ReportVariableAppendType, VariableWarningModel, CalculationType, SingleGroupVariableDefinition,
    PermissionFeaturesOptions
} from "../../../../BrandVueApi";
import * as BrandVueApi from "../../../../BrandVueApi";
import { Metric } from "../../../../metrics/metric";
import { useContext, useEffect, useState } from "react";
import { VariableContext } from "./Utils/VariableContext";
import SysAdminVariableModalContent from "./Content/SysAdminVariableModalContent";
import GroupedVariableModalContent from "./Content/GroupedVariableModalContent";
import FieldExpressionVariableModalContent from "./Content/FieldExpressionVariableModalContent";
import VariableNameInput from "./Components/VariableNameInput";
import VariableDescriptionInput from "./Components/VariableDescriptionInput";
import { handleError } from 'client/components/helpers/SurveyVueUtils';
import Throbber from "../../../throbber/Throbber";
import VariableModalButtonLayout from "./VariableModalButtonLayout";
import VariableWarning from "./Components/VariableWarning";
import { VariableDefinitionCreationService } from "./Utils/VariableDefinitionCreationService";
import { useEntityConfigurationStateContext } from "../../../../entity/EntityConfigurationStateContext";
import VariableCalculationTypeDropdown from "./Components/VariableCalculationTypeDropdown";
import { useMetricStateContext } from "../../../../metrics/MetricStateContext";
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import FeatureGuard from "client/components/FeatureGuard/FeatureGuard";

export enum ModalContent {
    SysAdminOptions,
    FieldExpression,
    Grouped
}

interface IVariableContentModalProps {
    isOpen: boolean;
    setIsOpen: (isOpen: boolean) => void;
    subsetId: string;
    isBase?: boolean
    variableIdToView?: number;
    relatedMetric?: Metric;
    metricToCopy?: Metric;
    variableIdToCopy?: number;
    shouldCreateWaveVariable?: boolean;
    selectedPart?: string;
    reportAppendType?: ReportVariableAppendType;
    shouldSetQueryParamOnCreateOverride?: boolean;
    flattenMultiEntity?: boolean;
    updateLocalMetricBase?: (variableId: number) => void;
    returnCreatedMetricName?: (metricName: string) => void;
}

const VariableContentModal = (props: IVariableContentModalProps) => {
    const { user,
        nonMapFileSurveys,
        questionTypeLookup,
        isSurveyGroup } = useContext(VariableContext);
    const { variables, loading: isVariablesLoading } = useAppSelector(selectHydratedVariableConfiguration);
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const variableDefinitionCreationService = new VariableDefinitionCreationService(variables, questionTypeLookup, entityConfiguration)
    const [content, setContent] = useState<ModalContent>(ModalContent.Grouped);
    const [name, setName] = useState<string>("");
    const [description, setDescription] = useState<string>("");
    const [variableDefinition, setVariableDefinition] = useState<VariableDefinition>(variableDefinitionCreationService.createGroupedVariableDefinition(props.isBase))
    const [isMetricLoading, setIsMetricLoading] = React.useState<boolean>(true);
    const [variableTitleName, setVariableTitleName] = React.useState(props.isBase ? "base" : "variable")
    const [errorMessage, setErrorMessage] = React.useState<string>("");
    const [groupThatIsEditing, setGroupThatIsEditing] = useState<number | undefined>(undefined)
    const [variableWarnings, setVariableWarnings] = React.useState<VariableWarningModel[] | undefined>(undefined);
    const [calculationType, setCalculationType] = React.useState<CalculationType>(CalculationType.YesNo);
    const { enabledMetricSet } = useMetricStateContext();
    const isNotBase = !props.isBase || props.isBase === undefined
    const requiresVariableReferenceCheck = props.variableIdToView && !isVariablesLoading && variables.some(v => v.id == props.variableIdToView);

    useEffect(() => {
        if (props.isOpen) {
            setIsMetricLoading(true);
            const metric = props.metricToCopy ? props.metricToCopy : (props.variableIdToView ? enabledMetricSet.metrics.find(m => m.variableConfigurationId === props.variableIdToView) : undefined);
            if (metric) {
                setCalculationType(metric.calcType);
            } else {
                setCalculationType(CalculationType.YesNo);
            }
            setUp();
            setErrorMessage("")
            setIsMetricLoading(false);
            if (requiresVariableReferenceCheck) {
                BrandVueApi.Factory.VariableConfigurationClient(error => error())
                    .isVariableReferencedByAnotherVariable(props.variableIdToView!)
                    .then(result => setVariableWarnings(result));
            } else {
                setVariableWarnings(undefined);
            }
        }
    }, [variables,
        isVariablesLoading,
        user?.isSystemAdministrator,
        props.variableIdToView,
        props.variableIdToCopy,
        props.metricToCopy,
        props.isOpen
    ]);

    const shouldShowGroups = (definition: VariableDefinition): boolean => {
        return definition instanceof BaseGroupedVariableDefinition
            || definition instanceof GroupedVariableDefinition
            || definition instanceof SingleGroupVariableDefinition;
    }

    const updateVariableSetUp = () => {
        const variableToView = variables.find(v => v.id === props.variableIdToView);
        if (variableToView) {
            setVariableTitleName(props.relatedMetric!.displayName);
            setName(props.relatedMetric!.displayName);
            setDescription(props.relatedMetric!.helpText);
            setVariableDefinition(variableToView.definition);
            setGroupThatIsEditing(undefined);
            if (shouldShowGroups(variableToView.definition)) {
                setContent(ModalContent.Grouped);
            }
            if (variableToView.definition instanceof BaseFieldExpressionVariableDefinition || variableToView.definition instanceof FieldExpressionVariableDefinition) {
                setContent(ModalContent.FieldExpression);
            }
        }
    };

    const updateBaseVariableSetUp = () => {
        const variableToView = variables.find(v => v.id === props.variableIdToView);
        if (variableToView) {
            setVariableTitleName(variableToView.displayName);
            setName(variableToView.displayName);
            setVariableDefinition(variableToView.definition);
            setGroupThatIsEditing(undefined);
            if (shouldShowGroups(variableToView.definition)) {
                setContent(ModalContent.Grouped);
            }
            if (variableToView.definition instanceof BaseFieldExpressionVariableDefinition || variableToView.definition instanceof FieldExpressionVariableDefinition) {
                setContent(ModalContent.FieldExpression);
            }
        }
    };

    const copyVariableSetUp = () => {
        const variable = variableDefinitionCreationService.getExistingVariableConfiguration(props.variableIdToCopy!);
        setVariableTitleName(`${props.relatedMetric!.displayName}${props.flattenMultiEntity ? "" : " - Copy"}`);
        setName(`${props.relatedMetric!.displayName}${props.flattenMultiEntity ? "" : " - Copy"}`);
        setDescription(props.relatedMetric!.helpText);
        setVariableDefinition(variable.definition);
        setGroupThatIsEditing(undefined);
        if (shouldShowGroups(variable.definition)) {
            setContent(ModalContent.Grouped);
        }
        if (variable.definition instanceof BaseFieldExpressionVariableDefinition || variable.definition instanceof FieldExpressionVariableDefinition) {
            setContent(ModalContent.FieldExpression);
        }
    };

    const copyBaseVariableSetUp = () => {
        const variable = variableDefinitionCreationService.getExistingVariableConfiguration(props.variableIdToCopy!);
        setVariableTitleName(`${variable.displayName} - Copy`);
        setName(`${variable.displayName} - Copy`);
        setVariableDefinition(variable.definition);
        setGroupThatIsEditing(undefined);
        if (shouldShowGroups(variable.definition)) {
            setContent(ModalContent.Grouped);
        }
        if (variable.definition instanceof BaseFieldExpressionVariableDefinition || variable.definition instanceof FieldExpressionVariableDefinition) {
            setContent(ModalContent.FieldExpression);
        }
    };

    const copyQuestionSetUp = () => {
        setVariableTitleName(`${props.metricToCopy!.displayName}${props.flattenMultiEntity ? "" : " - Copy"}`);
        setName(`${props.metricToCopy!.displayName}${props.flattenMultiEntity ? "" : " - Copy"}`);
        setDescription(props.metricToCopy!.helpText);
        setVariableDefinition(variableDefinitionCreationService.getVariableDefinitionFromMetric(props.metricToCopy!));
        setGroupThatIsEditing(undefined);
        setContent(ModalContent.Grouped);
    };

    const createWaveSetup = () => {
        setVariableTitleName("waves variable");
        setName("Waves");
        setVariableDefinition(variableDefinitionCreationService.createWaveDefinition(props.isBase));
        setGroupThatIsEditing(1);
        setContent(ModalContent.Grouped);
    };

    const createVariableSysAdminSetUp = () => {
        setVariableTitleName(props.isBase ? "base" : "variable");
        setName("");
        setDescription("");
        setVariableDefinition(variableDefinitionCreationService.createGroupedVariableDefinition(props.isBase));
        setGroupThatIsEditing(undefined);
        setContent(ModalContent.SysAdminOptions);
    };

    const createVariableSetUp = () => {
        setVariableTitleName("variable");
        setName("");
        setDescription("");
        setVariableDefinition(variableDefinitionCreationService.createGroupedVariableDefinition(props.isBase));
        setGroupThatIsEditing(undefined);
        setContent(ModalContent.Grouped);
    };

    const createBaseVariableSetUp = () => {
        setVariableTitleName("base");
        setName("");
        setVariableDefinition(variableDefinitionCreationService.createGroupedVariableDefinition(props.isBase));
        setGroupThatIsEditing(undefined);
        setContent(ModalContent.Grouped);
    };

    const setUp = () => {
        const variableToCopy = variables.find(v => v.id == props.variableIdToCopy);

        if (!isVariablesLoading) {
            const isVariableIdToViewDefined = props.variableIdToView !== undefined
                && props.relatedMetric !== undefined
                && isNotBase;
            const isBaseVariableIdToViewDefined = props.variableIdToView !== undefined && props.isBase;
            const isVariableIdToCopyDefined = props.variableIdToCopy !== undefined
                && !(variableToCopy?.definition instanceof BrandVueApi.QuestionVariableDefinition)
                && props.relatedMetric !== undefined
                && isNotBase;
            const isBaseVariableIdToCopyDefined = props.variableIdToCopy !== undefined
                && !(variableToCopy?.definition instanceof BrandVueApi.QuestionVariableDefinition)
                && props.isBase;
            const isMetricToCopyDefined = props.metricToCopy !== undefined;
            const shouldCreateWaveVariable = props.shouldCreateWaveVariable;
            const isUserSystemAdministrator = user?.isSystemAdministrator;
            const isBase = props.isBase;

            if (isVariableIdToViewDefined) {
                updateVariableSetUp();
            } else if (isBaseVariableIdToViewDefined) {
                updateBaseVariableSetUp();
            } else if (isVariableIdToCopyDefined) {
                copyVariableSetUp();
            } else if (isBaseVariableIdToCopyDefined) {
                copyBaseVariableSetUp();
            } else if (isMetricToCopyDefined) {
                copyQuestionSetUp();
            } else if (shouldCreateWaveVariable) {
                createWaveSetup();
            } else if (isUserSystemAdministrator) {
                createVariableSysAdminSetUp();
            } else if (isBase) {
                createBaseVariableSetUp();
            } else {
                createVariableSetUp();
            }
        }
    }

    const settingsContent = () => {
        let updatedMetrics = props.shouldCreateWaveVariable ? [] : enabledMetricSet.metrics

        switch (content) {
            case ModalContent.SysAdminOptions:
                return <SysAdminVariableModalContent
                    isBase={props.isBase}
                    setContent={setContent}
                    variableDefinitionCreationService={variableDefinitionCreationService}
                    setVariableDefinition={setVariableDefinition}
                />
            case ModalContent.Grouped:
                if (!(variableDefinition instanceof BaseGroupedVariableDefinition) && !(variableDefinition instanceof GroupedVariableDefinition)
                    && !(variableDefinition instanceof SingleGroupVariableDefinition)) {
                    return null;
                }
                return <GroupedVariableModalContent
                    variableName={name}
                    isBase={props.isBase}
                    nonMapFileSurveys={nonMapFileSurveys}
                    questionTypeLookup={questionTypeLookup}
                    variableDefinition={variableDefinition}
                    setVariableDefinition={setVariableDefinition}
                    metrics={updatedMetrics}
                    isSurveyGroup={isSurveyGroup}
                    variables={variables}
                    variableIdToView={props.variableIdToView}
                    hasWarning={props.variableIdToView !== undefined}
                    groupThatIsEditing={groupThatIsEditing}
                    flattenMultiEntity={props.flattenMultiEntity ?? false}
                    setGroupThatIsEditing={setGroupThatIsEditing}
                    subsetId={props.subsetId}
                />
            case ModalContent.FieldExpression:
                if (!(variableDefinition instanceof BaseFieldExpressionVariableDefinition) && !(variableDefinition instanceof FieldExpressionVariableDefinition))
                    return null
                return <FieldExpressionVariableModalContent
                    isDisabled={!user?.isSystemAdministrator}
                    isBase={props.isBase}
                    errorMessage={errorMessage}
                    handleError={handleError}
                    clearError={() => setErrorMessage("")}
                    subsetId={props.subsetId}
                    variableDefinition={variableDefinition}
                    setVariableDefinition={setVariableDefinition}
                />
        }
    }

    const createThrobber = () => {
        return (
            <div className={"throbber-container"}>
                <Throbber />
            </div>
        )
    };

    const modalBody = () => {
        return (
            <>
            {
                !props.flattenMultiEntity && 
                <div className={"name-and-calc-type-container"}>
                    <VariableNameInput
                        name={name}
                        setName={setName}
                        isBase={props.isBase} />
                    {isNotBase && <VariableCalculationTypeDropdown selectedCalculationType={calculationType} setSelectedCalculationType={setCalculationType} />}
                </div>
            }
                {(!props.flattenMultiEntity && (isNotBase || props.relatedMetric)) &&
                    <div className="description-container">
                        <VariableDescriptionInput
                            description={description}
                            setDescription={setDescription} />
                    </div>
                }
                {
                    settingsContent()
                }
                {
                    isVariablesLoading
                        ? createThrobber()
                        : <VariableWarning content={content}
                            variableWarnings={variableWarnings}
                            variableIdToView={props.variableIdToView}
                        />
                }
            </>
        )
    }

    const isEditMode = props.variableIdToView !== undefined;
    const permission = isEditMode ? PermissionFeaturesOptions.VariablesEdit : PermissionFeaturesOptions.VariablesCreate;

    return (
        <FeatureGuard permissions={[permission]} >
            <Modal isOpen={props.isOpen} toggle={() => { props.setIsOpen(!props.isOpen) }} centered={true} className={`variable-content-modal`} keyboard={false} autoFocus={false}>
                <ModalBody>
                    <VariableModalButtonLayout
                        title={variableTitleName}
                        content={content}
                        setContent={setContent}
                        setIsLoading={setIsMetricLoading}
                        setIsOpen={props.setIsOpen}
                        variableName={name}
                        description={description}
                        variableDefinition={variableDefinition!}
                        variableToView={props.variableIdToView}
                        isBase={props.isBase}
                        isWaveCreation={props.shouldCreateWaveVariable}
                        isCopiedFromExisting={props.metricToCopy !== undefined || props.variableIdToCopy !== undefined}
                        selectedPart={props.selectedPart}
                        appendType={props.reportAppendType}
                        resetSetUp={setUp}
                        updateLocalMetricBase={(variableId: number) => {
                            if (props.updateLocalMetricBase) {
                                props.updateLocalMetricBase(variableId);
                            }
                        }}
                        returnCreatedMetricName={(metricName: string) => {
                            if (props.returnCreatedMetricName) {
                                props.returnCreatedMetricName(metricName);
                            }
                        }}
                        calculationType={calculationType}
                        shouldSetQueryParamOnCreateOverride={props.shouldSetQueryParamOnCreateOverride}
                        isDeleteButtonHidden={(variableWarnings?.length ?? 0) > 0}
                        metric={props.metricToCopy ?? props.relatedMetric}
                        isFullyLoaded={!isMetricLoading && !isVariablesLoading && (!requiresVariableReferenceCheck || variableWarnings !== undefined)}
                        flattenMultiEntity={props.flattenMultiEntity ?? false}
                    >
                        {
                            isMetricLoading
                                ? createThrobber()
                                : modalBody()
                        }
                    </VariableModalButtonLayout>
                </ModalBody>
            </Modal>
        </FeatureGuard>
    );
}

export default VariableContentModal
