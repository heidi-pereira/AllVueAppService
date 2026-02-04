import React from 'react';
import { MainQuestionType, ReportVariableAppendType } from '../../../../BrandVueApi';
import MetricDropdownMenu from '../../Variables/MetricDropdownMenu';
import { Metric } from '../../../../metrics/metric';
import { DropdownToggle, ButtonDropdown, DropdownMenu } from 'reactstrap';
import FilterValueMappingSelector from '../Filtering/FilterValueMappingSelector';
import { MetricFilterState } from '../../../../filter/metricFilterState';
import { IFilterAndDefaultInstances, metricValidAsFilter } from '../Filtering/FilterHelper';
import { DragDropContext, Droppable, DroppableProvided, Draggable, DraggableProvided, DropResult, DraggableProvidedDragHandleProps } from "react-beautiful-dnd";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
interface IReportSettingsModalFiltersProps {
    questionTypeLookup: {[key: string]: MainQuestionType};
    filtersForReport: IFilterAndDefaultInstances[];
    setFiltersForReport: (filters: IFilterAndDefaultInstances[]) => void;
}

const ReportSettingsModalFilters = (props: IReportSettingsModalFiltersProps) => {
    const { metricsForReports } = useMetricStateContext();
    const metricsValidAsFilter = metricsForReports.filter(m => metricValidAsFilter(m) && !props.filtersForReport.some(f => f.metric?.name  === m.name));

    const getCreateNewFilterButton = () => {
        const toggleButton = <DropdownToggle tag="button" className="hollow-button create-filter-button">
                                <i className="material-symbols-outlined">add</i>
                                <div>Create new filter</div>
                            </DropdownToggle>

        return <MetricDropdownMenu
            toggleElement={toggleButton}
            metrics={metricsValidAsFilter}
            selectMetric={addNewFilter}
            groupCustomVariables={true}
            reportVariableAppendType={ReportVariableAppendType.Filters}
            showCreateVariableButton={false}
        />
    }

    const getDescriptionContent = () => {
        return (
            <aside className="report-filter-description-container">
                {getCreateNewFilterButton()}
                <div className="report-filter-description">
                    <i className="material-symbols-outlined filter-icon">filter_alt</i>
                    <p className="report-filter-description-line">Create filters that can be applied to the entire report (eg Region, Wave, etc). Report filters are available for everyone.</p>
                    <p className="report-filter-description-line">Filters with a default value are applied automatically when the report loads.</p>
                </div>
            </aside>
        );
    }

    const reorder = (sourceIndex: number, destinationIndex: number) => {
        const newFilters = Array.from(props.filtersForReport);
        const [reorderedItem] = newFilters.splice(sourceIndex, 1);
        newFilters.splice(destinationIndex, 0, reorderedItem);

        props.setFiltersForReport(newFilters);
    }

    const onDragEnd = (result: DropResult) => {
        if (!result.destination) {
            return;
        }

        reorder(result.source.index, result.destination.index);
    }

    const updateFilter = (filterAndInstances: IFilterAndDefaultInstances, index: number) => {
        const newFilters = [...props.filtersForReport];
        newFilters[index] = filterAndInstances;
        props.setFiltersForReport(newFilters);
    }

    const removeFilter = (index: number) => {
        const newFilters = [...props.filtersForReport];
        newFilters.splice(index, 1);
        props.setFiltersForReport(newFilters);
    }

    const addNewFilter = (metric: Metric | undefined) => {
        const newFilters = [...props.filtersForReport];
        newFilters.push({
            defaultReportFilter: undefined,
            metric: metric,
            filters: []
        });
        props.setFiltersForReport(newFilters);
    }

    if (props.filtersForReport.length === 0) {
        return getDescriptionContent();
    }

    const dragDropEnabled = props.filtersForReport.length > 1;

    return (
        <>
            {getCreateNewFilterButton()}
            <DragDropContext onDragEnd={onDragEnd}>
                <Droppable droppableId="report-settings-modal-filters">
                    {(droppableProvided: DroppableProvided) => (
                        <div className="report-filter-list" {...droppableProvided.droppableProps} ref={droppableProvided.innerRef}>
                            {props.filtersForReport.map((filterAndInstances, index) => {
                                const uniqueId = filterAndInstances.metric ? filterAndInstances.metric.name.replace(/\s/g, '_') : index.toString();

                                return (
                                    <Draggable key={uniqueId} draggableId={uniqueId} index={index}>
                                        {(draggableProvided: DraggableProvided) => (
                                            <div className="filter-and-default-instance-selection" {...draggableProvided.draggableProps} ref={draggableProvided.innerRef}>
                                                <FilterAndDefaultInstanceSelection
                                                    selectedFilterAndInstances={filterAndInstances}
                                                    updateFilter={(filterAndInstances: IFilterAndDefaultInstances) => updateFilter(filterAndInstances, index)}
                                                    removeFilter={() => removeFilter(index)}
                                                    validMetrics={metricsValidAsFilter}
                                                    questionTypeLookup={props.questionTypeLookup}
                                                    dragDropEnabled={dragDropEnabled}
                                                    dragHandleProps={draggableProvided.dragHandleProps}
                                                />
                                            </div>
                                        )}
                                    </Draggable>
                                );
                            })}
                            {droppableProvided.placeholder}
                        </div>
                    )}
                </Droppable>
            </DragDropContext>
        </>
    );
}

