import {
    AverageType,
    BaseExpressionDefinition,
    CrossMeasure,
    CrosstabAverageResults,
    MainQuestionType,
    ReportOrder,
    IEntityType,
    SampleSizeMetadata,
    DisplaySignificanceDifferences,
    SigConfidenceLevel,
} from '../../../../BrandVueApi';
import React, { useRef } from 'react';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { getColourMap } from '../../../helpers/ChromaHelper';
import { getMultiChartCrossbreakCompetitionResults } from './ChartData/CrossbreakCompetitionResultsDataHandler';
import { getSplitStackedColumnChartOptions } from './HighchartsOptions/StackedColumnOptions';
import ReportsPageColumnChartTemplate from './ReportsPageColumnChartTemplate';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from 'client/state/subsetSlice';
import { selectCurrentReport } from 'client/state/reportSelectors';
import styles from "./ReportsPageSplitStacked.module.less";
import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageSplitStackedColumnChartProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    breaks: CrossMeasure;
    setDataState(state: PageCardState): void;
    splitByType: IEntityType | undefined;
    filterInstances: FilterInstance[];
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
    setIsLowSample?(isLowSample: boolean): void;
    averageTypes: AverageType[];
    updateBreak(b: CrossMeasure[]): void;
}

const ReportsPageSplitStackedColumnChart = (props: IReportsPageSplitStackedColumnChartProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options[]>([]);
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [sampleSizeMeta, setSampleSizeMeta] = React.useState<SampleSizeMetadata[]>([]);
    const [footerAverages, setFooterAverages] = React.useState<CrosstabAverageResults[] | undefined>();
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId)
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;
    const selectSignificanceComparator = (filterInstanceComparand: string) => {
        const updatedBreak = new CrossMeasure({ ...props.breaks });
        updatedBreak.significanceFilterInstanceComparandName = filterInstanceComparand;
        props.updateBreak([updatedBreak]);
    }

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);
        const showTop: number | undefined = undefined;
        getMultiChartCrossbreakCompetitionResults(
            props.reportPart,
            props.curatedFilters,
            [props.breaks],
            ReportOrder.ScriptOrderDesc,
            showTop,
            props.filterInstances,
            props.averageTypes,
            questionTypeLookup,
            metrics,
            variables,
            subsetId,
            timeSelection,
            props.splitByType,
            props.baseExpressionOverride,
            report.highlightSignificance,
            undefined,
            undefined,
            report.sigConfidenceLevel,
        ).then(d => {
            if (!isCancelled) {
                if (props.setIsLowSample) {
                    props.setIsLowSample(d.results.some(r => 
                        r.groupedBreakResults.some(g => g.breakResults.lowSampleSummary.length > 0))
                    );
                }
                setFooterAverages(d.averages);
                const legendMap = getColourMap(d.results[0].groupedBreakResults[0].breakResults.instanceResults[0]
                    .entityResults.map(e => e.entityInstance.name));

                const options = d.results.map(result => {
                    return getSplitStackedColumnChartOptions(result.groupedBreakResults[0].breakResults,
                        props.reportPart.metric!,
                        report.decimalPlaces,
                        legendMap,
                        report.highlightLowSample,
                        props.showWeightedCounts,
                        d.averages,
                        (e) => selectSignificanceComparator(e),
                        props.reportPart.part.displayMeanValues,
                        report.displaySignificanceDifferences
                    )
                });

                setChartOptions(options);
                const sampleSizeMeta = d.results.map(r => r.groupedBreakResults[0].breakResults.sampleSizeMetadata);
                setSampleSizeMeta(sampleSizeMeta);
                setIsLoading(false);
            }
        })
            .catch((e: any) => {
                if (!isCancelled) {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        props.setDataState(PageCardState.NoData);
                    } else {
                        props.setDataState(PageCardState.Error);
                        throw e;
                    }
                }
            });

        return () => { isCancelled = true };
    }, [
        report.decimalPlaces,
        props.curatedFilters,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.breaks,
        props.baseExpressionOverride,
        report.highlightLowSample,
    ]);

    const includeTitles = chartOptions.length > 1;

    const getChart = (options: Options, chartRef: React.MutableRefObject<Highcharts.Chart | null>, index: number) => {
        return (
            <div className={styles.chartAndTitle}>
                {includeTitles && (
                    <div className={styles.chartTitle}>
                        {props.filterInstances[index]?.instance.name}
                    </div>
                )}
                <ReportsPageColumnChartTemplate
                    isLoading={isLoading}
                    sampleSizeMeta={sampleSizeMeta[index]}
                    chartOptions={options}
                    chart={chartRef}
                    metric={props.reportPart.metric!}
                    questionTypeLookup={props.questionTypeLookup}
                    filterInstanceNames={props.filterInstances.map(i => i.instance.name)}
                    baseExpressionOverride={props.baseExpressionOverride}
                    footerAverages={footerAverages}
                    decimalPlaces={report.decimalPlaces}
                    filterByIndex={index}
                />
            </div>
        )
    }

    const chartRefs = useRef<React.RefObject<Highcharts.Chart>[]>([]);

    return (
        <div className={styles.multiChartContainer}>
            {chartOptions.map((option, index) => {
                if (!chartRefs.current[index]) {
                    chartRefs.current[index] = React.createRef<Highcharts.Chart>();
                }
                return (
                    <React.Fragment key={index}>
                        {getChart(option, chartRefs.current[index], index)}
                    </React.Fragment>
                );
            })}
        </div>
    )
};

export default ReportsPageSplitStackedColumnChart;