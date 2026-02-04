import React from 'react';
import { Metric } from '../../../metrics/metric';
import { DropdownToggle } from 'reactstrap';
import { ReportVariableAppendType } from '../../../BrandVueApi';
import { getMatchedMetrics } from '../../../metrics/MetricDropdownHelper';
import { SharedDropdownMenu } from '../shared/SharedDropdownMenu';

export interface IMetricDropdownMenuProps {
    toggleElement: React.ReactElement<DropdownToggle>;
    metrics: Metric[];
    selectMetric(metric: Metric | undefined): void;
    showCreateVariableButton?: boolean | undefined;
    disabled?: boolean;
    groupCustomVariables: boolean;
    shouldCreateWaveVariable?: boolean
    reportVariableAppendType?: ReportVariableAppendType;
    selectedReportPart?: string;
    selectNoneText?: string;
    hasWarning?: boolean;
}

export const MetricDropdownMenu = (props: IMetricDropdownMenuProps) => {
    const [searchQuery, setSearchQuery] = React.useState<string>("");

    return (
        <SharedDropdownMenu
            dropdownItems={getMatchedMetrics(props.metrics, searchQuery, props.groupCustomVariables, props.selectMetric, props.hasWarning ?? false)}
            toggleElement={props.toggleElement}
            selectNone={() => props.selectMetric(undefined)}
            showCreateVariableButton={props.showCreateVariableButton}
            disabled={props.disabled}
            shouldCreateWaveVariable={props.shouldCreateWaveVariable}
            reportVariableAppendType={props.reportVariableAppendType}
            selectedReportPart={props.selectedReportPart}
            searchQuery={searchQuery}
            setSearchQuery={setSearchQuery}
            selectNoneText={props.selectNoneText}
            hasWarning={props.hasWarning}
        />
    );
};

export default MetricDropdownMenu;