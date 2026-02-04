import React from 'react';
import toast from 'react-hot-toast';
import { AsyncExportTaskModel, CrosstabExportRequest, CuratedResultsModel, Factory, ReportExportRequest, ReportPartExportRequest } from '../../../../BrandVueApi';
import { saveFile } from '../../../../helpers/FileOperations';
import { MixPanel } from "../../../mixpanel/MixPanel";
import { selectSubsetId } from 'client/state/subsetSlice';
import { useAppSelector } from 'client/state/store';

export enum ExportType {
    Crosstab,
    TableReport,
    ChartReport,
    SingleChart
}

export interface PendingExport {
    exportKey: string;
    exportType: ExportType;
    exportName: string;
    reportId: number;
    partId: number;
    metricName: string;
}

export type ExportAction =
    | { type: 'EXPORT_CROSSTAB_MULTI_ENTITY'; data: { request: CrosstabExportRequest, label: string }}
    | { type: 'EXPORT_CROSSTAB_TEXT'; data: { request: CuratedResultsModel, label: string }}
    | { type: 'EXPORT_TABLE_REPORT'; data: { request: ReportExportRequest, reportName: string }}
    | { type: 'EXPORT_CHART_REPORT'; data: { request: ReportExportRequest, reportName: string }}
    | { type: 'EXPORT_SINGLE_CHART'; data: { request: ReportPartExportRequest, reportName: string, partDisplayName: string }};

interface IAsyncExportContextState {
    pendingExports: PendingExport[];
    exportDispatch: (action: ExportAction) => Promise<void>;
}

const AsyncExportContext = React.createContext<IAsyncExportContextState>({ pendingExports: [], exportDispatch: () => Promise.resolve() });

export const useAsyncExportContext = () => React.useContext(AsyncExportContext);

const LOCALSTORAGE_PENDING_EXPORTS = "pending_exports";

