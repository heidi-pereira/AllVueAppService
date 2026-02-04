import React from "react";
import {
    EntityTypeAndInstance,
    MultipleEntitySplitByAndFilterBy,
    PartDescriptor, IEntityType,
} from "../../../../../BrandVueApi";
import { PartWithExtraData } from "../../ReportsPageDisplay";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { Metric } from "../../../../../metrics/metric";
import { useEntityConfigurationStateContext } from "../../../../../entity/EntityConfigurationStateContext";

interface IConfigureReportPartSplitByProps {
    reportPart: PartWithExtraData;
    canPickFilterInstances: boolean;
    savePartChanges(newPart: PartDescriptor): void;
}

const ConfigureReportPartSplitBy = (props: IConfigureReportPartSplitByProps) => {
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);
    const metric = props.reportPart.metric;
    const configuration = props.reportPart.part.multipleEntitySplitByAndFilterBy;
    const canPickSplitByEntityType = metric != null && metric.entityCombination.length > 1;
    const { entityConfiguration } = useEntityConfigurationStateContext();

    const updateSelectedSplitByType = (newSplitByType: IEntityType | null, metric: Metric) => {
        if (newSplitByType && newSplitByType.identifier !== configuration.splitByEntityType) {
            const entities = new MultipleEntitySplitByAndFilterBy();
            entities.splitByEntityType = newSplitByType.identifier;
            const filterByTypes = metric.entityCombination.filter(t => t.identifier !== newSplitByType.identifier);
            if (props.canPickFilterInstances) {
                entities.filterByEntityTypes = filterByTypes.map(type => {
                    var allInstances = entityConfiguration.getAllEnabledInstancesForTypeOrdered(type);
                    return new EntityTypeAndInstance({
                        type: type.identifier,
                        instance: allInstances[0].id
                    });
                })
            } else {
                entities.filterByEntityTypes = filterByTypes.map(type => new EntityTypeAndInstance({ type: type.identifier }));
            }
            const modifiedPart = new PartDescriptor(props.reportPart.part);
            modifiedPart.multipleEntitySplitByAndFilterBy = entities;
            modifiedPart.defaultSplitBy = newSplitByType.identifier;
            modifiedPart.selectedEntityInstances = undefined;
            props.savePartChanges(modifiedPart);
        }
    }

    if (metric && canPickSplitByEntityType) {
        const selectedSplitBy = metric.entityCombination.find(t => t.identifier === configuration.splitByEntityType) ?? metric.entityCombination[0];
        return (
            <>
                <label className="category-label">Split by</label>
                <ButtonDropdown isOpen={dropdownOpen} toggle={() => setDropdownOpen(!dropdownOpen)} className="configure-option-dropdown input">
                    <DropdownToggle className="toggle-button">
                        <span className='single-line'>{selectedSplitBy.displayNameSingular}</span>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu>
                        {metric.entityCombination.sort((a,b) => a.identifier.localeCompare(b.identifier)).map(entityType =>
                            <DropdownItem onClick={() => updateSelectedSplitByType(entityType, metric)} key={entityType.identifier}>
                                <div className='wrap-line'>{entityType.displayNameSingular}</div>
                            </DropdownItem>
                        )}
                    </DropdownMenu>
                </ButtonDropdown>
            </>
        );
    }
    return null;
}

export default ConfigureReportPartSplitBy;