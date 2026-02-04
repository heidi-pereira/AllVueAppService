import { Dropdown, DropdownToggle, DropdownMenu, DropdownItem } from "reactstrap";
import {useState} from "react";
import { IAverageDescriptor } from '../../BrandVueApi';

type Props = {
    average: IAverageDescriptor | undefined;
    userVisibleAverages: IAverageDescriptor[];
    updateFilterAverage: (average: IAverageDescriptor) => void;
    disabled?: boolean;
    titleOverride?: string;
}

const AverageSelector = (props: Props) => {
    const [dropdownOpen, setDropdownOpen] = useState(false);

    const toggle = () => {
        setDropdownOpen(!dropdownOpen)
    }

    const getTitle = () => {
        if (props.average) {
            return `Average: ${props.average.displayName}`;
        }
        return "Select an average";
    };

    if (props.userVisibleAverages.length == 0)
        return (<></>);
    return (
        <Dropdown isOpen={dropdownOpen} toggle={toggle} className="averageSelector styled-dropdown">
            <DropdownToggle caret className="btn-menu styled-toggle" disabled={props.disabled}>
                {props.titleOverride ?? getTitle()}
            </DropdownToggle>
            <DropdownMenu>
                {props.userVisibleAverages.map((v, i) => <DropdownItem key={i} onClick={() => props.updateFilterAverage(v)}>{v.displayName}</DropdownItem>)}
            </DropdownMenu>
        </Dropdown>
    );
}
export default AverageSelector;
