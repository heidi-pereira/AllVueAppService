import { getOverTimeFunnelChartOptions } from "client/components/visualisations/Reports/Charts/HighchartsOptions/FunnelOptions";
import { useAppSelector } from "client/state/store";
import { selectCurrentReport } from 'client/state/reportSelectors';
import { selectHydratedVariableConfiguration } from "client/state/variableConfigurationsSlice";
import { IReportsPageOvertimeCardProps } from "client/components/visualisations/Reports/Charts/ReportsPageOvertimeCard";
import { Options } from "highcharts";
import React from "react";
import { useMetricStateContext } from "client/metrics/MetricStateContext";
import { getOvertimeResults } from "client/components/visualisations/Reports/Charts/ChartData/OvertimeResultsDataHandler";
import { NoDataError } from "client/NoDataError";
import { PageCardState } from "client/components/visualisations/shared/SharedEnums";
import { PageCardPlaceholder } from "client/components/visualisations/shared/PageCardPlaceholder";
import TileTemplate from "client/components/visualisations/shared/TileTemplate";
import TileTemplateMultiChart from "client/components/visualisations/Cards/TileTemplateMultiChart";
import { selectSubsetId } from "client/state/subsetSlice";

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

const ReportsPageOvertimeFunnelCard = (props: IReportsPageOvertimeCardProps) => {
    const [chartOptions, setChartOptions] = React.useState<Options[]>([]);
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

        getOvertimeResults(
            props.reportPart,
            props.curatedFilters,
            [],
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
                    const isLowSample = d.results.lowSampleSummary.length > 0;
                    setIsLowSample(isLowSample);

                    const variable = variables.find(v => v.id === props.reportPart.metric?.variableConfigurationId);
                    const options = getOverTimeFunnelChartOptions(
                        d.results.entityWeightedDailyResults,
                        props.reportPart.metric!,
                        props.curatedFilters.average,
                        report.decimalPlaces,
                        report.isDataWeighted,
                        report.highlightLowSample,
                        false,
                        report.displaySignificanceDifferences,
                        variable
                    );
                setChartOptions(options);
                setIsLoading(false);
            }
        }).catch((e: any) => {
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
        props.order,
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

export default ReportsPageOvertimeFunnelCard;