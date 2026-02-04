import {
    AverageType,
    BaseExpressionDefinition,
    CrossMeasure,
    CrosstabAverageResults,
    MainQuestionType,
    IEntityType,
    SampleSizeMetadata,
} from '../../../../BrandVueApi';
import React from 'react';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { getColourMap } from '../../../helpers/ChromaHelper';
import { getLineChartOptions, getWaveComparisonSeriesNames } from './HighchartsOptions/LineOptions';
import ReportsPageColumnChartTemplate from './ReportsPageColumnChartTemplate';
import { getWaveComparisonResults } from './ChartData/WaveComparisonResultsDataHandler';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { getAverageDisplayText } from '../../AverageHelper';
import toast from 'react-hot-toast';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from '../../../../state/variableConfigurationSelectors';
import { useAppSelector } from "../../../../state/store";
import { selectSubsetId } from '../../../../state/subsetSlice';
import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";
import { selectCurrentReport } from 'client/state/reportSelectors';

interface IReportsPageLineChartProps {
    reportPart: PartWithExtraData;
    waves: CrossMeasure | undefined;
    breaks?: CrossMeasure;
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    setDataState(state: PageCardState): void;
    filterInstances: FilterInstance[];
    splitByType?: IEntityType;
    baseExpressionOverride?: BaseExpressionDefinition;
    setIsLowSample?(isLowSample: boolean): void;
    averageTypes: AverageType[];
    updateWave(w: CrossMeasure): void;
}

const ReportsPageLineChart = (props: IReportsPageLineChartProps) => {
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

    const selectComparandWave = (selectedWaveName: string) => {
        const updatedWave = new CrossMeasure({ ...props.waves! });
        updatedWave.significanceFilterInstanceComparandName = selectedWaveName;
        props.updateWave(updatedWave);
    }

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);

        getWaveComparisonResults(
            props.reportPart,
            props.curatedFilters,
            props.waves,
            props.filterInstances,
            props.averageTypes,
            report.highlightSignificance,
            questionTypeLookup,
            metrics,
            variables,
            subsetId,
            timeSelection,
            props.breaks,
            props.splitByType,
            props.baseExpressionOverride,
            report.sigConfidenceLevel
        ).then(d => {
            if (!isCancelled) {
                if (props.setIsLowSample) {
                    props.setIsLowSample(d.results.lowSampleSummary.length > 0);
                }
                
                setFooterAverages(d.averageResults);
                const colourMap = getColourMap(getWaveComparisonSeriesNames(d.results, props.reportPart.metric!)
                    .concat(d.averageResults.map(a => getAverageDisplayText(a.averageType))));

                const options = getLineChartOptions(d.results,
                    props.reportPart.metric!,
                    colourMap,
                    report.decimalPlaces,
                    report.highlightLowSample,
                    report.includeCounts,
                    true,
                    false,
                    report.highlightSignificance,
                    (selectedWaveName) => selectComparandWave(selectedWaveName),
                    props.reportPart.part.displayMeanValues,
                    report.displaySignificanceDifferences
                );
                setChartOptions(options);
                setSampleSizeMeta(d.results.sampleSizeMetadata);
                if (d.results.errorMessage) {
                    toast.error((d.results.errorMessage));
                }
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
        props.waves,
        props.breaks,
        report.decimalPlaces,
        props.curatedFilters,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        report.highlightLowSample,
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
            decimalPlaces={report.decimalPlaces}
        />
    );
}

export default ReportsPageLineChart;