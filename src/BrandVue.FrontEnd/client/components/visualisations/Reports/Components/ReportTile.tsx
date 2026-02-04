import React, { useMemo } from "react";
import {
    IAverageDescriptor,
    PageDescriptor,
    ReportType,
} from "../../../../BrandVueApi";
import TileTemplate from "../../shared/TileTemplate";
import { getUrlForPageName } from "../../../helpers/PagesHelper";
import { ReportWithPage } from "../ReportsPage";
import FuzzyDate from "../../../helpers/FuzzyDate";
import { IGoogleTagManager } from "../../../../googleTagManager";
import ReportExport from "../Utility/ReportExport";
import { CuratedFilters } from "../../../../filter/CuratedFilters";
import { ExportType, useAsyncExportContext } from "../Utility/AsyncExportContext";
import { Metric } from "../../../../metrics/metric";
import LargeExportPopover from "./LargeExportPopover";
import {getDateRangeLookup, getDefaultOverTimeSettings, getDefaultWave, getUserVisibleAverages, updateFiltersWithSelectedProperies} from "../../../helpers/SurveyVueUtils";
import {useFilterStateContext} from "../../../../filter/FilterStateContext";
import { PageHandler } from "../../../PageHandler";
import { useEntityConfigurationStateContext } from "../../../../entity/EntityConfigurationStateContext";
import { getMetricFilterFromDefault } from "../Filtering/FilterHelper";
import { useMetricStateContext } from "../../../../metrics/MetricStateContext";
import { MixPanel } from "../../../mixpanel/MixPanel";
import { ApplicationConfiguration } from "client/ApplicationConfiguration";
import { useLocation } from "react-router-dom";
import { useReadVueQueryParams } from "../../../helpers/UrlHelper";
import { selectAllAverages } from "client/state/averageSlice";
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from "client/state/subsetSlice";
import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";
import { selectDefaultReportId } from "client/state/reportSlice";

interface IReportTileProps {
    applicationConfiguration: ApplicationConfiguration;
    canEditReports: boolean;
    curatedFilters: CuratedFilters;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    metricsForReports: Metric[];
    editReportSettings(e: React.MouseEvent): void;
    averages: IAverageDescriptor[];
    reportPage: ReportWithPage;
}

