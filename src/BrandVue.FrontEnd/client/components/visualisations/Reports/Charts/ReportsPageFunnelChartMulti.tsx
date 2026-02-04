import { CuratedFilters } from '../../../../filter/CuratedFilters';
import React from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import Highcharts, { Options } from 'highcharts';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { getFunnelChartOptions } from './HighchartsOptions/FunnelOptions';
import { BaseExpressionDefinition, CrossMeasure, DisplaySignificanceDifferences, MainQuestionType, SampleSizeMetadata } from '../../../../BrandVueApi';
import ReportsPageColumnChartTemplate from './ReportsPageColumnChartTemplate';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { setIsSettingsChange, setReportErrorState } from '../../../../state/reportSlice';
import { ReportErrorState } from '../../shared/ReportErrorState';
import { useDispatch } from 'react-redux';
import { UnsupportedVariableError } from './HighchartsOptions/CustomErrors';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationsSlice';
import { useAppSelector } from 'client/state/store';
import styles from "./ReportsPageFunnelChartMulti.module.less";
import { getSampleSizeMetaSlice } from '../../../helpers/SampleSizeHelper';
import { getMultiFunnelResults, getLowSampleFromMultiFunnelResults, getMultiFunnelSampleSizeMetadata } from '../Utility/ReportsPageDataHelper';
import { selectSubsetId } from 'client/state/subsetSlice';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";
import { selectCurrentReport } from 'client/state/reportSelectors';

interface IReportsPageFunnelChartMultiProps { 
    reportPart: PartWithExtraData;
    waves: CrossMeasure | undefined;
    breaks: CrossMeasure[] | undefined;
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    order: BrandVueApi.ReportOrder;
    filterInstances: FilterInstance[];
    baseExpressionOverride?: BaseExpressionDefinition;
    setDataState(state: PageCardState): void;
    setIsLowSample?(isLowSample: boolean): void;
}

const ReportsPageFunnelChartMulti = (props: IReportsPageFunnelChartMultiProps) => {
    const dispatch = useDispatch();

    const [chartOptions, setChartOptions] = React.useState<Options[]>([]);
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [sampleSizeMeta, setSampleSizeMeta] = React.useState<SampleSizeMetadata>();
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { variables, loading: isVariablesLoading } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);

        const fetchData = async () => {
            try {
                const d = await getMultiFunnelResults(props.reportPart,
                    props.curatedFilters,
                    props.filterInstances,
                    questionTypeLookup,
                    metrics,
                    variables,
                    props.order,
                    subsetId,
                    timeSelection,
                    props.baseExpressionOverride,
                    props.waves,
                    props.breaks);
                if (d !== undefined && !isCancelled) {
                    if (props.setIsLowSample) {
                        props.setIsLowSample(getLowSampleFromMultiFunnelResults(d.results));
                    }

                    const variable = variables.find(v => v.id === props.reportPart.metric!.variableConfigurationId);
                    const options = getFunnelChartOptions(d.results,
                        props.reportPart.metric!,
                        report.decimalPlaces,
                        props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell,
                        report.highlightLowSample,
                        false,
                        report.displaySignificanceDifferences,
                        variable);
                    setChartOptions(options);
                    setSampleSizeMeta(getMultiFunnelSampleSizeMetadata(d.results));
                }
            } catch (e: any) {
                if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                    props.setDataState(PageCardState.NoData);
                } else {
                    if (e instanceof UnsupportedVariableError) {
                        dispatch(setIsSettingsChange(false));
                        dispatch(setReportErrorState({ isError: true, errorMessage: e.message } as ReportErrorState));
                        props.setDataState(PageCardState.UnsupportedVariable);
                    }
                    else {
                        props.setDataState(PageCardState.Error);
                        throw e;
                    }
                }
            } finally {
                if (!isCancelled) {
                    setIsLoading(false);
                }
            }
        }

        if (!isVariablesLoading) {
            fetchData();
        }

        return () => { isCancelled = true };
    }, [
        report.decimalPlaces,
        props.curatedFilters,
        props.order,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        report.highlightLowSample,
        isVariablesLoading,
        props.breaks,
        props.waves,
        timeSelection
    ]);

    const getChart = (options: Options, chartRef: React.MutableRefObject<Highcharts.Chart | undefined>, index: number) => {
        return <ReportsPageColumnChartTemplate
            key={`funnel${index}`}
            isLoading={isLoading}
            chartOptions={options}
            chart={chartRef}
            sampleSizeMeta={sampleSizeMeta ? getSampleSizeMetaSlice(sampleSizeMeta, 5, index) : sampleSizeMeta}
            metric={props.reportPart.metric!}
            questionTypeLookup={props.questionTypeLookup}
            filterInstanceNames={props.filterInstances.map(i => i.instance.name)}
            baseExpressionOverride={props.baseExpressionOverride}
            footerAverages={undefined}
            decimalPlaces={report.decimalPlaces}
        />
    }

    const chartRefs = React.useRef<Highcharts.Chart | undefined[]>([]);

    return (
        <div className={styles.multiChartContainer}>
            {chartOptions.map((co, i) => {
                if (!chartRefs.current[i]) {
                    chartRefs.current[i] = React.createRef<Highcharts.Chart>();
                }

                return getChart(co, chartRefs.current[i], i);
            })}
        </div>
    )
};

export default ReportsPageFunnelChartMulti;