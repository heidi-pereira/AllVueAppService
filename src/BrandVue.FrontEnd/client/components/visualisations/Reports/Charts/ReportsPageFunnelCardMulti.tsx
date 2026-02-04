import { CuratedFilters } from '../../../../filter/CuratedFilters';
import React from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import Highcharts, { Options } from 'highcharts';
import { PageCardState } from '../../shared/SharedEnums';
import { NoDataError } from '../../../../NoDataError';
import { PageCardPlaceholder } from '../../shared/PageCardPlaceholder';
import TileTemplate from '../../shared/TileTemplate';
import TileTemplateMultiChart from '../../Cards/TileTemplateMultiChart';
import { FilterInstance } from '../../../../entity/FilterInstance';
import { BaseExpressionDefinition, CrossMeasure, DisplaySignificanceDifferences } from '../../../../BrandVueApi';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { setIsSettingsChange, setReportErrorState } from '../../../../state/reportSlice';
import { ReportErrorState } from '../../shared/ReportErrorState';
import { useDispatch } from 'react-redux';
import { UnsupportedVariableError } from './HighchartsOptions/CustomErrors';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { selectCurrentReport } from 'client/state/reportSelectors';
import { useAppSelector } from 'client/state/store';
import { getFunnelChartOptions } from './HighchartsOptions/FunnelOptions';
import { getMultiFunnelResults, getLowSampleFromMultiFunnelResults } from '../Utility/ReportsPageDataHelper';
import { selectSubsetId } from 'client/state/subsetSlice';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IReportsPageFunnelCardMultiProps {
    reportPart: PartWithExtraData;
    waves: CrossMeasure | undefined;
    breaks: CrossMeasure[] | undefined;
    curatedFilters: CuratedFilters;
    getDescriptionNode: (isLowSample: boolean) => JSX.Element;
    setDataState(state: PageCardState): void;
    order: BrandVueApi.ReportOrder;
    filterInstances: FilterInstance[];
    baseExpressionOverride?: BaseExpressionDefinition;
    showWeightedCounts: boolean;
}

const ReportsPageFunnelCardMulti = (props: IReportsPageFunnelCardMultiProps) => {
    const dispatch = useDispatch();

    const [chartOptions, setChartOptions] = React.useState<Options[]>([]);
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
                const d = await getMultiFunnelResults(props.reportPart,
                    props.curatedFilters,
                    props.filterInstances,
                    questionTypeLookup,
                    metrics,
                    variables,
                    props.order,
                    subsetId,
                    timeSelection,
                    props.baseExpressionOverride,
                    props.waves,
                    props.breaks);

                if (d !== undefined && !isCancelled) {
                    setIsLowSample(getLowSampleFromMultiFunnelResults(d.results));

                    const variable = variables.find(v => v.id === props.reportPart.metric!.variableConfigurationId);
                    const options = getFunnelChartOptions(d.results,
                        props.reportPart.metric!,
                        report.decimalPlaces,
                        props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell,
                        report.highlightLowSample,
                        false,
                        report.displaySignificanceDifferences,
                        variable);
                    setChartOptions(options);
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
        props.curatedFilters,
        props.order,
        JSON.stringify(props.filterInstances.map(i => `${i.type.identifier}-${i.instance.id}`)),
        props.baseExpressionOverride,
        report.highlightLowSample,
        isVariablesLoading,
        props.waves,
        props.breaks
    ]);

    const chart = React.useRef<Highcharts.Chart>();

    if (isLoading) {
        return (
            <TileTemplate descriptionNode={props.getDescriptionNode(isLowSample)}>
                <PageCardPlaceholder />
            </TileTemplate>
        );
    }

    return <TileTemplateMultiChart
        handleWidth
        descriptionNode={props.getDescriptionNode(isLowSample)}
        getChartOptions={(width, height) => chartOptions}
        callback={c => chart.current = c}
    />;
};

export default ReportsPageFunnelCardMulti;