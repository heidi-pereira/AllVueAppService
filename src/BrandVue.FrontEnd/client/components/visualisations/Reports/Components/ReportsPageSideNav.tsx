import { Metric } from "../../../../metrics/metric";
import React from "react";
import { ICustomPeriod, PartDescriptor, ReportType, MainQuestionType } from "../../../../BrandVueApi";
import ReportListItem from "./ReportListItem";
import SearchInput from "../../../SearchInput";
import DataWavesDropdown from "./DataWavesDropdown";
import * as BrandVueApi from "../../../../BrandVueApi";
import { PartWithExtraData } from "../ReportsPageDisplay";
import { DragDropContext, Droppable, DroppableProvided, Draggable, DraggableProvided, DropResult } from "react-beautiful-dnd";
import { getReportPartDisplayText } from "../../../helpers/SurveyVueUtils";
import { IGoogleTagManager } from "../../../../googleTagManager";
import AddMetricsModal from "../Modals/AddMetricsModal";
import {useEffect, useRef} from "react";
import { PageHandler } from "../../../PageHandler";
import { useMetricStateContext } from "../../../../metrics/MetricStateContext";

interface IReportsPageSideNavProps {
    metricsForReports: Metric[];
    reportParts: PartWithExtraData[];
    canEditReport: boolean;
    defaultDataWave: ICustomPeriod;
    dataWaves: ICustomPeriod[];
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    reportType: BrandVueApi.ReportType;
    setSelectedWave(wave: ICustomPeriod): void;
    addPartsToReport(metrics: Metric[]): void;
    saveReordering(source: number, desination: number): void;
    removeFromReport(reportPart: PartWithExtraData): void;
    setFocusedPart(part: PartWithExtraData): void;
    getPrimaryButtonText(metrics: Metric[]): string;
    modalHeaderText: string;
    focusedPart: PartWithExtraData | undefined;
    scrollY: number | undefined;
    setScrollY: (scrollY: number | undefined) => void;
    onDragEnd: () => void;
    duplicatePart(partDescriptor: PartDescriptor): void;
}

