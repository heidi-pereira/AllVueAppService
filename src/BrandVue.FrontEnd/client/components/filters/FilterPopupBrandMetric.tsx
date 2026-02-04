import * as PageHandler from "../PageHandler";
import React from "react";
import {FilterTitle} from "./FilterComponents";
import { IGoogleTagManager } from "../../googleTagManager";
import {FilterPopupBrandValue} from "./FilterPopup";

interface IFilterPopupBrandMetricProps { 
    filterBrandValue: FilterPopupBrandValue,
    setValues: (values: string[]) => void,
    googleTagManager: IGoogleTagManager, 
    pageHandler: PageHandler.PageHandler,
}

export const FilterPopupBrandMetric = (props: IFilterPopupBrandMetricProps) => {
    const isEnabled = props.filterBrandValue.values !== undefined && props.filterBrandValue.values.length > 0;
    
    const change = (e) => {
        const currentValues = [...props.filterBrandValue.values];
        if (e.target.checked) {
            currentValues.push(e.target.value);
        } else {
            currentValues.splice(currentValues.indexOf(e.target.value), 1);
        }
        props.setValues(currentValues)
    }

    const clearFilter = () => {
        props.googleTagManager.addEvent("clearFilter", props.pageHandler, { value: props.filterBrandValue.name });
        props.setValues([])
    }

    if (props.filterBrandValue.filter.filterItems.length > 1) {
        return (
            <div id={"filter-" + props.filterBrandValue.name} className="row mb-2">
                <div className="col-sm-4 pr-0">
                    <FilterTitle metricName={props.filterBrandValue.name} enabled={isEnabled} clearFilter={clearFilter}/>
                </div>
                <div className="col pt-2">
                    {props.filterBrandValue.filter.filterItems.map(fi =>
                        <div key={fi.caption} className="form-check form-check-inline">
                            <label className={`label--checkbox-radio ${isEnabled ? "" : "disabled"}`}>
                                {fi.caption}
                                <input
                                    type="checkbox"
                                    onChange={change}
                                    className="input input--checkbox"
                                    name={props.filterBrandValue.filter.field}
                                    checked={props.filterBrandValue.values.indexOf(fi.idList.join(",")) >= 0}
                                    value={fi.idList.join(",")}/>
                            </label>
                        </div>
                    )}
                </div>
            </div>
        )
    }
    return null
}