import React from 'react';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { ICustomPeriod } from "../../../../BrandVueApi";
import {formatDate} from "../../../../helpers/DateFormattingHelper";

interface IProps {
    defaultWave: ICustomPeriod,
    waves: ICustomPeriod[],
    onWaveSelect: (selectedWave: ICustomPeriod) => void;
}

const DataWavesDropdown = ({defaultWave, waves, onWaveSelect}: IProps) => {
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);
    const [selectedWave, setSelectedWave] = React.useState<ICustomPeriod>(defaultWave);

    const getWaveDropdownItem = (wave: ICustomPeriod) : JSX.Element => {
        return (<DropdownItem key={wave.name} onClick={() => {
            setSelectedWave(wave);
            onWaveSelect(wave);
        }}>
            <div className="name-container">
                <span className='title' title={`Wave duration: ${formatDate(wave.startDate)} - ${formatDate(wave.endDate)}`}>{wave.name}</span>
            </div>
        </DropdownItem>);
    };

    return (
        <ButtonDropdown className="add-metric-dropdown" toggle={() => setDropdownOpen(!dropdownOpen)} isOpen={dropdownOpen}>
            <DropdownToggle className="jump-to-toggle hollow-button">
                <span>{selectedWave.name !== defaultWave.name ? `Filtered by wave: ${selectedWave.name}` : 'Filter by wave'}</span>
                <i className="material-symbols-outlined">{dropdownOpen ? "expand_less" : "expand_more"}</i>
            </DropdownToggle>
            <DropdownMenu>
                <div className="dropdown-menu-container">
                    <div className="search-and-list">
                        <div className="dropdown-metrics">
                            {waves.map(getWaveDropdownItem)}
                            <DropdownItem divider />
                            {getWaveDropdownItem(defaultWave)}
                        </div>
                    </div>
                </div>
            </DropdownMenu>
        </ButtonDropdown>
    );
};

export default DataWavesDropdown;