import {
    BaseExpressionDefinition,
    MainQuestionType,
    SampleSizeMetadata,
    OverTimeAverageResults,
    AverageType,
    OverTimeResults,
    IEntityType,
    ReportOrder
} from '../../../../BrandVueApi';
import React from 'react';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { getColourMapFromEntityInstances } from '../../../helpers/ChromaHelper';
import ReportsPageColumnChartTemplate from './ReportsPageColumnChartTemplate';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { getOvertimeResults } from './ChartData/OvertimeResultsDataHandler';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { useAppSelector } from '../../../../state/store';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { selectSubsetId } from 'client/state/subsetSlice';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";
import { selectCurrentReport } from 'client/state/reportSelectors';

export interface IReportsPageOvertimeChartProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    setDataState(state: PageCardState): void;
    filterInstances: FilterInstance[];
    splitByType?: IEntityType;
    baseExpressionOverride?: BaseExpressionDefinition;
    setIsLowSample?(isLowSample: boolean): void;
    averageTypes: AverageType[];
    order: ReportOrder;
}

interface ITypedOvertimeChartProps extends IReportsPageOvertimeChartProps {
    getChartOptions: (results: OverTimeResults, colourMap: Map<string, string>) => Options;
}

const ReportsPageOvertimeChart = (props: ITypedOvertimeChartProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [sampleSizeMeta, setSampleSizeMeta] = React.useState<SampleSizeMetadata>();
    const [footerAverages, setFooterAverages] = React.useState<OverTimeAverageResults[] | undefined>();
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);

        getOvertimeResults(
            props.reportPart,
            props.curatedFilters,
            props.averageTypes,
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

                setFooterAverages(d.averageResults);
                const colourMap = getColourMapFromEntityInstances(d.results.entityWeightedDailyResults.map(r => r.entityInstance), props.reportPart.metric);
                const options = props.getChartOptions(d.results, colourMap);
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
        props.reportPart.metric?.name,
        report.decimalPlaces,
        props.curatedFilters,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        report.highlightLowSample,
        report.reportOrder,
        props.reportPart.selectedEntitySet,
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
            averageDescriptor={props.curatedFilters.average}
            decimalPlaces={report.decimalPlaces}
        />
    );
}
export default ReportsPageOvertimeChart;