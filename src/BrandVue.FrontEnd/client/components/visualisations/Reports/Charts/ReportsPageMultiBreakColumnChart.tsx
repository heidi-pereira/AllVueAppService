import {
    AverageType,
    BaseExpressionDefinition,
    CrossMeasure,
    CrosstabAverageResults,
    MainQuestionType,
    ReportOrder,
    IEntityType,
    SampleSizeMetadata,
} from '../../../../BrandVueApi';
import React from 'react';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { SortType, getCrossbreakCompetitionResults } from './ChartData/CrossbreakCompetitionResultsDataHandler';
import { getMultiBreakColumnChartOptions } from './HighchartsOptions/ColumnOptions';
import ReportsPageColumnChartTemplate from './ReportsPageColumnChartTemplate';
import {PartWithExtraData} from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from 'client/state/subsetSlice';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";
import { selectCurrentReport } from 'client/state/reportSelectors';

interface IReportsPageMultiBreakColumnChartProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    questionTypeLookup: {[key: string]: MainQuestionType};
    order: ReportOrder;
    showTop: number | undefined;
    primaryFilterInstance: FilterInstance;
    filterInstances: FilterInstance[];
    splitByType: IEntityType | undefined;
    breaks: CrossMeasure[];
    baseExpressionOverride?: BaseExpressionDefinition;
    setDataState(state: PageCardState): void;
    setIsLowSample?(isLowSample: boolean): void;
    averageTypes: AverageType[];
    updateBreak(b: CrossMeasure[]): void;
}

const ReportsPageMultiBreakColumnChart = (props: IReportsPageMultiBreakColumnChartProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [sampleSizeMeta, setSampleSizeMeta] = React.useState<SampleSizeMetadata>();
    const [footerAverages, setFooterAverages] = React.useState<CrosstabAverageResults[] | undefined>();
    const {enabledMetricSet, selectableMetricsForUser: metrics, questionTypeLookup} = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const selectSignificanceComparator = (comparandInstanceAndName: string) => {
        const split = comparandInstanceAndName.split(", ");
        const filterInstanceComparand = split[0];
        const breakDisplayName = split[1];

        const updatedBreaks = props.breaks.map(b => new CrossMeasure(b));
    
        let breakToUpdate = updatedBreaks.find(b => b.measureName == breakDisplayName);
        if (!breakToUpdate){
            const metricFromVarcode = enabledMetricSet.getMetric(breakDisplayName);
            breakToUpdate = updatedBreaks.find(b => b.measureName == metricFromVarcode?.name);
        }

        if(!breakToUpdate){
            throw new Error("Unable to find break")
        }

        breakToUpdate.significanceFilterInstanceComparandName = filterInstanceComparand;
        props.updateBreak(updatedBreaks);
    }

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);

        getCrossbreakCompetitionResults(
                props.reportPart,
                props.curatedFilters,
                props.breaks,
                props.order,
                props.showTop,
                props.filterInstances,
                [],
                questionTypeLookup,
                metrics,
                variables,
                subsetId,
                timeSelection,
                props.splitByType,
                props.baseExpressionOverride,
                report.highlightSignificance,
                props.primaryFilterInstance,
                SortType.Breaks,
                report.sigConfidenceLevel
            ).then(d => {
                if (!isCancelled) {
                    if (props.setIsLowSample) {
                        props.setIsLowSample(d.results.groupedBreakResults.some(r => r.breakResults.lowSampleSummary.length > 0));
                    }
                    const options = getMultiBreakColumnChartOptions(d.results,
                        props.reportPart.metric!,
                        report.decimalPlaces,
                        report.highlightLowSample,
                        report.includeCounts,
                        (_, comparandInstanceAndName) => selectSignificanceComparator(comparandInstanceAndName),
                        report.displaySignificanceDifferences);
                    setChartOptions(options);
                    setSampleSizeMeta(d.results.groupedBreakResults[0].breakResults.sampleSizeMetadata);
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
        props.order,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.breaks,
        props.showTop,
        props.baseExpressionOverride,
        report.highlightLowSample,
        report.includeCounts,
        props.reportPart,
    ]);

    const chart = React.useRef<Highcharts.Chart>();

    return (
        <ReportsPageColumnChartTemplate
            isLoading={isLoading}
            sampleSizeMeta={sampleSizeMeta}
            chartOptions={chartOptions}
            chart={chart}
            metric={props.reportPart.metric!}
            questionTypeLookup={props.questionTypeLookup}
            filterInstanceNames={props.filterInstances.map(i => i.instance.name)}
            baseExpressionOverride={props.baseExpressionOverride}
            footerAverages={footerAverages}
            decimalPlaces={report.decimalPlaces}
        />
    );
};

export default ReportsPageMultiBreakColumnChart;
