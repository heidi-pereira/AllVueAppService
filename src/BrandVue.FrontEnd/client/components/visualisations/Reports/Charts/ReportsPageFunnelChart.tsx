import { CuratedFilters } from '../../../../filter/CuratedFilters';
import React from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import Highcharts, { Options } from 'highcharts';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { getCompetitionResults } from './ChartData/CompetitionResultsDataHandler';
import { getFunnelChartOptions } from './HighchartsOptions/FunnelOptions';
import { BaseExpressionDefinition, DisplaySignificanceDifferences, MainQuestionType, SampleSizeMetadata } from '../../../../BrandVueApi';
import ReportsPageColumnChartTemplate from './ReportsPageColumnChartTemplate';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { setIsSettingsChange, setReportErrorState } from '../../../../state/reportSlice';
import { ReportErrorState } from '../../shared/ReportErrorState';
import { useAppDispatch } from 'client/state/store';
import { UnsupportedVariableError } from './HighchartsOptions/CustomErrors';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from 'client/state/subsetSlice';
import { selectCurrentReport } from 'client/state/reportSelectors';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageFunnelChartProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    order: BrandVueApi.ReportOrder;
    filterInstances: FilterInstance[];
    baseExpressionOverride?: BaseExpressionDefinition;
    setDataState(state: PageCardState): void;
    setIsLowSample?(isLowSample: boolean): void;
}

const ReportsPageFunnelChart = (props: IReportsPageFunnelChartProps) => {
    const dispatch = useAppDispatch();

    const [chartOptions, setChartOptions] = React.useState<Options>();
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
                    undefined,
                    props.baseExpressionOverride
                );

                if (!isCancelled) {
                    if (props.setIsLowSample) {
                        props.setIsLowSample(d.results.lowSampleSummary.length > 0);
                    }

                    const variable = variables.find(v => v.id === props.reportPart.metric!.variableConfigurationId);
                    const options = getFunnelChartOptions(d.results.periodResults[0].resultsPerEntity,
                        props.reportPart.metric!,
                        report.decimalPlaces,
                        props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell,
                        report.highlightLowSample,
                        false,
                        report.displaySignificanceDifferences,
                        variable);
                    setChartOptions(options ? options[0]: undefined);
                    setSampleSizeMeta(d.results.sampleSizeMetadata);
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
        isVariablesLoading
    ]);

    const chart = React.useRef<Highcharts.Chart>();

    return (
        <ReportsPageColumnChartTemplate
            isLoading={isLoading}
            chartOptions={chartOptions}
            chart={chart}
            sampleSizeMeta={sampleSizeMeta}
            metric={props.reportPart.metric!}
            questionTypeLookup={props.questionTypeLookup}
            filterInstanceNames={props.filterInstances.map(i => i.instance.name)}
            baseExpressionOverride={props.baseExpressionOverride}
            footerAverages={undefined}
            decimalPlaces={report.decimalPlaces}
        />
    );
};

export default ReportsPageFunnelChart;