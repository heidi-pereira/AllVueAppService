import { createSlice, PayloadAction, createAsyncThunk } from '@reduxjs/toolkit';
import { ReportErrorState, initialReportErrorState } from '../components/visualisations/shared/ReportErrorState';
import { RootState } from './store';
import * as BrandVueApi from "../BrandVueApi";

interface ReportState {
    errorState: ReportErrorState;
    isSettingsChange: boolean;
    currentReportId?: number;
    currentReportGuid?: string;
    allReports: BrandVueApi.Report[];
    isLoading: boolean;
    isDataInSyncWithDatabase: boolean;
    defaultReportId?: number;
    reportsPageOverride?: BrandVueApi.PageDescriptor;
}

const initialState: ReportState = {
    errorState: initialReportErrorState,
    isSettingsChange: false,
    currentReportId: undefined,
    currentReportGuid: undefined,
    allReports: [],
    isLoading: false,
    isDataInSyncWithDatabase: true,
    defaultReportId: undefined,
    reportsPageOverride: undefined,
};


export interface ReloadReportModel {
    forceLoad: boolean;
    userId?: string;
}

export const reloadAllReports = createAsyncThunk(
    'report/reloadAllReports',
    async (payload: ReloadReportModel) => {
        const { forceLoad } = payload;
        const savedReportsClient = BrandVueApi.Factory.SavedReportClient(error => { throw error(); });
        const result: BrandVueApi.ReportsForSurveyAndUser = await savedReportsClient.getAll();
        return { result, forceLoad, userId: payload.userId };
    }
);

const reportSlice = createSlice({
    name: 'report',
    initialState,
    reducers: {
        setReportErrorState(state, action: PayloadAction<ReportErrorState>) {
            state.errorState = action.payload;
        },
        setIsSettingsChange(state, action: PayloadAction<boolean>) {
            state.isSettingsChange = action.payload;
        },
        setIsDataInSyncWithDatabase(state, action: PayloadAction<boolean>) {
            state.isDataInSyncWithDatabase = action.payload;
        },
        setCurrentReportId(state, action: PayloadAction<number | undefined>) {
            //If the report id is changed clear local page state
            if (action.payload !== state.currentReportId) {
                state.reportsPageOverride = undefined;
                state.currentReportId = action.payload;
            }
        },
        setReportsPageOverride(state, action: PayloadAction<BrandVueApi.PageDescriptor | undefined>) {
            state.reportsPageOverride = action.payload;
        }
    },
    extraReducers: builder => {
        builder
            .addCase(reloadAllReports.pending, (state) => {
                state.isLoading = true;
            })
            .addCase(reloadAllReports.fulfilled, (state, action) => {
                const { result, forceLoad, userId } = action.payload as { result: BrandVueApi.ReportsForSurveyAndUser, forceLoad: boolean, userId: string };
                state.defaultReportId = result.defaultReportId;

                if (forceLoad) {
                    state.allReports = result.reports;
                    state.currentReportId = result.defaultReportId;
                } else if (state.currentReportId && state.currentReportGuid) {
                    const currentReportDb = result.reports.find(r => r.savedReportId == state.currentReportId);
                    const selectedReportModifiedBySomeoneElse = currentReportDb && (currentReportDb?.modifiedGuid != state.currentReportGuid && currentReportDb?.lastModifiedByUser != userId);

                    if (selectedReportModifiedBySomeoneElse) {
                        state.allReports = result.reports;
                        state.currentReportId = result.defaultReportId;
                    }
                } else {
                    state.allReports = result.reports;
                }

                state.isLoading = false;
                state.isDataInSyncWithDatabase = true;
            })
            .addCase(reloadAllReports.rejected, (state) => {
                state.isLoading = false;
                state.isDataInSyncWithDatabase = false;
            });
    }
});

export const { setReportErrorState, setIsSettingsChange, setCurrentReportId, setIsDataInSyncWithDatabase, setReportsPageOverride } = reportSlice.actions;
export const selectCurrentReportId = (state: RootState): number | undefined => state.report.currentReportId;
export const selectAllReports = (state: RootState): BrandVueApi.Report[] => state.report.allReports;

export const selectDefaultReportId = (state: RootState): number | undefined => state.report.defaultReportId;
export const selectIsDataInSyncWithDatabase = (state: RootState): boolean => state.report.isDataInSyncWithDatabase;
export const selectIsLoadingReports = (state: RootState): boolean => state.report.isLoading;


export default reportSlice.reducer;