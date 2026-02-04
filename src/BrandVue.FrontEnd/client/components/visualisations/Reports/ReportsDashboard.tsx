import React from "react";
import {
    IAverageDescriptor,
    IApplicationUser,
    MainQuestionType,
} from "../../../BrandVueApi";
import CreateNewReportModal from "./Modals/CreateNewReportModal";
import { Metric } from "../../../metrics/metric";
import { ReportWithPage } from "./ReportsPage";
import ReportSettingsModal, { ReportSettingsModalTabSelection } from "./Modals/ReportSettingsModal";
import { IGoogleTagManager } from "../../../googleTagManager";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import ReportTile from "./Components/ReportTile";
import NoMetaDataNotification from "../NoMetadataNotification";
import { PageHandler } from "../../PageHandler";
import { VariableProvider } from "../Variables/VariableModal/Utils/VariableContext";
import { ProductConfigurationContext } from "../../../ProductConfigurationContext";
import { ApplicationConfiguration } from "../../../ApplicationConfiguration";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { selectDefaultReportId } from "client/state/reportSlice";
import { useAppSelector } from "client/state/store";
import { selectAllReportPages } from "client/state/reportSelectors";

interface INoReportsPageProps {
    canEditReports: boolean;
    metricsForReports: Metric[];
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    user: IApplicationUser | null;
    reportsPageUrl: string;
    applicationConfiguration: ApplicationConfiguration;
    averages: IAverageDescriptor[];
}

type SortField = "name" | "date";
type SortDirection = "asc" | "desc";

