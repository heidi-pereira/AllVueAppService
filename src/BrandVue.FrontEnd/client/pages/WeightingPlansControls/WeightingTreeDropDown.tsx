import React from "react";
import { useState } from "react";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { Metric } from "../../metrics/metric";
import VariableContentModal from "../../components/visualisations/Variables/VariableModal/VariableContentModal";
import { AutoGenerationType, IAverageDescriptor, WeightingMethod } from "../../BrandVueApi";
import { AverageIds } from "../../components/helpers/PeriodHelper";

interface IWeightingTreeDropDownProps {
    fullNodeDescription: string;
    metric: Metric | null;
    canClone: boolean;
    cloneNode(e: React.MouseEvent);
    flattenAndCloneNode(e: React.MouseEvent);
    canExport: boolean;
    export(e: React.MouseEvent, average: IAverageDescriptor);
    averages: IAverageDescriptor[];
    subsetId: string;
}

const WeightingTreeDropDown: React.FunctionComponent<IWeightingTreeDropDownProps> = (props) => {
    const [isVariableModalOpen, setIsVariableModalOpen] = React.useState<boolean>(false)

    const [dropdownOpen, setDropdownOpen] = useState<boolean>(false);

    const toggleDropdown = (e: React.MouseEvent) => {
        e.stopPropagation();
        setDropdownOpen(!dropdownOpen);
    }
    const averagesToDisplay = props.canExport ? props.averages.filter(x => !x.disabled && x.weightingMethod === WeightingMethod.QuotaCell).sort(x => x.order) : [];
    const customPeriodAverage = averagesToDisplay.find(x => x.averageId === AverageIds.CustomPeriod);
    const otherAverages = averagesToDisplay.filter(x => x.averageId !== AverageIds.CustomPeriod);
    const defaultAverage = customPeriodAverage ?? otherAverages.find(a => a.isDefault) ?? otherAverages[0];

    const editableVariable = props.metric;
    const canEditVariable = editableVariable && editableVariable.generationType != AutoGenerationType.CreatedFromField;

    if (canEditVariable || props.canClone || props.canExport) {
        return (
            <ButtonDropdown isOpen={dropdownOpen} toggle={() => setDropdownOpen(!dropdownOpen)} className="context-menu metric-list-item-menu">
                <div onClick={toggleDropdown}>
                    <DropdownToggle className={`btn-menu styled-toggle ${dropdownOpen ? 'dropdownopen' : ''}`}>
                        <i className="material-symbols-outlined menu-icon">more_vert</i>
                    </DropdownToggle>
                </div>
                <DropdownMenu>
                    {canEditVariable &&
                        <DropdownItem onClick={() => setIsVariableModalOpen(true)}>
                            <i className="material-symbols-outlined menu-icon">settings</i>Edit variable {editableVariable!.name}
                        </DropdownItem>
                    }
                    {props.canClone &&
                        <>
                            <DropdownItem onClick={(e) => props.cloneNode(e)}>
                                <i className="material-symbols-outlined menu-icon">content_copy</i>Copy weightings & keep nesting
                            </DropdownItem>

                            <DropdownItem onClick={(e) => props.flattenAndCloneNode(e)}>
                                <i className="material-symbols-outlined menu-icon">copy_all</i>Copy weightings & remove nesting
                            </DropdownItem>
                        </>
                    }
                    {props.canExport && props.canClone &&
                        <DropdownItem divider />
                    }
                    {props.canExport &&
                        <>
                            <DropdownItem header>Export weights for</DropdownItem>
                            {averagesToDisplay.length !== 1 &&
                                <DropdownItem header>{props.fullNodeDescription}</DropdownItem>
                            }
                            {defaultAverage &&
                                <DropdownItem key={-1} onClick={(e) => props.export(e, defaultAverage)}>
                                    <i className="material-symbols-outlined menu-icon">Weight</i>{`Default (${props.fullNodeDescription})`}
                                </DropdownItem>
                            }
                            {otherAverages.map((v, i) =>
                                <DropdownItem key={i} onClick={(e) => props.export(e, v)}>
                                    <i className="material-symbols-outlined menu-icon">Weight</i>{v.displayName}
                                </DropdownItem>
                            )}
                        </>
                    }
                </DropdownMenu>
                <VariableContentModal
                    isOpen={isVariableModalOpen}
                    setIsOpen={setIsVariableModalOpen}
                    variableIdToView={props.metric?.variableConfigurationId}
                    subsetId={props.subsetId}
                    relatedMetric={props.metric ?? undefined}
                />
            </ButtonDropdown>
        );
    }
    else {
        return <></>;
    }

}

export default WeightingTreeDropDown;