import React from "react";
import { IGoogleTagManager } from "../../../../googleTagManager";
import { ProductConfiguration } from "../../../../ProductConfiguration";
import { ApplicationConfiguration } from "../../../../ApplicationConfiguration";
import { PageHandler } from "../../../PageHandler";
import ReportVueUserEntryPage  from "./ReportVueUserEntryPage";
import ReportVueUserSelectedPage from "./ReportVueUserSelectedPage"
import { ActiveReport, Factory } from "../../../../BrandVueApi";

interface IReportVueUserPage {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
}


const ReportVueUserPage = (props: IReportVueUserPage) => {
    enum ReportVueUserPageView {
        Standard,
        Selected,
    }

    const [reportVuePage, setReportVuePage] = React.useState<ReportVueUserPageView>(ReportVueUserPageView.Standard);
    const [selectedReport, setSelectedReport] = React.useState<ActiveReport | undefined>();
    const [reports, setReports] = React.useState<ActiveReport[]>([]);


    React.useEffect(() => {
        const client = Factory.ReportVueClient(error => error());
        client.getAllActiveReports().then(documentsForPath => {
            setReports(documentsForPath.sort((a, b) => a.title.localeCompare(b.title)));
        });
    }, []);

    if (reports.length == 1)
        return (<ReportVueUserSelectedPage
            selectedReport={reports[0]}
            cancelSelection={() => { setReportVuePage(ReportVueUserPageView.Standard); }}
            skipInitialPage={true }
        />);
    switch (reportVuePage) {

        case ReportVueUserPageView.Standard:
            return (<ReportVueUserEntryPage
                reports={reports}
                setSelectedReport={(report) => {
                    setSelectedReport(report); setReportVuePage(ReportVueUserPageView.Selected);
                }}
                />);

        case ReportVueUserPageView.Selected:
            if (selectedReport != undefined) {
                return (<ReportVueUserSelectedPage
                    selectedReport={selectedReport}
                    cancelSelection={() => { setReportVuePage(ReportVueUserPageView.Standard); }}
                    skipInitialPage={false}
                />);
            }
    }
    return (<>Nothing defined</>);
}

export default ReportVueUserPage;
