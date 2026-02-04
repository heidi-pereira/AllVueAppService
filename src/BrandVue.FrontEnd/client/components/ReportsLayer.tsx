import React from "react";
import { useDispatch } from "react-redux";
import {
    IPaneDescriptor,
    IAverageDescriptor,
    IApplicationUser,
} from "../BrandVueApi";
import { CuratedFilters } from "../filter/CuratedFilters";
import { AsyncExportProvider } from "./visualisations/Reports/Utility/AsyncExportContext";
import { SavedReportsProvider } from "./visualisations/Reports/SavedReportsContext";
import ReportsPage, { ReportWithPage } from "./visualisations/Reports/ReportsPage";
import { PaneType } from "./panes/PaneType";
import { setCurrentReportId, selectDefaultReportId } from "client/state/reportSlice";
import { selectAllReportPages } from 'client/state/reportSelectors';
import { useAppSelector } from "client/state/store";
import { dsession } from "../dsession";
import { ApplicationConfiguration } from "../ApplicationConfiguration";
import { ProductConfiguration } from "../ProductConfiguration";
import { IGoogleTagManager } from "../googleTagManager";

interface IReportsLayerProps {
    pane: IPaneDescriptor;
    session: dsession;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    averages: IAverageDescriptor[];
    curatedFilters: CuratedFilters;
    productConfiguration: ProductConfiguration;
    user: IApplicationUser | null;
}

const ReportsLayer: React.FC<IReportsLayerProps> = (props) => {
    const dispatch = useDispatch();
    const allReportPages = useAppSelector(selectAllReportPages);
    const defaultReportId = useAppSelector(selectDefaultReportId);

    React.useEffect(() => {
        const activePage = props.session.activeDashPage;
        let reportPage: ReportWithPage | undefined = undefined;

        if (activePage.panes[0].paneType === PaneType.reportSubPage) {
            //viewing a specific report via the url
            reportPage = allReportPages.find(r => r.page.id === activePage.id);
        } else if (defaultReportId !== undefined && defaultReportId !== null) {
            //viewing root reports page
            reportPage = allReportPages.find(r => r.report.savedReportId === defaultReportId);
        }
        dispatch(setCurrentReportId(reportPage?.report.savedReportId));
    }, [props.session.activeDashPage, allReportPages, defaultReportId, dispatch]);

    return (
        <AsyncExportProvider key={props.pane.id}>
            <SavedReportsProvider session={props.session} key={props.pane.id} user={props.user}>
                <ReportsPage session={props.session}
                    googleTagManager={props.googleTagManager}
                    applicationConfiguration={props.applicationConfiguration}
                    averages={props.averages}
                    curatedFilters={props.curatedFilters}
                    productConfiguration={props.productConfiguration}
                />
            </SavedReportsProvider>
        </AsyncExportProvider>
    );
}

export default ReportsLayer;
