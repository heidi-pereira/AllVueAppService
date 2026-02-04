import { CuratedFilters } from '../../../../filter/CuratedFilters';
import React from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { PageCardPlaceholder } from '../../shared/PageCardPlaceholder';
import TileTemplate from '../../shared/TileTemplate';
import TileTemplateChart from '../../Cards/TileTemplateChart';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { getCompetitionResults } from './ChartData/CompetitionResultsDataHandler';
import { getBarChartOptions } from './HighchartsOptions/BarOptions';
import { BaseExpressionDefinition, DisplaySignificanceDifferences } from '../../../../BrandVueApi';
import {PartWithExtraData} from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import { selectCurrentReport } from 'client/state/reportSelectors';
import { selectSubsetId } from 'client/state/subsetSlice';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageBarCardProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    getDescriptionNode: (isLowSample: boolean) => JSX.Element;
    setDataState(state: PageCardState): void;
    order: BrandVueApi.ReportOrder;
    showTop: number | undefined;
    filterInstances: FilterInstance[];
    splitByType: BrandVueApi.IEntityType | undefined
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
}

const ReportsPageBarCard = (props: IReportsPageBarCardProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
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

        getCompetitionResults(
                props.reportPart,
                props.curatedFilters,
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
            ).then(d => {
                if (!isCancelled) {
                    const isLowSample = d.results.lowSampleSummary.length > 0;
                    setIsLowSample(isLowSample);
                    const options = getBarChartOptions(d.results.periodResults[0].resultsPerEntity,
                        props.reportPart.metric!,
                        report.decimalPlaces,
                        props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell,
                        report.highlightLowSample,
                        props.showWeightedCounts,
                        [],
                        report.displaySignificanceDifferences);
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
        props.showTop,
        props.splitByType,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        report.highlightLowSample,
        subsetId
    ]);

    const chart = React.useRef<Highcharts.Chart>();

    if (isLoading) {
        return (
            <TileTemplate descriptionNode={props.getDescriptionNode(isLowSample)}>
                <PageCardPlaceholder />
            </TileTemplate>
        );
    }

    return <TileTemplateChart
        handleWidth
        descriptionNode={props.getDescriptionNode(isLowSample)}
        getChartOptions={(width, height) => chartOptions}
        callback={c => chart.current = c}
    />;
};

export default ReportsPageBarCard;