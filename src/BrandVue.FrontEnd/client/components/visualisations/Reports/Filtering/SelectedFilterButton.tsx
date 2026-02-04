import React from 'react';
import { ButtonDropdown, DropdownToggle, DropdownMenu } from 'reactstrap';
import { MetricFilterState } from '../../../../filter/metricFilterState';
import { useFilterStateContext } from '../../../../filter/FilterStateContext';
import { Metric } from '../../../../metrics/metric';
import FilterValueMappingSelector from './FilterValueMappingSelector';
import {useEntityConfigurationStateContext} from "../../../../entity/EntityConfigurationStateContext";

interface ISelectedFilterButtonProps {
    selectedMetric: Metric;
    selectedFiltersForMetric: MetricFilterState[];
}

const SelectedFilterButton = (props: ISelectedFilterButtonProps) => {
    const displayName = props.selectedMetric.displayName;
    const { entityConfiguration} = useEntityConfigurationStateContext();
    const filterDescription = props.selectedFiltersForMetric.map(f => f.filterDescription(entityConfiguration)).join(", ");
    const [dropdownOpen, setDropdownOpen] = React.useState(false);
    const [selectedFilters, setSelectedFilters] = React.useState<MetricFilterState[]>(props.selectedFiltersForMetric);
    const { filterDispatch } = useFilterStateContext();

    React.useEffect(() => {
        setSelectedFilters(props.selectedFiltersForMetric);
    }, [props.selectedFiltersForMetric]);

    const toggle = () => {
        setDropdownOpen(!dropdownOpen);
        setSelectedFilters(props.selectedFiltersForMetric);
    }

    const applyFilter = (filters: MetricFilterState[] | undefined) => {
        if (filters) {
            filterDispatch({
                type: "UPDATE_FILTER",
                data: {
                    metricName: props.selectedMetric.name,
                    filters: filters
                },
            });
        } else {
            throw new Error("Must have filter defined");
        }

        toggle();
    }

    const removeFilter = () => {
        filterDispatch({
            type: "REMOVE_FILTER",
            data: {
                measureName: props.selectedMetric.name,
            }
        });
    }

    const tooltipText = `${displayName}: ${filterDescription}`

    return (
        <ButtonDropdown isOpen={dropdownOpen} toggle={() => setDropdownOpen(!dropdownOpen)} className="selected-filter-button" title={tooltipText}>
            <DropdownToggle caret tag="button" className="primary-button">
                <div className="remove-filter-button" tabIndex={0} onClick={() => removeFilter()}>
                    <i className="material-symbols-outlined">close</i>
                </div>
                <div className="filter-name-container">
                    <span className="filter-name">{displayName}</span>
                    <span className="filter-description">: {filterDescription}</span>
                </div>
            </DropdownToggle>
            <DropdownMenu>
                <FilterValueMappingSelector
                    id="selected-filter-btn"
                    selectedMetric={props.selectedMetric}
                    selectedFilters={selectedFilters}
                    setSelectedFilters={setSelectedFilters}
                    showApplyButtons={true}
                    onApply={(filters) => applyFilter(filters)}
                    close={toggle}
                />
            </DropdownMenu>
        </ButtonDropdown>
    );
}

export default SelectedFilterButton;