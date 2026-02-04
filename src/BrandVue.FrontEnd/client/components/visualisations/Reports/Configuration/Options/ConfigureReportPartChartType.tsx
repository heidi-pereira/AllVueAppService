import { MainQuestionType, PartDescriptor, ReportType, FeatureCode } from "../../../../../BrandVueApi";
import { PartType } from '../../../../panes/PartType';
import { PartWithExtraData } from '../../ReportsPageDisplay';
import { getPartTypeForMetric } from '../../Utility/ReportPageBuilder';
import { useMetricStateContext } from '../../../../../metrics/MetricStateContext';
import { GetBaseMetric } from '../../../Variables/VariableModal/Utils/VariableComponentHelpers';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import { MixPanel } from '../../../../../components/mixpanel/MixPanel';
import { isFeatureEnabled } from '../../../../helpers/FeaturesHelper';
import { validateMetricForFunnelChart } from '../../Charts/HighchartsOptions/FunnelOptions';
import { ChartType, getCurrentChartType } from '../../Charts/ReportsChartHelper';
import Tooltip from 'client/components/Tooltip';
import { selectCurrentReport } from "client/state/reportSelectors";

interface IConfigureReportPartChartTypeProps {
    reportPart: PartWithExtraData;
    savePartChanges(newPart: PartDescriptor);
    isUsingOverTime: boolean;
    isUsingWaves: boolean;
    isUsingBreaks: boolean;
}

interface ChartOption {
    chartType: ChartType;
    disabled?: boolean;
    tooltipMessage?: string;
}

