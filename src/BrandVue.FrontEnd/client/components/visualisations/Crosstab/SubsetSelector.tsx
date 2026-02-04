import React from "react";
import { useSelector } from "react-redux";
import { useState } from "react";
import { useWriteVueQueryParams } from "../../../components/helpers/UrlHelper";
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from "reactstrap";
import styles from './SubsetSelector.module.css';
import { useLocation, useNavigate } from "react-router-dom";
import { selectSubsetConfigurations} from '../../../state/subsetSlice';
import { ProductConfigurationContext } from "../../../ProductConfigurationContext";

interface ISubsetSelectorProps {
    subsetId: string;
    updateUrlOnChange: boolean;
    onSubsetChange(subsetId: string): void;
}

const SubsetSelector: React.FC<ISubsetSelectorProps> = (props: ISubsetSelectorProps) => {
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());
    const subsetConfigurations = useSelector(selectSubsetConfigurations);
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const [isSubsetDropdownOpen, setIsSubsetDropdownOpen] = useState(false);

    const handleOnChange = (identifier: string) => {
        props.updateUrlOnChange && setQueryParameter("Subset", identifier);
        props.onSubsetChange(identifier);
        setIsSubsetDropdownOpen(false);
    };

    const getSelectedSubsetDisplayName = () => {
        const selectedSubsetConfig = subsetConfigurations.find(subset => subset.identifier === props.subsetId);
        return selectedSubsetConfig ? selectedSubsetConfig.displayName : props.subsetId;
    };

    const isSubsetSelectionEnabled = subsetConfigurations 
        && subsetConfigurations.length > 1 
        && productConfiguration?.isSurveyVue();

    return (
        isSubsetSelectionEnabled ? (
            <>
                <label className={styles.label}>Segment</label>
                <ButtonDropdown isOpen={isSubsetDropdownOpen} toggle={() => setIsSubsetDropdownOpen(!isSubsetDropdownOpen)} className={`subset-dropdown ${styles.subsetDropdownButton}`}>
                    <DropdownToggle className="toggle-button">
                        <span>{getSelectedSubsetDisplayName()}</span>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu className={styles.subsetDropdownMenu}>
                        {subsetConfigurations.map(subset => (
                            <DropdownItem key={subset.identifier} onClick={() => handleOnChange(subset.identifier)}>
                                {subset.displayName}
                            </DropdownItem>
                        ))}
                    </DropdownMenu>
                </ButtonDropdown>
            </>
        ) : null
    );
};

export default SubsetSelector;