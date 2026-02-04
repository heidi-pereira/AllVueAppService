import {
    AverageType,
    BaseExpressionDefinition,
    CrossMeasure,
} from '../../../../BrandVueApi';
import * as BrandVueApi from "../../../../BrandVueApi";
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
import { getCrossbreakCompetitionResults } from './ChartData/CrossbreakCompetitionResultsDataHandler';
import { getSplitBarChartOptions } from './HighchartsOptions/BarOptions';
import {PartWithExtraData} from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from 'client/state/subsetSlice';
import { selectCurrentReport } from 'client/state/reportSelectors';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageSplitBarCardProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    getDescriptionNode: (isLowSample: boolean) => JSX.Element;
    setDataState(state: PageCardState): void;
    order: BrandVueApi.ReportOrder;
    showTop: number | undefined;
    filterInstances: FilterInstance[];
    splitByType: BrandVueApi.IEntityType | undefined;
    breaks: CrossMeasure;
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
    averageTypes: AverageType[];
    updateBreak(b: CrossMeasure[]): void;
}

const ReportsPageSplitBarCard = (props: IReportsPageSplitBarCardProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [breakToColourMap, setBreakToColourMap] = React.useState<Map<string, string>>(new Map<string, string>());
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
                undefined, // Placeholder for optional parameter: significance threshold
                undefined, // Placeholder for optional parameter: additional configuration
        report.sigConfidenceLevel
            ).then(d => {
                if (!isCancelled) {
                    const singleResult = d.results.groupedBreakResults[0].breakResults;
                    const isLowSample = singleResult.lowSampleSummary.length > 0;
                    setIsLowSample(isLowSample);
                    const legendItems = singleResult.instanceResults.map(r => r.breakName);
                    const legendMap = getColourMap(legendItems);
                    const options = getSplitBarChartOptions(singleResult, props.reportPart.metric!, legendMap, report.decimalPlaces,
                        props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell, report.highlightLowSample,
                        props.showWeightedCounts, [], report.displaySignificanceDifferences);
                    setBreakToColourMap(legendMap);
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
        report.decimalPlaces,
        props.curatedFilters,
        props.order,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.breaks,
        props.showTop,
        props.baseExpressionOverride,
        report.highlightLowSample,
        subsetId,
        timeSelection
    ]);

    const chart = React.useRef<Highcharts.Chart>();

    const getDescriptionWithLegend = () => {
        return <>
            {props.getDescriptionNode(isLowSample)}
            <HighchartsCustomLegend keyToColourMap={breakToColourMap}
                chartReference={chart} />
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

export default ReportsPageSplitBarCard;