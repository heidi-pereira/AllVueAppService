import React from "react";
import { IAverageDescriptor } from "../../BrandVueApi";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem, Button } from "reactstrap";
import {useState} from "react";
import { selectBestAverage } from "../helpers/AveragesHelper";
import { IDropdownToggleAttributes } from "../helpers/DropdownToggleAttributes";

interface IFixedPeriodAverageSelectorProps {
    selectedAverage: IAverageDescriptor;
    validRollingAverages: IAverageDescriptor[];
    validFixedAverages: IAverageDescriptor[];
    handleAverageChange(s: IAverageDescriptor);
    buttonAttr?: IDropdownToggleAttributes;
}

const FixedPeriodAverageSelector = (props: IFixedPeriodAverageSelectorProps) => {
    const [dropdownOpen, setDropdownOpen] = useState<boolean>(false);

    const { selectedAverage, validRollingAverages, validFixedAverages, handleAverageChange, buttonAttr } = props;

    const change = (e, period: IAverageDescriptor) => {
        handleAverageChange(period);
    }

    const toggle = () => {
        setDropdownOpen(!dropdownOpen);
    }

    const movingAverageStyleToString = (average: IAverageDescriptor): string => {
        return (validFixedAverages.find(a => a.averageId === average.averageId)) ? "Fixed" : "Rolling";
    }

    React.useEffect(() => {
        if (!validFixedAverages.some(a => a.averageId === selectedAverage.averageId) &&
            !validRollingAverages.some(a => a.averageId === selectedAverage.averageId) &&
            validFixedAverages.length > 0) {
            handleAverageChange(selectBestAverage(validFixedAverages, selectedAverage.averageId));
        }
    }, [])

    var availableNumberOfAverages = validFixedAverages.length + validRollingAverages.length;
    if (availableNumberOfAverages <= 1) {
        return (
            <div className="averageSelector me-5 btn-group average-selector-disabled" {...buttonAttr}>
                <span className="filter-title-disabled me-1"> {movingAverageStyleToString(selectedAverage)}</span>
                <span> {selectedAverage.displayName}</span>
            </div >
        );
    }

    return (
        <div>
            <ButtonDropdown isOpen={dropdownOpen} toggle={toggle} className="averageSelector styled-dropdown" >
                <DropdownToggle caret className="btn-menu styled-toggle" {...buttonAttr} aria-label="Select a period">
                    <i className="material-symbols-outlined me-2">calendar_today</i>
                    <span className="filterTitle"> {movingAverageStyleToString(selectedAverage)}</span>
                    <span> {selectedAverage.displayName}</span>
                </DropdownToggle>
                <DropdownMenu>
                    {validFixedAverages.length > 0 &&
                        <DropdownItem header>Fixed</DropdownItem>
                    }
                    {validFixedAverages.map((v, i) => <DropdownItem key={i} onClick={(e) => change(e, v)}>{v.displayName}</DropdownItem>)}

                    {validFixedAverages.length > 0 && validRollingAverages.length > 0 &&
                        <DropdownItem divider />
                    }
                    {validRollingAverages.length > 0 &&
                        <DropdownItem header>Rolling</DropdownItem>
                    }
                    {validRollingAverages.map((v, i) => <DropdownItem key={i} onClick={(e) => change(e, v)}>{v.displayName}</DropdownItem>)}

                </DropdownMenu>
            </ButtonDropdown>
        </div>
    );
}
export default FixedPeriodAverageSelector