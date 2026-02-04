import {
    AverageType,
    BaseExpressionDefinition,
    CrossMeasure,
    ReportOrder,
    IEntityType,
    DisplaySignificanceDifferences,
    SigConfidenceLevel,
} from '../../../../BrandVueApi';
import React from 'react';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { PageCardPlaceholder } from '../../shared/PageCardPlaceholder';
import TileTemplate from '../../shared/TileTemplate';
import TileTemplateChart from '../../Cards/TileTemplateChart';
import { FilterInstance } from '../../../../entity/FilterInstance';
import HighchartsCustomLegend from '../../HighchartsCustomLegend';
import { getColourMap } from '../../../helpers/ChromaHelper';
import { getMultiChartCrossbreakCompetitionResults } from './ChartData/CrossbreakCompetitionResultsDataHandler';
import { getSplitStackedBarChartOptions } from './HighchartsOptions/StackedBarOptions';
import {PartWithExtraData} from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from 'client/state/subsetSlice';
import { selectCurrentReport } from 'client/state/reportSelectors';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageSplitStackedBarCardProps {
    reportPart: PartWithExtraData;
    getDescriptionNode: (isLowSample: boolean) => JSX.Element;
    curatedFilters: CuratedFilters;
    breaks: CrossMeasure;
    setDataState(state: PageCardState): void;
    splitByType: IEntityType | undefined;
    filterInstances: FilterInstance[];
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
    averageTypes: AverageType[];
}

const ReportsPageSplitStackedBarCard = (props: IReportsPageSplitStackedBarCardProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [breakToColourMap, setBreakToColourMap] = React.useState<Map<string, string>>(new Map<string, string>());
    const [isLowSample, setIsLowSample] = React.useState<boolean>(false);//todo: this is always false
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;
    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);
        const showTop: number | undefined = undefined;
        getMultiChartCrossbreakCompetitionResults(
            props.reportPart,
            props.curatedFilters,
            [props.breaks],
            ReportOrder.ScriptOrderDesc,
            showTop,
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
            report.sigConfidenceLevel,
        ).then(d => {
            if (!isCancelled) {
                const legendMap = getColourMap(d.results[0].groupedBreakResults[0].breakResults.instanceResults[0]
                    .entityResults.map(e => e.entityInstance.name));

                const options = d.results.map(result => {
                    return getSplitStackedBarChartOptions(result.groupedBreakResults[0].breakResults,
                        props.reportPart.metric!,
                        report.decimalPlaces,
                        legendMap,
                        report.highlightLowSample,
                        props.showWeightedCounts,
                        report.displaySignificanceDifferences
                    )
                });
                setBreakToColourMap(legendMap);
                setChartOptions(options[0]);
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
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.breaks,
        props.baseExpressionOverride,
        report.highlightLowSample,
    ]);

    const chart = React.useRef<Highcharts.Chart>();

    const getDescriptionWithLegend = () => {
        return <>
            {props.getDescriptionNode(isLowSample)}
            <HighchartsCustomLegend keyToColourMap={breakToColourMap}
                chartReference={chart}
                reverse />
        </>;
    }

    if (isLoading) {
        return (
            <TileTemplate descriptionNode={props.getDescriptionNode(isLowSample)}>
                <PageCardPlaceholder />
            </TileTemplate>
        );
    }

    return <TileTemplateChart
        handleWidth
        descriptionNode={getDescriptionWithLegend()}
        getChartOptions={(width, height) => chartOptions}
        callback={c => chart.current = c}
    />;
};

export default ReportsPageSplitStackedBarCard;