export const AsyncExportProvider = (props: {children: any }) => {
    const dataClient = Factory.DataClient(error => error());
    const [pendingExports, setPendingExports] = React.useState<PendingExport[]>([]);
    const subsetId = useAppSelector(selectSubsetId);

    const reloadPendingFromStorage = () => setPendingExports(getPendingExports());

    React.useEffect(() => {
        let isCancelled = false;

        reloadPendingFromStorage();
        const checkTimingSeconds = 10;
        const repeatingTimedCheckResults = () => {
            checkAllExportResults().then(() => {
                if (!isCancelled) {
                    setTimeout(() => {
                        if (!isCancelled) {
                            repeatingTimedCheckResults()
                        }
                    }, checkTimingSeconds * 1000);
                };
            });
        }

        //This event listener is to handle a case where multiple allvue tabs are open:
        //As export keys are stored in localstorage, an export could be started in one tab,
        //then the key loaded from storage in the other tab, resulting in it being saved
        //in the second tab with the first tab's state not getting updated
        window.addEventListener('storage', reloadPendingFromStorage);
        repeatingTimedCheckResults();

        return () => {
            isCancelled = true;
            window.removeEventListener('storage', reloadPendingFromStorage);
        };
    }, []);

    const checkAllExportResults = async () => {
        const pending = getPendingExports();
        setPendingExports(pending);
        return pending.forEach(async e => await checkExportResult(e));
    }

    const checkExportResult = async (pendingExport: PendingExport) => {
        return await dataClient.checkExportProgress(getExportTaskModel(pendingExport.exportKey))
            .then(response => {
                if (response.status === 202) {
                    //accepted, export still pending
                } else {
                    const extension = [ExportType.Crosstab, ExportType.TableReport].includes(pendingExport.exportType) ? 'xlsx' : 'pptx';
                    saveFile(response, `Export - Private.${extension}`);
                    clearExportResult(pendingExport.exportKey);
                }
            }).catch(error => {
                toast.error(`${pendingExport.exportName} export failed - please try exporting again`);
                clearExportResult(pendingExport.exportKey);
            })
    }

    const clearExportResult = async (key: string) => {
        const pending = getPendingExports();
        const keyIndex = pending.findIndex(e => e.exportKey === key);
        if (keyIndex >= 0) {
            pending.splice(keyIndex, 1);
            localStorage.setItem(LOCALSTORAGE_PENDING_EXPORTS, JSON.stringify(pending));
        }
        setPendingExports(pending);
        return await dataClient.clearExportResult(getExportTaskModel(key));
    }

    const storePendingExportTask = (key: string, type: ExportType, name: string, reportId: number, partId: number, metricName: string) => {
        const pending = getPendingExports();
        const newExportTask: PendingExport = {
            exportKey: key,
            exportType: type,
            exportName: name,
            reportId: reportId,
            partId: partId,
            metricName: metricName
        }
        pending.push(newExportTask);
        const distinct = pending.filter((task, i) => pending.findIndex(e => e.exportKey === task.exportKey) === i);
        localStorage.setItem(LOCALSTORAGE_PENDING_EXPORTS, JSON.stringify(distinct));
        setPendingExports(distinct);
    }

    const getPendingExports = (): PendingExport[] => {
        const pendingExports = JSON.parse(localStorage.getItem(LOCALSTORAGE_PENDING_EXPORTS) ?? '[]');
        if (Array.isArray(pendingExports) && pendingExports.every((k): k is PendingExport => k.exportKey != null && k.exportType != null && k.exportName != null)) {
            return pendingExports;
        }
        return [];
    }

    const getExportTaskModel = (exportKey: string): AsyncExportTaskModel => {
        return new AsyncExportTaskModel({
            exportKey: exportKey,
            subsetId: subsetId
        });
    }

    const asyncDispatch = async (action: ExportAction) => {
        switch (action.type) {
            case "EXPORT_CROSSTAB_MULTI_ENTITY":
                MixPanel.track("exportCrossTabRequested", { ActionType: action.type, Question: action.data.label });
                return dataClient.exportCrosstabResults(action.data.request)
                    .then(exportKey => storePendingExportTask(exportKey, ExportType.Crosstab, `Crosstab - ${action.data.label}`, -1, -1, action.data.request.requestModel.primaryMeasureName));
            case "EXPORT_CROSSTAB_TEXT":
                MixPanel.track("exportCrossTabRequested", { ActionType: action.type, Question: action.data.label });
                return dataClient.exportCrosstabTextResults(action.data.request)
                    .then(exportKey => storePendingExportTask(exportKey, ExportType.Crosstab, `Crosstab - ${action.data.label}`, -1, -1, action.data.request.measureName[0]));
            case "EXPORT_TABLE_REPORT":
                MixPanel.track("exportExcelRequested", { ActionType: action.type, ReportName: action.data.reportName, SavedReportId: action.data.request.savedReportId});
                return dataClient.exportReportTables(action.data.request)
                    .then(exportKey => storePendingExportTask(exportKey, ExportType.TableReport, `Report - ${action.data.reportName}`, action.data.request.savedReportId, -1, ''));
            case "EXPORT_CHART_REPORT":
                MixPanel.track("exportPowerpointRequested", { ActionType: action.type, ReportName: action.data.reportName, SavedReportId: action.data.request.savedReportId, UseGenerativeAi: action.data.request.useGenerativeAi });
                return dataClient.exportReportPowerpoint(action.data.request)
                    .then(exportKey => storePendingExportTask(exportKey, ExportType.ChartReport, `Report - ${action.data.reportName}`, action.data.request.savedReportId, -1, ''));
            case "EXPORT_SINGLE_CHART":
                MixPanel.track("exportPowerpointRequested", { ActionType: action.type, ReportName: action.data.reportName, SavedReportId: action.data.request.savedReportId, Part: action.data.partDisplayName, UseGenerativeAi: action.data.request.useGenerativeAi });
                return dataClient.exportReportSingleChartPowerpoint(action.data.request)
                    .then(exportKey => storePendingExportTask(
                        exportKey,
                        ExportType.SingleChart,
                        `Report - ${action.data.reportName} - ${action.data.partDisplayName}`,
                        action.data.request.savedReportId,
                        action.data.request.partId,
                        ''));
            default:
                throw new Error("Unsupported action type");
        }
    }

    return (
        <AsyncExportContext.Provider value={{ pendingExports: pendingExports, exportDispatch: asyncDispatch }}>
            {props.children}
        </AsyncExportContext.Provider>
    );
}