import React from "react";
import BarChartRoundedIcon from '@mui/icons-material/BarChartRounded';
import { CuratedFilters } from "../../../../filter/CuratedFilters";
import { PartWithExtraData } from "../ReportsPageDisplay";
import { UserContext } from "../../../../GlobalContext";
import ReportExport from "../Utility/ReportExport";
import { ExportType, useAsyncExportContext } from "../Utility/AsyncExportContext";
import LargeExportPopover from "./LargeExportPopover";
import { Metric } from "../../../../metrics/metric";
import { useEntityConfigurationStateContext } from "../../../../entity/EntityConfigurationStateContext";
import { SplitButton } from "../../../../shared/components/SplitButton/SplitButton";
import DownloadRoundedIcon from '@mui/icons-material/DownloadRounded';
import { SparkleIcon } from "../../../../shared/components/Icons/SparkleIcon";
import { useAppSelector } from "client/state/store";
import { PartType } from "../../../panes/PartType";
import Tooltip from "../../../Tooltip";
import { isUsingOverTime } from "client/components/visualisations/Reports/Charts/ReportsChartHelper";
import { selectSubsetId } from "client/state/subsetSlice";
import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";
import { selectCurrentReport } from "client/state/reportSelectors";

export interface IReportPowerpointExportButtonProps {
    metrics: Metric[];
    curatedFilters: CuratedFilters;
    overTimeFilters: CuratedFilters;
    isDataInSyncWithDatabase: boolean;
    reportPart?: PartWithExtraData;
}

const ReportPowerpointExportButton = (props: IReportPowerpointExportButtonProps) => {
    const reportErrorState = useAppSelector(state => state.report.errorState);

    const { pendingExports, exportDispatch } = useAsyncExportContext();
    const [exportPopoverOpen, setExportPopoverOpen] = React.useState({ open: false, useGenerativeAi: false });
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;
    const page = currentReportPage.page;


    const partUsesWaves = (report.waves !== undefined && props.reportPart?.part.waves === undefined) || props.reportPart?.part.waves?.waves !== undefined;
    const partUsesBreaks = (report.breaks.length > 0 && !props.reportPart?.part.overrideReportBreaks) || (props.reportPart?.part.breaks && props.reportPart?.part.breaks.length > 0);
    const partUsesOverTime = props.reportPart != undefined && isUsingOverTime(report, props.reportPart);
    const isMultiFunnel = props.reportPart?.part.partType === PartType.ReportsCardFunnel && (partUsesWaves || partUsesBreaks || partUsesOverTime);

    const hasPendingExports = () => {
        if (props.reportPart?.part) {
            return pendingExports.some(e => e.exportType === ExportType.SingleChart && e.partId === props.reportPart!.part.id);
        } else {
            return pendingExports.some(e => e.exportType === ExportType.ChartReport && e.reportId === report.savedReportId);
        }
    }

    const doExport = (useGenerativeAi: boolean) => {
        if (props.reportPart) {
            exportPart(useGenerativeAi);
        } else {
            checkReportExportSize(useGenerativeAi);
        }
    }

    const checkReportExportSize = (useGenerativeAi: boolean) => {
        if (ReportExport.isLargeExport(currentReportPage, props.metrics, entityConfiguration)) {
            setExportPopoverOpen({ open: true, useGenerativeAi: useGenerativeAi });
        } else {
            exportReport(useGenerativeAi);
        }
    }

    const exportReport = (useGenerativeAi: boolean) => {
        setExportPopoverOpen({ open: false, useGenerativeAi: false });
        const request = ReportExport.getExportReportPowerpointRequest(
            currentReportPage,
            props.curatedFilters,
            props.overTimeFilters,
            useGenerativeAi,
            subsetId,
            timeSelection
        );
        if (request) {
            exportDispatch({ type: 'EXPORT_CHART_REPORT', data: { request: request, reportName: page.displayName } });
        }
    }

    const exportPart = (useGenerativeAi: boolean) => {
        if (!props.reportPart?.part || !props.reportPart?.metric) {
            throw new Error("Can't export without a part");
        }
        const request = ReportExport.getExportReportPartPowerpointRequest(
            currentReportPage.report.savedReportId,
            props.reportPart.part.id,
            props.curatedFilters,
            props.overTimeFilters,
            useGenerativeAi,
            subsetId,
            timeSelection
        );
        exportDispatch({ type: 'EXPORT_SINGLE_CHART', data: { request: request, reportName: page.displayName, partDisplayName: props.reportPart.part.helpText } });
    }

    const isLoading = hasPendingExports();
    const exportButtonId = 'report-export-button';

    const options = [
        { label: 'With Summary', onItemClick: () => { doExport(true) }, icon: <SparkleIcon />, tooltip: 'Summary generated by AI' },
        { label: 'Chart only', onItemClick: () => { doExport(false) }, icon: <BarChartRoundedIcon /> }
    ];

    return (
        <UserContext.Consumer>
            {(user) => {
                const isExportForbidden = (user?.isTrialUser || reportErrorState.isError || isMultiFunnel) ?? true;
                const getSplitButton = () => {
                    return (
                        <SplitButton
                            id={exportButtonId}
                            variant="primary"
                            size="small"
                            title="Export"
                            options={options}
                            disabled={isLoading || isExportForbidden || !props.isDataInSyncWithDatabase}
                            icon={<DownloadRoundedIcon />}
                        />
                    );
                }
                
                const getButtonWithPopover = () => {
                    return (
                    <>
                        {getSplitButton()}
                        <LargeExportPopover
                            isOpen={exportPopoverOpen.open}
                            attachedElementId={exportButtonId}
                            close={() => setExportPopoverOpen({ open: false, useGenerativeAi: false })}
                            doExport={() => exportReport(exportPopoverOpen.useGenerativeAi)}
                        />
                    </>   
                );
                };

                if (isMultiFunnel) {
                    const tooltipText = "Multi funnel exports coming soon";
                    return (
                        <Tooltip placement="top" title={tooltipText}>
                            <span>
                            {getSplitButton()}
                            </span>
                        </Tooltip>
                    )
                }

                return getButtonWithPopover();
            }}
        </UserContext.Consumer>
    );
}

export default ReportPowerpointExportButton;