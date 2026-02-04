import { Metric } from '../../../../metrics/metric';
import { FilterValueMapping } from '../../../../metrics/metricSet';
import {MainQuestionType, IEntityType} from '../../../../BrandVueApi';
import { EntityInstance } from '../../../../entity/EntityInstance';
import { MetricFilterState } from '../../../../filter/metricFilterState';
import { getMetricFilter } from './FilterHelper';
import { DropdownItem } from 'reactstrap';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { ArrayHelper } from "../../../helpers/ArrayHelper";

export interface IFilterValueMappingSelectorProps {
    id: string;
    selectedMetric: Metric;
    selectedFilters: MetricFilterState[] | undefined;
    setSelectedFilters(filters: MetricFilterState[]): void;
    showApplyButtons?: boolean;
    onApply?(filters: MetricFilterState[] | undefined): void;
    close?(): void;
    selectNoneText?: string;
}

const FilterValueMappingSelector = (props: IFilterValueMappingSelectorProps) => {
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { questionTypeLookup } = useMetricStateContext();
    const isMultipleChoice = questionTypeLookup[props.selectedMetric.name] === MainQuestionType.MultipleChoice;
    const canPickEntityInstance = isMultipleChoice &&
        props.selectedMetric.entityCombination.length > 0 &&
        !props.selectedMetric.isBasedOnCustomVariable;

    const filtersAreEqual = (a: MetricFilterState, b: MetricFilterState) => {
        return a.invert == b.invert &&
            a.treatPrimaryValuesAsRange == b.treatPrimaryValuesAsRange &&
            ArrayHelper.isEqual(a.values!, b.values!);
    }

    const toggleFilterValueMapping = (filter: FilterValueMapping) => {
        const metricFilter = getMetricFilter(props.selectedMetric, filter, {});

        const index = props.selectedFilters?.findIndex(f => filtersAreEqual(f, metricFilter)) ?? -1;
        const newFilters = [...props.selectedFilters ?? []];
        if (index >= 0) {
            newFilters.splice(index, 1);
        } else {
            newFilters.push(metricFilter);
        }
        props.setSelectedFilters(newFilters);
    }

    const toggleEntityInstance = (entityType: IEntityType, entityInstance: EntityInstance) => {
        const entityKey = entityType.identifier;
        const metricFilter = getMetricFilter(props.selectedMetric, new FilterValueMapping("Yes", "Yes", ["1"]), { [entityKey]: [entityInstance.id] });
        const index = props.selectedFilters?.findIndex(f => ArrayHelper.isEqual(f.entityInstances[entityKey], [entityInstance.id])) ?? -1;
        const newFilters = [...props.selectedFilters ?? []];
        if (index >= 0) {
            newFilters.splice(index, 1);
        } else {
            newFilters.push(metricFilter);
        }

        props.setSelectedFilters(newFilters);
    }

    const selectSingleValue = (metricFilter: MetricFilterState) => {
        props.setSelectedFilters([metricFilter]);

        if (props.onApply) {
            props.onApply([metricFilter]);
        }
    }

    const selectNone = () => {
        props.setSelectedFilters([]);

        if (props.onApply) {
            props.onApply([]);
        }
    }

    const getSelectNoneItem = () => {
        if (!props.selectNoneText) {
            return null;
        }

        const isSelected = props.selectedFilters?.length === 0;

        return (
            <>
                <button className="dropdown-item" onClick={selectNone}>
                    <div className="selected-filter-icon">
                        {isSelected ? <i className="material-symbols-outlined">done</i> : ''}
                    </div>
                    <span className={`title ${isSelected ? 'selected' : ''}`}>{props.selectNoneText}</span>
                </button>
                <DropdownItem divider />
            </>
        )
    }

    const getFilterInstanceCheckboxes = () => {
        let filterValueMapping = props.selectedMetric.filterValueMapping;
        // Sort by first value in values array
        filterValueMapping = filterValueMapping.sort((a, b) => {
            const aVal = a.values && a.values.length > 0 ? Number(a.values[0]) : 0;
            const bVal = b.values && b.values.length > 0 ? Number(b.values[0]) : 0;
            return aVal - bVal;
        });

        if (filterValueMapping.length <= 2) {
            return <div className="filter-instances">
                {getSelectNoneItem()}
                {filterValueMapping.map((filter, i) => {
                    const id = `${props.id}:filter${i}:${filter.fullText}`;
                    const metricFilter = getMetricFilter(props.selectedMetric, filter, {});
                    const isSelected = props.selectedFilters?.some(f => filtersAreEqual(f, metricFilter)) ?? false;

                    //Note - the "done" icon is hidden for the AddFilterButton component using css
                    return (
                        <button key={id} className="dropdown-item" onClick={() => selectSingleValue(metricFilter)}>
                            <div className="selected-filter-icon">
                                {isSelected ? <i className="material-symbols-outlined">done</i> : ''}
                            </div>
                            <span className={`title ${isSelected ? 'selected' : ''}`} title={filter.fullText}>{filter.fullText}</span>
                        </button>
                    )
                })}
            </div>
        }

        return <div className="filter-instances many">
            {getSelectNoneItem()}
            {filterValueMapping.map((filter, i) => {
                const id = `${props.id}:filter${i}:${filter.fullText}`;
                const metricFilter = getMetricFilter(props.selectedMetric, filter, {});
                const checked = props.selectedFilters?.some(f => filtersAreEqual(f, metricFilter)) ?? false;
                return (
                    <div className="filter-instance-checkbox" key={id}>
                        <input type="checkbox" className="checkbox" id={id} checked={checked} onChange={() => toggleFilterValueMapping(filter)} />
                        <label className="filter-instance-label" htmlFor={id} title={filter.fullText}>
                            {filter.fullText}
                        </label>
                    </div>
                );
            })}
        </div>
    };

    const getEntityInstanceCheckboxes = () => {
        if (!canPickEntityInstance) {
            throw new Error("Only multiple choice should pick an entityInstance");
        }
        const entityType = props.selectedMetric.entityCombination[0];
        let entityInstances = entityConfiguration.getAllEnabledInstancesForTypeOrdered(entityType);
        // Sort by id
        entityInstances = [...entityInstances].sort((a, b) => Number(a.id) - Number(b.id));
        return <div className="filter-instances many">
            {getSelectNoneItem()}
            {entityInstances.map((instance, i) => {
                const id = `${props.id}:entityInstance${i}:${instance.name}`;
                const checked = props.selectedFilters?.some(f => f.entityInstances[entityType.identifier]?.some(x => x == instance.id)) ?? false;
                return (
                    <div className="filter-instance-checkbox" key={id}>
                        <input type="checkbox" className="checkbox" id={id} checked={checked} onChange={() => toggleEntityInstance(entityType, instance)} />
                        <label className="filter-instance-label" htmlFor={id} title={instance.name}>
                            {instance.name}
                        </label>
                    </div>
                );
            })}
        </div>
    }

    const disableApplyButton = () => {
        return !props.selectedFilters || props.selectedFilters.length <= 0
    }

    const showApplyButtons = () => {
        return props.showApplyButtons && (canPickEntityInstance || props.selectedMetric.filterValueMapping.length > 2);
    }

    return (
        <>
            {canPickEntityInstance
                ? getEntityInstanceCheckboxes()
                : getFilterInstanceCheckboxes()
            }
            {showApplyButtons() &&
                <div className="apply-filter-buttons">
                    <button className="modal-button primary-button" disabled={disableApplyButton()} onClick={() => props.onApply && props.onApply(props.selectedFilters)}>Apply</button>
                    <button className="modal-button secondary-button" onClick={() => props.close && props.close()}>Cancel</button>
                </div>
            }
        </>
    );
}

export default FilterValueMappingSelector;
