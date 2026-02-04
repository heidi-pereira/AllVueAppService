import React from "react";
import { useEffect, useLayoutEffect, useState } from "react";
import { EntityInstance } from "../../../entity/EntityInstance";
import { Metric } from "../../../metrics/metric";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import * as BrandVueApi from "../../../BrandVueApi";
import { IAxisRange } from "../../../BrandVueApi";
import { AxisOptions, Options, XAxisOptions, YAxisOptions } from 'highcharts';
import { ViewHelper } from "../ViewHelper";
import { tickPositioner }  from "../ScatterPlot";
import TileTemplateChart from "./TileTemplateChart";
import { EntitySet } from "../../../entity/EntitySet";
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from '../../../state/subsetSlice';

import {selectTimeSelection} from "../../../state/timeSelectionStateSelectors";

function getTickPositions(range: IAxisRange, steps: number): Array<number> {
    if (range.min === undefined || range.max === undefined) return [];
    return tickPositioner(range.min, range.max, steps);
}

function buildChartOptions(props: IScatterPlotCardProps, isLoading: boolean, xValue: number, yValue: number, height: number) : Options {

    const axisFormat: AxisOptions = {
        labels: {enabled: false},
        tickLength: 0,
        tickWidth: 0,
        lineWidth: 1,
        gridLineWidth: 1,
        lineColor: "#CCCCCC"
    }

    const rows = props.sections.length;
    const cols = rows ? props.sections[0].length : 0;

    const name = props.entityInstance ? props.entityInstance.name : "";
    const colour = props.entityInstance ? props.entitySet.getInstanceColor(props.entityInstance) : "#FF0000";

    return {
        chart: {
            type: "scatter",
            animation: false,
            height: height
        },
        xAxis: {
            ...axisFormat as XAxisOptions,
            title: {
                text: props.xMetric.name,
                style: {fontSize: "12px"},
                margin: 3
            },
            min: props.xAxisRange.min,
            max: props.xAxisRange.max,
            tickPositioner: () => getTickPositions(props.xAxisRange, cols)
        },
        yAxis: {
            ...axisFormat as YAxisOptions,
            title: {
                text: props.yMetric.name,
                style: {fontSize: "12px"},
                margin: 8
            },
            min: props.yAxisRange.min,
            max: props.yAxisRange.max,
            tickPositioner: () => getTickPositions(props.yAxisRange, rows)
        },
        legend: {enabled: false},
        tooltip: {enabled: false},
        plotOptions: {
            series: {states: {hover: {enabled: false}}},
            scatter: {
                marker: {radius: 5},
                dataLabels: {
                    enabled: true,
                    format: "{point.label}",
                    align: "right",
                    position: "left",
                    verticalAlign: "middle",
                    x: -5
                }
            }
        },
        series: [{
            type: "scatter",
            data: isLoading? [] : [{x: xValue, y: yValue, label: name}],
            color: colour,
            animation: !isLoading
        }]
    };
}

function getAdjective(ticks: number[], value: number): string | undefined {
    if (ticks.length < 3) return undefined;
    if (value <= ticks[1]) return "Low";
    if (value > ticks[ticks.length - 2]) return "High";
    return "Average"; // Might need more than this if there are more than 3 sections per axis
}

function getSectionIndex(ticks: number[], value: number): number {
    // There's always one more tick than sections
    for (let i = 1; i < ticks.length; i++) {
        if (value <= ticks[i]) return i - 1
    }
    return ticks.length - 2;
}

function getSection(sections: string[][], xTicks: number[], yTicks: number[], resultBreakdown: IResultBreakdown): string {
    const xIndex = getSectionIndex(xTicks, resultBreakdown.xValue);
    const yIndex = getSectionIndex(yTicks, resultBreakdown.yValue);

    // Sections go from highest y value to lowest whereas ticks got from lowest to highest
    const inValueOrder = Array.from(sections).reverse();
    return inValueOrder[yIndex][xIndex];
}

