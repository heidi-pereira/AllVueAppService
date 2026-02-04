import React from "react";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from "reactstrap";
import style from "./ConfigureReportPartFilterInstance.module.less";
import { IFilterInstancePickerProps } from "./FilterInstancePicker.types";
import { MultipleEntitySplitByAndFilterBy, EntityTypeAndInstance } from "client/BrandVueApi";
import { EntityInstance } from "../../../../../entity/EntityInstance";
import { getDropdownLabel } from "./OptionsHelper";
import { MixPanel } from "client/components/mixpanel/MixPanel";
import { InstanceTooltip } from "./FilterInstanceTooltip";

export const FilterMultiInstancePicker = (props: IFilterInstancePickerProps) => {
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);

    const toggleEntity = (typeIdentifier: string,
        instance: EntityInstance,
        newConfig: MultipleEntitySplitByAndFilterBy) => {
        if (props.config!.filterByEntityTypes.some(t => t.type === typeIdentifier && t.instance === instance.id)) {
            newConfig.filterByEntityTypes = newConfig.filterByEntityTypes.filter(
                t => !(t.type === typeIdentifier && t.instance === instance.id)
            );
        } else {
            const newFilterByEntityType = new EntityTypeAndInstance({
                type: typeIdentifier,
                instance: instance.id
            });
            newConfig.filterByEntityTypes.push(newFilterByEntityType);
        }
    };
            
    const selectMultipleFilterInstances = (instances: EntityInstance[]) => {
        const newConfig = new MultipleEntitySplitByAndFilterBy(props.config);
        if(instances.length === 0) {
            MixPanel.track("clearAllFilterInstances");
            //A filter must be selected so revert to default
            const firstInstance = props.allInstances[0];
            const defaultInstance = [new EntityTypeAndInstance({
                type: props.entityType.identifier,
                instance: firstInstance.id
            })];
            newConfig.filterByEntityTypes = defaultInstance;
        } else if (instances.length === 1) {
            const userIsTogglingOffOnlySelectedInstance = newConfig.filterByEntityTypes.length === 1 && instances[0].id === newConfig.filterByEntityTypes[0].instance;

            if(userIsTogglingOffOnlySelectedInstance) {
                //users cannot toggle off the only selected instance
                return;
            }
            toggleEntity(props.entityType.identifier, instances[0], newConfig);
        } else {
            MixPanel.track("selectAllFilterInstances");
            newConfig.filterByEntityTypes = [
                ...newConfig.filterByEntityTypes.filter(t => t.type !== props.entityType.identifier),
                ...instances.map(instance =>
                    new EntityTypeAndInstance({ type: props.entityType.identifier, instance: instance.id })
                )
            ];
        }
        props.updatePartWithConfig!(newConfig);
    }

    return (
        <ButtonDropdown isOpen={dropdownOpen}
            toggle={() => setDropdownOpen(!dropdownOpen)}
            className={style.configureOptionDropdown}>
            <DropdownToggle caret className="toggle-button">
                <InstanceTooltip placement="top" title={props.selectedInstances.length > 1 && props.selectedInstances.map(i => i.name).join(', ')}>
                        <span className="single-line">{getDropdownLabel(props.selectedInstances)}</span>
                </InstanceTooltip>
            </DropdownToggle>
            <DropdownMenu className={style.dropdownMenu}>
                <div className={style.entitySetSelectorButtons}>
                    <button className="modal-button secondary-button" onClick={() => { selectMultipleFilterInstances(props.allInstances); }}>Select All</button>
                    <button className="modal-button secondary-button" onClick={() => { selectMultipleFilterInstances([]); }}>Clear</button>
                </div>
                <>
                    {props.allInstances.map((instance) => {
                        const checked = props.selectedInstances?.some(i => i.id === instance.id) ?? false;
                        return (
                            <DropdownItem className={style.dropdownItem}
                                onClick={() => selectMultipleFilterInstances([instance])}
                                key={instance.id}>
                                <input type="checkbox"
                                    id={`${props.entityType.identifier}-${instance.id}`}
                                    checked={checked}
                                    onChange={() => selectMultipleFilterInstances([instance])} />
                                <label className={style.filterInstanceLabel}
                                    htmlFor={`${props.entityType.identifier}-${instance.id}`}
                                    title={instance.name}>
                                    {instance.name}
                                </label>
                            </DropdownItem>
                        );
                    })}
                </>
            </DropdownMenu>
        </ButtonDropdown>
    );
};

export default FilterMultiInstancePicker;