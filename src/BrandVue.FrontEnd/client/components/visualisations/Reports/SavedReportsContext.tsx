import * as BrandVueApi from "../../../BrandVueApi";
import { useContext, createContext } from "react";
import { CopySavedReportRequest, CreateNewReportRequest, DeleteReportPartRequest, ModifyReportPartsRequest, PageDescriptor, PartDescriptor, UpdateReportSettingsRequest } from '../../../BrandVueApi';
import { Report } from '../../../BrandVueApi';
import { dsession } from '../../../dsession';
import { getUrlSafePageName, useReadVueQueryParams } from '../../helpers/UrlHelper';
import { getUrlForPageName } from "../../helpers/PagesHelper";
import { PaneType } from '../../panes/PaneType';
import {useLocation, useNavigate} from "react-router-dom";
import { useAppDispatch } from "../../../state/store";
import { reloadAllReports, setIsDataInSyncWithDatabase, setReportsPageOverride } from '../../../state/reportSlice';

export type SavedReportsAction =
    | {type: 'CREATE_REPORT', data: CreateNewReportRequest }
    | {type: 'COPY_REPORT', data: {reportId: number, page: PageDescriptor, newName: string, isShared: boolean, isDefault: boolean}}
    | {type: 'DELETE_REPORT', data: {reportId: number}}
    | {type: 'UPDATE_REPORT_SETTINGS', data: UpdateReportSettingsRequest }
    | {type: 'ADD_PARTS', data: {report: Report, parts: PartDescriptor[]}}
    | {type: 'UPDATE_PARTS', data: {report: Report, parts: PartDescriptor[], forceReload: boolean}}
    | {type: 'UPDATE_PART_COLOUR', data: {partId: number, colours: string[]}}
    | {type: 'DELETE_PART', data: {report: Report, partIdToDelete: number, partsToUpdate: PartDescriptor[]}}
    | {type: 'POLL_FOR_UPDATES', data: {reportGUID: string, reportId: number}}
    | {type: 'CREATE_REPORT_FROM_TEMPLATE', data: {templateId: number, reportName: string}}
    | {type: 'TRIGGER_RELOAD'};

interface SavedReportsContextState {
    reportsDispatch: (action: SavedReportsAction) => Promise<void>;
}

const SavedReportsContext = createContext<SavedReportsContextState>({
    reportsDispatch: () => Promise.resolve()
});

export const useSavedReportsContext = () => useContext(SavedReportsContext);

interface IProps {
    session: dsession;
    children: any;
    user: BrandVueApi.IApplicationUser | null;
}

