import { IGoogleTagManager } from "../../../../googleTagManager";
import React from "react";
import { DraggableProvidedDragHandleProps } from "react-beautiful-dnd";
import ReportsPageCardContextMenu from "../Cards/ReportsPageCardContextMenu";
import {Metric} from "../../../../metrics/metric";
import { PageHandler } from "../../../PageHandler";
import { PartDescriptor } from "../../../../BrandVueApi";
import style from "./ReportListItem.module.less";

interface IReportListItemProps {
    name: string;
    metric: Metric | undefined;
    helpText: string;
    dragDropEnabled: boolean;
    isFocused: boolean;
    select(): void;
    dragHandleProps?: DraggableProvidedDragHandleProps | null;
    index: number;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    canEditReport: boolean;
    canExploreData: boolean;
    removeFromReport(): void;
    duplicatePart(partDescriptor: PartDescriptor): void;
    currentPart: PartDescriptor;
}

const ReportListItem = (props: IReportListItemProps) => {

    const getDragIndicatorClassName = props.dragDropEnabled ? "material-symbols-outlined" : "material-symbols-outlined disabled";

    return (
        <div className={`row ${style.noBootstrap} ${props.isFocused ? 'focused' : ''}`} onClick={() => props.select()}>
            <div className="item-number">
                {props.index + 1}
            </div>
            <div className="report-item" key={props.name}>
                <i className={getDragIndicatorClassName} {...props.dragHandleProps}>drag_indicator</i>
                <div className="name-container">
                    <div className="var-name">{props.name}</div>
                    <div className="item-name" title={props.helpText}>{props.helpText}</div>
                </div>
            </div>
            <ReportsPageCardContextMenu
                metric={props.metric}
                googleTagManager={props.googleTagManager}
                pageHandler={props.pageHandler}
                canEditReport={props.canEditReport}
                canExploreData={props.canExploreData}
                removeFromReport={props.removeFromReport}
                duplicatePart={props.duplicatePart}
                currentPart={props.currentPart}
            />
        </div>
    );
}

export default ReportListItem;