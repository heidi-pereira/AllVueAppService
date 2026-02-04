import { CuratedFilters } from '../../../../filter/CuratedFilters';
import React from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { PageCardPlaceholder } from '../../shared/PageCardPlaceholder';
import TileTemplate from '../../shared/TileTemplate';
import TileTemplateChart from '../../Cards/TileTemplateChart';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { getCompetitionResults } from './ChartData/CompetitionResultsDataHandler';
import { getDoughnutChartOptions, getLegendMapForIncludedInstances } from './HighchartsOptions/DoughnutOptions';
import { BaseExpressionDefinition, DisplaySignificanceDifferences } from '../../../../BrandVueApi';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import HighchartsCustomLegend from '../../HighchartsCustomLegend';
import { getReverseLegendMap } from './ReportsChartHelper';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { OverlapError } from './HighchartsOptions/CustomErrors';
import { setReportErrorState, setIsSettingsChange } from '../../../../state/reportSlice';
import { ReportErrorState } from '../../shared/ReportErrorState';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppDispatch } from 'client/state/store';
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from 'client/state/subsetSlice';
import { selectCurrentReport } from 'client/state/reportSelectors';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageDoughnutCardProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    getDescriptionNode: (isLowSample: boolean) => JSX.Element;
    setDataState(state: PageCardState): void;
    order: BrandVueApi.ReportOrder;
    showTop: number | undefined;
    filterInstances: FilterInstance[];
    splitByType: BrandVueApi.IEntityType | undefined
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
    updatePart(colours: string[]): void;
}

const ReportsPageDoughnutCard = (props: IReportsPageDoughnutCardProps) => {
    const dispatch = useAppDispatch();
    
    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [isLowSample, setIsLowSample] = React.useState<boolean>(false);
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const [instanceToColourMap, setInstanceToColourMap] = React.useState<Map<string, string>>(new Map<string, string>());
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    function getVariableDefinition(metricName: string): BrandVueApi.GroupedVariableDefinition | undefined {
        var variableDefinition = variables.find(v => v.displayName === metricName);
        return variableDefinition?.definition as BrandVueApi.GroupedVariableDefinition;
    }

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);

        const fetchData = async () => {
            try {
                const d = await getCompetitionResults(
                    props.reportPart,
                    props.curatedFilters,
                    props.order,
                    undefined,
                    props.filterInstances,
                    [],
                    questionTypeLookup,
                    metrics,
                    variables,
                    subsetId,
                    timeSelection,
                    props.splitByType,
                    props.baseExpressionOverride
                );

                if (!isCancelled) {
                    const isLowSample = d.results.lowSampleSummary.length > 0;
                    setIsLowSample(isLowSample);
                    const { legendMap, storeColours } = getReverseLegendMap(props.reportPart.part.colours,
                        d.results.periodResults[0].resultsPerEntity.map(r => r.entityInstance.name));
                    if (storeColours) {
                        const coloursArray = Array.from(legendMap).map(([key, value]) => `${key}:${value}`);
                        props.updatePart(coloursArray);
                    }

                    const results = d.results.periodResults[0].resultsPerEntity;
                    const variableDefinition = await getVariableDefinition(props.reportPart.metric!.name);
                    const options = getDoughnutChartOptions(results,
                    props.reportPart.metric!,
                    legendMap,
                    report.decimalPlaces,
                    props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell,
                    report.highlightLowSample,
                    props.showTop,
                    metrics,
                    entityConfiguration,
                    props.reportPart.part.defaultSplitBy,
                    (a, b) => { },
                    props.order,
                    report.displaySignificanceDifferences,
                    variableDefinition);
                    setInstanceToColourMap(getLegendMapForIncludedInstances(results, legendMap));
                    setChartOptions(options);
                    setIsLoading(false);
                }
            } catch (e: any) {
                if (!isCancelled) {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        props.setDataState(PageCardState.NoData);
                    } else {
                        if (e instanceof OverlapError) {
                            dispatch(setIsSettingsChange(false));
                            dispatch(setReportErrorState({ isError: true, errorMessage: e.message } as ReportErrorState));
                            props.setDataState(PageCardState.NotSupportedOverlap);
                        } else {
                            props.setDataState(PageCardState.Error);
                            throw e;
                        }
                    }
                }
            }
        };

        fetchData();

        return () => { isCancelled = true };
    }, [
        report.decimalPlaces,
        props.curatedFilters,
        props.order,
        props.showTop,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        report.highlightLowSample,
        subsetId,
        timeSelection
    ]);

    const chart = React.useRef<Highcharts.Chart>();

    const getDescriptionWithLegend = () => {
        return <>
            {props.getDescriptionNode(isLowSample)}
            <HighchartsCustomLegend keyToColourMap={instanceToColourMap}
                chartReference={chart} />
        </>;
    }

    if (isLoading) {
        return (
            <TileTemplate descriptionNode={props.getDescriptionNode(isLowSample)}>
                <PageCardPlaceholder />
            </TileTemplate>
        );
    }

    const resizeDoughnut = () => {
        const containerHeight = chart?.current?.container?.parentElement?.parentElement?.parentElement?.clientHeight;
        if (containerHeight) {
            return {
                ...chartOptions,
                chart: {
                    animation: false,
                    height: `${containerHeight}px`
                }
            };
        }
        return chartOptions;
    }

    return <TileTemplateChart
        handleWidth
        descriptionNode={getDescriptionWithLegend()}
        getChartOptions={resizeDoughnut}
        callback={c => chart.current = c}
    />;
};

export default ReportsPageDoughnutCard;