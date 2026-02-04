import {filter} from "../../filter/filter";
import {IFilterState, IFilterStateCondensed, MetricFilterState} from '../../filter/metricFilterState';
import React from "react";
import {ReactElement, useEffect, useState} from "react";
import { UncontrolledAlert, Collapse, Modal, ModalBody, ModalHeader} from 'reactstrap';
import {FilterPopupMetricFilter} from "./FilterPopupMetricFilter";
import {FilterPopupBrandMetric} from "./FilterPopupBrandMetric";
import {IQueryStringParam, useReadVueQueryParams, useWriteVueQueryParams} from "../helpers/UrlHelper";
import { IGoogleTagManager } from "../../googleTagManager";
import {filterSet} from "../../filter/filterSet";
import {viewBase} from "../../core/viewBase";
import {MetricSet} from "../../metrics/metricSet";
import {PageHandler} from "../PageHandler";
import {IEntityConfiguration} from "../../entity/EntityConfiguration";
import {useEntityConfigurationStateContext} from "../../entity/EntityConfigurationStateContext";
import {GroupPopupMetricFilter} from "./GroupPopupMetricFilter";
import {GroupFilterConfiguration} from "../../filter/GroupFilterConfiguration";
import { MixPanel } from "../mixpanel/MixPanel";
import {ArrayHelper} from "../helpers/ArrayHelper";
import {useLocation, useNavigate, Location} from "react-router-dom";
import { mock } from "jest-mock-extended";
export type FilterPopupBrandValue =
    {
        name: string;
        filter: filter;
        values: string[];
        description: string | undefined;
    };

export type FilterPopupMetric =
    {
        name: string;
        initialValues: string;
        metricFilterState: MetricFilterState;
    };

export type GroupOrAdvancedFilterPopupMetric = {
    name: string;
    metricFilterState?: MetricFilterState;
    groupFilterState?: GroupFilterConfiguration[];
}

const initializeMetricFilters = (props: IFilterPopupParams, location: Location): MetricFilterState[] => {
    var groupFilters = props.pageHandler.getGroupMetricFilters(props.metrics, props.entityConfiguration, location)
    return props.pageHandler.getMetricFilters(props.metrics, location).filter(mf=> mf.metric.filterGroup == null || !groupFilters.some(gf=> gf.metric.filterGroup === mf.metric.filterGroup));
}

const initializeGroupMetricFilters = (props: IFilterPopupParams, location: Location): GroupFilterConfiguration[] => {
    return props.pageHandler.getGroupMetricFilters(props.metrics, props.entityConfiguration, location);
}

const filterValues = (f: string, getQueryParameter: <T = string>(parameterName: string, defaultValue?: T) => T | undefined): string[] => {
    const filterValue = getQueryParameter(f);
    let values: string[] = [];

    if (filterValue !== undefined) {
        values = Array.isArray(filterValue) ? filterValue : [filterValue];
    }
    return values;
}

const createRequiredFilterValues = (props: IFilterPopupParams, filters: filter[], getQueryParameter: <T = string>(parameterName: string, defaultValue?: T) => T | undefined) => {
    let map: FilterPopupBrandValue[] = [];
    let curatedFilters = props.activeView.curatedFilters;
    filters.forEach(x => {
        const description = curatedFilters.filterDescriptions[x.name];
        const item: FilterPopupBrandValue = { name: x.name, filter: x, values: filterValues(x.name, getQueryParameter), description: description == undefined ? undefined : description.filter };
        map.push(item);
    });
    return map;
}

const createGroupedFilterMetricValues = (props:IFilterPopupParams, location: Location): {[name: string] : GroupFilterConfiguration[]} => {
    let map: {[name: string] : GroupFilterConfiguration[]} = {};
    initializeGroupMetricFilters(props, location).forEach(x => {
        const name = x.metric.filterGroup || x.name;
        if (name in map) {
            map[name].push(x)
        } else {
            map[name] = [x];
        }
    });
    return map;
}


const createFilterMetricValues = (props:IFilterPopupParams, location: Location) => {
    let filterMetricValues: FilterPopupMetric[] = [];
    initializeMetricFilters(props, location).forEach(x => {
        const item: FilterPopupMetric = { name: x.name, initialValues: x.valueToString(), metricFilterState: x };
        if(!filterMetricValues.some(x=>x.name == item.name)) {
            filterMetricValues.push(item);
        }
    });
    return filterMetricValues;
}

