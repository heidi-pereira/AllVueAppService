import {
    AverageType,
    BaseExpressionDefinition,
    CrossMeasure,
    MainQuestionType,
    IEntityType,
} from '../../../../BrandVueApi';
import React from 'react';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { getColourMap } from '../../../helpers/ChromaHelper';
import { getLineChartOptions, getWaveComparisonSeriesNames } from './HighchartsOptions/LineOptions';
import { getWaveComparisonResults } from './ChartData/WaveComparisonResultsDataHandler';
import HighchartsCustomLegend from '../../HighchartsCustomLegend';
import TileTemplate from '../../shared/TileTemplate';
import TileTemplateChart from '../../Cards/TileTemplateChart';
import { PageCardPlaceholder } from '../../shared/PageCardPlaceholder';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { getAverageDisplayText, splitCrosstabAverageResults } from '../../AverageHelper';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from 'client/state/subsetSlice';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";
import { selectCurrentReport } from 'client/state/reportSelectors';

interface IReportsPageLineCardProps {
    reportPart: PartWithExtraData;
    waves: CrossMeasure | undefined;
    breaks?: CrossMeasure;
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    getDescriptionNode: (isLowSample: boolean) => JSX.Element;
    setDataState(state: PageCardState): void;
    filterInstances: FilterInstance[];
    splitByType?: IEntityType;
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
    averageTypes: AverageType[];
}

const ReportsPageLineCard = (props: IReportsPageLineCardProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [seriesColourMap, setSeriesColourMap] = React.useState<Map<string, string>>(new Map<string, string>());
    const [isLowSample, setIsLowSample] = React.useState<boolean>(false);
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);
        
        getWaveComparisonResults(
            props.reportPart,
            props.curatedFilters,
            props.waves,
            props.filterInstances,
            [], //averages are not shown on cards
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
                const isLowSample = d.results.lowSampleSummary.length > 0;
                setIsLowSample(isLowSample);

                const { averagesToChart } = splitCrosstabAverageResults(d.averageResults, props.reportPart.metric);
                const averageLabels = averagesToChart.map(a => getAverageDisplayText(a.averageType));
                const waveLabels = getWaveComparisonSeriesNames(d.results, props.reportPart.metric!);
                let colourMap = getColourMap(waveLabels.concat(averageLabels));
                const options = getLineChartOptions(d.results,
                    props.reportPart.metric!,
                    colourMap,
                    report.decimalPlaces,
                    report.highlightLowSample,
                    props.showWeightedCounts,
                    false,
                    true,
                    report.highlightSignificance,
                    () => { }, //do nothing on a card
                    false,
                    report.displaySignificanceDifferences
                ); 
                setSeriesColourMap(colourMap);
                setChartOptions(options);
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
        subsetId,
        timeSelection
    ]);

    const chart = React.useRef<Highcharts.Chart>();

    const getDescriptionWithLegend = () => {
        return <>
            {props.getDescriptionNode(isLowSample)}
            <HighchartsCustomLegend keyToColourMap={seriesColourMap}
                chartReference={chart} />
        </>;
    }

    if (isLoading) {
        return (
            <TileTemplate descriptionNode={props.getDescriptionNode(isLowSample)} noScroll>
                <PageCardPlaceholder />
            </TileTemplate>
        );
    }

    return <TileTemplateChart
        handleHeight
        handleWidth
        descriptionNode={getDescriptionWithLegend()}
        getChartOptions={(width, height) => chartOptions}
        callback={c => chart.current = c}
        resizeElementClass="reports-page-line-card"
    />;
};

export default ReportsPageLineCard;