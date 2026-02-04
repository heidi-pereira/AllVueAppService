import { CuratedFilters } from '../../../../filter/CuratedFilters';
import React from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import Highcharts, { Options } from 'highcharts';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { getCompetitionResults } from './ChartData/CompetitionResultsDataHandler';
import { getColumnChartOptions } from './HighchartsOptions/ColumnOptions';
import { AverageType, BaseExpressionDefinition, CrosstabAverageResults, DisplaySignificanceDifferences, MainQuestionType, SampleSizeMetadata } from '../../../../BrandVueApi';
import ReportsPageColumnChartTemplate from './ReportsPageColumnChartTemplate';
import {PartWithExtraData} from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from '../../../../state/variableConfigurationSelectors';
import { useAppSelector } from "../../../../state/store";
import { selectCurrentReport } from 'client/state/reportSelectors';
import { selectSubsetId } from '../../../../state/subsetSlice';
import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageCardChartProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    questionTypeLookup: {[key: string]: MainQuestionType};
    order: BrandVueApi.ReportOrder;
    showTop: number | undefined;
    filterInstances: FilterInstance[];
    splitByType: BrandVueApi.IEntityType | undefined;
    baseExpressionOverride?: BaseExpressionDefinition;
    setDataState(state: PageCardState): void;
    setIsLowSample?(isLowSample: boolean): void;
    averageTypes: AverageType[];
    setAverageMentions(result: CrosstabAverageResults): void;
}

const ReportsPageColumnChart = (props: IReportsPageCardChartProps) => {
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

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);

        getCompetitionResults(
                props.reportPart,
                props.curatedFilters,
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
                props.baseExpressionOverride
            ).then(d => {
                if (!isCancelled) {
                    if (props.setIsLowSample) {
                        props.setIsLowSample(d.results.lowSampleSummary.length > 0);
                    }

                    setFooterAverages(d.averages);

                    const options = getColumnChartOptions(d.results.periodResults[0].resultsPerEntity,
                        props.reportPart.metric!,
                        report.decimalPlaces,
                        props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell,
                        report.highlightLowSample,
                        props.reportPart.part.displayMeanValues,
                        (a, b) => {},
                        report.displaySignificanceDifferences); //single column charts do not show significance
                    setChartOptions(options);
                    setSampleSizeMeta(d.results.sampleSizeMetadata);
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
        props.showTop,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        report.highlightLowSample,
        props.averageTypes,
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
            footerAverages={footerAverages}
            decimalPlaces={report.decimalPlaces}
        />
    );
};

export default ReportsPageColumnChart;