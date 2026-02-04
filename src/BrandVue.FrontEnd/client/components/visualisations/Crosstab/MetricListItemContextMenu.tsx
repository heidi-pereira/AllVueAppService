import { Metric } from "../../../metrics/metric";
import React from 'react';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { useState } from "react";
import { CalculationType, IEntityType, PermissionFeaturesOptions, QuestionVariableDefinition } from "../../../BrandVueApi";
import { IGoogleTagManager } from "../../../googleTagManager";
import VariableContentModal from "../Variables/VariableModal/VariableContentModal";
import { ProductConfigurationContext } from "../../../ProductConfigurationContext";
import { QuestionEditModal } from "../Variables/QuestionEditModal";
import { VariableListItem } from './VariableListItem';
import { VariableType } from "./VariableType";
import { hasAllVuePermissionsOrSystemAdmin } from '../../../components/helpers/FeaturesHelper';

interface IMetricListItemContextMenuProps {
    splitByEntityType: IEntityType | undefined;
    canEditMetrics: boolean;
    googleTagManager: IGoogleTagManager;
    eligibleForCrosstabOrAllVue: boolean;
    metricEnabled: boolean;
    filterEnabled: boolean;
    subsetId: string;
    variableListItem: VariableListItem;
    setEligibleForCrosstabOrAllVue(metric: Metric, isEligible: boolean): void;
    setDisableMeasure(isDisable: boolean): void;
    setDisableFilterMeasure(isDisable: boolean): void;
    setMetricDefaultSplitBy(entityType: IEntityType): void;
    setConvertCalculationTypeModalVisible(): void;
}

