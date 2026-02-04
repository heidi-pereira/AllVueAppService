import React from 'react';
import { MetricFilterState } from './metricFilterState';
import { IGoogleTagManager } from '../googleTagManager';
import { Metric } from '../metrics/metric';
import { metricValidAsFilter } from '../components/visualisations/Reports/Filtering/FilterHelper';
import { PageHandler } from '../components/PageHandler';
import { MixPanel } from '../components/mixpanel/MixPanel';

export type FilterAction =
    | { type: 'UPDATE_FILTER'; data: IFilterData }
    | { type: 'REMOVE_FILTER'; data: { measureName: string }}

export interface IFilterData {
    //We could infer this property from the passed filters (they should all have the same metric) but I included it for clarity
    metricName: string;
    filters: MetricFilterState[];
}

interface IFilterContextState {
    filters: MetricFilterState[];
    metricsValidAsFilter : Metric[];
    filterDispatch: (action: FilterAction) => void;
}

const FilterStateContext = React.createContext<IFilterContextState>({filters: [], metricsValidAsFilter : [], filterDispatch: () => {}});
export const useFilterStateContext = () => React.useContext(FilterStateContext);

interface IFilterStateProviderProps {
    initialFilters: MetricFilterState[];
    metrics: Metric[];
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    children: React.ReactNode;
}

export const FilterStateProvider = (props: IFilterStateProviderProps) => {
    const [filters, setFilters] = React.useState<MetricFilterState[]>(props.initialFilters);

    React.useEffect(() => {
        setFilters(props.initialFilters);
    }, props.initialFilters);

    const dispatch = (action: FilterAction) => {
        let filterList = [...filters];

        switch (action.type) {
            case 'UPDATE_FILTER':
                const firstIndex = filterList.findIndex(f => f.name === action.data.metricName);
                filterList = filterList.filter(f => f.name !== action.data.metricName);

                let index = firstIndex >= 0 ? firstIndex : filterList.length;
                action.data.filters.forEach(filter => {
                    filterList.splice(index, 0, filter);
                    index++;
                });

                setFilters(filterList);
                props.googleTagManager.addEvent("applyFilter", props.pageHandler);
                MixPanel.track("filterAdded");
                break;
            case 'REMOVE_FILTER':
                filterList = filterList.filter(f => f.name !== action.data.measureName);
                setFilters(filterList);
                props.googleTagManager.addEvent("removeFilter", props.pageHandler);
                MixPanel.track("filterRemoved");
                filterList.length === 0 && MixPanel.track("allFiltersRemoved");
                break;
            default:
                throw new Error("Unsupported action type");
        }
    };

    const metricsWithoutAppliedFilters = props.metrics
        .filter(m => metricValidAsFilter(m))
        .filter(m => !filters.find(f => f.name === m.name));

    return (
        <FilterStateContext.Provider value={{filters, metricsValidAsFilter : metricsWithoutAppliedFilters, filterDispatch: dispatch}}>
            {props.children}
        </FilterStateContext.Provider>
    );
};