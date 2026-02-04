import React from "react";
import * as BrandVueApi from "../../../../BrandVueApi";
import BaseTypeDropdownMenu from "./BaseTypeDropdownMenu";
import {BaseDefinitionType, BaseGroupedVariableDefinition, VariableComponent} from "../../../../BrandVueApi";
import { Metric } from "../../../../metrics/metric";
import { BaseVariableContext } from "../../Variables/BaseVariableContext";
import { useContext } from "react";
import { ProductConfiguration } from "../../../../ProductConfiguration";

interface IBaseOptionsSelector {
    className?: string | undefined;
    metric: Metric | undefined;
    baseType: BaseDefinitionType | undefined;
    baseVariableId: number | undefined;
    selectDefaultBase(): void;
    setBaseProperties(baseType: BaseDefinitionType | undefined, baseVariableId: number | undefined): void;
    defaultBaseType?: BaseDefinitionType;
    defaultBaseVariableId?: number;
    canCreateNewBase: boolean | undefined;
    selectedPart?: string;
    updateLocalMetricBase(variableId: number): void;
    productConfiguration?: ProductConfiguration;
    showPadlock?: boolean;
}

const BaseOptionsSelector = (props: IBaseOptionsSelector) => {
    const { baseVariables } = useContext(BaseVariableContext);
    const defaultBaseVariableId = props.metric?.baseVariableConfigurationId ?? props.defaultBaseVariableId;

    const getBaseVariableIntersectingResultTypes = (): BrandVueApi.IEntityType[] => {
        const variableId = props.baseVariableId ?? defaultBaseVariableId;
        const selectedBaseVariable = baseVariables.find(v => v.id === variableId);
        if (props.metric && selectedBaseVariable && selectedBaseVariable.definition instanceof BaseGroupedVariableDefinition) {
            const baseVariableResultTypes = getResultTypes(selectedBaseVariable.definition.groups[0].component);
            const metricEntityTypes = props.metric.primaryFieldEntityCombination;
            const sharedResultTypes = metricEntityTypes.filter(type => baseVariableResultTypes.includes(type.identifier));
            return sharedResultTypes;
        }
        return [];
    }

    const updateForEachChosenType = (checked: boolean) => {
        const baseType = checked ? BaseDefinitionType.SawThisChoice : BaseDefinitionType.SawThisQuestion;
        props.setBaseProperties(baseType, props.baseVariableId);
    }

    const updateFromBaseTypeDropdown = (baseType: BaseDefinitionType | undefined, baseVariableId: number | undefined) => {
        props.setBaseProperties(baseType, baseVariableId);
    }

    const sharedBaseVariableResultTypes = getBaseVariableIntersectingResultTypes();
    const entityTypeNamesJoined = sharedBaseVariableResultTypes.map(t => t.displayNameSingular).join(", ").toLowerCase();
    return (
        <div className={props.className}>
            <div className="base-label">Base</div>
            <div className="base-option">
                <BaseTypeDropdownMenu
                    metric={props.metric}
                    baseType={props.baseType}
                    baseVariableId={props.baseVariableId}
                    selectDefaultBase={props.selectDefaultBase}
                    setBaseProperties={updateFromBaseTypeDropdown}
                    defaultBaseType={props.defaultBaseType}
                    defaultBaseVariableId={defaultBaseVariableId}
                    canCreateNewBase={props.canCreateNewBase}
                    selectedPart={props.selectedPart}
                    updateLocalMetricBase={(variableId: number) => props.updateLocalMetricBase(variableId)}
                    productConfiguration={props.productConfiguration}
                    showPadlock={props.showPadlock}
                />
            </div>
            {sharedBaseVariableResultTypes.length > 0 &&
                <div className="base-option">
                    <input
                        type="checkbox"
                        className="checkbox"
                        id="for-each-chosen-type-checkbox"
                        checked={props.baseType === BaseDefinitionType.SawThisChoice}
                        onChange={e => updateForEachChosenType(e.target.checked)}
                        disabled={!props.canCreateNewBase || !props.baseVariableId}/>
                    <label htmlFor="for-each-chosen-type-checkbox">
                        For each chosen {entityTypeNamesJoined}
                    </label>
                    <div className="base-hint">Base for each {entityTypeNamesJoined} will be respondents who answered with that {entityTypeNamesJoined}</div>
                </div>
            }
        </div>
    );
}

function getResultTypes(variableComponent: VariableComponent): string[] {
    if (variableComponent instanceof BrandVueApi.InstanceListVariableComponent || variableComponent instanceof BrandVueApi.InclusiveRangeVariableComponent) {
        return variableComponent.resultEntityTypeNames;
    } else if (variableComponent instanceof BrandVueApi.CompositeVariableComponent) {
        return variableComponent.compositeVariableComponents.flatMap(c => getResultTypes(c));
    }
    return [];
}

export default BaseOptionsSelector