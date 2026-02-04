import React from 'react';
import * as BrandVueApi from "../../../../../../BrandVueApi";
import { Metric } from '../../../../../../metrics/metric';
import {VariableComponentSelector} from "../VariableComponentSelector";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import {
    InclusiveRangeVariableComponent,
    InstanceListVariableComponent,
    MainQuestionType, IEntityType,
    VariableComponent
} from '../../../../../../BrandVueApi';
import {getSelectedMetric} from "../../Utils/VariableComponentHelpers";

interface ICompositeVariableComponentProps {
    component: BrandVueApi.CompositeVariableComponent;
    validMetrics: Metric[];
    allMetrics: Metric[];
    questionTypeLookup: {[key: string]: MainQuestionType};
    nonMapFileSurveys: BrandVueApi.SurveyRecord[];
    setComponentForGroup(component: VariableComponent | undefined): void;
    getEmptyVariableComponentForMetric(metric: Metric): VariableComponent;
    hasWarning: boolean
    variables: BrandVueApi.VariableConfigurationModel[];
    subsetId: string;
}

export const CompositeVariableComponent = (props: ICompositeVariableComponentProps) => {
    const [separatorDropdownOpen, setSeparatorDropdownOpen] = React.useState<boolean>(false);

    const setComponentForGroup = (index: number, component: InstanceListVariableComponent | InclusiveRangeVariableComponent | undefined) => {
        if (component instanceof InstanceListVariableComponent || component instanceof InclusiveRangeVariableComponent) {
            const compositeComponent = props.component;
            compositeComponent.compositeVariableComponents[index] = component;
            props.setComponentForGroup(compositeComponent);
        } else {
            throw new Error(`Invalid component type for composite variable component`);
        }
    }

    const removeInnerComponent = (index: number) => {
        const compositeComponent = props.component;
        compositeComponent.compositeVariableComponents.splice(index, 1);
        if (compositeComponent.compositeVariableComponents.length == 0) {
            props.setComponentForGroup(undefined);
        } else if (compositeComponent.compositeVariableComponents.length == 1) {
            props.setComponentForGroup(compositeComponent.compositeVariableComponents[0]);
        } else {
            props.setComponentForGroup(compositeComponent);
        }
    }

    const reOrderConditionsByShiftingByOne = () => {
        const compositeComponent = props.component;
        if (compositeComponent.compositeVariableComponents != undefined && compositeComponent.compositeVariableComponents.length >= 2) {
            const first = 0;
            const last = compositeComponent.compositeVariableComponents.length - 1;

            const tempComponent = compositeComponent.compositeVariableComponents[first];
            for (let index = 1; index < compositeComponent.compositeVariableComponents.length; index++) {
                compositeComponent.compositeVariableComponents[index - 1] = compositeComponent.compositeVariableComponents[index];
            }
            compositeComponent.compositeVariableComponents[last] = tempComponent;

            props.setComponentForGroup(compositeComponent);
        }
    }

    const setSeparator = (separator: BrandVueApi.CompositeVariableSeparator) => {
        const compositeComponent = props.component;
        compositeComponent.compositeVariableSeparator = separator;
        props.setComponentForGroup(compositeComponent);
    }

    const getDivider = () => {
        return <div className="composite-component-separator">{separatorToString(props.component.compositeVariableSeparator)}</div>
    }

    const separatorToString = (separator: BrandVueApi.CompositeVariableSeparator): string => {
        switch (separator) {
            case BrandVueApi.CompositeVariableSeparator.And:
                return "AND";
            case BrandVueApi.CompositeVariableSeparator.Or:
                return "OR";
        }
        throw new Error(`Unhandled separator type ${separator}`);
    }

    const separatorToDisplayString = (separator: BrandVueApi.CompositeVariableSeparator): string => {
        switch (separator) {
            case BrandVueApi.CompositeVariableSeparator.And:
                return "All of";
            case BrandVueApi.CompositeVariableSeparator.Or:
                return "Any of";
        }
        throw new Error(`Unhandled separator type ${separator}`);
    }

    //only multi entity instance list components can select alternate entity type
    const multiEntityComponents = props.component.compositeVariableComponents.filter(c => c instanceof  InstanceListVariableComponent && getSelectedMetric(props.validMetrics, c, props.variables).entityCombination.length > 1) as InstanceListVariableComponent[]

    const getAvailableMetricsAndEntityTypes = (component: InstanceListVariableComponent | InclusiveRangeVariableComponent | undefined) => {
        let metrics = props.validMetrics;
        let availableEntityTypes: IEntityType[]  = []

        if (component){
            const selectedMetric = getSelectedMetric(props.validMetrics, component, props.variables)
            availableEntityTypes = selectedMetric?.entityCombination ?? []
        }

        const isMultiEntityMetric = availableEntityTypes && availableEntityTypes.length > 1;

        multiEntityComponents.forEach(c => {
            if (c !== component) {
                const componentMetric = getSelectedMetric(props.validMetrics, c, props.variables)
                const resultTypes = componentMetric.entityCombination.filter(e => e.identifier != c.fromEntityTypeName).map(e => e.identifier)

                metrics = metrics.filter(
                    m => m.entityCombination.length <= 1 ||
                    resultTypes.every(resultType => m.entityCombination.some(e => e.identifier == resultType))
                );

                if (isMultiEntityMetric) {
                    availableEntityTypes = availableEntityTypes.filter(e => !resultTypes.includes(e.identifier));
                }
            }
        });

        return { metrics, availableEntityTypes };
    }

    const getComponentEditors = () => {
        let components:  (VariableComponent | undefined)[] = props.component.compositeVariableComponents;
        if (components.length === 1) {
            components = [...components, undefined ];
        }

        const lastIndex = components.length - 1;
        return components.map((component, index) => {
            const {metrics, availableEntityTypes} = getAvailableMetricsAndEntityTypes(component as (InstanceListVariableComponent | InclusiveRangeVariableComponent));
            return (
                <div key={index} className="variable-component-selector">
                    <VariableComponentSelector
                        component={component}
                        updateComponent={(c) => setComponentForGroup(index, c as (InstanceListVariableComponent | InclusiveRangeVariableComponent | undefined))}
                        availableEntityTypes={availableEntityTypes}
                        validMetrics={metrics}
                        allMetrics={props.allMetrics}
                        questionTypeLookup={props.questionTypeLookup}
                        nonMapFileSurveys={props.nonMapFileSurveys}
                        isInsideCompositeComponent={true}
                        removeComponent={() => removeInnerComponent(index)}
                        getEmptyVariableComponentForMetric={(c) => props.getEmptyVariableComponentForMetric(c!)}
                        hasWarning={props.hasWarning}
                        variables={props.variables}
                    />
                    {index != lastIndex && getDivider()}
                </div>
            )
        });
    };

    const allowedSeparators = [BrandVueApi.CompositeVariableSeparator.And, BrandVueApi.CompositeVariableSeparator.Or];

    const getSeparatorSelector = () => {
        return (
            <div className="composite-selector">
                <ButtonDropdown isOpen={separatorDropdownOpen} toggle={() => setSeparatorDropdownOpen(!separatorDropdownOpen)} className="composite-dropdown">
                    <DropdownToggle className="composite-toggle toggle-button">
                        <div>{separatorToDisplayString(props.component.compositeVariableSeparator)}</div>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu>
                        {allowedSeparators.map(s =>
                            <DropdownItem onClick={() => setSeparator(s)} key={s}>
                                {separatorToDisplayString(s)}
                            </DropdownItem>
                        )}
                    </DropdownMenu>
                </ButtonDropdown>
                <span>the following are true</span>
                {((props.component.compositeVariableComponents?.length > 0) && (props.component.compositeVariableSeparator == BrandVueApi.CompositeVariableSeparator.And)) &&
                    <button className="cycle-button" onClick={() => reOrderConditionsByShiftingByOne()} title="Re-order conditions">
                        <i className="material-symbols-outlined">change_circle</i>
                    </button>
                }
            </div>
        );
    }

    return <>
        {getSeparatorSelector()}
        {getComponentEditors()}
    </>;
}