const ReportsDashboard = (props: INoReportsPageProps) => {
    const [isCreateNewReportModalVisible, SetCreateNewReportModalVisible] = React.useState<boolean>(false);
    const [isReportSettingsModalVisible, setReportSettingsModalVisible] = React.useState<boolean>(false);
    const [reportSettingsModalActiveTab, setReportSettingsModalActiveTab] = React.useState<ReportSettingsModalTabSelection>(
        ReportSettingsModalTabSelection.Details
    );
    const [reportToEdit, setReportToEdit] = React.useState<ReportWithPage | undefined>(undefined);
    const [sortField, setSortField] = React.useState<SortField>("name");
    const [sortDirection, setSortDirection] = React.useState<SortDirection>("asc");
    const [isSortDropdownOpen, setIsSortDropdownOpen] = React.useState<boolean>(false);
    const defaultReportId = useAppSelector(selectDefaultReportId);
    const allReportPages = useAppSelector(selectAllReportPages);
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    
    const orderedReportPages = React.useMemo(() => {
        return [...allReportPages].sort((a, b) => {
            let comparison = 0;
            if (sortField === "name") {
                comparison = a.page.displayName.localeCompare(b.page.displayName);
            } else {
                const dateA = new Date(a.report.modifiedDate).getTime();
                const dateB = new Date(b.report.modifiedDate).getTime();
                comparison = dateA - dateB;
            }

            return sortDirection === "asc" ? comparison : -comparison;
        });
    }, [allReportPages, defaultReportId, sortField, sortDirection]);

    const getSortLabel = (field: SortField, direction: SortDirection): string => {
        if (field === "name") {
            return direction === "asc" ? "Name (A-Z)" : "Name (Z-A)";
        }
        return direction === "asc" ? "Oldest to newest" : "Newest to oldest";
    };

    const handleSortChange = (field: SortField, direction: SortDirection) => {
        setSortField(field);
        setSortDirection(direction);
    };

    const getNoReportsContent = (): JSX.Element => {
        if (!props.metricsForReports || props.metricsForReports.length == 0) {
            return <NoMetaDataNotification />;
        }

        if (!props.canEditReports) {
            return (
                <div className="no-reports">
                    <div className="description">
                        <span>No reports have been created for this survey yet.</span>
                        <span>After reports have been created by an Administrator, you will be able to view them here.</span>
                    </div>
                </div>
            );
        }

        return (
            <div className="no-reports">
                <div className="description">
                    <span>Create a report to present results from this project.</span>
                    <span>You can create as many reports as you like.</span>
                </div>
                <button className="primary-button" onClick={() => SetCreateNewReportModalVisible(!isCreateNewReportModalVisible)}>
                    <i className="material-symbols-outlined">add</i>
                    Create new report
                </button>
            </div>
        );
    };

    const getReportsListContent = (): JSX.Element => {
        return (
            <div className="reports-list">
                <div className="topbar">
                    {props.canEditReports && (
                        <button className="primary-button" onClick={() => SetCreateNewReportModalVisible(!isCreateNewReportModalVisible)}>
                            <i className="material-symbols-outlined">add</i>
                            <div>Create new report</div>
                        </button>
                    )}
                    <div className="sort-controls">
                        <label id="sort-by-label">Sort by:</label>
                        <ButtonDropdown isOpen={isSortDropdownOpen} toggle={() => setIsSortDropdownOpen(!isSortDropdownOpen)} className="configure-option-dropdown">
                            <DropdownToggle
                                className="toggle-button"
                                aria-labelledby="sort-by-label"
                            >
                                <span>{getSortLabel(sortField, sortDirection)}</span>
                                <i className="material-symbols-outlined">arrow_drop_down</i>
                            </DropdownToggle>
                            <DropdownMenu>
                                <DropdownItem onClick={() => handleSortChange("name", "asc")}>Name (A-Z)</DropdownItem>
                                <DropdownItem onClick={() => handleSortChange("name", "desc")}>Name (Z-A)</DropdownItem>
                                <DropdownItem onClick={() => handleSortChange("date", "asc")}>Oldest to newest</DropdownItem>
                                <DropdownItem onClick={() => handleSortChange("date", "desc")}>Newest to oldest</DropdownItem>
                            </DropdownMenu>
                        </ButtonDropdown>
                    </div>
                </div>
                <div className="list">
                    {orderedReportPages.map((reportPage, i) => (
                        <ReportTile
                            key={i}
                            applicationConfiguration={props.applicationConfiguration}
                            canEditReports={props.canEditReports}
                            curatedFilters={props.curatedFilters}
                            googleTagManager={props.googleTagManager}
                            pageHandler={props.pageHandler}
                            metricsForReports={props.metricsForReports}
                            editReportSettings={(e) => editReportSettings(e, reportPage)}
                            averages={props.averages}
                            reportPage={reportPage}
                        />
                    ))}
                </div>
            </div>
        );
    };

    const editReportSettings = (e: React.MouseEvent, reportPage: ReportWithPage) => {
        e.stopPropagation();
        setReportToEdit(reportPage);
        setReportSettingsModalVisible(true);
        props.googleTagManager.addEvent("reportsPageViewReportSettings", props.pageHandler);
    };
    
    return (
        <VariableProvider
            user={props.user}
            nonMapFileSurveys={productConfiguration.nonMapFileSurveys}
            googleTagManager={props.googleTagManager}
            pageHandler={props.pageHandler}
            isSurveyGroup={productConfiguration.isSurveyGroup}
        >
            {reportToEdit && props.canEditReports && (
                <ReportSettingsModal
                    isOpen={isReportSettingsModalVisible}
                    setIsOpen={(isOpen) => setReportSettingsModalVisible(isOpen)}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.pageHandler}
                    user={props.user}
                    questionTypeLookup={props.questionTypeLookup}
                    reportsPageUrl={props.reportsPageUrl}
                    activeTab={reportSettingsModalActiveTab}
                    setActiveTab={setReportSettingsModalActiveTab}
                    applicationConfiguration={props.applicationConfiguration}
                    averages={props.averages}
                    currentReportPage={reportToEdit}
                    reportPartsHaveBreaks={reportToEdit.page.panes[0].parts.some((p) => p.overrideReportBreaks && (p.breaks ?? []).length > 0)}
                />
            )}
            {props.canEditReports && (
                <CreateNewReportModal
                    user={props.user}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.pageHandler}
                    isCreateNewReportModalVisible={isCreateNewReportModalVisible}
                    setCreateNewReportModalVisibility={(isVisible) => SetCreateNewReportModalVisible(isVisible)}
                    questionTypeLookup={props.questionTypeLookup}
                    applicationConfiguration={props.applicationConfiguration}
                    averages={props.averages}
                />
            )}
            <div id="reports-dashboard">{orderedReportPages.length == 0 ? getNoReportsContent() : getReportsListContent()}</div>
        </VariableProvider>
    );
};

export default ReportsDashboard;