export const SavedReportsProvider = (props: IProps) => {
    const savedReportsClient = BrandVueApi.Factory.SavedReportClient(error => error());
    const dispatch = useAppDispatch();
    const userId = props.user?.userId;
    const navigate = useNavigate();
    const location = useLocation();
    const readVueQueryParams = useReadVueQueryParams();

    const reloadPartsAndReports = async (forceLoad: boolean, pageName?: string) => {
        await props.session.loadPagesPanesParts();
        if(pageName) {
            navigate(getUrlForPageName(pageName, location, readVueQueryParams), {state: location.state});
        }
        dispatch(reloadAllReports({ forceLoad, userId }));
        dispatch(setReportsPageOverride(undefined));
    }

    const createReport = async (request: CreateNewReportRequest) => {
        await savedReportsClient.createReport(request);
        await reloadPartsAndReports(true, request.page.name);
    }

    const copyReport = async (reportId: number, page: PageDescriptor, newName: string, isShared: boolean, isDefault: boolean) => {
        const urlSafePageName = getUrlSafePageName(newName);
        const request = new CopySavedReportRequest({
            reportId: reportId,
            existingPage: page,
            newName: urlSafePageName,
            newDisplayName: newName,
            isShared: isShared,
            isDefault: isDefault,
        });
        await savedReportsClient.copyReport(request);
        await reloadPartsAndReports(true, request.existingPage.name);
    }

    const deleteReport = async (reportId: number) => {
        await savedReportsClient.deleteSavedReport(reportId);
        await reloadPartsAndReports(true);
    }

    const updateReportSettings = async (request: UpdateReportSettingsRequest) => {
    dispatch(setIsDataInSyncWithDatabase(true));
        await savedReportsClient.updateReportSettings(request).catch(async error => {
            throw error;
        });
        const pageName = props.session.activeDashPage?.panes[0]?.paneType === PaneType.reportSubPage ? request.pageName : undefined;
        await reloadPartsAndReports(true, pageName);
    }

    const addParts = async (report: Report, parts: PartDescriptor[]) => {
        await savedReportsClient.addParts(new ModifyReportPartsRequest({
            savedReportId: report.savedReportId,
            expectedGuid: report.modifiedGuid,
            parts: parts
        })).finally(async () => {
            await reloadPartsAndReports(true);
        });
    }

    const updateParts = async (report: Report, parts: PartDescriptor[], forceReload: boolean) => {
        dispatch(setIsDataInSyncWithDatabase(false));
        try {
            await savedReportsClient.updateParts(new ModifyReportPartsRequest({
                savedReportId: report.savedReportId,
                expectedGuid: report.modifiedGuid,
                parts: parts
            }));
            // Update succeeded — mark in-sync. Only perform a full reload if the caller requested it.
            dispatch(setIsDataInSyncWithDatabase(true));
            if (forceReload) {
                await reloadPartsAndReports(true);
            }
        } catch (err) {
            // On error (e.g., report out of date), reload to reconcile and rethrow so callers can handle it
            reloadPartsAndReports(true);
            throw err;
        }
    }

    const updatePartColor = (partId: number, colors: string[]) => {
        savedReportsClient.updatePartColors(partId, colors);
    }

    const deletePart = async (report: Report, partIdToDelete: number, partsToUpdate: PartDescriptor[]) => {
        try {
            await savedReportsClient.deletePart(new DeleteReportPartRequest({
                savedReportId: report.savedReportId,
                expectedGuid: report.modifiedGuid,
                partIdToDelete: partIdToDelete,
                partsToUpdate: partsToUpdate
            }));
            dispatch(setIsDataInSyncWithDatabase(true));
        } catch (err) {
            await reloadPartsAndReports(true);
            throw err;
        }
    }

    const pollForUpdates = (reportGUID: string, reportId: number) => {
        savedReportsClient.hasReportChanged(reportId, reportGUID)
            .then(async (hasChanged) => {
                if(hasChanged){
                    await reloadPartsAndReports(true);
                }
            }
        ).catch(async () => {
            // If the `hasReportChanged` endpoint errors, we should reload for safety
            await reloadPartsAndReports(true);
        })
    }

    const createReportFromTemplate = async (templateId: number, reportName: string) => {
        await savedReportsClient.createReportFromTemplate(templateId, reportName);
        await reloadPartsAndReports(true);
    }

    const asyncDispatch = async (action: SavedReportsAction) => {
        switch (action.type) {
            case "CREATE_REPORT":
                return createReport(action.data);
            case "COPY_REPORT":
                return copyReport(action.data.reportId, action.data.page, action.data.newName, action.data.isShared, action.data.isDefault);
            case "DELETE_REPORT":
                return deleteReport(action.data.reportId);
            case "UPDATE_REPORT_SETTINGS":
                return updateReportSettings(action.data);
            case 'ADD_PARTS':
                return addParts(action.data.report, action.data.parts);
            case "UPDATE_PARTS":
                return updateParts(action.data.report, action.data.parts, action.data.forceReload);
            case "UPDATE_PART_COLOUR":
                return updatePartColor(action.data.partId, action.data.colours);
            case "DELETE_PART":
                return deletePart(action.data.report, action.data.partIdToDelete, action.data.partsToUpdate);
            case "POLL_FOR_UPDATES":
                return pollForUpdates(action.data.reportGUID, action.data.reportId)
            case "TRIGGER_RELOAD":
                return reloadPartsAndReports(true);
            case "CREATE_REPORT_FROM_TEMPLATE":
                return createReportFromTemplate(action.data.templateId, action.data.reportName)
            default:
                throw new Error(`Unsupported action type: ${action}`);
        }
    }

    return (
        <SavedReportsContext.Provider value={{
            reportsDispatch: asyncDispatch,
        }}>
            {props.children}
        </SavedReportsContext.Provider>
    );
}