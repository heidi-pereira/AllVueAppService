import React from "react";
import { useNavigate } from "react-router-dom";
import { Metric } from "client/metrics/metric";
import toast from "react-hot-toast";
import { handleError } from 'client/components/helpers/SurveyVueUtils';
import * as BrandVueApi from "../../../BrandVueApi";
import * as actionTypes from '../../../metrics/metricsActionTypeConstants';
import { useMetricStateContext } from '../../../metrics/MetricStateContext';
import { MixPanel } from "../../mixpanel/MixPanel";
import MeanValueAssignerContent from "./VariableModal/Content/MeanValueAssignerContent";
import { EntityMeanMap, EntityMeanMapping, MainQuestionType, QuestionVariableDefinition } from "../../../BrandVueApi";
import { useEntityConfigurationStateContext } from "../../../entity/EntityConfigurationStateContext";
import { Modal, ModalBody } from "reactstrap";
import BaseModalHeader from "./VariableModal/Components/BaseModalHeader";
import BaseInput from "./VariableModal/Components/BaseInput";
import BaseModalButtons from "./VariableModal/Components/BaseModalButtons";
import { useAppDispatch } from "../../../state/store";
import { setActiveEntityTypeByIdentifier } from "../../../state/entityConfigurationSlice";

export interface QuestionEditModalProps {
    isOpen: boolean;
    setIsOpen: React.Dispatch<React.SetStateAction<boolean>>;
    metric: Metric;
    subsetId: string;
    canEditDisplayName: boolean;
    variableDefinition?: QuestionVariableDefinition;
    showAdvancedInfo?: boolean;
}

