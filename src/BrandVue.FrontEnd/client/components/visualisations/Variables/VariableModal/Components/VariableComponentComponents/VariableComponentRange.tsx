import React from 'react';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import {InclusiveRangeVariableComponent, VariableRangeComparisonOperator} from '../../../../../../BrandVueApi';

interface IVariableComponentRangeProps {
    component: InclusiveRangeVariableComponent;
    setComponentForGroup(component: InclusiveRangeVariableComponent): void;
}

const VariableComponentRange = (props: IVariableComponentRangeProps) => {

    const [isOperatorDropdownOpen, setIsOperatorDropdownOpen] = React.useState<boolean>(false);
    const minInputRef = React.useRef<HTMLInputElement>(null);

    const toggleOperatorDropdown = () => {
        setIsOperatorDropdownOpen(!isOperatorDropdownOpen);
    };

    const operator = props.component ? props.component.operator : VariableRangeComparisonOperator.Between;
    const minValue = props.component && props.component.min != undefined ? props.component.min : '';
    const maxValue = props.component && props.component.max != undefined ? props.component.max : '';
    const itemsValue = props.component && props.component.exactValues != undefined ? props.component.exactValues.map(x => Number.isNaN(x) ? '': x).join(',') : '';

    const getOperatorText = (selectedOperator: VariableRangeComparisonOperator) => {
        switch (selectedOperator) {
            case VariableRangeComparisonOperator.Between:
                return "Between (inclusive)";
            case VariableRangeComparisonOperator.Exactly:
                return "Equal to";
            case VariableRangeComparisonOperator.GreaterThan:
                return "Greater than or equal to";
            case VariableRangeComparisonOperator.LessThan:
                return "Less than or equal to";
        }
    };

    const selectOperator = (selectedOperator: VariableRangeComparisonOperator) => {
        var component: InclusiveRangeVariableComponent = new InclusiveRangeVariableComponent({
            ...props.component,
            operator: selectedOperator,
        });

        if (minInputRef.current) {
            minInputRef.current.focus();
        }

        props.setComponentForGroup(component);
    };

    const duplicateWithOutMinMax = (component: InclusiveRangeVariableComponent) => {
        const clonedComponent = new InclusiveRangeVariableComponent();
        clonedComponent.exactValues = [...component.exactValues];
        clonedComponent.operator = component.operator;
        clonedComponent.fromVariableIdentifier = component.fromVariableIdentifier;
        clonedComponent.inverted = component.inverted;
        clonedComponent.resultEntityTypeNames = [...component.resultEntityTypeNames];
        return clonedComponent
    }

    const onMinValueChange = (stringValue: string) => {
        const numValue = parseInt(stringValue);

        if (stringValue && !Number.isInteger(numValue)) {
            return;
        }

        const component: InclusiveRangeVariableComponent = duplicateWithOutMinMax(props.component);
        component.max = props.component.max;

        if (!isNaN(numValue)) {
            component.min = numValue;
        }

        props.setComponentForGroup(component);
    };

    const onMinValueChangeExactly = (stringValue: string) => {
        const numbersAsString = stringValue.split(',');
        const numbers = numbersAsString.map(x => parseInt(x));

        if (numbers.length > 0 && !isNaN(numbers[0])) {
            var component: InclusiveRangeVariableComponent = new InclusiveRangeVariableComponent({
                ...props.component,
                   min: numbers[0],
                   exactValues: numbers.length > 1 ? numbers : [],
            });
            props.setComponentForGroup(component);
            return;
        }
        onMinValueChange(stringValue);
    };

    const onMaxValueChange = (stringValue: string) => {

        const numValue = parseInt(stringValue);

        if (stringValue && !Number.isInteger(numValue)) {
            return;
        }

        const component: InclusiveRangeVariableComponent = duplicateWithOutMinMax(props.component);
        component.min = props.component.min;

        if (!isNaN(numValue)) {
            component.max = numValue;
        }

        props.setComponentForGroup(component);
    };

    return (
        <div className="range-selector">
            <ButtonDropdown isOpen={isOperatorDropdownOpen} toggle={toggleOperatorDropdown} className="range-dropdown operator-dropdown">
                <DropdownToggle className="range-selector-toggle toggle-button">
                    <div>{getOperatorText(operator)}</div>
                    <i className="material-symbols-outlined">arrow_drop_down</i>
                </DropdownToggle>
                <DropdownMenu>
                    <DropdownItem key="betweencompare" onClick={() => selectOperator(VariableRangeComparisonOperator.Between)}>
                        <span>{getOperatorText(VariableRangeComparisonOperator.Between)}</span>
                    </DropdownItem>
                    <DropdownItem key="gtcompare" onClick={() => selectOperator(VariableRangeComparisonOperator.GreaterThan)}>
                        <span>{getOperatorText(VariableRangeComparisonOperator.GreaterThan)}</span>
                    </DropdownItem>
                    <DropdownItem key="ltcompare" onClick={() => selectOperator(VariableRangeComparisonOperator.LessThan)}>
                        <span>{getOperatorText(VariableRangeComparisonOperator.LessThan)}</span>
                    </DropdownItem>
                    <DropdownItem key="exactlycompare" onClick={() => selectOperator(VariableRangeComparisonOperator.Exactly)}>
                        <span>{getOperatorText(VariableRangeComparisonOperator.Exactly)}</span>
                    </DropdownItem>
                </DropdownMenu>
            </ButtonDropdown>
            {operator == VariableRangeComparisonOperator.Exactly &&
                <input type="text"
                className="base-input range-input"
                    autoFocus
                    autoComplete="off"
                    ref={minInputRef}
                    value={itemsValue.length > 0 ? itemsValue: minValue}
                    onChange={(e) => onMinValueChangeExactly(e.target.value)}
                />
            }
            {operator != VariableRangeComparisonOperator.Exactly &&
                <input type="number"
                    className="base-input range-input"
                    autoFocus
                    autoComplete="off"
                    step="1"
                    ref={minInputRef}
                    value={minValue}
                    onChange={(e) => onMinValueChange(e.target.value)}
                />
            }
            {operator === VariableRangeComparisonOperator.Between &&
                <>
                    <span>and</span>
                    <input type="number"
                        className="base-input range-input"
                        step="1"
                        value={maxValue}
                        onChange={(e) => onMaxValueChange(e.target.value)}
                    />
                </>
            }
        </div>
    );
}
export default VariableComponentRange;