import React from "react";
import { CatchReportAndDisplayErrors } from "../../../components/CatchReportAndDisplayErrors";
import { IGoogleTagManager } from "../../../googleTagManager";
import { ProductConfiguration } from "../../../ProductConfiguration";
import { ApplicationConfiguration } from "../../../ApplicationConfiguration";
import { PageHandler } from "../../PageHandler";

import style from "./ReportVuePage.module.less";

import ReportVueUserPage from "./UserPages/ReportVueUserPage";
import ReportVueAdminPage from "./AdminPages/ReportVueAdminPage";
import ReportVuePageSidePanel from "./ReportVuePageSidePanel";
import { IntegrationReferenceType } from "../../../BrandVueApi";

interface IReportVuePage {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
}

export enum ReportVuePageView {
    Standard,
    Administation,
}
const ReportVuePage = (props: IReportVuePage) => {
    const [reportVueSubPage, setReportVueSubPage] = React.useState<ReportVuePageView>(ReportVuePageView.Standard);
    const getPageContent = () => {
        if (reportVueSubPage == ReportVuePageView.Administation) {
            return (
                <CatchReportAndDisplayErrors applicationConfiguration={props.applicationConfiguration} childInfo={{ "Component": "UsersSettingsPage" }}>
                    <ReportVueAdminPage/> 
                </CatchReportAndDisplayErrors>
            );
        }
        if (reportVueSubPage == ReportVuePageView.Standard) {
            return (<ReportVueUserPage
                applicationConfiguration={props.applicationConfiguration}
                googleTagManager={props.googleTagManager}
                pageHandler={props.pageHandler}
                productConfiguration={props.productConfiguration} /> 
            );

        }
        return <div></div>;
    }

    if (!props.productConfiguration.user.isSystemAdministrator) {
        return (getPageContent());
    }
    const reportVues = props.productConfiguration.additionalUiWidgets.filter(x => x.referenceType == IntegrationReferenceType.ReportVue);
    return (<div className={style.pageBuffer}>
        <ReportVuePageSidePanel icon={reportVues[0].icon} currentView={reportVueSubPage} setCurrentView={setReportVueSubPage} />
                    <div className={style.pageContent}>
                        {getPageContent()}
                </div>
            </div>
    );
}

export default ReportVuePage;
