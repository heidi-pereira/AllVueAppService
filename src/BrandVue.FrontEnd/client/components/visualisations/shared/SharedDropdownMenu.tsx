import React, { useContext } from "react";
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from "reactstrap"
import SearchInput from "../../SearchInput"
import VariableContentModal from "../Variables/VariableModal/VariableContentModal"
import { ProductConfigurationContext } from "../../../ProductConfigurationContext";
import { PermissionFeaturesOptions, ReportVariableAppendType } from "../../../BrandVueApi";
import { hasAllVuePermissionsOrSystemAdmin } from "client/components/helpers/FeaturesHelper";
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from "client/state/subsetSlice";
import FeatureGuard from "client/components/FeatureGuard/FeatureGuard";

export interface ISharedDropdownMenu {
    dropdownItems: React.ReactElement<any, any>;
    toggleElement: React.ReactElement<DropdownToggle>;
    selectNone: () => void;
    showCreateVariableButton?: boolean | undefined;
    searchQuery: string;
    setSearchQuery: (text: string) => void;
    disabled?: boolean;
    shouldCreateWaveVariable?: boolean
    reportVariableAppendType?: ReportVariableAppendType;
    selectedReportPart?: string;
    selectNoneText?: string;
    hasWarning?: boolean;
}

export const SharedDropdownMenu = (props: ISharedDropdownMenu) => {
    const [isOpen, setIsOpen] = React.useState(false);
    const [isVariableModalOpen, setIsVariableModalOpen] = React.useState<boolean>(false)
    const { productConfiguration } = useContext(ProductConfigurationContext);
    const subsetId = useAppSelector(selectSubsetId);

    const toggleMetricDropdown = () => {
        setIsOpen(!isOpen);
        props.setSearchQuery('');
    };

    const getNewVariableButton = () => {
        const showCreateVariableButton = props.showCreateVariableButton === undefined
            ? hasAllVuePermissionsOrSystemAdmin(productConfiguration, [PermissionFeaturesOptions.VariablesCreate])
            : props.showCreateVariableButton;

        if (showCreateVariableButton) {
            return (
                <FeatureGuard permissions={[PermissionFeaturesOptions.VariablesCreate]}>
                    <button id="new-variable-button" className={`hollow-button new-variable-button`} onClick={() => setIsVariableModalOpen(true)}>
                        <i className="material-symbols-outlined">add</i>
                        <div className="new-variable-button-text">Create new variable</div>
                    </button>
                </FeatureGuard>
            );
        }
        return null;
    }

    const renderSelectNone = () => {
        return (props.selectNoneText &&
            <>
                <DropdownItem onClick={() => props.selectNone()}>
                    <div className="name-container">
                        <span className='title'>{props.selectNoneText}</span>
                    </div>
                </DropdownItem>
                <DropdownItem divider />
            </>
        )
    }

    return (
        <div className="metric-dropdown-menu">
            <ButtonDropdown isOpen={isOpen} toggle={toggleMetricDropdown} className="metric-dropdown" disabled={props.disabled}>
                {props.toggleElement}
                <DropdownMenu>
                    {renderSelectNone()}
                    <SearchInput id="metric-search-input"
                        text={props.searchQuery}
                        onChange={(text) => props.setSearchQuery(text)}
                        autoFocus={true}
                    />
                    <DropdownItem divider />
                    <div className={"dropdown-metrics"}>
                        {props.dropdownItems}
                    </div>
                    {getNewVariableButton()}
                </DropdownMenu>
            </ButtonDropdown>
            <VariableContentModal isOpen={isVariableModalOpen}
                subsetId={subsetId}
                setIsOpen={setIsVariableModalOpen}
                shouldCreateWaveVariable={props.shouldCreateWaveVariable}
                selectedPart={props.selectedReportPart}
                reportAppendType={props.reportVariableAppendType}
            />
        </div>
    )
}