interface IFilterAndDefaultInstanceSelectionProps {
    validMetrics: Metric[];
    questionTypeLookup: {[key: string]: MainQuestionType};
    dragDropEnabled: boolean;
    dragHandleProps?: DraggableProvidedDragHandleProps | null;

    selectedFilterAndInstances: IFilterAndDefaultInstances;
    updateFilter: (filterAndInstances: IFilterAndDefaultInstances) => void;

    removeFilter: () => void;
}

const FilterAndDefaultInstanceSelection = (props: IFilterAndDefaultInstanceSelectionProps) => {

    const [instanceDropdownOpen, setInstanceDropdownOpen] = React.useState(false);
    const {metric: selectedMetric, filters: selectedFilters} = props.selectedFilterAndInstances;
    const { entityConfiguration} = useEntityConfigurationStateContext();
    const getInstanceDropdownDescription = () => {
        if (selectedFilters.length === 0) {
            return "No default value";
        }

        return selectedFilters.map(f => f.filterDescription(entityConfiguration)).join(", ");
    }

    const toggleInstanceDropdown = () => {
        setInstanceDropdownOpen(!instanceDropdownOpen);
    }

    const getMetricTitleClassname = () => {
        if (!selectedMetric) {
            return "title placeholder";
        }

        return "title"
    }

    const getInstanceTitleClassname = () => {
        if (!selectedMetric || selectedFilters.length === 0) {
            return "title placeholder";
        }

        return "title";
    }

    const getDragIndicatorClassName = () => {
        if (!props.dragDropEnabled) {
            return "reorder-report-filter-handle material-symbols-outlined hidden";
        }

        return "reorder-report-filter-handle material-symbols-outlined";
    }

    const getMetricDropdownToggle = () => {
        const isMisconfigured = props.selectedFilterAndInstances.metric == null && props.selectedFilterAndInstances.defaultReportFilter != null;
        if (isMisconfigured) {
            const measureName = props.selectedFilterAndInstances.defaultReportFilter?.measureName ?? "Invalid question";
            return (
                <DropdownToggle className="metric-selector-toggle toggle-button">
                    <div className='title misconfigured-filter' title={measureName}>
                        <i className="material-symbols-outlined error-icon" title='Unable to find question, please select again'>error</i>
                        {measureName}
                    </div>
                    <i className="material-symbols-outlined">arrow_drop_down</i>
                </DropdownToggle>
            );
        }
        const metricDropdownToggleText = selectedMetric ? selectedMetric.displayName : "Choose a question or variable";
        return (
            <DropdownToggle className="metric-selector-toggle toggle-button">
                <div className={getMetricTitleClassname()} title={metricDropdownToggleText}>{metricDropdownToggleText}</div>
                <i className="material-symbols-outlined">arrow_drop_down</i>
            </DropdownToggle>
        );
    }

    const updateSelectedMetric = (newMetric: Metric | undefined) => {
        const currentFilter = props.selectedFilterAndInstances;
        currentFilter.metric = newMetric;
        currentFilter.filters = [];
        props.updateFilter(currentFilter);
    }

    const updateSelectedFilters = (filters: MetricFilterState[]) => {
        const currentFilter = props.selectedFilterAndInstances;
        currentFilter.filters = filters;

        props.updateFilter(currentFilter);
    }

    return (
        <>
            <i className={getDragIndicatorClassName()} {...props.dragHandleProps}>drag_indicator</i>
            <div className="metric-and-instance-dropdown-container">
                <MetricDropdownMenu
                    {...props}
                    groupCustomVariables={true}
                    selectMetric={updateSelectedMetric}
                    toggleElement={getMetricDropdownToggle()}
                    reportVariableAppendType={ReportVariableAppendType.Filters}
                    metrics={props.validMetrics}
                />
                {selectedMetric !== undefined &&
                    <ButtonDropdown className="report-filter-value-dropdown" isOpen={instanceDropdownOpen} toggle={toggleInstanceDropdown}>
                        <DropdownToggle className="metric-selector-toggle toggle-button">
                            <div className={getInstanceTitleClassname()} title={getInstanceDropdownDescription()}>{getInstanceDropdownDescription()}</div>
                            <i className="material-symbols-outlined">arrow_drop_down</i>
                        </DropdownToggle>
                        <DropdownMenu>
                            <FilterValueMappingSelector
                                id="report-modal-filters"
                                selectedMetric={selectedMetric}
                                selectedFilters={props.selectedFilterAndInstances.filters}
                                setSelectedFilters={updateSelectedFilters}
                                showApplyButtons={false}
                                onApply={toggleInstanceDropdown}
                                selectNoneText="No default value"
                            />
                        </DropdownMenu>
                    </ButtonDropdown>
                }
            </div>
            <div className="remove-report-filter-button" onClick={props.removeFilter}>
                <i className="material-symbols-outlined">close</i>
            </div>
        </>
    );
}

export default ReportSettingsModalFilters;