import React from "react";
import { UserContext } from "../../../../GlobalContext";
import { CuratedFilters } from "../../../../filter/CuratedFilters";
import ReportExport from "../Utility/ReportExport";
import { ExportType, useAsyncExportContext } from "../Utility/AsyncExportContext";
import { Metric } from "../../../../metrics/metric";
import LargeExportPopover from "../Components/LargeExportPopover";
import { useEntityConfigurationStateContext } from "../../../../entity/EntityConfigurationStateContext";
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from "client/state/subsetSlice";

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";
import { selectCurrentReport } from "client/state/reportSelectors";

interface IReportExcelDownloadProps {
    metrics: Metric[];
    curatedFilters: CuratedFilters;
    canExportData: boolean;
    shrink: boolean;
    isDataInSyncWithDatabase: boolean;
}

const ReportPageExcelDownload = (props: IReportExcelDownloadProps) => {
    const { pendingExports, exportDispatch } = useAsyncExportContext();
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const isLoading = pendingExports.some(e => e.exportType === ExportType.TableReport && e.reportId === report.savedReportId);
    const [exportPopoverOpen, setExportPopoverOpen] = React.useState<boolean>(false);
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const subsetId = useAppSelector(selectSubsetId);

    const exportExcel = () => {
        if (ReportExport.isLargeExport(currentReportPage, props.metrics, entityConfiguration)) {
            setExportPopoverOpen(true);
        } else {
            doExport();
        }
    }

    const doExport = () => {
        setExportPopoverOpen(false);
        const request = ReportExport.getExportReportTablesRequest(currentReportPage, props.curatedFilters, subsetId, timeSelection);
        if (request) {
            exportDispatch({type: 'EXPORT_TABLE_REPORT', data: {request: request, reportName: currentReportPage.page.displayName}});
        }
    }

    const exportButtonId = 'report-export-excel-button';
    return <UserContext.Consumer>
        {(user) => {
            const isExportForbidden = user?.isTrialUser ?? true;

            return (
                <>
                    <button id={exportButtonId} className={`primary-button excelDownload ${isLoading ? "loading" : ""}`} disabled={!props.canExportData || isLoading || isExportForbidden ||!props.isDataInSyncWithDatabase} onClick={() => exportExcel()}>
                        <i className="material-symbols-outlined">file_download</i>
                        {!props.shrink && <div>Export</div>}
                    </button>
                    <LargeExportPopover
                        isOpen={exportPopoverOpen}
                        attachedElementId={exportButtonId}
                        close={() => setExportPopoverOpen(false)}
                        doExport={doExport}
                    />
                </>
            );
        }}
    </UserContext.Consumer>;
}
export default ReportPageExcelDownload;