const ReportTile = (props: IReportTileProps) => {
    const { pendingExports, exportDispatch } = useAsyncExportContext();
    const currentReportPage = props.reportPage;
    const report = currentReportPage.report;
    const page = currentReportPage.page;

    const isExporting = pendingExports.some(e =>
        [ExportType.TableReport, ExportType.ChartReport].includes(e.exportType) && e.reportId === report.savedReportId);
    const [exportPopoverOpen, setExportPopoverOpen] = React.useState<boolean>(false);
    const defaultReportId = useAppSelector(selectDefaultReportId);

    const numCharts = (page.panes && page.panes.length >= 1 && page.panes[0].parts) ? page.panes[0].parts.length : 0;
    const isDefaultReport = report.savedReportId === defaultReportId;
    const singularName = report.reportType === ReportType.Chart ? "chart" : "table";
    const pluralName = `${singularName}s`;
    const exportButtonId = `export-button-${report.savedReportId}`;
    const isDataWeighted = report?.isDataWeighted ?? false;
    const { filters } = useFilterStateContext();
    const defaultWave = getDefaultWave(props.curatedFilters)
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { metricsForReports } = useMetricStateContext();
    const location = useLocation();
    const readVueQueryParams = useReadVueQueryParams();

    const subsetId = useAppSelector(selectSubsetId);
    const allAverages = useAppSelector(selectAllAverages);
    const timeSelection = useAppSelector(selectTimeSelection);

    const dateRangeLookup = useMemo(() => getDateRangeLookup(props.applicationConfiguration),
    [
        props.applicationConfiguration.dateOfFirstDataPoint.getTime(),
        props.applicationConfiguration.dateOfLastDataPoint.getTime()
    ]);

    const getDescription = () => {
        if (isDefaultReport) return "Default report";
        return report.isShared ? "Shared report" : "My report";
    }

    const exportReport = (e: React.MouseEvent) => {
        e.stopPropagation();
        if (!isExporting) {
            if (ReportExport.isLargeExport(currentReportPage, props.metricsForReports, entityConfiguration)) {
                setExportPopoverOpen(true);
            } else {
                doExport();
            }
        }
    }

    const doExport = () => {
        setExportPopoverOpen(false);
        let updatedFilters;

        //when exporting from the reports home page, no report is actively selected on the parent state so we cannot blindly trust the filterState to be correctly populated
        if(!filters || filters.length == 0) {
            const selectedReportFilters = report.defaultFilters.map(f =>
                getMetricFilterFromDefault(f, metricsForReports, entityConfiguration)).flatMap(f => {
                    return f.filters;
                });
            updatedFilters = updateFiltersWithSelectedProperies(props.curatedFilters, props.averages, isDataWeighted, selectedReportFilters, true, defaultWave)
        } else {
            updatedFilters = updateFiltersWithSelectedProperies(props.curatedFilters, props.averages, isDataWeighted, filters, true, defaultWave)
        }

        const userVisibleAverages = getUserVisibleAverages(props.applicationConfiguration,
            allAverages,
            report.isDataWeighted,
            subsetId);

        const overTimeDefaults = getDefaultOverTimeSettings(report.overTimeConfig, userVisibleAverages, dateRangeLookup, props.applicationConfiguration);
        const overTimeFilters = updateFiltersWithSelectedProperies(
            props.curatedFilters,
            props.averages,
            isDataWeighted,
            filters,
            true,
            defaultWave,
            overTimeDefaults.startDate,
            overTimeDefaults.endDate,
            overTimeDefaults.average);

        if (!isExporting) {
            if (report.reportType === ReportType.Chart) {
                const request = ReportExport.getExportReportPowerpointRequest(currentReportPage, updatedFilters, overTimeFilters, false, subsetId, timeSelection);
                if (request) {
                    exportDispatch({type: 'EXPORT_CHART_REPORT', data: {request: request, reportName: page.displayName}});
                }
            } else {
                const request = ReportExport.getExportReportTablesRequest(currentReportPage, updatedFilters, subsetId, timeSelection);
                if (request) {
                    exportDispatch({type: 'EXPORT_TABLE_REPORT', data: {request: request, reportName: currentReportPage.page.displayName}});
                }
            }
        }
    }

    const sendViewReportAnalyticsEvent = () => {
        props.googleTagManager.addEvent("reportsPageViewReport", props.pageHandler);
        MixPanel.track("reportsPageLoaded", { ReportName: page.name });
    }

    const getReportUrl = (location, readVueQueryParams) : string => {
        if (!report.userHasAccess) return "";
        return getUrlForPageName(page.name, location, readVueQueryParams);
    }

    return (
        <TileTemplate nextPageUrl={getReportUrl(location, readVueQueryParams)}
            className="report-card"
            key={report.savedReportId}
        >
            <div className="report-card-content" onClick={() => sendViewReportAnalyticsEvent()}>
                {!report.userHasAccess &&
                <div className="report-card-noaccess">You cannot access this report because you don't have permission to view one or more of its questions.</div>
                }
                {report.userHasAccess &&
                <LargeExportPopover
                    isOpen={exportPopoverOpen}
                    attachedElementId={exportButtonId}
                    placement='bottom'
                    close={() => setExportPopoverOpen(false)}
                    doExport={doExport}
                />
                }
                <div className="report-card-header">
                    {report.reportType === ReportType.Chart &&
                        <i className="material-symbols-outlined report-type rotate">bar_chart</i>
                    }
                    {report.reportType === ReportType.Table &&
                        <i className="material-symbols-outlined report-type">table_chart</i>
                    }
                    {report.userHasAccess &&
                    <div className="report-tile-buttons">
                        <div className={`export-button ${isExporting ? 'loading' : ''}`} onClick={exportReport} id={exportButtonId}>
                            <i className="material-symbols-outlined">file_download</i>
                        </div>
                        {props.canEditReports &&
                            <div className="settings-button" onClick={props.editReportSettings}>
                                <i className="material-symbols-outlined">settings</i>
                            </div>
                        }
                    </div>
                    }
                </div>
                <div className="report-card-name">
                    {page.displayName}
                </div>
                <div className="report-card-description">
                    {getDescription()}
                </div>
                <div className="report-card-footer">
                    <span className="chart-count">
                        <strong>{numCharts} </strong>
                        {numCharts == 1 ? singularName : pluralName}
                    </span>
                    <span>Updated {<FuzzyDate date={report.modifiedDate} lowerCase={true} includePastFuture={true}/>}</span>
                </div>
            </div>
        </TileTemplate>
    );
}
export default ReportTile;