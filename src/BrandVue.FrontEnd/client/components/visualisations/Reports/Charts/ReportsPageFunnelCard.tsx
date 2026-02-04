import { CuratedFilters } from '../../../../filter/CuratedFilters';
import React from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { useAppSelector } from 'client/state/store';
import { PageCardPlaceholder } from '../../shared/PageCardPlaceholder';
import TileTemplate from '../../shared/TileTemplate';
import TileTemplateChart from '../../Cards/TileTemplateChart';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { getCompetitionResults } from './ChartData/CompetitionResultsDataHandler';
import { getFunnelChartOptions } from './HighchartsOptions/FunnelOptions';
import { BaseExpressionDefinition } from '../../../../BrandVueApi';
import {PartWithExtraData} from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { setIsSettingsChange, setReportErrorState } from '../../../../state/reportSlice';
import { ReportErrorState } from '../../shared/ReportErrorState';
import { useAppDispatch } from 'client/state/store';
import { UnsupportedVariableError } from './HighchartsOptions/CustomErrors';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { selectSubsetId } from 'client/state/subsetSlice';
import { selectCurrentReport } from 'client/state/reportSelectors';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageFunnelCardProps {
    reportPart: PartWithExtraData;
    curatedFilters: CuratedFilters;
    getDescriptionNode: (isLowSample: boolean) => JSX.Element;
    setDataState(state: PageCardState): void;
    order: BrandVueApi.ReportOrder;
    filterInstances: FilterInstance[];
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
}

const ReportsPageFunnelCard = (props: IReportsPageFunnelCardProps) => {
    const dispatch = useAppDispatch();

    const [chartOptions, setChartOptions] = React.useState<Options>();
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [isLowSample, setIsLowSample] = React.useState<boolean>(false);
    const { questionTypeLookup, selectableMetricsForUser: metrics } = useMetricStateContext();
    const { variables, loading: isVariablesLoading } = useAppSelector(selectHydratedVariableConfiguration);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;
    
    React.useEffect(() => {
        let isCancelled = false;
        setIsLoading(true);

        const fetchData = async () => {
            try {
                const d = await getCompetitionResults(
                    props.reportPart,
                    props.curatedFilters,
                    props.order,
                    undefined,
                    props.filterInstances,
                    [],
                    questionTypeLookup,
                    metrics,
                    variables,
                    subsetId,
                    timeSelection,
                    undefined,
                    props.baseExpressionOverride
                );

                if (!isCancelled) {
                    const isLowSample = d.results.lowSampleSummary.length > 0;
                    setIsLowSample(isLowSample);
                    const variable = variables.find(v => v.id === props.reportPart.metric!.variableConfigurationId);
                    const options = getFunnelChartOptions(d.results.periodResults[0].resultsPerEntity,
                        props.reportPart.metric!,
                        report.decimalPlaces,
                        props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell,
                        report.highlightLowSample,
                        props.showWeightedCounts,
                        report.displaySignificanceDifferences,
                        variable);
                    setChartOptions(options ? options[0] : undefined);
                    setIsLoading(false);
                }
            } catch (e: any) {
                if (!isCancelled) {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        props.setDataState(PageCardState.NoData);
                    } else {
                        if (e instanceof UnsupportedVariableError) {
                            dispatch(setIsSettingsChange(false));
                            dispatch(setReportErrorState({ isError: true, errorMessage: e.message } as ReportErrorState));
                            props.setDataState(PageCardState.UnsupportedVariable);
                        }
                        else {
                            props.setDataState(PageCardState.Error);
                            throw e;
                        }
                    }
                }
            }
        };

        if (!isVariablesLoading) {
            fetchData();
        }

        return () => { isCancelled = true };
    }, [
        report.decimalPlaces,
        props.curatedFilters,
        props.order,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        report.highlightLowSample,
        isVariablesLoading
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

export default ReportsPageFunnelCard;