import {
    BaseExpressionDefinition,
    MainQuestionType,
    EntityType,
    OverTimeResults,
    ReportOrder,
    IEntityType,
    DisplaySignificanceDifferences
} from '../../../../BrandVueApi';
import React from 'react';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { getColourMapFromEntityInstances } from '../../../helpers/ChromaHelper';
import HighchartsCustomLegend from '../../HighchartsCustomLegend';
import TileTemplate from '../../shared/TileTemplate';
import TileTemplateChart from '../../Cards/TileTemplateChart';
import { PageCardPlaceholder } from '../../shared/PageCardPlaceholder';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { getOvertimeResults } from './ChartData/OvertimeResultsDataHandler';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { useAppSelector } from '../../../../state/store';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { selectSubsetId } from 'client/state/subsetSlice';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";
import { selectCurrentReport } from 'client/state/reportSelectors';

export interface IReportsPageOvertimeCardProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    getDescriptionNode: (isLowSample: boolean) => JSX.Element;
    setDataState(state: PageCardState): void;
    filterInstances: FilterInstance[];
    splitByType?: IEntityType;
    baseExpressionOverride?: BaseExpressionDefinition;
    order: ReportOrder;
}

interface ITypedOvertimeChartProps extends IReportsPageOvertimeCardProps {
    getChartOptions: (results: OverTimeResults, colourMap: Map<string, string>) => Options;
    noScroll?: boolean;
    handleHeight?: boolean;
    resizeElementClass?: string;
}

const ReportsPageOvertimeCard = (props: ITypedOvertimeChartProps) => {
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

        getOvertimeResults(
            props.reportPart,
            props.curatedFilters,
            [], //averages are not shown on cards
            questionTypeLookup,
            metrics,
            variables,
            props.filterInstances,
            subsetId,
            timeSelection,
            props.splitByType,
            props.baseExpressionOverride,
            true,
        ).then(d => {
            if (!isCancelled) {
                const isLowSample = d.results.lowSampleSummary.length > 0;
                setIsLowSample(isLowSample);

                const colourMap = getColourMapFromEntityInstances(d.results.entityWeightedDailyResults.map(r => r.entityInstance), props.reportPart.metric);
                const options = props.getChartOptions(d.results, colourMap);
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
        report.decimalPlaces,
        props.curatedFilters,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        report.highlightLowSample,
        report.reportOrder,
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
            <TileTemplate descriptionNode={props.getDescriptionNode(isLowSample)} noScroll={props.noScroll}>
                <PageCardPlaceholder />
            </TileTemplate>
        );
    }

    return <TileTemplateChart
        handleHeight={props.handleHeight}
        handleWidth
        descriptionNode={getDescriptionWithLegend()}
        getChartOptions={(width, height) => chartOptions}
        callback={c => chart.current = c}
        resizeElementClass={props.resizeElementClass}
    />;
};

export default ReportsPageOvertimeCard;