export const QuestionEditModal: React.FC<QuestionEditModalProps> = (props) => {
    const [helpText, setHelpText] = React.useState<string>(props.metric.helpText);
    const [displayName, setDisplayName] = React.useState<string>(props.metric.displayName);
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { questionTypeLookup } = useMetricStateContext();

    const navigate = useNavigate();

    const isEligibleForMeanMapping = props.metric.entityCombination.length == 1 && questionTypeLookup[props.metric.name] == MainQuestionType.SingleChoice;

    const getDefaultMeanMap = () => {
        if(isEligibleForMeanMapping) {
            const entityType = props.metric.entityCombination[0];
            const entitySet = entityConfiguration.getDefaultEntitySetFor(entityType);
            const instances = entitySet.getInstances().getAll().sort((a, b) => (a.id - b.id));
    
            const mapping = instances.map(i => new EntityMeanMapping({
                entityId: i.id,
                entityInstanceName: i.name,
                includeInCalculation: true,
                meanCalculationValue: i.id
            }))
            return new EntityMeanMap({
                entityTypeIdentifier: entityType.identifier,
                mapping: mapping
            })
        }
    }
    const defaultMeanMap = getDefaultMeanMap();

    const getEntityMeanMap = () => {
        if(isEligibleForMeanMapping) {
            const storedMapping = props.metric.entityInstanceIdMeanCalculationValueMapping
            return storedMapping && storedMapping.mapping.length > 0 ? storedMapping : defaultMeanMap;
        }
    }

    const [entityMeanMap, setEntityMeanMap] = React.useState<EntityMeanMap | undefined>(getEntityMeanMap())
    const [entityMeanMapIsDefault, setEntityMeanMapIsDefault] = React.useState<boolean>(JSON.stringify(entityMeanMap) == JSON.stringify(defaultMeanMap));

    React.useEffect(() => {
        setEntityMeanMapIsDefault(JSON.stringify(entityMeanMap) == JSON.stringify(defaultMeanMap))
    }, [entityMeanMap])

    const { metricsDispatch } = useMetricStateContext();

    React.useEffect(() => {
        setHelpText(props.metric.helpText);
        setDisplayName(props.metric.displayName);
        setEntityMeanMap(getEntityMeanMap());
        setEntityMeanMapIsDefault(JSON.stringify(entityMeanMap) == JSON.stringify(defaultMeanMap));
    }, [props.metric.helpText,
        props.metric.displayName,
        props.metric.entityInstanceIdMeanCalculationValueMapping]);

    const closeHandler = () => {
        props.setIsOpen(false);
        setHelpText(props.metric.helpText);
        setDisplayName(props.metric.displayName);
        setEntityMeanMap(getEntityMeanMap());
    };

    const saveHandler = async () => {
        try {
            if (props.metric.helpText !== helpText || props.metric.displayName !== displayName || props.metric.entityInstanceIdMeanCalculationValueMapping != entityMeanMap) {
                if(entityMeanMap && entityMeanMap.mapping.some(m => m.meanCalculationValue == undefined || Number.isNaN(m.meanCalculationValue))) {
                    toast.error("All choices must be assigned a value")
                    throw new Error;
                }
                await metricsDispatch({
                    type: actionTypes.UPDATE_MODAL_DATA,
                    data: { metric: props.metric, newHelpText: helpText, newDisplayName: displayName, newEntityMeanMap: JSON.stringify(entityMeanMap) }
                });
                MixPanel.track("editQuestionHelpText");
            }
            closeHandler();
        } catch (error) {
            handleError(error);
        }
    };

    const restoreDefaults = () => {
        const entityType = props.metric.entityCombination[0];
        const entitySet = entityConfiguration.getDefaultEntitySetFor(entityType);
        const instances = entitySet.getInstances().getAll().sort((a, b) => (a.id - b.id));

        const mapping = instances.map(i => new EntityMeanMapping({
            entityId: i.id,
            entityInstanceName: i.name,
            includeInCalculation: true,
            meanCalculationValue: i.id
        }))

        const restoredMap = new EntityMeanMap({
            entityTypeIdentifier: entityType.identifier,
            mapping: mapping
        })
        setEntityMeanMap(restoredMap);
    }

    const dispatch = useAppDispatch();

    const handleEntityTypeClick = async (entityType: BrandVueApi.IEntityType) => {
        dispatch(setActiveEntityTypeByIdentifier(entityType.identifier));
        navigate("/entity-type-configuration");
    };

    return (
        <Modal isOpen={props.isOpen} centered={true} className={`variable-edit-content-modal`} keyboard={false} autoFocus={false}>
            <ModalBody>
                <BaseModalHeader
                    title={`Edit ${props.metric.displayName}`}
                    canGoBack={false}
                    closeHandler={closeHandler}
                    canShowDelete={false}
                    goBackHandler={() => { }}
                    setIsDeleteModalOpen={() => { }}
                    isBase={false}
                />
                <div className="name-and-calc-type-container">
                    <div className="variable-name-input-container">
                        <div className="input-label">
                            <label htmlFor="question-name" className="base-input-label">Question name</label>
                        </div>
                        <input type="text"
                            id="question-name"
                            name="question-name"
                            className="base-input"
                            value={displayName}
                            onChange={(e) => setDisplayName(e.target.value)}
                            autoFocus={true}
                            autoComplete="off"
                            readOnly={!props.canEditDisplayName} />
                    </div>
                </div>
                {
                    props.showAdvancedInfo &&
                    <div className="name-and-calc-type-container">
                        <div className="readonly-container">
                            <div className="readonly-label">
                                <label className="base-input-label">Original survey question name</label>
                            </div>
                            <div className="readonly-text">
                                {props.variableDefinition?.questionVarCode}
                            </div>
                        </div>
                    </div>
                }
                <div className="name-and-calc-type-container">
                    <BaseInput
                        label={"Question text"}
                        inputClassName={"variable-name-input-container"}
                        id={"question-text"}
                        value={helpText}
                        setName={setHelpText}
                        isDisabled={false}
                        name={""}
                    />
                </div>
                {
                    entityMeanMap &&
                    <MeanValueAssignerContent entityMeanMap={entityMeanMap}
                        updateEntityMeanMap={(map) => setEntityMeanMap(map)}
                        entityMeanMapIsDefault={entityMeanMapIsDefault}
                        restoreDefaults={restoreDefaults}
                    />
                }
                {
                    props.showAdvancedInfo &&
                    <div className="readonly-container">
                        <div className="readonly-label">
                            <label className="base-input-label">Example usage in variable expression</label>
                        </div>
                        <code>
                            max(response.{props.metric.primaryVariableIdentifier}(
                                {
                                    props.metric.primaryFieldEntityCombination.map((entityType, index) => (
                                        <React.Fragment key={entityType.identifier}>
                                            {index > 0 && ", "}
                                            <span
                                                onClick={() => handleEntityTypeClick(entityType)}
                                                style={{ cursor: 'pointer', textDecoration: 'underline' }}
                                            >
                                                {entityType.identifier}
                                            </span>=result.{entityType.identifier}
                                        </React.Fragment>
                                    ))
                                }
                            ))
                        </code>
                    </div>
                }
                <BaseModalButtons primaryButtonName={"Save"}
                    primaryButtonAction={saveHandler}
                    secondaryButtonAction={closeHandler}
                    secondaryButtonName={"Cancel"}
                    isShown={true} />
            </ModalBody>
        </Modal>
    );
};
