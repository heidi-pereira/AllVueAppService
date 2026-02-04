import React from "react";
import { ReportType } from "../../../BrandVueApi";

interface INoBreaksMessageProps {
    reportType: ReportType;
    isReportSettings?: boolean;
    isDisabled: boolean;
}

const NoBreaksMessage = (props: INoBreaksMessageProps) => {
    const getIcon = () => {
        if (props.reportType === ReportType.Chart) {
            return <i className="material-symbols-outlined breaks-icon rotate">read_more</i>
        }
        return <i className="material-symbols-outlined breaks-icon">tab_unselected</i>
    }

    const reportTypeText = props.reportType === ReportType.Chart ? "charts" : "tables"

    return (
        <aside className={`no-breaks-message ${props.isDisabled ? "disabled" : ""}`}>
            {getIcon()}
            <p>Add breaks to compare how different groups of repondents answered a question (eg by Age, Gender, etc).</p>
            {props.isReportSettings &&
                <p>Report breaks are applied to the entire report, but can be changed for individual {reportTypeText}.</p>
            }
        </aside>
    );
}

export default NoBreaksMessage;