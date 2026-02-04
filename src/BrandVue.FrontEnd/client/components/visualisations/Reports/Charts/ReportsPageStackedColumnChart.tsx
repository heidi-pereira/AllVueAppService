import { AverageType, BaseExpressionDefinition, MainQuestionType, OverTimeAverageResults, ReportOrder, IEntityType, SampleSizeMetadata, Features, SelectedEntityInstances, CrosstabAverageResults, DisplaySignificanceDifferences } from '../../../../BrandVueApi';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import { Metric } from '../../../../metrics/metric';
import React from 'react';
import Highcharts, { Options } from 'highcharts';
import { getColourMap } from '../../../helpers/ChromaHelper';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { getStackedMultiEntityResults } from './ChartData/StackedMultiEntityResultsDataHandler';
import { getStackedColumnChartOptions, getStackedSingleEntityColumnChartOptions } from './HighchartsOptions/StackedColumnOptions';
import ReportsPageColumnChartTemplate from './ReportsPageColumnChartTemplate';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { PartWithExtraData } from '../ReportsPageDisplay';
import { selectHydratedVariableConfiguration } from '../../../../state/variableConfigurationSelectors';
import { useAppSelector } from "../../../../state/store";
import { selectCurrentReport } from 'client/state/reportSelectors';
import { getCompetitionResults } from "./ChartData/CompetitionResultsDataHandler";
import { selectSubsetId } from "../../../../state/subsetSlice";
import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageStackedColumnChartProps {
    reportPart: PartWithExtraData;
    metric: Metric;
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    order: ReportOrder;
    filterInstances: FilterInstance[];
    splitByType: IEntityType;
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
    averageTypes: AverageType[];
    setDataState(state: PageCardState): void;
    setIsLowSample?(isLowSample: boolean): void;
    selectedEntityInstances?: SelectedEntityInstances | undefined;
}

const ReportsPageStackedColumnChart = (props: IReportsPageStackedColumnChartProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [sampleSizeMeta, setSampleSizeMeta] = React.useState<SampleSizeMetadata>();
    const [footerAverages, setFooterAverages] = React.useState<CrosstabAverageResults[] | OverTimeAverageResults[][] | undefined>();
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
            props.averageTypes,
            questionTypeLookup,
            metrics,
            variables,
            subsetId,
            timeSelection,
            props.splitByType,
            props.baseExpressionOverride
        ).then(d => {
            if (!cancelled()) {
                if (props.setIsLowSample) {
                    props.setIsLowSample(d.results.lowSampleSummary.length > 0);
                }
                setFooterAverages(d.averages);
                const legendMap = getColourMap(d.results.periodResults[0].resultsPerEntity.map(d => d.entityInstance?.name ?? props.metric.displayName));
                const options = getStackedSingleEntityColumnChartOptions(
                    d.results.periodResults[0].resultsPerEntity,
                    props.metric,
                    legendMap,
                    report.decimalPlaces,
                    report.highlightLowSample,
                    props.showWeightedCounts,
                    props.order,
                    (e) => {}, //not supported in this chart type
                    props.reportPart.part.displayMeanValues,
                    report.displaySignificanceDifferences
                );
                setChartOptions(options);
                setSampleSizeMeta(d.results.sampleSizeMetadata);
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
            props.splitByType,
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
                if (props.setIsLowSample) {
                    props.setIsLowSample(d.results.lowSampleSummary.length > 0);
                }
                setFooterAverages(d.averages);
                const legendMap = getColourMap(d.results.resultsPerInstance[0].data.map(d => d.entityInstance?.name ?? props.metric.displayName));
                setChartOptions(
                    getStackedColumnChartOptions(
                        d.results,
                        props.metric,
                        legendMap,
                        report.decimalPlaces,
                        report.highlightLowSample,
                        props.showWeightedCounts,
                        d.averages,
                        props.order,
                        (e) => {}, //not supported in this chart type
                        props.reportPart.part.displayMeanValues,
                        report.displaySignificanceDifferences
                    )
                );
                setSampleSizeMeta(d.results.sampleSizeMetadata);
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
    }

    React.useEffect(() => {
        let isCancelled = false;
        const checkIsCancelled = () => isCancelled;

        if (props.metric.entityCombination.length === 1) {
            loadSingleEntity(checkIsCancelled);
        } else {
            loadMultiEntity(checkIsCancelled);
        }

        return () => { isCancelled = true };
    }, [
        report.decimalPlaces,
        props.curatedFilters,
        props.order,
        props.splitByType?.identifier,
        props.baseExpressionOverride,
        report.highlightLowSample
    ]);

    const chartRef = React.useRef<Highcharts.Chart | undefined>();
    return (
        <ReportsPageColumnChartTemplate
            isLoading={isLoading}
            chartOptions={chartOptions}
            chart={chartRef}
            sampleSizeMeta={sampleSizeMeta}
            metric={props.metric}
            questionTypeLookup={props.questionTypeLookup}
            baseExpressionOverride={props.baseExpressionOverride}
            footerAverages={footerAverages}
            decimalPlaces={report.decimalPlaces}
        />
    )
};

export default ReportsPageStackedColumnChart;

