import { ReportWithPage } from 'client/components/visualisations/Reports/ReportsPage';
import { RootState } from './store';
import { throwIfNullish } from 'client/components/helpers/ThrowHelper';
import { getReportsPage } from 'client/components/helpers/PagesHelper';
import { getReportPagesFrom } from 'client/components/visualisations/Reports/Utils/ReportHelpers';
import _ from 'lodash';
import { PageDescriptor } from 'client/BrandVueApi';
import { createSelector } from 'reselect';

export const selectAllReportPages = createSelector(
    [(state: RootState) => state.report.allReports],
    (allReports) => {
        const rootReportsPage = getReportsPage();
        return getReportPagesFrom(allReports, rootReportsPage);
    }
);

export const selectReportState = (state: RootState) => state.report;

const selectCurrentReportInternal = createSelector(
    [
        (state: RootState) => state.report.currentReportId,
        (state: RootState) => state.report.allReports,
        (state: RootState) => state.report.reportsPageOverride,
    ],
    (currentReportId, allReports, reportsPageOverride): ReportWithPage | undefined => {
        if (!currentReportId) {
            return undefined;
        }
        
        const report = allReports.find(r => r.savedReportId === currentReportId);
        if (!report) {
            throw new Error(`Report with id ${currentReportId} not found`);
        }

        const parentReportsPage = getReportsPage();    
        const page = reportsPageOverride ?? parentReportsPage?.childPages?.find(p => p.id === report.pageId);
        if (!page) {
            throw new Error(`Page with id ${report.pageId} not found`);
        }

        const clonedPageForSorting: PageDescriptor = _.cloneDeep(page);
        if (clonedPageForSorting.panes && Array.isArray(clonedPageForSorting.panes[0]?.parts)) {
            clonedPageForSorting.panes[0].parts = [...clonedPageForSorting.panes[0].parts].sort((a, b) => parseInt(a.spec2) - parseInt(b.spec2));
        }

        return { report: report, page: clonedPageForSorting };
    }
);

export const selectCurrentReport = (state: RootState): ReportWithPage => {
    const result = selectCurrentReportInternal(state);
    throwIfNullish(result, "Current report not defined");
    return result!;
};

export const selectCurrentReportOrNull = selectCurrentReportInternal;

export const selectReportIsSelected = (state: RootState): boolean => {
    return state.report.currentReportId != null;
}