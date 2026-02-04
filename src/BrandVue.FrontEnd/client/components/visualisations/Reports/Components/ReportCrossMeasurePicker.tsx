import { Metric } from '../../../../metrics/metric';
import { CrossMeasure, MainQuestionType, ReportVariableAppendType } from '../../../../BrandVueApi';
import { DropdownToggle } from 'reactstrap';
import { getAvailableCrossMeasureFilterInstances } from '../../../helpers/SurveyVueUtils';
import MetricDropdownMenu from '../../Variables/MetricDropdownMenu';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';

interface IReportCrossMeasurePickerProps {
    metricsForBreaks: Metric[];
    selectedCrossMeasure: CrossMeasure | undefined;
    setCrossMeasures: (crossMeasures: CrossMeasure[]) => void;
    disabled: boolean;
    selectNoneText: string;
    reportVariableAppendType: ReportVariableAppendType;
    showCreateVariableButton?: boolean | undefined;
    selectedPart?: string;
    forceablySelectTwo?: boolean;
}

const ReportCrossMeasurePicker = (props: IReportCrossMeasurePickerProps) => {
    const crossMeasure = props.selectedCrossMeasure;
    const metric = crossMeasure ? props.metricsForBreaks.find(m => m.name == crossMeasure.measureName) : undefined;
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const {questionTypeLookup} = useMetricStateContext();

    const selectMetric = (metric: Metric | undefined) => {
        if (!metric) {
            props.setCrossMeasures([]);
        } else {
            const isBasedOnSingleChoice = questionTypeLookup[metric.name] == MainQuestionType.SingleChoice;
            const instances = getAvailableCrossMeasureFilterInstances(metric, entityConfiguration, false, isBasedOnSingleChoice)
            var newCrossMeasure = new CrossMeasure({
                measureName: metric.name,
                filterInstances: props.forceablySelectTwo ? instances.slice(0, 2) : instances,
                childMeasures: [],
                multipleChoiceByValue: false,
            });

            props.setCrossMeasures([newCrossMeasure]);
        }
    }

    const getSelectedDisplayText = () => {
        return metric ? metric.displayName : crossMeasure ? crossMeasure.measureName : props.selectNoneText;
    }

    const getToggleButton = () => {
        return (
            <DropdownToggle className="metric-selector-toggle toggle-button" disabled={props.disabled}>
                <div className="title">{getSelectedDisplayText()}</div>
                <i className="material-symbols-outlined">arrow_drop_down</i>
            </DropdownToggle>
        );
    }

    return (
        <MetricDropdownMenu
            toggleElement={getToggleButton()}
            metrics={props.metricsForBreaks}
            selectMetric={selectMetric}
            disabled={props.disabled}
            groupCustomVariables={true}
            reportVariableAppendType={props.reportVariableAppendType}
            selectedReportPart={props.selectedPart}
            showCreateVariableButton={props.showCreateVariableButton }
            selectNoneText={props.selectNoneText}
        />
    );
}

export default ReportCrossMeasurePicker;