import {
    AverageType,
    BaseExpressionDefinition,
    CrossMeasure,
    CrosstabAverageResults,
    MainQuestionType,
    ReportOrder,
    IEntityType,
    SampleSizeMetadata,
    WeightingMethod,
    DisplaySignificanceDifferences,
    SigConfidenceLevel,
} from '../../../../BrandVueApi';
import React from 'react';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import Highcharts, { Options } from 'highcharts';
import { getColourMap } from '../../../helpers/ChromaHelper';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { getCrossbreakCompetitionResults } from './ChartData/CrossbreakCompetitionResultsDataHandler';
import { getSplitColumnChartOptions } from './HighchartsOptions/ColumnOptions';
import ReportsPageColumnChartTemplate from './ReportsPageColumnChartTemplate';
import {PartWithExtraData} from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from '../../../../state/variableConfigurationSelectors';
import { useAppSelector } from "../../../../state/store";
import { selectSubsetId } from '../../../../state/subsetSlice';
import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";
import { selectCurrentReport } from 'client/state/reportSelectors';

interface IReportsPageSplitColumnChartProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    questionTypeLookup: {[key: string]: MainQuestionType};
    order: ReportOrder;
    showTop: number | undefined;
    filterInstances: FilterInstance[];
    splitByType: IEntityType | undefined;
    breaks: CrossMeasure;
    baseExpressionOverride?: BaseExpressionDefinition;
    setDataState(state: PageCardState): void;
    setIsLowSample?(isLowSample: boolean): void;
    averageTypes: AverageType[];
    updateBreak(b: CrossMeasure[]): void;
}

const ReportsPageSplitColumnChart = (props: IReportsPageSplitColumnChartProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [sampleSizeMeta, setSampleSizeMeta] = React.useState<SampleSizeMetadata>();
    const [footerAverages, setFooterAverages] = React.useState<CrosstabAverageResults[] | undefined>();
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;
    
    const selectSignificanceComparator = (filterInstanceComparand: string) => {
        const updatedBreak = new CrossMeasure({...props.breaks});
        updatedBreak.significanceFilterInstanceComparandName = filterInstanceComparand;
        props.updateBreak([updatedBreak]);
    }

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);

        getCrossbreakCompetitionResults(
                props.reportPart,
                props.curatedFilters,
                [props.breaks],
                props.order,
                props.showTop,
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
                report.sigConfidenceLevel
            ).then(d => {
                if (!isCancelled) {
                    const singleResult = d.results.groupedBreakResults[0].breakResults;
                    if (props.setIsLowSample) {
                        props.setIsLowSample(singleResult.lowSampleSummary.length > 0);
                    }
                    setFooterAverages(d.averages);
                    const legendItems = singleResult.instanceResults.map(r => r.breakName);
                    const legendMap = getColourMap(legendItems);
                    const options = getSplitColumnChartOptions(singleResult,
                        props.reportPart.metric!,
                        legendMap,
                        report.decimalPlaces,
                        props.curatedFilters.average.weightingMethod == WeightingMethod.QuotaCell,
                        report.highlightLowSample,
                        (filterInstanceComparand, _) => selectSignificanceComparator(filterInstanceComparand),
                        props.reportPart.part.displayMeanValues,
                        report.displaySignificanceDifferences
                    );
                    setChartOptions(options);
                    setSampleSizeMeta(singleResult.sampleSizeMetadata);
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

export default ReportsPageSplitColumnChart;