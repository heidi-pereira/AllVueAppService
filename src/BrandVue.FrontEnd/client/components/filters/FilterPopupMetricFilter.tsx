import React from "react";
import { PageHandler } from "../PageHandler";
import { FilterTitle } from "./FilterComponents";
import { IGoogleTagManager } from "../../googleTagManager";
import OptionSelector from "./OptionSelector";
import RangeFilter from "./RangeFilter";
import FilterEntityInstanceSelector from "./FilterEntityInstanceSelector";
import {useEntityConfigurationStateContext} from "../../entity/EntityConfigurationStateContext";
import {IFilterState, MetricFilterState} from "../../filter/metricFilterState";
import FilterMultipleEntityInstanceSelector from "./FilterMultiEntityInstanceSelector";

interface IFilterPopupMetricFilter {
    pageHandler: PageHandler,
    googleTagManager: IGoogleTagManager,
    metricFilterState: MetricFilterState,
    updateMetricFilters: (filterState: IFilterState) => void
}

export const FilterPopupMetricFilter = (props: IFilterPopupMetricFilter) => {
    const isFilterEnabled = (props.metricFilterState.values && props.metricFilterState.values.length > 0) ?? false;
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const [allowMultipleSelection, setAllowMultipleSelection] = React.useState(true);
    const clearFilter = () => {
        props.googleTagManager.addEvent("clearFilter", props.pageHandler, { value: props.metricFilterState.name });
        const newState = props.metricFilterState.withCleared();
        props.updateMetricFilters(newState);
    }

    const onChangeEntityInstance = (entityInstanceType: string, entityInstanceId: string) => {
        const newState = props.metricFilterState.withInstance(entityInstanceType, entityInstanceId);
        props.updateMetricFilters(newState);
    }

    const onChangeMultipleEntityInstance = (entityInstanceType: string, entityInstanceIds: number[], invert: boolean) => {
        const newState = props.metricFilterState
            .withInstances(entityInstanceType, entityInstanceIds, invert)
            .withConstantValues();
        props.updateMetricFilters(newState);

        // Only one entity type can have multiple selections
        const otherSelections = Object.entries(newState.entityInstances)
            .some(([, values]) => values.length > 1);
        setAllowMultipleSelection(!otherSelections);
    }

    const onChangeOption = (value: string) => {
        const newState = props.metricFilterState.withValues(value, false);
        props.updateMetricFilters(newState);
    }

    const onChangeRange = (min: number, max: number) => {
        const newState = props.metricFilterState.withRange(min, max);
        props.updateMetricFilters(newState);
    }
    const metricFilter = props.metricFilterState;
    const entityCombination = metricFilter.metric.entityCombination;

    // Lots of brandvue filters do this. But we should migrate them to using a variable since they're just netting/renaming options.
    const legacySingleEntityMapping = !metricFilter.isRange && metricFilter.metric.filterValueMapping.length > 1 && entityCombination.length === 1 && !entityCombination[0].isBrand;

    const onChangeLegacyEntityMapping = (values: string) => {
        const arrayValueArguments = MetricFilterState.getArrayValueArgumentsFromString(values, false);
        onChangeMultipleEntityInstance(entityCombination[0].identifier, arrayValueArguments.values, arrayValueArguments.invert);
    }

    const onChangeValue = legacySingleEntityMapping ? onChangeLegacyEntityMapping : onChangeOption;

    const showValuePicker = metricFilter.isRange || legacySingleEntityMapping || metricFilter.metric.entityCombination.every(e => e.isBrand);

    return (
        <div className="row mb-2">
            <div className="col-sm-4 pr-0">
                <FilterTitle metricName={metricFilter.name} enabled={isFilterEnabled} clearFilter={clearFilter} />
            </div>
            <div className="col mt-1 mt-sm-0">
                {
                    metricFilter.metric.entityCombination.map((ec) =>
                        ec.isBrand ? <FilterEntityInstanceSelector key={ec.identifier} entityType={ec} disabled={!isFilterEnabled}
                                    allEntityInstances={entityConfiguration.getAllEnabledInstancesForType(ec)}
                                    onChange={onChangeEntityInstance}
                                    selectedInstanceId={metricFilter.entityInstances[ec.identifier]?.[0]}
                                />
                            : legacySingleEntityMapping ? null : 
                                <FilterMultipleEntityInstanceSelector key={ec.identifier} entityType={ec} disabled={!isFilterEnabled}
                                        allEntityInstances={entityConfiguration.getAllEnabledInstancesForType(ec)}
                                        onChange={(entityTypeId, entityInstances)=>onChangeMultipleEntityInstance(entityTypeId, entityInstances, false)}
                                        selectedInstances={metricFilter.entityInstances[ec.identifier] ?? []}
                                        allowMultipleSelection={metricFilter.metric.filterMulti && (allowMultipleSelection || metricFilter.entityInstances[ec.identifier].length > 1)}
                            />
                    )
                }
                {
                    showValuePicker ?
                        metricFilter.isRange ?
                            <RangeFilter metric={metricFilter.metric} changeOption={onChangeRange} value={metricFilter.values} />
                            : <OptionSelector metric={metricFilter.metric} changeOption={onChangeValue} option={metricFilter.valueToString()} />
                        : null
                }
            </div>
        </div>
    );
}