interface IFilterPopupParams {
    filters: filterSet;
    activeView: viewBase;
    metrics: MetricSet;
    entityConfiguration: IEntityConfiguration;
    pageHandler: PageHandler;
    googleTagManager: IGoogleTagManager;
}

export const FilterPopup = (props: IFilterPopupParams) : ReactElement => {
    const [modal, setModal] = useState(false);
    const [advancedSectionOpen, setAdvancedSectionOpen] = useState(false);
    const [demographicFilterValues, setDemographicFilterValues] = useState<FilterPopupBrandValue[]>([]);
    const [filterMetricValues, setFilterMetricValues] = useState<FilterPopupMetric[]>([])
    const [groupedFilterMetricValues, setGroupedFilterMetricValues] = useState<{[name: string] : GroupFilterConfiguration[]}>({})
    const { entityConfiguration } = useEntityConfigurationStateContext();
    
    const getRequiredFilters = (): filter[] => {
        const filters = ["Gender", "Region", "Seg"];
        const lookup = props.filters.filterLookup;
        return filters.filter(x=>lookup[x] != null).map(x=>lookup[x]);
    };
    const location = useLocation();
    const navigate = useNavigate();
    const { getQueryParameter } = useReadVueQueryParams();
    const { setQueryParameters } = useWriteVueQueryParams(navigate, location);
    useEffect(() => {
        setDemographicFilterValues(createRequiredFilterValues(props, getRequiredFilters(), getQueryParameter))
        setFilterMetricValues(createFilterMetricValues(props, location))
        setGroupedFilterMetricValues(createGroupedFilterMetricValues(props, location))
    }, [JSON.stringify(props.activeView.curatedFilters), location])

    const toggleAdvanced = () => {
        setAdvancedSectionOpen(!advancedSectionOpen);
    }

    const updateBrandValues = (filterName: string, values: string[]) => {
        const currentBrandFilters = [...demographicFilterValues]
        const brandFilter = currentBrandFilters.find(f => f.name === filterName)!

        const isEnabled = values !== undefined && values.length > 0;
        const currentValues = values.length !== 0 ? values : brandFilter!.filter.filterItems.map(i => i.idList.join(","));
        const description = isEnabled ? brandFilter.filter.filterItems.filter(f => currentValues.indexOf(f.idList.join(",")) >= 0).map(f => f.caption).join(", ") : undefined

        let hasChanged = values.length !== brandFilter.values.length || description !== brandFilter.description

        if (hasChanged) {
            brandFilter.values = values;
            brandFilter.description = description;
            setDemographicFilterValues(currentBrandFilters)
        }
    }
    
    const getQuery = (filterState:IFilterState): string => {
        const compressed: IFilterStateCondensed = {
            v:filterState.values,
            i:filterState.invert,
            e:filterState.entityInstances,
            r:filterState.treatPrimaryValuesAsRange
        }
        if (filterState.values?.length) {
            return JSON.stringify(compressed);
        } else {
            return "";
        }
    }

    const getLegacyQuery = (filter:GroupFilterConfiguration) : string => {
        if (filter.metric.entityCombination.length == 0) {
            return '-1.'+filter.value();
        } else {
            return `${filter.state.entityInstances[filter.metric.entityCombination[0].identifier]}.${filter.value()}`
        }
    }

    const updateGroupMetricFiltersState = (groupName: string, metricNames: string[], filterState: IFilterState) => {
        const updatedValues = {...groupedFilterMetricValues, [groupName]: groupedFilterMetricValues[groupName].map(x=> {
            const newGroupFilter = Object.assign(new GroupFilterConfiguration(), { ...x })
            if( metricNames.includes(x.name)) {
                newGroupFilter.state = {...filterState} //set values on selected
            } else {
                newGroupFilter.state.values = []//clear the values on unselected filters;
            }
            return newGroupFilter;
        })};
        setGroupedFilterMetricValues(updatedValues);
    }
    
    const updateMetricFiltersState = (name: string, filterState: IFilterState) => {
        let hasChanged = false;
        const updatedFilters: FilterPopupMetric[] = []
        filterMetricValues.forEach(filter => {
            if (filter.name === name) {                
                if (!ArrayHelper.isEqual(filter.metricFilterState.values ?? [], filterState.values ?? []) || 
                    filter.metricFilterState.entityInstances != filterState.entityInstances ||
                    filter.metricFilterState.treatPrimaryValuesAsRange != filter.metricFilterState.treatPrimaryValuesAsRange ||
                    filter.metricFilterState.invert != filterState.invert
                ) {
                    let newState = new MetricFilterState(filter.metricFilterState, filterState);
                    
                    updatedFilters.push({...filter, metricFilterState: newState});
                    hasChanged = true;
                } else {
                    updatedFilters.push(filter)
                }
            } else {
                updatedFilters.push(filter)
            }
        })
        if (hasChanged)
            setFilterMetricValues(updatedFilters)
    }

    const applyFiltersAndCloseDialog = () => {
        const currentFilterBrands = [...demographicFilterValues];
        currentFilterBrands.forEach(filter => {
            if (filter.values == null || filter.description == undefined) {
                props.activeView.curatedFilters.update(filter.name, filter.filter.getDefaultValue(), undefined);
            } else {
                props.activeView.curatedFilters.update(filter.name, filter.values, { name: filter.name, filter: filter.description });
            }
        });
        const params = paramsForFilters();
        const groupedParams = paramsForLegacyFilters();
        setQueryParameters([...params, ...groupedParams]);
        toggle(true);
    }

    const paramsForFilters = () => {
        var params: IQueryStringParam[] = filterMetricValues.flatMap(filter => {
            if (filter.metricFilterState.isEnabled()) {
                props.activeView.curatedFilters.updateMeasureFilter(filter.name,
                    filter.metricFilterState,
                    { name: filter.name, filter: filter.metricFilterState.description(filter.metricFilterState.entityInstances, filter.metricFilterState.valueToString(), entityConfiguration) });
                return [{ name: "f" + filter.name, value: getQuery(filter.metricFilterState)}, {name: filter.name, value: ""}];
                
            } else {
                props.activeView.curatedFilters.removeMeasureFilter(filter.name);
                return [{ name: "f" + filter.name, value: "" }];
            }
        });
        return params;
    }
    
    const paramsForLegacyFilters = () => {
        var params: IQueryStringParam[] = Object.entries(groupedFilterMetricValues).flatMap(group => group[1].map(filter => {
                if (filter.isEnabled()) {
                    const entityType = filter.metric.entityCombination[0]?.identifier;
                    props.activeView.curatedFilters.updateMeasureFilter(filter.name,
                        filter.state,
                        { name: filter.name, filter: filter.description(entityType ? entityConfiguration.getEnabledInstancesById(entityType, filter.state.entityInstances[entityType])[0] : null, filter.value()) });
                    return { name: filter.name, value: getLegacyQuery(filter)};
    
                } else {
                    props.activeView.curatedFilters.removeMeasureFilter(filter.name);
                    return {name: filter.name, value: ""};
                }}));
        var demographicParams: IQueryStringParam[] = demographicFilterValues.map(x=> {
            if (x.values) {
                return {name: x.name, value: x.values}
            } else {
                return {name: x.name, value: []}
            }
        });
        return [...params,...demographicParams];
    }
    
    const closeDialog = () => {
        toggle();
    }

    const toggle = (applyingChanges?: boolean) => {
        if (!modal) {    //When Opening Dialog box then refresh the state
            props.googleTagManager.addEvent("openFilterDialog", props.pageHandler);
            MixPanel.track("filtersOpened");
        } else {
            if (!applyingChanges) {
                props.googleTagManager.addEvent("closeFilterDialog", props.pageHandler);
                MixPanel.track("closeFilterDialog");
            } else {
                props.googleTagManager.addEvent("applyFilter", props.pageHandler);
                MixPanel.track("filtersApplied");
            }
        }
        setModal(!modal)
    }
        
    const advancedFiltersSortedAlphabetically = (): GroupOrAdvancedFilterPopupMetric[] => {
        const unsortedMapOfGroupFilters: GroupOrAdvancedFilterPopupMetric[] = Object.entries(groupedFilterMetricValues)
            .filter(([_, filterMetrics]) => filterMetrics[0].isAdvanced)
            .map(x=> ({name: x[0], groupFilterState: x[1]}));
        const unsortedMapOfAdvancedFilters: GroupOrAdvancedFilterPopupMetric[] = filterMetricValues.filter((filterMetrics) => filterMetrics.metricFilterState.isAdvanced && !unsortedMapOfGroupFilters.some(x => x.name == filterMetrics.name));
        return [
            ...unsortedMapOfAdvancedFilters, 
            ...unsortedMapOfGroupFilters].sort((key1, key2) => key1.name.localeCompare(key2.name));
    }

    return (
        <span className="filterPopup">
            <button onClick={() => toggle()} id="filterToggleButton" className="hollow-button not-exported">
                <i className="material-symbols-outlined">tune</i>
                <div>Filters</div>
            </button>
            <Modal isOpen={modal} toggle={() => toggle()} className="right filterPopup" style={{ width: "900px" }}>
                <ModalHeader style={{ width: "100%" }}>
                    <div className="icon-title">
                        <span><i className="material-symbols-outlined">tune</i></span>
                        <span>Filter data</span>
                    </div>
                    <div>
                        <button type="button" data-bs-dismiss="modal" onClick={applyFiltersAndCloseDialog} className="me-4 primary-button d-inline">Apply filters</button>
                        <button type="button" className="btn btn-close" onClick={closeDialog}></button>
                    </div>
                </ModalHeader>

                <ModalBody className="p-4">
                    <div className="mt-3">
                        {filterMetricValues.filter((filterMetrics) => !filterMetrics.metricFilterState.isAdvanced).map((filterMetricValue) =>
                            <FilterPopupMetricFilter key={filterMetricValue.name}
                                updateMetricFilters={(filterState: IFilterState) => updateMetricFiltersState(filterMetricValue.name, filterState)}
                                metricFilterState={filterMetricValue.metricFilterState}
                                googleTagManager={props.googleTagManager}
                                pageHandler={props.pageHandler}
                        />)}
                        {demographicFilterValues.map(f =>
                            <FilterPopupBrandMetric key={f.name} 
                                setValues={(values: string[]) => updateBrandValues(f.name, values)} 
                                filterBrandValue={f} 
                                googleTagManager={props.googleTagManager} pageHandler={props.pageHandler} />)}
                    </div>

                    <div className="mt-3 mb-3 clickable seperator" onClick={toggleAdvanced} >
                        <span className="float-end"><i className="material-symbols-outlined align-top">keyboard_arrow_down</i></span>
                        <h4>Advanced</h4>
                    </div>
                    {advancedSectionOpen &&
                        <Collapse isOpen={advancedSectionOpen} timeout={100}>
                            <div className="mb-5">
                                {
                                    advancedFiltersSortedAlphabetically().map((x)=> {
                                            if (x.metricFilterState) {
                                                return <FilterPopupMetricFilter key={x.metricFilterState.name}
                                                    updateMetricFilters={(filterState: IFilterState) => updateMetricFiltersState(x.name, filterState)}
                                                    metricFilterState={x.metricFilterState}
                                                    googleTagManager={props.googleTagManager}
                                                    pageHandler={props.pageHandler}/>
                                            } else {
                                                var entityType = x.groupFilterState?.[0].metric.entityCombination?.[0];
                                                return <GroupPopupMetricFilter key={x.name}
                                                    updateGroupedMetricFilters={(groupName: string, metricNames: string[], filterState: IFilterState) => updateGroupMetricFiltersState(groupName, metricNames, filterState)}
                                                    groupFilterName={x.name}
                                                    groupFilterStates={x.groupFilterState!}
                                                    allBrands={entityType ? entityConfiguration.getAllEnabledInstancesForType(entityType) : []}
                                                    googleTagManager={props.googleTagManager}
                                                    pageHandler={props.pageHandler}/>
                                            }
                                        }
                                    )
                                    
                                }
                            </div>
                        </Collapse>
                    }
                    <UncontrolledAlert color="info" className="p-3">    
                        <div className="d-flex">
                            <p className="me-2">
                                <i className="material-symbols-outlined">info</i>
                            </p>
                            <p>
                                Filters can be applied to any report or chart and will stay with you as you explore the dashboard.<br />
                                As you apply filters the sample size will reduce because you are looking at a subset of your data.
                            </p>
                        </div>                   
                    </UncontrolledAlert>              
                </ModalBody>
            </Modal>
        </span>
    );
}
