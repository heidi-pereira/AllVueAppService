import React from "react";
import {
    IAverageDescriptor,
    IApplicationUser,
    MainQuestionType,
    ReportType,
} from "../../../../BrandVueApi";
import _ from "lodash";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import CreateNewReportModal from "../Modals/CreateNewReportModal";
import { ReportWithPage } from "../ReportsPage";
import { getUrlForPageName } from "../../../helpers/PagesHelper";
import { IGoogleTagManager } from "../../../../googleTagManager";
import { PageHandler } from "../../../PageHandler";
import { MixPanel } from "../../../mixpanel/MixPanel";
import {useLocation, useNavigate} from "react-router-dom";
import { useReadVueQueryParams } from "../../../helpers/UrlHelper";
import { ApplicationConfiguration } from "../../../../ApplicationConfiguration";
import { useAppSelector } from "client/state/store";
import { selectDefaultReportId } from "client/state/reportSlice";
import { selectAllReportPages, selectCurrentReport } from "client/state/reportSelectors";

interface IReportSelectorProps {
    user: IApplicationUser | null;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    questionTypeLookup: {[key: string]: MainQuestionType};
    canEditReports: boolean;
    applicationConfiguration: ApplicationConfiguration;
    averages: IAverageDescriptor[];
}

const ReportSelector = (props: IReportSelectorProps) => {
    const defaultReportId = useAppSelector(selectDefaultReportId);
    const allReportPages = useAppSelector(selectAllReportPages);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const page = currentReportPage.page;
        
    const [isReportSelectorOpen, setIsReportSelectorOpen] = React.useState(false);
    const [isCreateNewReportModalVisible, setIsCreateNewReportModalVisible] = React.useState(false);
    const navigate = useNavigate();
    const sortedReports = allReportPages.sort((a, b) => a.page.displayName.localeCompare(b.page.displayName));
    const defaultReport = sortedReports.filter(r => r.report.savedReportId == defaultReportId);
    const sharedReports = sortedReports.filter(r => r.report.isShared && r.report.savedReportId != defaultReportId);
    const myReports = sortedReports.filter(r => !r.report.isShared);
    const location = useLocation();
    const readVueQueryParams = useReadVueQueryParams();
    const navigateToReport = (reportPage: ReportWithPage) => {
        navigate(getUrlForPageName(reportPage.page.name, location, readVueQueryParams));
        MixPanel.track("reportsPageLoaded", { ReportName: reportPage.page.name });
        props.googleTagManager.addEvent("reportsPageViewReport", props.pageHandler);
    }

    const getReportDropdownItem = (reportPage: ReportWithPage) => {
        return (
            <DropdownItem key={reportPage.report.savedReportId} onClick={() => navigateToReport(reportPage)}>
                <span className="report-selector-icon">
                {reportPage.report.reportType === ReportType.Chart &&
                    <i className="material-symbols-outlined rotate">bar_chart</i>
                }
                {reportPage.report.reportType === ReportType.Table &&
                    <i className="material-symbols-outlined">table_chart</i>
                    }
                </span>
                {reportPage.page.displayName}
            </DropdownItem>
        );
    }

    return (
        <>
            {props.canEditReports &&
                <>
                <CreateNewReportModal
                    user={props.user}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.pageHandler}
                    isCreateNewReportModalVisible={isCreateNewReportModalVisible}
                    setCreateNewReportModalVisibility={(isVisible) => setIsCreateNewReportModalVisible(isVisible)}
                    questionTypeLookup={props.questionTypeLookup}
                    applicationConfiguration={props.applicationConfiguration}
                    averages={props.averages}
                />
                </>
            }
            <ButtonDropdown isOpen={isReportSelectorOpen} toggle={() =>
                setIsReportSelectorOpen(!isReportSelectorOpen)} className="report-selector-dropdown">
                <DropdownToggle className="report-selector-toggle">
                    <div className="report-name">{page.displayName}</div>
                    <i className="material-symbols-outlined">arrow_drop_down</i>
                </DropdownToggle>
                <DropdownMenu>
                    <div className="report-list">
                        {defaultReport.length > 0 &&
                            <DropdownItem header>
                                Default report
                            </DropdownItem>
                        }
                        {defaultReport.map(r => getReportDropdownItem(r))}
                        {sharedReports.length > 0 &&
                            <DropdownItem header>
                                Shared reports
                            </DropdownItem>
                        }
                        {sharedReports.map(r => getReportDropdownItem(r))}
                        {myReports.length > 0 &&
                            <DropdownItem header>
                                My reports
                            </DropdownItem>
                        }
                        {myReports.map(r => getReportDropdownItem(r))}
                    </div>
                    {props.canEditReports &&
                        <div className="create-report-button-container">
                            <button className="hollow-button" onClick={() => setIsCreateNewReportModalVisible(true)}>
                                <i className="material-symbols-outlined">add</i>
                                <div>Create new report</div>
                            </button>
                        </div>
                    }
                </DropdownMenu>
            </ButtonDropdown>
        </>
    )
}

export default ReportSelector;