import { useState } from 'react'
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { DataSubsetManager } from "../../DataSubsetManager";
import { Subset } from '../../BrandVueApi';

interface ISubsetSelectorProps {
    selectedSubset: Subset;
    onChange: (subset: Subset) => void;
    darkStyling?: boolean;
}

const SubsetSelector = (props: ISubsetSelectorProps) => {
    const [subsetDropdownOpen, setSubsetDropdownOpen] = useState(false);
    const [parentGroupDropdownOpen, setParentGroupDropdownOpen] = useState(false);

    const subsets = DataSubsetManager.getAllByParentGroup(props.selectedSubset.parentGroupName) || [];
    const hasManySubsets = subsets.filter(s => !s.disabled).length > 1;

    const parentGroups = DataSubsetManager.getAllParentGroups();
    const hasManyParentGroups = parentGroups.length > 1;

    function onParentGroupChange(parentGroup: string | undefined) {
        DataSubsetManager.setSelectedParentGroup(parentGroup);
        const subsetsInParentGroup = DataSubsetManager.getAllByParentGroup(parentGroup);

        const stripPrefix = (id: string) => id.includes('-') ? id.split('-')[1] : id;

        // try to change to the equivalent subset in the new parent group, if not possible, just pick the first one
        const equivalentSubset = subsetsInParentGroup.find(s => stripPrefix(s.id) === stripPrefix(props.selectedSubset.id));
        props.onChange(equivalentSubset ?? subsetsInParentGroup[0]);
    };

    return (
        <div className="countrySwitch px-3 px-md-0">
            {hasManyParentGroups &&
                <ButtonDropdown isOpen={parentGroupDropdownOpen} toggle={() => setParentGroupDropdownOpen(!parentGroupDropdownOpen)} className="menu" title="Choose a segment">
                    <DropdownToggle id={'ParentGroup-' + props.selectedSubset.id} className="nav-button subset" tag="button">
                        <div className={props.darkStyling == true ? "circular-nav-button-normal" : "circular-nav-button"}>
                            <div className="circle">
                                <i className="material-symbols-outlined">public</i>
                            </div>
                            <div className="text">{DataSubsetManager.selectedParentGroup || "Other"}</div>
                        </div>
                    </DropdownToggle>
                    <DropdownMenu>
                        {parentGroups.map(p =>
                            <DropdownItem key={p || "Other"} onClick={() => onParentGroupChange(p)}>
                                <span>{p || "Other"}</span></DropdownItem>
                        )}
                    </DropdownMenu>
                </ButtonDropdown>
            }
            {hasManySubsets && 
                <ButtonDropdown isOpen={subsetDropdownOpen} toggle={() =>
                    setSubsetDropdownOpen(!subsetDropdownOpen)} className="menu" title="Choose a segment">
                    <DropdownToggle id={'Subset-' + props.selectedSubset.id} className="nav-button subset" tag="button">
                        <div className={props.darkStyling == true ? "circular-nav-button-normal" : "circular-nav-button"}>
                            <div className="circle">
                                <i className="material-symbols-outlined">{hasManyParentGroups ? "pie_chart" : "public"}</i>
                            </div>
                            <div className="text">{props.selectedSubset.displayName}</div>
                        </div>
                    </DropdownToggle>
                    <DropdownMenu>
                        {subsets.map(s =>
                            <DropdownItem key={s.id} onClick={() => props.onChange(s)}>
                                <span>{s.displayName}</span></DropdownItem>
                        )}
                    </DropdownMenu>
                </ButtonDropdown>
            }
        </div>
        );
};

export default SubsetSelector;