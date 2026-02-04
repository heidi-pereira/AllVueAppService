import highchartsGroupedCategories from "highcharts-grouped-categories";
import React from "react";
import { useState } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { useResizeDetector } from 'react-resize-detector';

require('highcharts/highcharts-more')(Highcharts);
highchartsGroupedCategories(Highcharts);

import { PageHandler } from "../PageHandler";
import { GlobalContext, GlobalParameters } from "../../GlobalContext";
import { useReadVueQueryParams, useWriteVueQueryParams } from "../helpers/UrlHelper";
import { IGoogleTagManager } from "../../googleTagManager";
import { MixPanel } from "../mixpanel/MixPanel";
import {useLocation, useNavigate} from "react-router-dom";

interface Props {
    config: any;
    legendVisibilityChanged?: (id: string, toggled: boolean) => void;
    afterRender?: (chart: any) => void;
    googleTagManager: IGoogleTagManager;
}

const AugmentedReactHighCharts  = (props: Props) => (
    <GlobalContext.Consumer>
        {(i: GlobalParameters) => (<AugmentedReactHighChartsInternal {...props} pageHandler={i.pageHandler!} />)}
    </GlobalContext.Consumer>
);

export default AugmentedReactHighCharts;

interface AugmentedReactHighChartsInternalProps extends Props {
    pageHandler: PageHandler;
}

const AugmentedReactHighChartsInternal = ({ config, legendVisibilityChanged, afterRender, pageHandler, googleTagManager }: AugmentedReactHighChartsInternalProps) => {
    const legendQueryParameterName = "Legend";
    const location = useLocation();
    const hiddenLegendIdsFromUrl = pageHandler.getQueryStringParameter(legendQueryParameterName, location) || [];    
    const [hiddenLegendIds, setHiddenLegendIdsInState] = useState(hiddenLegendIdsFromUrl);
    const { height, ref } = useResizeDetector<HTMLDivElement>({ refreshMode: 'debounce', refreshRate: 100 });
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());
    const { getQueryParameter } = useReadVueQueryParams();
    const getNewHiddenLegendIds = (legendIdToToggle: string) : string[] => {
        const newHiddenLegendIds = [ ...hiddenLegendIds ];
        if (newHiddenLegendIds.indexOf(legendIdToToggle) >= 0) {
            newHiddenLegendIds.splice(newHiddenLegendIds.indexOf(legendIdToToggle), 1);
            MixPanel.track("toggleOnLegend");
        } else {
            newHiddenLegendIds.push(legendIdToToggle);
            MixPanel.track("toggleOffLegend");
        }
        return newHiddenLegendIds;
    }

    const legendItemClick = e => {
        const canToggle = e.target.options.canToggle == null || e.target.options.canToggle === true;
        if (!canToggle) {
            // Returning false prevents the default behaviour of this event whis is to hide the series.
            return false;
        }

        const eventName = e.target.options.visible ? "toggleOnLegend" : "toggleOffLegend";
        const fullName = (e.target.options.fullName || e.target.name);
        const legendIdToToggle = (e.target.options.id || fullName).toString();
        googleTagManager.addEvent(eventName, pageHandler, { value: `${fullName || legendIdToToggle}` });
        
        const newHiddenLegendIds = getNewHiddenLegendIds(legendIdToToggle);
        
        setHiddenLegendIdsInState(newHiddenLegendIds);
        setQueryParameter(legendQueryParameterName, newHiddenLegendIds);
        if (legendVisibilityChanged) {
            legendVisibilityChanged(legendIdToToggle, newHiddenLegendIds.indexOf(legendIdToToggle)>=0);
        }
        return false;
    }

    const options = { ...config };
    // Deep clone everything and make sure fields are initialised at least to empty objects
    options.chart = { ...options.chart };
    options.plotOptions = { ...options.plotOptions };
    options.plotOptions.series = { ...options.plotOptions.series };
    options.plotOptions.series.events = { ...options.plotOptions.series.events };
    options.xAxis = { ...options.xAxis };
    options.xAxis.labels = { ...options.xAxis.labels };

    // If categories have been set - clone, otherwise leave as undefined which is the expected default.
    options.xAxis.categories = options.xAxis.categories ? [ ...options.xAxis.categories ] : undefined;

    if (options.xAxis.categories && options.xAxis.categories.length && options.xAxis.categories[0].categories) {

        //config.xAxis.tickWidth = 0; // Can be used to control border
        options.xAxis.labels.groupedOptions =
        [
            {
                style: {
                    fontWeight: "bold"
                }
            }
        ];
    }

    options.plotOptions.series.events.legendItemClick = legendItemClick;
    options.plotOptions.series.turboThreshold = 0;

    options.series.forEach(s => {
        const fullName = (s.fullName || s.name);
        const id = ((s.id || fullName) || "").toString();
        s.visible = (s.defaultVisibility == undefined ? true : s.defaultVisibility);
        if (hiddenLegendIds.indexOf(id) >= 0) {
            s.visible = !s.visible;
        }
    });

    if (options.legend) {
        const isExportImage = getQueryParameter<string>("appMode") === "export-image";
        options.legend = { ...options.legend, navigation: { enabled: !isExportImage }, alignColumns: false }
        options.legend.itemStyle = { ...options.legend.itemStyle, fontSize: 11 }
    }

    const optionsWithHeight = { ...options };
    optionsWithHeight.chart.height = height;

    return (
        <div className="chart-container" ref={ref}>
            <HighchartsReact highcharts={Highcharts} options={optionsWithHeight} callback={afterRender} immutable={true} />
        </div>
    );
}