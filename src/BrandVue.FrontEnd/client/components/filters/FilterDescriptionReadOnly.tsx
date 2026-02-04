import React from "react";
import { PageHandler } from "../PageHandler";
import {useLocation} from "react-router-dom";

const FilterDescriptionReadOnly = (props: { pageHandler: PageHandler}) => {
    const location = useLocation();
    const filterDescriptions = props.pageHandler.getFilterDescriptions(location);
    if (filterDescriptions.length) {
        return (
            <div>
                <div className="filterDescriptions">
                    {filterDescriptions.map((d, i) =>
                        <span key={i} className="filterPair">
                            <span className="filterDescriptionName ps-1">{d.name}:&nbsp;</span>
                            <span className="filterDescriptionValue pe-2">{d.filter}</span>

                        </span>
                    )}
                </div>
            </div>
        );
    }
    return null;
}

export default FilterDescriptionReadOnly