function generateDescription(instance: EntityInstance | undefined, sections: string[][], xRange: IAxisRange, yRange: IAxisRange, resultBreakdown: IResultBreakdown, isLoading: boolean): React.ReactNode | undefined {

    if (isLoading) {
        return (
            <div className="card-description-placeholder">
                <div className="first-sentence"></div>
                <div className="second-sentence"></div>
            </div>
        );
    }

    const rows = sections.length;
    const cols = rows ? sections[0].length : 0;

    const xTicks = getTickPositions(xRange, cols);
    const yTicks = getTickPositions(yRange, rows);

    const xAdjective = getAdjective(xTicks, resultBreakdown.xValue);
    const yAdjective = getAdjective(yTicks, resultBreakdown.yValue);

    if (xAdjective === undefined || yAdjective === undefined) return undefined;

    const section = getSection(sections, xTicks, yTicks, resultBreakdown).toLowerCase();
    const instanceName = instance ? instance.name : "";

    return (
        <p className="description">
            {xAdjective} {resultBreakdown.xMetric.toLowerCase()} and {yAdjective.toLowerCase()} {resultBreakdown.yMetric.toLowerCase()} mean {instanceName} is seen as <strong>{section}</strong> in this market
        </p>
    );
}

interface IResultBreakdown {
    xMetric: string;
    xValue: number;
    yMetric: string;
    yValue: number;
}

interface IScatterPlotCardProps {
    linkText: string;
    entitySet: EntitySet;
    entityInstance: EntityInstance | undefined;
    xMetric: Metric;
    yMetric: Metric;
    nextPageUrl: string;
    curatedFilters: CuratedFilters;
    xAxisRange: IAxisRange;
    yAxisRange: IAxisRange;
    sections: string[][];
}

const ScatterPlotCard: React.FunctionComponent<IScatterPlotCardProps> = (props: IScatterPlotCardProps) => {

    const [isLoading, setIsLoading] = useState(true);
    const [layoutReady, setLayoutReady] = useState(false);
    const [resultBreakdown, setResultBreakdown] = useState<IResultBreakdown>({
        xMetric: props.xMetric.name,
        xValue: 0,
        yMetric: props.yMetric.name,
        yValue: 0
    });

    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const getEntityInstanceIds = () => {
        if (props.xMetric.entityCombination.length === 0 && props.yMetric.entityCombination.length === 0) {
            return [];
        }

        return [props.entityInstance?.id ?? 0];
    }

    useEffect(() => {
        setIsLoading(true);

        const instanceId = props.entityInstance?.id ?? 0;

        const model = ViewHelper.createCuratedRequestModel(
            getEntityInstanceIds(),
            [props.xMetric, props.yMetric],
            props.curatedFilters,
            instanceId,
            {},
            subsetId,
            timeSelection
        );

        BrandVueApi.Factory.DataClient(throwError => throwError()).getImpactMapResults(model)
            .then(results => {
                const x = results.data[0].current.metric1.weightedResult;
                const y = results.data[0].current.metric2.weightedResult;
                setResultBreakdown({...resultBreakdown, xValue: x, yValue: y});
                setIsLoading(false);
            });
    }, [
        props.entityInstance,
        props.xMetric,
        props.yMetric,
        props.curatedFilters.startDate,
        props.curatedFilters.endDate,
        props.curatedFilters.average.averageId,
        props.curatedFilters.demographicFilter.genders,
        props.curatedFilters.demographicFilter.ageGroups,
        props.curatedFilters.demographicFilter.regions,
        props.curatedFilters.demographicFilter.socioEconomicGroups,
        props.curatedFilters.measureFilters.length,
        timeSelection
    ]);

    useLayoutEffect(() => {
        setLayoutReady(true);
    },[]);

    return <TileTemplateChart
        handleHeight
        nextPageUrl={props.nextPageUrl}
        linkText={props.linkText}
        descriptionNode={generateDescription(props.entityInstance, props.sections, props.xAxisRange, props.yAxisRange, resultBreakdown, isLoading)}
        className="scatter-plot-tile"
        getChartOptions={(width, height) => buildChartOptions(props, isLoading, resultBreakdown.xValue, resultBreakdown.yValue, height)}
        refreshMode={layoutReady ? 'debounce' : undefined}
    />
}

export default ScatterPlotCard;