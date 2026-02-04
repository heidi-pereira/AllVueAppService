import { getOverTimeFunnelChartOptions } from "client/components/visualisations/Reports/Charts/HighchartsOptions/FunnelOptions";
import * as BrandVueApi from "../../../../BrandVueApi";
import { IReportsPageOvertimeChartProps } from './ReportsPageOvertimeChart';
import { useAppSelector } from "client/state/store";
import { selectCurrentReport } from 'client/state/reportSelectors';
import { selectHydratedVariableConfiguration } from "client/state/variableConfigurationsSlice";
import { createRef, useEffect, useRef } from "react";
import ReportsPageColumnChartTemplate from "client/components/visualisations/Reports/Charts/ReportsPageColumnChartTemplate";
import { Options } from "highcharts";
import styles from "./ReportsPageFunnelChartMulti.module.less";
import { getOvertimeResults } from "client/components/visualisations/Reports/Charts/ChartData/OvertimeResultsDataHandler";
import React from "react";
import { NoDataError } from "client/NoDataError";
import { PageCardState } from "client/components/visualisations/shared/SharedEnums";
import { useMetricStateContext } from "client/metrics/MetricStateContext";
import { selectSubsetId } from "client/state/subsetSlice";

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

const ReportsPageOvertimeFunnelChart = (props: IReportsPageOvertimeChartProps) => {

    const [chartOptions, setChartOptions] = React.useState<Options[]>([]);
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [sampleSizeMeta, setSampleSizeMeta] = React.useState<BrandVueApi.SampleSizeMetadata>();
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);

        getOvertimeResults(
            props.reportPart,
            props.curatedFilters,
            [],
            questionTypeLookup,
            metrics,
            variables,
            props.filterInstances,
            subsetId,
            timeSelection,
            props.splitByType,
            props.baseExpressionOverride,
            true
        ).then(d => {
            if (!isCancelled) {
                if (props.setIsLowSample) {
                    props.setIsLowSample(d.results.lowSampleSummary.length > 0);
                }

                const variable = variables.find(v => v.id === props.reportPart.metric?.variableConfigurationId);
                const options = getOverTimeFunnelChartOptions(
                    d.results.entityWeightedDailyResults,
                    props.reportPart.metric!,
                    props.curatedFilters.average,
                    report.decimalPlaces,
                    report.isDataWeighted,
                    report.highlightLowSample,
                    false,
                    report.displaySignificanceDifferences,
                    variable
                );
                setChartOptions(options);
                setSampleSizeMeta(d.results.sampleSizeMetadata);
                setIsLoading(false);
            }
        }).catch((e: any) => {
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
        props.reportPart.metric?.name,
        props.curatedFilters,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        props.order,
        props.reportPart.selectedEntitySet,
        currentReportPage.report.decimalPlaces,
        currentReportPage.report.isDataWeighted,
        currentReportPage.report.highlightLowSample,
        currentReportPage.report.displaySignificanceDifferences,
    ]);

    const getChart = (options: Options, chartRef: React.MutableRefObject<Highcharts.Chart | null>, index: number) => {
        return <ReportsPageColumnChartTemplate
            key={`funnel${index}`}
            isLoading={isLoading}
            chartOptions={options}
            chart={chartRef}
            sampleSizeMeta={sampleSizeMeta}
            metric={props.reportPart.metric!}
            questionTypeLookup={props.questionTypeLookup}
            filterInstanceNames={props.filterInstances.map(i => i.instance.name)}
            baseExpressionOverride={props.baseExpressionOverride}
            footerAverages={undefined}
            decimalPlaces={currentReportPage.report.decimalPlaces}
        />
    }

    const chartRefs = useRef<React.RefObject<Highcharts.Chart>[]>([]);

    return (
        <div className={styles.multiChartContainer}>
            {chartOptions.map((co, i) => {
                if (!chartRefs.current[i]) {
                    chartRefs.current[i] = createRef<Highcharts.Chart>();
                }

                return getChart(co, chartRefs.current[i], i);
            })}
        </div>
    )
};

export default ReportsPageOvertimeFunnelChart;