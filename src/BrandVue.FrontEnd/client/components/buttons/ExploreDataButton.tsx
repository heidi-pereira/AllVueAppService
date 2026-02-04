import React from "react";
import { PageDescriptor } from "../../BrandVueApi";
import { Metric } from "../../metrics/metric";
import { getPathByPageName, constructQueryStringIncludeEntitySet, QueryStringParamNames } from "../helpers/UrlHelper";
import { MixPanel } from "../mixpanel/MixPanel";
import { useNavigate } from "react-router-dom";
import { selectEntitySelectionState } from "client/state/entitySelectionSelectors";
import { useAppSelector } from "client/state/store";
import { PageHandler } from "../PageHandler";

interface IExploreDataButtonProps {
    metrics: Metric[];
    crosstabPage: PageDescriptor | undefined;
    pageHandler: PageHandler;
}

const ExploreDataButton = (props: IExploreDataButtonProps) => {
    const navigate = useNavigate();
    const entitySelectionSlice = useAppSelector(selectEntitySelectionState);
    const timeSelectionSlice = useAppSelector((state) => state.timeSelection);
    const navigateToCrosstabPage = (e: any, urlSafeMetricName: string) => {
        MixPanel.track("exploreDataSelected");
        if (props.crosstabPage) {
            const crosstabPageUrl = getPathByPageName(props.crosstabPage.name);
            const urlParameters = constructQueryStringIncludeEntitySet(
                window.location.search,
                [{ name: QueryStringParamNames.urlSafeMetricName, value: urlSafeMetricName }],
                entitySelectionSlice,
                timeSelectionSlice,
                props.pageHandler
            );
            const crosstabPageUrlWithParameters = crosstabPageUrl.concat(urlParameters);
            navigate(crosstabPageUrlWithParameters);
        }
    };

    const metric = props.metrics[0];
    if (metric) {
        return (
            <button onClick={(e) => navigateToCrosstabPage(e, metric.urlSafeName)} className="hollow-button not-exported" id="exploreDataButton">
                <i className="material-symbols-outlined">grid_on</i>
                <div>Explore data</div>
            </button>
        );
    }

    return <></>;
};

export default ExploreDataButton;
