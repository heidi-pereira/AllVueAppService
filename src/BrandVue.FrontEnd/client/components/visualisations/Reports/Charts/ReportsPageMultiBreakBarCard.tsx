import React from 'react';
import Highcharts, {Options} from "highcharts";
import {getCrossbreakCompetitionResults} from "./ChartData/CrossbreakCompetitionResultsDataHandler";
import {getMultiBreakBarChartOptions} from "./HighchartsOptions/BarOptions";
import {NoDataError} from "../../../../NoDataError";
import {PageCardState} from "../../shared/SharedEnums";
import HighchartsCustomLegend from "../../HighchartsCustomLegend";
import TileTemplate from "../../shared/TileTemplate";
import {PageCardPlaceholder} from "../../shared/PageCardPlaceholder";
import TileTemplateChart from "../../Cards/TileTemplateChart";
import {PartWithExtraData} from "../ReportsPageDisplay";
import {CuratedFilters} from "../../../../filter/CuratedFilters";
import * as BrandVueApi from "../../../../BrandVueApi";
import {FilterInstance} from "../../../../entity/FilterInstance";
import {
    AverageType,
    BaseExpressionDefinition,
    CrossMeasure,
    DisplaySignificanceDifferences,
    Features,
    SigConfidenceLevel
} from "../../../../BrandVueApi";
import {BarColour} from "../Cards/ReportsPageCardChartContent";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from 'client/state/subsetSlice';
import { selectCurrentReport } from 'client/state/reportSelectors';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";


interface IReportsPageMultiBreakBarCardProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    getDescriptionNode: (isLowSample: boolean) => JSX.Element;
    setDataState(state: PageCardState): void;
    order: BrandVueApi.ReportOrder;
    showTop: number | undefined;
    primaryFilterInstance: FilterInstance;
    filterInstances: FilterInstance[];
    splitByType: BrandVueApi.IEntityType | undefined;
    breaks: CrossMeasure[];
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
    averageTypes: AverageType[];
}

const ReportsPageMultiBreakBarCard = (props: IReportsPageMultiBreakBarCardProps) => {
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
            props.breaks,
            props.order,
            props.showTop,
            props.filterInstances,
            [], // Average selector has been disabled for multi break charts
            questionTypeLookup,
            metrics,
            variables,
            subsetId,
            timeSelection,
            props.splitByType,
            props.baseExpressionOverride,
            report.highlightSignificance,
            props.primaryFilterInstance,
            undefined,
            report.sigConfidenceLevel
        ).then(d => {
            if (!isCancelled) {
                const isLowSample = d.results.groupedBreakResults.some(r => r.breakResults.lowSampleSummary.length > 0)
                setIsLowSample(isLowSample);
                setBreakToColourMap(new Map([[props.primaryFilterInstance.instance.name, BarColour]]));

                const options = getMultiBreakBarChartOptions(d.results,
                    props.reportPart.metric!,
                    report.decimalPlaces,
                    props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell,
                    report.highlightLowSample,
                    props.showWeightedCounts,
                    report.displaySignificanceDifferences
                );
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
        props.primaryFilterInstance
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
}

export default ReportsPageMultiBreakBarCard;
