import { dsession } from "../../dsession";
import React from "react";
import {useLocation, useNavigate} from 'react-router-dom';
import { CancelFilter } from "./FilterComponents";
import { IGoogleTagManager } from "../../googleTagManager";
import { MetricSet } from "../../metrics/metricSet";
import { IEntityConfiguration } from "../../entity/EntityConfiguration";
import { MixPanel } from "../mixpanel/MixPanel";
import {useWriteVueQueryParams} from "../helpers/UrlHelper";

const FilterDescription = (props: { session: dsession, enabledMetricSet: MetricSet, entityConfiguration: IEntityConfiguration, googleTagManager: IGoogleTagManager }) => {
    const location = useLocation()
    const { setQueryParameter, setQueryParameters } = useWriteVueQueryParams(useNavigate(), location);
    const clickHandler = () => {
        clearFilterState();
    }

    const clearSpecificFilter = (filterName: string) => {
        setQueryParameter("f"+filterName, "");
        MixPanel.track("filterRemoved");
        setQueryParameter(filterName, "");
        
        props.googleTagManager.addEvent("removeFilter", props.session.pageHandler, { value: filterName });
        
        for (let metricFilter of props.session.pageHandler.getMetricFilters(props.enabledMetricSet, location)) {
            if (filterName === metricFilter.name) {
                props.session.activeView.curatedFilters.removeMeasureFilter(metricFilter.name);
            }
        }

        for (let filter of props.session.filters.filters) {
            if (filterName === filter.name) {
                props.session.activeView.curatedFilters.update(filter.field, filter.getDefaultValue(), undefined);
            }
        }
    }

    const clearFilterState = () => {
        props.googleTagManager.addEvent("removeAllFilters", props.session.pageHandler);
        MixPanel.track("allFiltersRemoved");
        props.session.pageHandler.clearFilterState(location, setQueryParameters);
    }

    const filterDescriptions = props.session.pageHandler.getFilterDescriptions(location);
    return (filterDescriptions.length ?
                <div className="filterDescriptions">
                    {filterDescriptions.map((d, i) =>
                        <span key={i} className="filterPair">
                            <span className="filterDescriptionName ps-1">
                                {d.name}
                                {d.filter !== "" && <>:&nbsp;</>}
                            </span>
                            <span className="filterDescriptionValue pe-2">{d.filter}</span>
                            <CancelFilter metricName={d.name} enabled={true} clearFilter={clearSpecificFilter} />
                        </span>
                    )}
                    {filterDescriptions.length > 1 &&
                        <button type="button" className="filterClear not-exported" onClick={clickHandler}>Clear all</button>
                    }
                </div>: <></>
    );
}

export default FilterDescription