import { EntityTypeAndInstance, MultipleEntitySplitByAndFilterBy } from "client/BrandVueApi";
import React from "react";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from "reactstrap";
import style from "./ConfigureReportPartFilterInstance.module.less";
import { IFilterInstancePickerProps } from "./FilterInstancePicker.types";
import { EntityInstance } from "../../../../../entity/EntityInstance";
import { getDropdownLabel } from "./OptionsHelper";
import { InstanceTooltip } from "./FilterInstanceTooltip";

export const FilterInstancePicker = (
    props: IFilterInstancePickerProps & { updateState?: (instance: EntityInstance) => void }
) => {
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);

    const selectInstance = (instance: EntityInstance) => {
        if (props.config && props.updatePartWithConfig) {
            const newConfig = new MultipleEntitySplitByAndFilterBy(props.config);
            const matchedFilterIndex = props.config.filterByEntityTypes.findIndex(
                t => t.type === props.entityType.identifier
            );
            if (matchedFilterIndex >= 0) {
                newConfig.filterByEntityTypes = props.config.filterByEntityTypes.map((t, index) => {
                    if (index === matchedFilterIndex) {
                        return new EntityTypeAndInstance({
                            ...t,
                            instance: instance.id
                        });
                    }
                    return new EntityTypeAndInstance(t);
                });
            }
            props.updatePartWithConfig(newConfig);
        } else if (props.updateState) {
            props.updateState(instance);
        }
    };

    return (
        <ButtonDropdown
            isOpen={dropdownOpen}
            toggle={() => setDropdownOpen(!dropdownOpen)}
            className={style.configureOptionDropdown}
        >
            <DropdownToggle caret className="toggle-button" data-testid="dropdown-toggle">
                <InstanceTooltip placement="top" title={props.selectedInstances.length > 0 ? props.selectedInstances[0].name : ""}>
                    <span className="single-line" data-testid="dropdown-label">{getDropdownLabel(props.selectedInstances)}</span>
                </InstanceTooltip>
            </DropdownToggle>
            <DropdownMenu className={style.dropdownMenu} data-testid="dropdown-menu">
                {props.allInstances.map(instance =>
                        <DropdownItem
                            className={style.dropdownItem}
                            onClick={() => selectInstance(instance)}
                            key={`${props.entityType.identifier}-${instance.id}`}
                            data-testid={`dropdown-item-${instance.id}`}
                        >
                            <InstanceTooltip placement="top" title={instance.name}>
                                <div className="single-line">{instance.name}</div>
                            </InstanceTooltip>
                        </DropdownItem>
                    )}
            </DropdownMenu>
        </ButtonDropdown>
    );
};

export default FilterInstancePicker;