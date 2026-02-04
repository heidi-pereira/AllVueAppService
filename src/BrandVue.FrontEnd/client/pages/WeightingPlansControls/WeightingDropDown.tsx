import React from "react";
import { useState } from "react";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';

interface IWeightingDropDownProps {
    onShowStats();
}

const WeightingDropDown: React.FunctionComponent<IWeightingDropDownProps> = (props) => {
    const [dropdownOpen, setDropdownOpen] = useState<boolean>(false);

    const toggleDropdown = (e: React.MouseEvent) => {
        e.stopPropagation();
        setDropdownOpen(!dropdownOpen);
    }
 
        return (
            <>
                <ButtonDropdown isOpen={dropdownOpen} toggle={() => setDropdownOpen(!dropdownOpen)} className="context-menu metric-list-item-menu">
                    <div onClick={toggleDropdown}>
                        <DropdownToggle className={`btn-menu styled-toggle `}>
                            <i className="material-symbols-outlined menu-icon">more_vert</i>
                        </DropdownToggle>
                    </div>
                    <DropdownMenu>
                        <DropdownItem onClick={() => props.onShowStats()}>
                            <i className="material-symbols-outlined menu-icon">bar_chart</i>Weighting statistics
                        </DropdownItem>

                    </DropdownMenu>
                </ButtonDropdown>
            </>
        );
   
}



export default WeightingDropDown;