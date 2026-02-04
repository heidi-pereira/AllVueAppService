import React from 'react';
import {IEntityConfiguration} from "../../../../../../entity/EntityConfiguration";
import { EntityInstance } from '../../../../../../entity/EntityInstance';
import {
    InstanceListVariableComponent,
    InstanceVariableComponentOperator
} from '../../../../../../BrandVueApi';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { useEntityConfigurationStateContext } from '../../../../../../entity/EntityConfigurationStateContext';

interface IVariableComponentInstanceListProps {
    component: InstanceListVariableComponent;
    isInsideCompositeComponent: boolean | undefined;
    setComponentForGroup(component: InstanceListVariableComponent): void;
    hasWarning: boolean
}

const VariableComponentInstanceList = (props: IVariableComponentInstanceListProps) => {
    const [operatorDropdownOpen, setOperatorDropdownOpen] = React.useState(false);
    const [instanceDropdownOpen, setInstanceDropdownOpen] = React.useState(false);
    const { entityConfiguration } = useEntityConfigurationStateContext();

    const getAllEntityInstances = (entityConfiguration: IEntityConfiguration): EntityInstance[] => {
        return entityConfiguration.getAllEnabledInstancesForResponseTypeNameOrdered(props.component.fromEntityTypeName);
    }

    const allInstances = getAllEntityInstances(entityConfiguration);
    const selectedInstanceIds = [...props.component.instanceIds];

    const toggleInstanceDropdown = () => {
        setInstanceDropdownOpen(!instanceDropdownOpen);
    }

    const toggleInstance = (id: number) => {
        const indexOfId = selectedInstanceIds.indexOf(id);
        if (indexOfId > -1) {
            //Already contains instance id so remove
            selectedInstanceIds.splice(indexOfId, 1);
        } else {
            //Add instance id
            selectedInstanceIds.push(id);
        }

        const newComponent: InstanceListVariableComponent = new InstanceListVariableComponent({
            ...props.component,
            instanceIds: selectedInstanceIds,
        });

        props.setComponentForGroup(newComponent);
    }

    const updateAll = (select: boolean) => {
        const newIds = select ? allInstances.map(i => i.id) : [];
        const newComponent: InstanceListVariableComponent = new InstanceListVariableComponent({
            ...props.component,
            instanceIds: newIds,
        });
        props.setComponentForGroup(newComponent);
    }

    const setOperator = (operator: InstanceVariableComponentOperator) => {
        const newComponent: InstanceListVariableComponent = new InstanceListVariableComponent({
            ...props.component,
            operator: operator,
        });
        props.setComponentForGroup(newComponent);
    }

    const operatorToDisplayString = (operator: InstanceVariableComponentOperator) => {
        switch (operator) {
            case InstanceVariableComponentOperator.Or:
                return "is any of";
            case InstanceVariableComponentOperator.And:
                return "is all of";
            case InstanceVariableComponentOperator.Not:
                return "is not";
        }
    }

    const getSelectedInstanceDescription = () => {
        if (selectedInstanceIds.length === 0) {
            return "Choose answers";
        }

        return selectedInstanceIds.sort().map(selectedInstanceId => allInstances.find(i => i.id === selectedInstanceId)?.name ?? "").join(", ");
    }

    const getSelectedInstanceDescriptionClassname = () => {
        if (selectedInstanceIds.length === 0) {
            return "title placeholder";
        }
        return "title";
    }

    const getOperatorSelector = () => {
        const operators = [InstanceVariableComponentOperator.Or, InstanceVariableComponentOperator.And, InstanceVariableComponentOperator.Not];
        return (
            <div className="operator-selector">
                <ButtonDropdown isOpen={operatorDropdownOpen} toggle={() => setOperatorDropdownOpen(!operatorDropdownOpen)} className="instance-operator-dropdown">
                    <DropdownToggle className="instance-operator-toggle toggle-button">
                        <div>{operatorToDisplayString(props.component.operator)}</div>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu>
                        {operators.map(op =>
                            <DropdownItem onClick={() => setOperator(op)} key={op}>
                                {operatorToDisplayString(op)}
                            </DropdownItem>
                        )}
                    </DropdownMenu>
                </ButtonDropdown>
            </div>
        )
    }

    const getCompositeInstanceSelector = () => {
        return (
            <ButtonDropdown className="variable-instance-dropdown" isOpen={instanceDropdownOpen} toggle={toggleInstanceDropdown}>
                <DropdownToggle className="metric-selector-toggle toggle-button" >
                    <div className={getSelectedInstanceDescriptionClassname()} title={getSelectedInstanceDescription()}>{getSelectedInstanceDescription()}</div>
                    <i className="material-symbols-outlined">arrow_drop_down</i>
                </DropdownToggle>
                <DropdownMenu>
                    <div className="instance-buttons">
                        <button className="instance-button secondary-button" onClick={() => updateAll(true)}>Select all</button>
                        <button className="instance-button secondary-button" onClick={() => updateAll(false)}>Clear all</button>
                    </div>
                    <div className={props.hasWarning ? "variable-instances variable-has-warning" : "variable-instances"}>
                        {allInstances.map((instance, index) => {
                            return (
                                <div className="instance-checkbox" key={`${props.component.fromVariableIdentifier}-${instance.id}-${index}`}>
                                    <input type="checkbox" className="checkbox"
                                        checked={selectedInstanceIds.includes(instance.id)}
                                        onChange={() => toggleInstance(instance.id)}/>
                                    <label className="instance-checkbox-label" title={instance.name} onClick={() => toggleInstance(instance.id)}>
                                        <span>{instance.name}</span>
                                    </label>
                                </div>
                            )
                        })}
                    </div>
                </DropdownMenu>
            </ButtonDropdown>
        )
    }

    if (props.isInsideCompositeComponent) {
        return (
            <div className="composite-instance-selector">
                {getOperatorSelector()}
                {getCompositeInstanceSelector()}
            </div>
        )
    }

    return (
        <div className="instance-selector">
             <div className="instance-buttons">
                {getOperatorSelector()}
                <>
                    <button className="instance-button secondary-button" onClick={() => updateAll(true)}>Select all</button>
                    <button className="instance-button secondary-button" onClick={() => updateAll(false)}>Clear all</button>
                </>
            </div>
            {allInstances.map((instance, index) => {
                return (
                    <div className="instance-checkbox" key={`${props.component.fromVariableIdentifier}-${instance.id}-${index}`}>
                        <input type="checkbox" className="checkbox"
                            checked={selectedInstanceIds.includes(instance.id)}
                            onChange={() => toggleInstance(instance.id)}/>
                        <label className="instance-checkbox-label" title={instance.name} onClick={() => toggleInstance(instance.id)}>
                            <span>{instance.name}</span>
                        </label>
                    </div>
                )
            })}
        </div>
    );
}
export default VariableComponentInstanceList;