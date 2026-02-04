import React from 'react';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import {
    CompositeVariableComponent,
    DateRangeVariableComponent,
    InclusiveRangeVariableComponent,
    InstanceListVariableComponent,
    MainQuestionType,
    IEntityType, SurveyIdVariableComponent,
    SurveyRecord,
    VariableComponent, VariableConfigurationModel
} from '../../../../../BrandVueApi';
import MetricDropdownMenu from "../../MetricDropdownMenu";
import VariableComponentDateTimeRange from "./VariableComponentComponents/VariableComponentDateTimeRange";
import VariableComponentSurveyIds from "./VariableComponentComponents/VariableComponentSurveyIds";
import VariableComponentRange from "./VariableComponentComponents/VariableComponentRange";
import VariableComponentInstanceList from "./VariableComponentComponents/VariableComponentInstanceList";
import {Metric} from "../../../../../metrics/metric";
import {useEffect} from "react";
import {getSelectedMetric} from "../Utils/VariableComponentHelpers";

export interface IVariableComponentSelectorProps {
    component: VariableComponent | undefined;
    updateComponent: (selectedComponent: VariableComponent | undefined) => void;
    availableEntityTypes: IEntityType[] | undefined;
    validMetrics: Metric[];
    allMetrics: Metric[];
    questionTypeLookup: {[key: string]: MainQuestionType};
    nonMapFileSurveys: SurveyRecord[];
    isInsideCompositeComponent?: boolean;
    removeComponent?(): void;
    getEmptyVariableComponentForMetric(metric: Metric | undefined) : VariableComponent;
    hasWarning: boolean;
    variables: VariableConfigurationModel[];
}

export const VariableComponentSelector = (props: IVariableComponentSelectorProps) => {
    const [entityTypeDropdownOpen, setEntityTypeDropdownOpen] = React.useState<boolean>(false)

    const [selectedMetric, setSelectedMetric] = React.useState<Metric | undefined>(undefined)

    const updateSelectedMetric = (metric: Metric | undefined) => {
        if (metric) {
            const component = props.getEmptyVariableComponentForMetric(metric)
            props.updateComponent(component)
        } else {
            props.updateComponent(undefined)
        }
    }

    useEffect(() => {
        if (props.component && !(props.component instanceof CompositeVariableComponent)) {
            const metric = getSelectedMetric(props.allMetrics, props.component, props.variables);
            setSelectedMetric(metric);
        }else {
            setSelectedMetric(undefined)
        }
    }, [props.component])

    const getMetricTitleClassname = () => {
        if (!props.component) {
            return "title placeholder";
        }
        return "title"
    }

    const setEntityType = (component: InstanceListVariableComponent, entityType: IEntityType) => {
        if (component && component.fromEntityTypeName !== entityType.identifier) {
            const allEntityTypes = props.availableEntityTypes ?? [];
            const resultEntityTypeNames = allEntityTypes.filter(e => e.identifier !== entityType.identifier).map(e => e.identifier);
            const newComponent = new InstanceListVariableComponent({
                ...component,
                fromEntityTypeName: entityType.identifier,
                instanceIds: [],
                resultEntityTypeNames: resultEntityTypeNames
            });
            props.updateComponent(newComponent);
        }
    }

    const getToggleElement = (selectedMetric: Metric | undefined, className : string) => {
        return (
            <DropdownToggle className="metric-selector-toggle toggle-button">
                <div className={className} title={selectedMetric?.displayName}>
                    {selectedMetric ? selectedMetric.displayName : "Choose a question"}
                </div>
                <i className="material-symbols-outlined">arrow_drop_down</i>
             </DropdownToggle>
        );
    };

    const getMetricSelector = () => {
        return (
            <MetricDropdownMenu
                toggleElement={getToggleElement(selectedMetric, getMetricTitleClassname())}
                metrics={props.validMetrics}
                selectMetric={updateSelectedMetric}
                showCreateVariableButton={false}
                groupCustomVariables={true}
                hasWarning={props.hasWarning}
            />
        );
    }

    const getEntityTypeSelector = () => {
        if (selectedMetric && props.availableEntityTypes && selectedMetric.entityCombination.length > 1 && props.component && props.component instanceof InstanceListVariableComponent) {
            const component = props.component as InstanceListVariableComponent;

            return (
                <ButtonDropdown isOpen={entityTypeDropdownOpen}
                    toggle={() => setEntityTypeDropdownOpen(!entityTypeDropdownOpen)}
                    className="entity-type-dropdown">
                    <DropdownToggle className="entity-type-selector-toggle toggle-button">
                        <div>{props.availableEntityTypes.find(e => e.identifier === component.fromEntityTypeName)?.displayNameSingular ?? component.fromEntityTypeName}</div>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu>
                        <div className="dropdown-entity-types">
                            {props.availableEntityTypes.map(e =>
                                <DropdownItem key={e.identifier} onClick={() => setEntityType(component, e)}>
                                    {e.displayNameSingular}
                                </DropdownItem>
                            )}
                        </div>
                    </DropdownMenu>
                </ButtonDropdown>
            );
        }
    }

    const getComponentEditor = () => {
        if (props.component) {

            if (props.component instanceof DateRangeVariableComponent) {
                return <VariableComponentDateTimeRange
                    component={props.component}
                    setComponentForGroup={props.updateComponent}
                />;
            }

            if (props.component instanceof SurveyIdVariableComponent) {
                return <VariableComponentSurveyIds
                    component={props.component}
                    availableSurveys={props.nonMapFileSurveys}
                    setComponentForGroup={props.updateComponent}
                />;
            }

            if (props.component instanceof InclusiveRangeVariableComponent) {
                return <VariableComponentRange
                    component={props.component}
                    setComponentForGroup={props.updateComponent}
                />;
            }

            if (props.component instanceof InstanceListVariableComponent) {
                return <VariableComponentInstanceList
                    component={props.component}
                    isInsideCompositeComponent={props.isInsideCompositeComponent}
                    setComponentForGroup={props.updateComponent}
                    hasWarning={props.hasWarning}
                />;
            }
        }
    };

    const showRemoveButton = props.isInsideCompositeComponent;
    const containerClass = `component-selector-container ${props.isInsideCompositeComponent ? 'inside-composite' : ''}`;

    return (
        <div className={containerClass}>
            <div className="component-column">
                {getMetricSelector()}
                {getEntityTypeSelector()}
                {getComponentEditor()}
            </div>
            {showRemoveButton &&
                <div className="remove-column">
                    <i className="material-symbols-outlined remove-component-button" onClick={props.removeComponent}>close</i>
                </div>
            }
        </div>
    );
}