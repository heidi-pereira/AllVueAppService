import React from "react";
import { IApplicationUser, MainQuestionType, PartDescriptor, ReportType, CalculationType, Report, FeatureCode, PermissionFeaturesOptions } from "../../../../BrandVueApi";
import { PartWithExtraData } from "../ReportsPageDisplay";
import { TabContent, TabPane, Nav, NavItem, NavLink } from 'reactstrap';
import ConfigureReportPartOptionsTab from "./ConfigureReportPartOptionsTab";
import ConfigureReportPartBreaksTab from "./ConfigureReportPartBreaksTab";
import { IGoogleTagManager } from "../../../../googleTagManager";
import ConfigureReportPartOverTimeTab from "./ConfigureReportPartOverTimeTab";
import SidePanelHeader from "../../../SidePanelHeader";
import CreateNetSidePanel from "./CreateNetSidePanel";
import {useEffect} from "react";
import {useConfigureNets} from "./ConfigureNets";
import { CSSTransition } from "react-transition-group";
import ConfigureReportPartQuestionTab from "./ConfigureReportPartQuestionTab";
import { PageHandler } from "../../../PageHandler";
import { useMetricStateContext } from "../../../../metrics/MetricStateContext";
import { useAppDispatch, useAppSelector } from '../../../../state/store';
import { setIsSettingsChange } from '../../../../state/reportSlice';
import { Metric } from "../../../../metrics/metric";
import { isUsingBreaks, isUsingOverTime, isUsingWaves } from "../Charts/ReportsChartHelper";
import { isFeatureEnabled } from "../../../helpers/FeaturesHelper";
import { selectSubsetId } from "client/state/subsetSlice";
import { ProductConfigurationContext } from "../../../../ProductConfigurationContext";
import { hasAllVuePermissionsOrSystemAdmin } from '../../../../components/helpers/FeaturesHelper';
import { selectCurrentReport } from "client/state/reportSelectors";

interface IReportsPageConfigureChartMenuProps {
    reportPart: PartWithExtraData;
    visible: boolean;
    questionTypeLookup: {[key: string]: MainQuestionType};
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler
    user: IApplicationUser | null;
    updatePart(newPart: PartWithExtraData): void;
    closeMenu(): void;
}

enum TabSelection {
    Question,
    Breaks,
    OverTime,
    Options,
};