const ReportsPageSideNav = (props: IReportsPageSideNavProps) => {
    const { questionTypeLookup } = useMetricStateContext();
    const sideNavScrollbarValueRef = useRef<HTMLDivElement>(null)
    const [searchText, setSearchText] = React.useState("");
    const [isAddChartsModalVisible, setAddChartsModalVisible] = React.useState<boolean>(false);
    const [metricsForReports, setMetricsForReports] = React.useState<Metric[]>([]);

    useEffect(() => {
        const handler = setTimeout(() => {
            if (sideNavScrollbarValueRef.current && props.scrollY && sideNavScrollbarValueRef.current?.scrollTop !== props.scrollY){
                sideNavScrollbarValueRef.current.scrollTop = props.scrollY
            }
        }, 50)
        return () => {clearTimeout(handler)}
    }, [props.scrollY])

    useEffect(() => {
        setMetricsForReports(filterMetricsByReportType(props.metricsForReports));
    }, [props.metricsForReports]);

    const textToSearch = searchText.trim().toLowerCase();

    const filterMetricsByReportType = (metrics: Metric[]): Metric[] => {
        if (props.reportType == ReportType.Chart) {
            return metrics;
        }
        return metrics.filter(metric => questionTypeLookup[metric.name] != MainQuestionType.HeatmapImage);
    };

    const getPartsToShow = () => {
        return props.reportParts.filter(p =>
            p.metric?.name?.toLowerCase().includes(textToSearch) ||
            p.metric?.varCode?.toLowerCase().includes(textToSearch) ||
            p.metric?.displayName?.toLowerCase().includes(textToSearch) ||
            p.metric?.helpText?.toLowerCase().includes(textToSearch) ||
            getReportPartDisplayText(p)?.toLowerCase().includes(textToSearch)
        );
    }

    const partsToShow = getPartsToShow();

    const selectPart = (part: PartWithExtraData) => {
        props.setFocusedPart(part);
        scrollToPart(part);
        focusPart(part);
    }

    const scrollToPart = (p: PartWithExtraData) => {
        if (p.ref.current) {
            p.ref.current.scrollIntoView({behavior: 'smooth', block: 'start'});
        }
    };

    const focusPart = (part: PartWithExtraData) => {
        if (part.ref.current) {
            part.ref.current.focus({preventScroll: true});
        }
    }

    const onDragEnd = (result: DropResult) => {
        props.onDragEnd()
        // dropped outside the list
        if (!result.destination) {
            return;
        }

        props.saveReordering(result.source.index, result.destination.index);
    }

    const highlightFocusedPart = props.reportType === ReportType.Table;
    const dragDropEnabled = props.canEditReport && props.reportParts.length > 1 && textToSearch == "";

    return (
        <div className="side-nav">
            {props.canEditReport &&
                <AddMetricsModal isOpen={isAddChartsModalVisible}
                    metrics={metricsForReports}
                    getPrimaryButtonText={props.getPrimaryButtonText}
                    modalHeaderText={props.modalHeaderText}
                    onMetricsSubmitted={props.addPartsToReport}
                    setAddChartModalVisibility={setAddChartsModalVisible}
                />
            }
            <div className="question-search-container">
                <SearchInput id="reports-search" onChange={(text) => setSearchText(text)} text={searchText} className="question-search-input-group" autoFocus={true} />
            </div>
            <div>
                {props.canEditReport &&
                    <button className="add-chart-toggle hollow-button" onClick={() => setAddChartsModalVisible(true)}>
                        <i className="material-symbols-outlined">add</i>
                        <div>{props.reportType == BrandVueApi.ReportType.Chart ? "Add charts" : "Add tables"}</div>
                    </button>
                }
            </div>
            {props.dataWaves.length > 0 && <DataWavesDropdown defaultWave={props.defaultDataWave} waves={props.dataWaves} onWaveSelect={props.setSelectedWave} />}
            <div className="reports-drag-drop-container" onScroll={() => props.setScrollY(sideNavScrollbarValueRef.current?.scrollTop)}>
                <DragDropContext onDragEnd={onDragEnd} >
                    <Droppable droppableId="report-order" >
                        {(droppableProvided: DroppableProvided) => (
                            <div ref={sideNavScrollbarValueRef}>
                            <div {...droppableProvided.droppableProps} ref={droppableProvided.innerRef}>
                                {partsToShow.map((part) => {
                                    const uniqueId = (part.part.spec1 + part.part.spec2 + part.part.spec3).replace(/\s/g, '_');
                                    const index = parseInt(part.part.spec2);
                                    const name = part.metric ? part.metric.displayName : part.part.spec1;
                                    return (
                                        <Draggable key={uniqueId} draggableId={uniqueId} index={index}>
                                            {(draggableProvided: DraggableProvided) => (
                                                <div {...draggableProvided.draggableProps} ref={draggableProvided.innerRef}>
                                                    <ReportListItem
                                                        name={name}
                                                        metric={part.metric}
                                                        helpText={getReportPartDisplayText(part)}
                                                        dragDropEnabled={dragDropEnabled}
                                                        isFocused={highlightFocusedPart && part.part.spec2 === props.focusedPart?.part.spec2}
                                                        select={() => selectPart(part)}
                                                        dragHandleProps={draggableProvided.dragHandleProps}
                                                        index={index}
                                                        googleTagManager={props.googleTagManager}
                                                        pageHandler={props.pageHandler}
                                                        canEditReport={props.canEditReport}
                                                        canExploreData={props.reportType == BrandVueApi.ReportType.Chart}
                                                        removeFromReport={() => props.removeFromReport(part)}
                                                        duplicatePart={props.duplicatePart}
                                                        currentPart={part.part}

                                                    />
                                                </div>
                                            )}
                                        </Draggable>
                                    )})}
                                {droppableProvided.placeholder}
                                </div>
                            </div>
                        )}

                    </Droppable>
                </DragDropContext>
            </div>
        </div>
    );
};

export default ReportsPageSideNav;