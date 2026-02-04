import { AverageType, BaseExpressionDefinition, ReportOrder, IEntityType, DisplaySignificanceDifferences, SelectedEntityInstances } from '../../../../BrandVueApi';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import { Metric } from '../../../../metrics/metric';
import React from 'react';
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { PageCardPlaceholder } from '../../shared/PageCardPlaceholder';
import TileTemplate from '../../shared/TileTemplate';
import TileTemplateChart from '../../Cards/TileTemplateChart';
import { getColourMap } from '../../../helpers/ChromaHelper';
import HighchartsCustomLegend from '../../HighchartsCustomLegend';
import { getStackedBarChartOptions, getStackedSingleEntityBarChartOptions } from './HighchartsOptions/StackedBarOptions';
import { getStackedMultiEntityResults } from './ChartData/StackedMultiEntityResultsDataHandler';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import { getCompetitionResults } from './ChartData/CompetitionResultsDataHandler';
import { PartWithExtraData } from '../ReportsPageDisplay';
import { selectSubsetId } from 'client/state/subsetSlice';
import { selectCurrentReport } from 'client/state/reportSelectors';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageStackedBarCardProps {
    reportPart: PartWithExtraData;
    metric: Metric;
    curatedFilters: CuratedFilters;
    getDescriptionNode: (isLowSample: boolean, hideFilterInstances?: boolean) => JSX.Element;
    filterInstances: FilterInstance[];
    splitByEntityType: IEntityType;
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
    averageTypes: AverageType[];
    order: ReportOrder;
    setDataState(state: PageCardState): void;
    selectedEntityInstances?: SelectedEntityInstances;
}

const ReportsPageStackedBarCard = (props: IReportsPageStackedBarCardProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [instanceToColourMap, setInstanceToColourMap] = React.useState<Map<string, string>>(new Map<string, string>());
    const [isLowSample, setIsLowSample] = React.useState<boolean>(false);
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const loadSingleEntity = (cancelled: () => boolean) => {
            setIsLoading(true);

            getCompetitionResults(
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
                    props.splitByEntityType,
                    props.baseExpressionOverride
                ).then(d => {
                    if (!cancelled()) {
                        const isLowSample = d.results.lowSampleSummary.length > 0;
                        setIsLowSample(isLowSample);
                        const legendItems = d.results.periodResults[0].resultsPerEntity.map(d => d.entityInstance?.name ?? props.metric.displayName);
                        const legendMap = getColourMap(legendItems);
                        const options = getStackedSingleEntityBarChartOptions(
                                    d.results.periodResults[0].resultsPerEntity,
                                    props.metric,
                                    legendMap,
                                    report.decimalPlaces,
                                    report.highlightLowSample,
                                    props.showWeightedCounts,
                                    props.order,
                                    report.displaySignificanceDifferences
                                );
                        setInstanceToColourMap(legendMap);
                        setChartOptions(options);
                        setIsLoading(false);
                    }
                })
                .catch((e: any) => {
                    if (!cancelled()) {
                        if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                            props.setDataState(PageCardState.NoData);
                        } else {
                            props.setDataState(PageCardState.Error);
                            throw e;
                        }
                    }
                });
        };

    const loadMultiEntity = (cancelled: () => boolean) => {
        if (props.filterInstances.length == 0) {
            throw Error(`Single filter instance is required for stacked multi-entity charts`);
        }
        setIsLoading(true);

        const filterByEntityType = props.filterInstances[0].type;
        getStackedMultiEntityResults(
                props.metric,
                entityConfiguration,
                props.curatedFilters,
                props.order,
                filterByEntityType,
                props.splitByEntityType,
                props.averageTypes,
                questionTypeLookup,
                metrics,
                variables,
                subsetId,
                timeSelection,
                props.baseExpressionOverride,
                props.selectedEntityInstances
            ).then(d => {
                if (!cancelled()) {
                    const isLowSample = d.results.lowSampleSummary.length > 0;
                    setIsLowSample(isLowSample);
                    const legendItems = d.results.resultsPerInstance[0].data.map(d => d.entityInstance?.name ?? props.metric.displayName);
                    const legendMap = getColourMap(legendItems);
                    const options = getStackedBarChartOptions(
                        d.results,
                        props.metric,
                        legendMap,
                        report.decimalPlaces,
                        report.highlightLowSample,
                        props.showWeightedCounts,
                        props.order,
                        report.displaySignificanceDifferences
                    );
                    setInstanceToColourMap(legendMap);
                    setChartOptions(options);
                    setIsLoading(false);
                }
            })
            .catch((e: any) => {
                if (!cancelled()) {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        props.setDataState(PageCardState.NoData);
                    } else {
                        props.setDataState(PageCardState.Error);
                        throw e;
                    }
                }
            });
    };

    React.useEffect(() => {
        let isCancelled = false;
        const checkIsCancelled = () => isCancelled;

        if (props.metric.entityCombination.length === 1) {
            loadSingleEntity(checkIsCancelled);
        } else {
            loadMultiEntity(checkIsCancelled);
        }

        return () => { isCancelled = true };
    }, [props.curatedFilters, props.splitByEntityType, props.baseExpressionOverride, report.highlightLowSample, props.order, subsetId]);

    const chart = React.useRef<Highcharts.Chart>();

    const getDescriptionWithLegend = () => {
        return <>
            {props.getDescriptionNode(isLowSample, true)}
            <HighchartsCustomLegend keyToColourMap={instanceToColourMap}
                chartReference={chart}
                reverse />
        </>;
    }

    if (isLoading) {
        return (
            <TileTemplate descriptionNode={props.getDescriptionNode(isLowSample, true)}>
                <PageCardPlaceholder />
            </TileTemplate>
        );
    }

    return <TileTemplateChart
        handleWidth
        descriptionNode={getDescriptionWithLegend()}
        getChartOptions={(width, height) => chartOptions}
        callback={c => chart.current = c}
    />;
};

export default ReportsPageStackedBarCard;
