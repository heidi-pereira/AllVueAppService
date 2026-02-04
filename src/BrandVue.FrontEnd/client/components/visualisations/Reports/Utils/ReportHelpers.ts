import { Report, PageDescriptor } from '../../../../BrandVueApi';
import { ReportWithPage } from '../ReportsPage';

export const getReportPagesFrom = (reports: Report[], rootReportsPage: PageDescriptor | undefined): ReportWithPage[] => {
    const reportPages = reports.map(r => {
        const page = rootReportsPage?.childPages?.find(p => p.id === r.pageId);
        return {
            report: r,
            page: page,
        }
    });
    return reportPages.filter((r): r is ReportWithPage => r.page != undefined);
}

export default { getReportPagesFrom };
