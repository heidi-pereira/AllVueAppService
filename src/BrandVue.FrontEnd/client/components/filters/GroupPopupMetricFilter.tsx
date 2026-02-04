import React from "react";
import { PageHandler } from "../PageHandler";
import { FilterTitle } from "./FilterComponents";
import { IGoogleTagManager } from "../../googleTagManager";
import MultiMetricSelector from "./MultiMetricSelector";
import OptionSelector from "./OptionSelector";
import BrandSelector from "./BrandSelector";
import RangeFilter from "./RangeFilter";
import {GroupFilterConfiguration} from "../../filter/GroupFilterConfiguration";
import {EntityInstance} from "../../entity/EntityInstance";
import {IFilterState} from "../../filter/metricFilterState";

interface IGroupPopupMetricFilter {
    pageHandler: PageHandler,
    googleTagManager: IGoogleTagManager,
    groupFilterName: string,
    groupFilterStates: GroupFilterConfiguration[],
    allBrands: EntityInstance[],
    updateGroupedMetricFilters: (groupName: string, metricNames: string[], filterState: IFilterState) => void
}

export const GroupPopupMetricFilter = (props: IGroupPopupMetricFilter) => {
    const getEntityTypeIdentifier = () => props.groupFilterStates[0].metric.entityCombination[0]?.identifier ?? "";    
    const getBrandId = () => {
        return props.groupFilterStates.find(mf => mf.state.entityInstances[getEntityTypeIdentifier()]?.length)?.state.entityInstances[getEntityTypeIdentifier()][0] ?? null;
    }
    
    const [brandSelected, setBrandSelected] = React.useState<number | null>(getBrandId());
    
    const isFilterEnabled = props.groupFilterStates.some(x=>x.isEnabled());

    const firstMetricFilter = props.groupFilterStates[0];
    
    
    const getAssignedState = (): IFilterState => {
        return props.groupFilterStates.find(mf => mf.state.values?.length && mf.metric.entityCombination.length > 0)?.state ?? {
            values: [],
            invert: false,
            treatPrimaryValuesAsRange: false,
            entityInstances: brandSelected ? { [getEntityTypeIdentifier()]: [brandSelected]} : {}
        } as IFilterState;
    }
    
    const getActiveOptions = () => {
        return (props.groupFilterStates.length == 1 ? props.groupFilterStates : props.groupFilterStates.filter(x=>x.state.values.length)).map(x=>x.name);
    }
    
    const clearFilter = () => {
        props.googleTagManager.addEvent("clearFilter", props.pageHandler, { value: firstMetricFilter.metric.filterGroup || firstMetricFilter.metric.name });
        props.updateGroupedMetricFilters(props.groupFilterName, props.groupFilterStates.map(x=>x.name), { values: [], entityInstances: {}, invert: false, treatPrimaryValuesAsRange: false });
    }

    const changeBrand = (brandId: string) => {
        setBrandSelected(parseInt(brandId));
        props.updateGroupedMetricFilters(props.groupFilterName, getActiveOptions(), { ...getAssignedState(), entityInstances: {brand: [parseInt(brandId)]}});
    }

    const changeMetricOption = (metrics: string) => {
        props.updateGroupedMetricFilters(props.groupFilterName, metrics.split(','), { ...getAssignedState(), values:[1] });
    }

    const changeFilterMappingOption = (option: string) => {
        props.updateGroupedMetricFilters(props.groupFilterName, getActiveOptions(), { ...getAssignedState(), ...PageHandler.getValuesFromFilterString(option)})
    }
    
    const changeMinMaxOption = (min: number, max: number) => {
        props.updateGroupedMetricFilters(props.groupFilterName, getActiveOptions(), { ...getAssignedState(), values: [min, max], treatPrimaryValuesAsRange: true });
    }

    return (
        <div className="row mb-2">
            <div className="col-sm-4 pr-0">
                <FilterTitle metricName={firstMetricFilter.metric.filterGroup || firstMetricFilter.metric.name} enabled={isFilterEnabled} clearFilter={clearFilter} />
            </div>
            <div className="col mt-1 mt-sm-0">
                {firstMetricFilter.metric.isBrandMetric() ?
                    <BrandSelector disabled={!isFilterEnabled} brands={props.allBrands} changeBrand={
                        changeBrand} chosenBrand={getBrandId() ?? brandSelected ?? EntityInstance.AllInstancesId} /> : null
                }
                {props.groupFilterStates.length === 1 ?
                    firstMetricFilter.isRange ?
                    <RangeFilter metric={firstMetricFilter.metric} changeOption={changeMinMaxOption} value={props.groupFilterStates[0].state.values} /> :
                    <OptionSelector metric={firstMetricFilter.metric} changeOption={changeFilterMappingOption
                        } option={props.groupFilterStates[0].value()} /> : null
                }
                {props.groupFilterStates.length > 1 &&
                    <MultiMetricSelector metrics={props.groupFilterStates.map(mf => mf.metric)} changeOption={
                        changeMetricOption} option={props.groupFilterStates.filter(x=>x.state.values?.length).map(x=>x.name).join(',')} />
                }
            </div>
        </div>
    );
}