import React from "react";
import { PageHandler } from "./PageHandler";
import * as BrandVueApi from "../BrandVueApi";
import { saveAs } from "file-saver";
import { constructQueryStringIncludeEntitySet, QueryStringParamNames } from "./helpers/UrlHelper";
import Tooltip from "./Tooltip";
import { IGoogleTagManager } from "../googleTagManager";
import { PageDescriptor } from "../BrandVueApi";
import { useCallback } from "react";
import { useLocation } from "react-router-dom";
import { getViewTypeEnum } from "./helpers/ViewTypeHelper";
import { useState } from "react";
import { useAppSelector } from "../state/store";
import { selectEntitySelectionState } from "client/state/entitySelectionSelectors";
import { selectSubsetId } from "client/state/subsetSlice";

interface ISaveChartProps {
    googleTagManager: IGoogleTagManager;
    activeDashPage: PageDescriptor;
    pageHandler: PageHandler;
    coreViewType: number;
    label?: string;
}

export default function SaveChart(props: ISaveChartProps) {
    const [disabled, setDisabled] = useState(false);
    const buttonLabel = props.label ?? "Save chart";
    const location = useLocation();
    const entitySelectionState = useAppSelector(selectEntitySelectionState);
    const timeSelectionState = useAppSelector((state) => state.timeSelection);
    const subsetId = useAppSelector(selectSubsetId);

    const onclick = useCallback(() => {
        props.googleTagManager.addEvent("downloadChart", props.pageHandler);
        setDisabled(true);

        const currentSearch = location.search;
        const values = [{ name: QueryStringParamNames.subset, value: subsetId }];

        let queryString = constructQueryStringIncludeEntitySet(currentSearch, values, entitySelectionState, timeSelectionState, props.pageHandler);

        BrandVueApi.Factory.DataClient((throwErr) => setDisabled(throwErr))
            .exportChart(
                new BrandVueApi.ExportChartModel({
                    url: window.location.href.split("?")[0] + queryString,
                    name: props.activeDashPage.name,
                    viewType: getViewTypeEnum(props.coreViewType),
                    metrics: props.pageHandler.getDisplayedMetrics(),
                    width: window.innerWidth,
                    height: window.innerHeight,
                    subsetId: subsetId,
                })
            )
            .then((r) => {
                setDisabled(false);
                if (r.data == null || r.data.size == 0) {
                    alert("Failed to capture image");
                } else {
                    const fileName = r.fileName !== undefined ? r.fileName : "export.png";
                    saveAs(r.data, fileName);
                }
            });
    }, [props]);

    return (
        <Tooltip placement="top" title={`${buttonLabel} as PNG`}>
            <button disabled={disabled} id="saveChartButton" className={`hollow-button saveChart ${disabled ? "loading" : ""}`} onClick={onclick}>
                <i className="material-symbols-outlined">image</i>
                <div>{buttonLabel}</div>
            </button>
        </Tooltip>
    );
}
