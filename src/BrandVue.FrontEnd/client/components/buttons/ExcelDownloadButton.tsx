import React from "react";
import Tooltip from "../Tooltip";

interface IExcelDownloadProps {
    disabled?: boolean;
    loading?: boolean;
    tooltipContent?: string;
    onClick: () => void;
    exportedObjectName?: string;
}

const ExcelDownloadButton: React.FunctionComponent<IExcelDownloadProps> = (props: IExcelDownloadProps) => {
    const buttonTitle = props?.tooltipContent ?? "Download data to a spreadsheet";
    return (
        <Tooltip placement="top" title={buttonTitle}>
            <button disabled={props.loading || props.disabled} id="excelDownloadButton" className={`hollow-button excelDownload ${props.loading ? "loading" : ""}`} onClick={props.onClick}>
                <i className="material-symbols-outlined">file_download</i>
                <div>Export {props.exportedObjectName} data</div>
            </button>
        </Tooltip>
    );
};

export default ExcelDownloadButton;