const MetricListItemContextMenu = (props: IMetricListItemContextMenuProps) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const [dropdownOpen, setDropdownOpen] = useState<boolean>(false);
    const [isVariableModalOpen, setIsVariableModalOpen] = useState<boolean>(false);
    const [flattenMultiEntity, setFlattenMultiEntity] = useState<boolean>(false);
    const [isVariableEditModalOpen, setIsVariableEditModalOpen] = useState<boolean>(false);
    const [metricToCopy, setMetricToCopy] = useState<undefined | Metric>(undefined);
    const [variableToView, setVariableToView] = useState<undefined | number>(undefined);

    const canEditVariable = props.canEditMetrics && props.variableListItem.metric.variableConfigurationId;
    const canCopyVariable = props.canEditMetrics && props.variableListItem.metric?.calcType != CalculationType.Text;

    const toggleDropdown = (e: React.MouseEvent) => {
        e.stopPropagation();
        setDropdownOpen(!dropdownOpen);
    }

    const copyAsNewVariable = (flatten: boolean) => {
        setVariableToView(undefined);
        setMetricToCopy(props.variableListItem.metric);
        setIsVariableModalOpen(true);
        setFlattenMultiEntity(flatten);
    }

    const viewVariable = () => {
        setVariableToView(props.variableListItem.metric.variableConfigurationId);
        setMetricToCopy(undefined);
        setIsVariableModalOpen(true);
        setFlattenMultiEntity(false);
    }

    const showConfirmationModal = () => props.setConvertCalculationTypeModalVisible();

    const editMetric = () => {
        if (props.variableListItem.variableType !== VariableType.Question) {
            viewVariable();
            return;
        }

        setIsVariableEditModalOpen(true);
    }

    const toggleDisableMeasure = () => {
        if (props.canEditMetrics) {
            props.setDisableMeasure(!props.variableListItem.metric.disableMeasure);
        }
    }

    const toggleDisableFilterForMeasure = () => {
        if (props.canEditMetrics) {
            props.setDisableFilterMeasure(!props.variableListItem.metric.disableFilter);
        }
    }

    const toggleEligibleForCrosstabOrAllVue = () => {
        if (props.canEditMetrics) {
            props.setEligibleForCrosstabOrAllVue(props.variableListItem.metric, !props.eligibleForCrosstabOrAllVue);
        }
    }

    const canFlatten = props.variableListItem.metric!.entityCombination?.length == 2
        && props.variableListItem.metric!.calcType != CalculationType.Text;

    const permissionToCreate = hasAllVuePermissionsOrSystemAdmin(productConfiguration, [PermissionFeaturesOptions.VariablesCreate]);
    const permissionToEdit = hasAllVuePermissionsOrSystemAdmin(productConfiguration, [PermissionFeaturesOptions.VariablesEdit]);
    if (props.canEditMetrics && (permissionToCreate || permissionToEdit)) {
        const extraText = productConfiguration.isSurveyVue() ? "" : " in Data Explore and Audiences";
        return (
            <>
                <ButtonDropdown isOpen={dropdownOpen} toggle={() => setDropdownOpen(!dropdownOpen)} className="styled-dropdown metric-list-item-menu">
                    <div onClick={toggleDropdown}>
                        <DropdownToggle className={`btn-menu styled-toggle ${dropdownOpen ? 'dropdownopen' : ''}`}>
                            <i className="material-symbols-outlined menu-icon">more_vert</i>
                        </DropdownToggle>
                    </div>
                    <DropdownMenu>
                        <div className="variable-items">
                            {(canCopyVariable && permissionToCreate) &&
                            <>
                                <DropdownItem onClick={() => copyAsNewVariable(false)}>
                                    <i className="material-symbols-outlined menu-icon">content_copy</i>Copy as new variable
                                </DropdownItem>
                                {canFlatten &&
                                    <DropdownItem onClick={() => copyAsNewVariable(true)}>
                                        <i className="material-symbols-outlined menu-icon">content_copy</i>Convert into multiple variables by row
                                    </DropdownItem>
                                }
                            </>
                            }
                            {(permissionToEdit && props.variableListItem.metric.calcType == CalculationType.Average) &&
                                <DropdownItem onClick={showConfirmationModal}>
                                    <i className="material-symbols-outlined menu-icon">settings</i>Convert to NPS
                                </DropdownItem>
                            }
                            {(permissionToEdit && props.variableListItem.metric.calcType == CalculationType.NetPromoterScore) &&
                                <DropdownItem onClick={showConfirmationModal}>
                                    <i className="material-symbols-outlined menu-icon">settings</i>Convert to Average
                                </DropdownItem>
                            }
                        </div>
                        {(permissionToEdit && props.variableListItem.metric.entityCombination.length > 1 && props.splitByEntityType) &&
                            <div className="split-by-items">
                                {props.variableListItem.metric.entityCombination.map((entityType, i) =>
                                    <DropdownItem key={i} onClick={(e) => {
                                        props.setMetricDefaultSplitBy(entityType);
                                        e.stopPropagation();
                                    }}>
                                        <i className="material-symbols-outlined menu-icon">call_split</i>
                                        <span className="split-by-name">Split by {entityType.displayNameSingular}</span>
                                        {entityType.identifier == props.splitByEntityType?.identifier &&
                                            <i className="material-symbols-outlined checkmark">check</i>
                                        }
                                    </DropdownItem>
                                )}
                            </div>
                        }
                        {permissionToEdit &&
                            <div className="metric-items">
                                {canEditVariable &&
                                    <DropdownItem onClick={editMetric}>
                                        <i className="material-symbols-outlined menu-icon">edit</i>Edit
                                </DropdownItem>
                                }
                                <DropdownItem onClick={toggleEligibleForCrosstabOrAllVue}>
                                    <i className="material-symbols-outlined menu-icon">{!props.eligibleForCrosstabOrAllVue ? 'visibility' : 'visibility_off'}</i>
                                    {!props.eligibleForCrosstabOrAllVue ? 'Show' + extraText : 'Hide' + extraText}
                                </DropdownItem>
                                {!productConfiguration.isSurveyVue() &&
                                    <DropdownItem onClick={toggleDisableMeasure}>
                                        <i className="material-symbols-outlined menu-icon">{!props.metricEnabled ? 'visibility' : 'visibility_off'}</i>
                                        {!props.metricEnabled ? 'Enable' : 'Disable'}
                                    </DropdownItem>
                                }
                                {!productConfiguration.isSurveyVue() &&
                                    <DropdownItem onClick={toggleDisableFilterForMeasure}>
                                        <i className="material-symbols-outlined menu-icon">{!props.filterEnabled ? 'tune' : 'visibility_off'}</i>
                                        {!props.filterEnabled ? 'Enable filter' : 'Disable filter'}
                                    </DropdownItem>
                                }
                            </div>
                        }
                    </DropdownMenu>
                </ButtonDropdown>
                <VariableContentModal
                    isOpen={isVariableModalOpen}
                    setIsOpen={setIsVariableModalOpen}
                    variableIdToView={variableToView}
                    metricToCopy={metricToCopy}
                    variableIdToCopy={metricToCopy?.variableConfigurationId}
                    subsetId={props.subsetId}
                    relatedMetric={props.variableListItem.metric}
                    flattenMultiEntity={flattenMultiEntity}
                />
                <QuestionEditModal isOpen={isVariableEditModalOpen}
                    setIsOpen={setIsVariableEditModalOpen}
                    metric={props.variableListItem.metric}
                    subsetId={props.subsetId}
                    showAdvancedInfo={canCopyVariable}
                    canEditDisplayName={!props.variableListItem.metric.isBasedOnCustomVariable || props.variableListItem.metric.isAutoGeneratedNumeric()}
                    variableDefinition={props.variableListItem.variable?.definition as QuestionVariableDefinition}
                />
            </>
        );
    }
    return null;
}

export default MetricListItemContextMenu;