const ReportsPageConfigureMenu = (props: IReportsPageConfigureChartMenuProps) => {
    const dispatch = useAppDispatch();
    const isSettingsChange = useAppSelector(state => state.report.isSettingsChange);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;
    
    const [activeTab, setActiveTab] = React.useState<TabSelection>(TabSelection.Question);
    const [isSidePanelOpen, setIsSidePanelOpen] = React.useState(false);
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const { selectableMetricsForUser: metrics } = useMetricStateContext();
    const subsetId = useAppSelector(selectSubsetId);
    const netAPI = useConfigureNets(props.reportPart, metrics, subsetId, props.googleTagManager, props.pageHandler)

    const closeMenu = () => {
        props.closeMenu();
        setIsSidePanelOpen(false);
    }

    const getMenuClassName = () => {
        if (props.visible) {
            return "configure-part-area visible";
        }

        return "configure-part-area";
    }

    const savePartChanges = (modifiedPart: PartDescriptor) => {
        const newPartWithExtra: PartWithExtraData = {
            ...props.reportPart,
            part: modifiedPart,
        };
        if(!isSettingsChange) {
            dispatch(setIsSettingsChange(true));
        }
        props.updatePart(newPartWithExtra);
    }

    const getTitle = (reportType: ReportType, metric: Metric|undefined): string => {
        switch (reportType) {
            case ReportType.Chart:
                if (metric) {
                    if (props.questionTypeLookup[metric.name] == MainQuestionType.HeatmapImage) {
                        return "heatmap";
                    }
                }
                return "chart";
            case ReportType.Table:
                return "table";
        }
        return "unknown";
    }
    const isHeatMap = (props.reportPart.metric && props.questionTypeLookup[props.reportPart.metric.name] == MainQuestionType.HeatmapImage)??false;

    const getConfigureContent = () => {
        if (!props.reportPart.metric) {
            const reportType = getTitle(report.reportType, props.reportPart.metric);
            return (
                <div className="warning-text">
                    Unable to find question '{props.reportPart.part.spec1}'.
                    If it has been renamed, you can remove this {reportType} and add a new {reportType} for the renamed question, or you can change the name back to '{props.reportPart.part.spec1}'.
                </div>
            );
        }
        const canUseWaves = report.reportType === ReportType.Chart && props.reportPart.metric?.calcType !== CalculationType.Text;
        const isNotText = props.reportPart.metric?.calcType !== CalculationType.Text;
        const permissionToCreateVariables = hasAllVuePermissionsOrSystemAdmin(productConfiguration, [PermissionFeaturesOptions.VariablesCreate]);

        return (
            <>
            <Nav tabs>
                <NavItem>
                    <NavLink className={activeTab === TabSelection.Question ? 'tab-active' : 'tab-item'}
                        onClick={() => setActiveTab(TabSelection.Question)}>
                        Question
                    </NavLink>
                </NavItem>
                {canUseWaves &&
                    <NavItem>
                        <NavLink className={activeTab === TabSelection.OverTime ? 'tab-active' : 'tab-item'}
                            onClick={() => setActiveTab(TabSelection.OverTime)}>
                            {isFeatureEnabled(FeatureCode.Overtime_data) ? "Over time" : "Waves"}
                        </NavLink>
                    </NavItem>
                }
                {
                    isNotText &&
                    <NavItem>
                        <NavLink className={activeTab === TabSelection.Breaks ? 'tab-active' : 'tab-item'}
                            onClick={() => setActiveTab(TabSelection.Breaks)}>
                            Breaks
                        </NavLink>
                    </NavItem>
                }

                <NavItem>
                    <NavLink className={activeTab === TabSelection.Options ? 'tab-active' : 'tab-item'}
                        onClick={() => setActiveTab(TabSelection.Options)}>
                        Options
                    </NavLink>
                </NavItem>
            </Nav>
            <TabContent activeTab={activeTab}>
                <TabPane tabId={TabSelection.Question}>
                    <ConfigureReportPartQuestionTab
                            reportPart={props.reportPart}
                            savePartChanges={savePartChanges}
                            isUsingOverTime={isUsingOverTime(report, props.reportPart)}
                            isUsingWaves={isUsingWaves(report, props.reportPart)}
                            isUsingBreaks={isUsingBreaks(props.reportPart, report)}
                            setIsSidePanelOpen={setIsSidePanelOpen}
                            configureNets={netAPI}
                            isNotText={isNotText}

                    />
                </TabPane>
                {canUseWaves &&
                    <TabPane tabId={TabSelection.OverTime}>
                        <ConfigureReportPartOverTimeTab
                                reportPart={props.reportPart}
                                reportWaves={report.waves}
                                questionTypeLookup={props.questionTypeLookup}
                                savePartChanges={savePartChanges}
                        />
                    </TabPane>
                }
                <TabPane tabId={TabSelection.Breaks}>
                    <ConfigureReportPartBreaksTab
                            reportType={report.reportType}
                            reportPart={props.reportPart}
                            reportBreaks={report.breaks}
                            reportOrderBy={report.reportOrder}
                            questionTypeLookup={props.questionTypeLookup}
                            googleTagManager={props.googleTagManager}
                            pageHandler={props.pageHandler}
                            user={props.user}
                            isUsingOverTime={isUsingOverTime(report, props.reportPart)}
                            savePartChanges={savePartChanges}
                    />
                </TabPane>
                <TabPane tabId={TabSelection.Options}>
                    <ConfigureReportPartOptionsTab
                        reportPart={props.reportPart}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        savePartChanges={savePartChanges}
                        isUsingBreaks={isUsingBreaks(props.reportPart, report)}
                        canCreateNewBase={permissionToCreateVariables}
                        isNotText={isNotText}
                        configureNets={netAPI}
                        isHeatMap={isHeatMap}
                    />
                </TabPane>
            </TabContent>
            </>
        );
    }

    const getReturnHandler = () => {
        if (isSidePanelOpen){
            return () => {setIsSidePanelOpen(false)}
        }
        return undefined
    }

    useEffect(() => {
        setIsSidePanelOpen(false)
    }, [props.reportPart])


    return (
        <div className={getMenuClassName()}>
            <SidePanelHeader closeButtonHandler={closeMenu} returnButtonHandler={getReturnHandler()}>
                {!isSidePanelOpen ? `Configure ${getTitle(report.reportType, props.reportPart.metric)}` : `Add net`}
            </SidePanelHeader>
            <div className="transition-container">
                <CSSTransition in={!isSidePanelOpen} timeout={200} classNames="configure-part-page-one">
                    <div className="configure-part-page-one">
                        {getConfigureContent()}
                    </div>
                </CSSTransition>
                {!isHeatMap &&
                    <CSSTransition in={isSidePanelOpen} timeout={200} classNames="configure-part-page-two">
                        <div className="configure-part-page-two">
                            <CreateNetSidePanel
                                setIsSidePanelOpen={setIsSidePanelOpen}
                                isSidePanelOpen={isSidePanelOpen}
                                selectedMetric={props.reportPart.metric!}
                                googleTagManager={props.googleTagManager}
                                pageHandler={props.pageHandler}
                                configureNets={netAPI}
                            />
                        </div>
                    </CSSTransition>
                }
            </div>
        </div>
    );
}

export default ReportsPageConfigureMenu;