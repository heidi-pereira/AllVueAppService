import React from "react";
import { Metric } from "../../../../../metrics/metric";
import { VariableComponentSelector } from "./VariableComponentSelector";
import {
    DateRangeVariableComponent,
    InclusiveRangeVariableComponent,
    InstanceListVariableComponent,
    MainQuestionType, SurveyIdVariableComponent, VariableComponent, VariableGrouping,
} from "../../../../../BrandVueApi";
import * as BrandVueApi from "../../../../../BrandVueApi";
import {
    getEmptyVariableComponentForMetric, duplicateComponent, getSelectedMetric
} from "../Utils/VariableComponentHelpers";
import { CompositeVariableComponent } from "./VariableComponentComponents/CompositeVariableComponent";
import { VariableGroupWithSample } from "../Content/GroupedVariableModalContent";
import { useEntityConfigurationStateContext } from "../../../../../entity/EntityConfigurationStateContext";
import EstimatedResultBar from "./EstimatedResultBar";

interface IVariableConditionSelectorProps {
    isBase?: boolean;
    activeGroupId: number | undefined;
    metrics: Metric[];
    allMetrics: Metric[];
    nonMapFileSurveys: BrandVueApi.SurveyRecord[];
    questionTypeLookup: { [key: string]: MainQuestionType };
    updateGroup: (group: VariableGrouping) => void;
    hasWarning: boolean;
    variables: BrandVueApi.VariableConfigurationModel[];
    subsetId: string;
    groups: VariableGroupWithSample[];
}

const VariableConditionSelector = (props: IVariableConditionSelectorProps) => {
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const activeGroup = props.groups.find(g => g.group.toEntityInstanceId === props.activeGroupId);

    const updateComponent = (selectedComponent: VariableComponent | undefined) => {
        if (activeGroup) {
            const group = new VariableGrouping({ ...activeGroup.group })
            group.component = duplicateComponent(selectedComponent)!
            props.updateGroup(group)
        }
    }

    const addConditionToComponent = () => {
        if (!activeGroup?.group.component) {
            throw new Error("Unable to find active group");
        }

        let composite: BrandVueApi.CompositeVariableComponent

        if (activeGroup?.group.component instanceof BrandVueApi.CompositeVariableComponent) {
            composite = new BrandVueApi.CompositeVariableComponent({
                compositeVariableComponents: activeGroup.group.component.compositeVariableComponents,
                compositeVariableSeparator: activeGroup.group.component.compositeVariableSeparator,
            })
        } else {
            composite = new BrandVueApi.CompositeVariableComponent({
                compositeVariableComponents: [activeGroup!.group.component],
                compositeVariableSeparator: BrandVueApi.CompositeVariableSeparator.And,
            })
        }

        //TODO: This is a hack to get around the fact that components can't be undefined in the model
        // @ts-ignore
        composite.compositeVariableComponents.push(undefined);
        updateComponent(composite);
    }

    const getEntityTypes = (component: VariableComponent) => {
        if (!component) {
            return [];
        }

        const metric = getSelectedMetric(props.allMetrics, component, props.variables);
        return metric?.entityCombination ?? [];
    }

    function isCompositeVariableComponent(component: any): component is BrandVueApi.CompositeVariableComponent {
        return component instanceof BrandVueApi.CompositeVariableComponent;
    }

    const getActiveGroupComponentSelectors = () => {
        if (activeGroup) {
            if (activeGroup.group.component instanceof BrandVueApi.CompositeVariableComponent) {
                let preselectedComponents = props.groups
                    .filter(group => isCompositeVariableComponent(group.group.component))
                    .flatMap(group => (group.group.component as BrandVueApi.CompositeVariableComponent).compositeVariableComponents)
                    .filter((comp: any): comp is InstanceListVariableComponent => comp instanceof BrandVueApi.InstanceListVariableComponent);

                return <CompositeVariableComponent
                    component={activeGroup.group.component}
                    validMetrics={props.metrics}
                    allMetrics={props.allMetrics}
                    questionTypeLookup={props.questionTypeLookup}
                    nonMapFileSurveys={props.nonMapFileSurveys}
                    setComponentForGroup={updateComponent}
                    getEmptyVariableComponentForMetric={(c) => getEmptyVariableComponentForMetric(c!, props.questionTypeLookup, props.variables, entityConfiguration, preselectedComponents) as (InstanceListVariableComponent | InclusiveRangeVariableComponent)}
                    hasWarning={props.hasWarning}
                    variables={props.variables}
                    subsetId={props.subsetId}
                />;
            }
            if (!(activeGroup.group.component instanceof BrandVueApi.CompositeVariableComponent)) {
                return <VariableComponentSelector
                    component={activeGroup.group.component}
                    updateComponent={updateComponent}
                    availableEntityTypes={getEntityTypes(activeGroup.group.component)}
                    validMetrics={props.metrics}
                    allMetrics={props.allMetrics}
                    questionTypeLookup={props.questionTypeLookup}
                    nonMapFileSurveys={props.nonMapFileSurveys}
                    getEmptyVariableComponentForMetric={(c) => getEmptyVariableComponentForMetric(c!, props.questionTypeLookup, props.variables, entityConfiguration)}
                    hasWarning={props.hasWarning}
                    variables={props.variables}
                />;
            }
        }
    }

    const shouldShowAddConditionButton =
        activeGroup?.group.component &&
        !(activeGroup.group.component instanceof DateRangeVariableComponent) &&
        !(activeGroup.group.component instanceof SurveyIdVariableComponent);

    return (
        <div className={`nets-container ${(props.isBase) ? "base-container" : ""}`}>
            <div className="header-flex">
                <div className="sample-size-title variable-page-label">Conditions</div>
                <div className={"variable-condition-buttons"}>
                    {shouldShowAddConditionButton &&
                        <button className="hollow-button add-condition-button" onClick={addConditionToComponent}>
                            <i className="material-symbols-outlined">add</i>
                            <div className="add-condition-button-text">Add condition</div>
                        </button>
                    }
                </div>
            </div>
            <div className="nets-scroll-area">
                {getActiveGroupComponentSelectors()}
            </div>
            <EstimatedResultBar sample={activeGroup?.sample} forFieldExpression={false} />
        </div>
    );
}

export default VariableConditionSelector