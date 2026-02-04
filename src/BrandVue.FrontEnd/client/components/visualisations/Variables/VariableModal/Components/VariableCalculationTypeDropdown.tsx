import React from "react";
import {ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle} from "reactstrap";
import {CalculationType} from "../../../../../BrandVueApi";

interface IVariableCalculationTypeDropdownProps {
    selectedCalculationType: CalculationType;
    setSelectedCalculationType: (calculationType: CalculationType) => void;
}

const VariableCalculationTypeDropdown = (props: IVariableCalculationTypeDropdownProps) => {
    const [isDropdownOpen, setIsDropdownOpen] = React.useState<boolean>(false);
    const allCalculationTypes = [CalculationType.YesNo, CalculationType.Average, CalculationType.NetPromoterScore];
    
    const calculationDisplayNames = {}
    calculationDisplayNames[CalculationType.YesNo] = "Percentage"
    calculationDisplayNames[CalculationType.Average] = "Average"
    calculationDisplayNames[CalculationType.NetPromoterScore] = "NPS"
    
    return (
        <div className={"calculation-type-container"}>
            <label className="variable-page-label">Calculation type</label>
            <ButtonDropdown isOpen={isDropdownOpen} toggle={() => setIsDropdownOpen(!isDropdownOpen)} className={"calculation-type-dropdown"}>
                <DropdownToggle  className={"toggle-button"}>
                    <div>{calculationDisplayNames[props.selectedCalculationType]}</div>
                    <i className="material-symbols-outlined">arrow_drop_down</i>
                </DropdownToggle>
                <DropdownMenu>
                    <div className={"calculation-type-dropdown-menu"}>
                        {allCalculationTypes.map(calculationType =>
                            <DropdownItem key={calculationDisplayNames[calculationType]} onClick={() => props.setSelectedCalculationType(calculationType)}>
                                {calculationDisplayNames[calculationType]}
                            </DropdownItem>
                        )}
                    </div>
                </DropdownMenu>
            </ButtonDropdown>
        </div>
    );
}

export default VariableCalculationTypeDropdown