const ConfigureReportPartChartType = (props: IConfigureReportPartChartTypeProps) => {
    const { selectableMetricsForUser: metrics, questionTypeLookup } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const reportType = currentReportPage.report.reportType;
    
    const reportPart = props.reportPart;
    const partType = reportPart.part.partType;
    const isMultiEntity = reportPart.metric!.entityCombination.length > 1;
    const isSingleEntity = reportPart.metric?.entityCombination.length === 1;

    const metricToCheck = GetBaseMetric(reportPart.metric, metrics, variables) ?? reportPart.metric;
    let mainQuestionType = questionTypeLookup[metricToCheck!.name];

    const updateChartType = (newPartType: string) => {
        const modifiedPart = new PartDescriptor(reportPart.part);
        modifiedPart.partType = newPartType;
        props.savePartChanges(modifiedPart);
    }

    const getPartTypeForChartType = (chartType: ChartType) => {
        switch (chartType) {
            case ChartType.Line:
                return PartType.ReportsCardLine;
            case ChartType.Stacked:
                return PartType.ReportsCardStackedMulti;
            case ChartType.Bar:
                return isMultiEntity ? PartType.ReportsCardMultiEntityMultipleChoice : getPartTypeForMetric(reportPart.metric!, questionTypeLookup, ReportType.Chart, false);
            case ChartType.Doughnut:
                return PartType.ReportsCardDoughnut;
            case ChartType.Heatmap:
                return PartType.ReportsCardHeatmapImage;
            case ChartType.Funnel:
                return PartType.ReportsCardFunnel;
            default:
                return getPartTypeForMetric(reportPart.metric!, questionTypeLookup, ReportType.Chart, false);
        }
    }

    const selectChartType = (chartType: ChartType) => {
        const newPartType = getPartTypeForChartType(chartType);

        MixPanel.track("reportChartTypeChanged", {ReportChartType: chartType});
        updateChartType(newPartType);
    }

    const isButtonSelected = (chartType: ChartType) => {
        return chartType == getCurrentChartType(partType, props.isUsingWaves, props.isUsingOverTime, reportPart.metric!, questionTypeLookup);
    }

    const getButtonClass = (chartType: ChartType) => {
        return "hollow-button" + (isButtonSelected(chartType) ? "" : " inactive");
    }

    const getButtonIconName = (chartType: ChartType) => {
        switch (chartType) {
            case ChartType.Line:
                return "timeline";
            case ChartType.Stacked:
                return "stacked_bar_chart";
            case ChartType.Doughnut:
                return "donut_large";
            case ChartType.Funnel:
                return "filter_alt";
            default:
                return "bar_chart";
        }
    }

    const getLineChartTooltipMessage = (isLineChartDisabled: boolean,
        overTimeName: string,
        hasMultipleFilterBySelected: boolean) => {
        if (isLineChartDisabled) {
            return `Enable ${overTimeName} to use line charts`;
        }
        if(hasMultipleFilterBySelected) {
            return 'Line charting is only available for single charts';
        }
        return undefined;
    }


    const getChartTypeButtonsToShow = (): ChartOption[] => {
        if (partType == PartType.ReportsCardHeatmapImage) {
            return [];
        }
        const chartTypes: ChartOption[] = [];
        
        const hasMultipleFilterBySelected = reportPart.part.multipleEntitySplitByAndFilterBy?.filterByEntityTypes.length > 1;
        chartTypes.push({
            chartType: ChartType.Bar,
            disabled: hasMultipleFilterBySelected,
            tooltipMessage: hasMultipleFilterBySelected ? 'Column charting is only available for single charts' : undefined
        });

        const isZeroEntity = !isSingleEntity && !isMultiEntity;
        chartTypes.push({
            chartType: ChartType.Stacked,
            disabled: isZeroEntity,
            tooltipMessage: isZeroEntity ? 'Stacked charts are not supported for this question' : undefined
        });

        const donutOption: ChartOption = { chartType: ChartType.Doughnut };
        if (isMultiEntity || mainQuestionType !== MainQuestionType.SingleChoice) {
            donutOption.disabled = true;
            donutOption.tooltipMessage = "Doughnut charts are not supported for this question";
        } else if (props.isUsingBreaks) {
            donutOption.disabled = true;
            donutOption.tooltipMessage = "Doughnut charts are not compatible with breaks";
        } else if (props.isUsingWaves) {
            donutOption.disabled = true;
            donutOption.tooltipMessage = "Doughnut charts are not compatible with waves";
        } else if (props.isUsingOverTime) {
            donutOption.disabled = true;
            donutOption.tooltipMessage = `Doughnut charts are not compatible with over time data`;
        }
        chartTypes.push(donutOption);

        const isOverTimeFeatureEnabled = isFeatureEnabled(FeatureCode.Overtime_data);
        const overTimeName = isOverTimeFeatureEnabled ? 'over time data' : 'waves';
        const isLineChartDisabled = !(props.isUsingOverTime || props.isUsingWaves);
        chartTypes.push({
            chartType: ChartType.Line,
            disabled: isLineChartDisabled || hasMultipleFilterBySelected,
            tooltipMessage: getLineChartTooltipMessage(isLineChartDisabled, overTimeName, hasMultipleFilterBySelected)
        });

        const funnelOption: ChartOption = { chartType: ChartType.Funnel };
        const metricVariable = variables.find(v => v.id == reportPart.metric!.variableConfigurationId);
        const metricVariableValidationErrors = validateMetricForFunnelChart(reportPart.metric!, metricVariable);
        if (metricVariableValidationErrors.length > 0) {
            funnelOption.disabled = true;
            funnelOption.tooltipMessage = "Funnel charts are not supported for this question";
        } else if (props.isUsingBreaks && props.isUsingWaves) {
            funnelOption.disabled = true;
            funnelOption.tooltipMessage = "Funnel charts are not compatible with both breaks and waves at the same time";
        }
        chartTypes.push(funnelOption);

        return chartTypes;
    }

    const getButton = (option: ChartOption) => {
        const isSelected = isButtonSelected(option.chartType);
        const isDisabled = !isSelected && option.disabled == true;
        const button = (
            <button title={option.chartType}
                className={getButtonClass(option.chartType)}
                onClick={() => selectChartType(option.chartType)}
                aria-selected={isButtonSelected(option.chartType)}
                aria-label={option.chartType}
                disabled={isDisabled}
            >
                <i className="material-symbols-outlined">{getButtonIconName(option.chartType)}</i>
            </button>
        );
        if (isDisabled && option.tooltipMessage) {
            return (<Tooltip placement='top' title={option.tooltipMessage}>{button}</Tooltip>);
        }
        return button;
    }

    const getButtons = (buttonsToShow: ChartOption[]) => {
        return buttonsToShow.map(getButton);
    }

    const buttonsToShow = getChartTypeButtonsToShow();
    if (reportType == ReportType.Table || partType == PartType.ReportsCardText || reportPart.metric == null || buttonsToShow.length <= 1) {
        return null;
    }

    return (
        <div className="configure-chart-type">
            <label className="category-label">Chart type</label>
            <div className="buttons">
                {getButtons(buttonsToShow)}
            </div>
        </div>
    );
};

export default ConfigureReportPartChartType;