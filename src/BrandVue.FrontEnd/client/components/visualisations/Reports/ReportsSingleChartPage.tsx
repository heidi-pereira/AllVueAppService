import {
    MainQuestionType, CrossMeasure, IApplicationUser, PartDescriptor, ReportWaveConfiguration, ReportWavesOptions,
    IAverageDescriptor, Report, PageDescriptor, ReportType,
} from "../../../BrandVueApi";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import { IGoogleTagManager } from "../../../googleTagManager";
import { PartWithExtraData } from "./ReportsPageDisplay";
import ReportsPageConfigureMenu from "./Configuration/ConfigureReportPartMenu";
import {useEffect, useState} from "react";
import { getDateRangePickerTitleFromDates, getReportPartDisplayText } from "../../helpers/SurveyVueUtils";
import ReportsPageCard, { ReportsPageCardType } from "./Cards/ReportsPageCard";
import { ApplicationConfiguration } from "../../../ApplicationConfiguration";
import FiltersBar from "./Filtering/FiltersBar";
import { FilterButtonType } from "./Filtering/AddFilterButton";
import ReportCardLowSampleWarning from "./Components/ReportCardLowSampleWarning";
import { useFilterStateContext } from "../../../filter/FilterStateContext";
import ReportPowerpointExportButton from "./Components/ReportPowerpointExportButton";
import { CatchReportAndDisplayErrors } from "../../CatchReportAndDisplayErrors";
import { PartType } from "../../panes/PartType";
import { PageHandler } from "../../PageHandler";
import DropdownSelector from "../../dropdown/DropdownSelector";
import {useConfigureNets} from "./Configuration/ConfigureNets";
import {EntityInstance} from "../../../entity/EntityInstance";
import WarningBanner from "../WarningBanner";
import { useMetricStateContext } from "../../../metrics/MetricStateContext";
import { canSelectFilterInstances, isUsingOverTime } from "./Charts/ReportsChartHelper";
import AverageSelector from "../../filters/AverageSelector";
import { useAppSelector } from "client/state/store";
import ConfigureReportPartFilterInstance from "./Configuration/Options/ConfigureReportPartFilterInstance";
import { selectSubsetId } from "client/state/subsetSlice";
import AllVueDateRangePicker from "client/components/visualisations/Reports/Components/AllVueDateRangePicker";
import { selectCurrentReport } from "client/state/reportSelectors";

interface IReportsPageSingleChartPageProps {
    reportPart: PartWithExtraData;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    user: IApplicationUser | null;
    curatedFilters: CuratedFilters;
    overTimeFilters: CuratedFilters;
    questionTypeLookup: {[key: string]: MainQuestionType};
    canEditReport: boolean;
    applicationConfiguration: ApplicationConfiguration;
    getPartBreaks(part: PartWithExtraData): CrossMeasure[];
    getPartWaves(part: PartDescriptor): CrossMeasure | undefined;
    updatePart(newPart: PartWithExtraData): void;
    viewAllCharts(): void;
    removeFromReport(): void;
    openModalToFilterPage(): void;
    showWeightedCounts: boolean;
    isDataInSyncWithDatabase: boolean;
    duplicatePart(partDescriptor: PartDescriptor): void;
    userVisibleAverages: IAverageDescriptor[];
    isReportUsingOverTime: boolean;
    startDate: Date | undefined;
    endDate: Date | undefined;
    overTimeAverage: IAverageDescriptor | undefined;
    setDates: (startDate: Date, endDate: Date) => void;
    setOverTimeAverage: (average: IAverageDescriptor) => void;
}

const ReportsSingleChartPage = (props: IReportsPageSingleChartPageProps) => {
    const [configureChartMenuVisible, setConfigureChartMenuVisible] = useState<boolean>(props.canEditReport);
    const [isLowSample, setIsLowSample] = useState<boolean>(false);
    const { filters, metricsValidAsFilter } = useFilterStateContext();
    const [allMultiBreakInstanceOptions, setAllMultiBreakInstanceOptions] = useState<EntityInstance[]>([]);
    const { selectableMetricsForUser: metrics, metricsForReports } = useMetricStateContext();
    const subsetId = useAppSelector(selectSubsetId);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;
    const page = currentReportPage.page;
    
    const netAPI = useConfigureNets(props.reportPart, metrics, subsetId, props.googleTagManager, props.pageHandler)
    const availableEntityInstances = netAPI.availableEntityInstances;

    const breaks = getPartBreaks();

    const isPartUsingOverTime = isUsingOverTime(report, props.reportPart);

    useEffect(() => {
        const selectedInstances = props.reportPart.part.selectedEntityInstances?.selectedInstances ? availableEntityInstances.filter(entity => props.reportPart.part.selectedEntityInstances?.selectedInstances.some(i => entity.id === i)) : availableEntityInstances;
        setAllMultiBreakInstanceOptions(selectedInstances)
    }, [
        JSON.stringify(availableEntityInstances.map(e => e.name).sort()),
        JSON.stringify(props.reportPart.part.selectedEntityInstances?.selectedInstances.sort())
    ]);

    const getSelectedEntity = () => {
        return allMultiBreakInstanceOptions.find(e => e.id === props.reportPart.part.multiBreakSelectedEntityInstance)
    }

    function getPartBreaks(): CrossMeasure[] | undefined {
        const breaks = props.getPartBreaks(props.reportPart);
        const useSingleBreak = breaks != null && breaks.length > 0 && breaks[0].filterInstances.length > 0;
        const useMultipleBreaks = breaks != null && breaks.length > 1;
        if (useSingleBreak || useMultipleBreaks) {
            return breaks;
        }
    }

    const closeConfigureChartMenu = () => {
        setConfigureChartMenuVisible(false);
    }

    const getAddFiltersButton = () => {
        if (props.canEditReport) {
            return (
                <button className="hollow-button open-report-filter-menu-button" onClick={props.openModalToFilterPage}>
                    <i className="material-symbols-outlined">filter_alt</i>
                    <div>Filter</div>
                </button>
            );
        }
    }

    const updateSelectedMultiBreakOptions = (instance: EntityInstance | undefined) => {
        const clonedPart = new PartDescriptor(props.reportPart.part);
        clonedPart.multiBreakSelectedEntityInstance = instance?.id;
        props.updatePart({...props.reportPart, part: clonedPart});
    }

    const updateBreaks = (breaks: CrossMeasure[]) => {
        const clonedPart = new PartDescriptor(props.reportPart.part);
        clonedPart.breaks = breaks;
        clonedPart.overrideReportBreaks = true;
        props.updatePart({...props.reportPart, part: clonedPart});
    }

    const updateWaves = (waves: CrossMeasure) => {
        const clonedPart = new PartDescriptor(props.reportPart.part);
        if(clonedPart.waves){
            clonedPart.waves.waves = waves;
        } else {
            clonedPart.waves = new ReportWaveConfiguration({
                wavesToShow: ReportWavesOptions.SelectedWaves,
                numberOfRecentWaves: waves.filterInstances.length,
                waves: waves
        })}

        props.updatePart({...props.reportPart, part: clonedPart});
    }

    const updatePart = (colours: string[]) => {
        const clonedPart = new PartDescriptor(props.reportPart.part);
        clonedPart.colours = colours;
        props.updatePart({...props.reportPart, part: clonedPart});
    }

    const isNonExportablePartType = () => props.reportPart?.metric &&
        (props.questionTypeLookup[props.reportPart.metric.name] == MainQuestionType.Text);

    const helptext = props.reportPart.metric?.isAutoGeneratedNumeric() ?
        "Auto grouped: " + getReportPartDisplayText(props.reportPart) :
        getReportPartDisplayText(props.reportPart);

    const canPickFilterInstances = canSelectFilterInstances(props.reportPart, report);

    const shouldShowEntityInstanceSelector = breaks &&
        breaks.length > 1 &&
        allMultiBreakInstanceOptions.length > 0 &&
        getSelectedEntity() &&
        props.reportPart.part.waves?.waves === undefined &&
        !isPartUsingOverTime;

    return (
        <div className="configure-chart-page">
            <div className="reports-configure-chart-container">
                <div className="chart-area">
                    <div className="header">
                        <div className="top-row">
                            <div>
                                <a onClick={props.viewAllCharts} className="report-link" title={page.displayName}>{page.displayName}</a>
                                <span className="divider">/</span>
                                <span className="question-text">{helptext}</span>
                            </div>
                        </div>
                        <div className="bottom-row">
                            <div className="left-items">
                                {(props.isReportUsingOverTime || isPartUsingOverTime) &&
                                    <>
                                        <AllVueDateRangePicker
                                            applicationConfiguration={props.applicationConfiguration}
                                            overtimeConfig={report.overTimeConfig}
                                            dropdownTitle={getDateRangePickerTitleFromDates(props.startDate, props.endDate)}
                                            onRangeSelected={(range, start, end) => props.setDates(start, end)}
                                            onCustomRangeSelected={(customRange, start, end) => props.setDates(start, end)}
                                            startDate={props.startDate}
                                            endDate={props.endDate}
                                            onDatesSelected={(start, end) => props.setDates(start, end)}
                                        />
                                        {isPartUsingOverTime && props.userVisibleAverages.length > 0 &&
                                            <AverageSelector
                                                average={props.overTimeAverage}
                                                userVisibleAverages={props.userVisibleAverages}
                                                updateFilterAverage={props.setOverTimeAverage}
                                            />
                                        }
                                    </>
                                }
                                {filters.length === 0 && metricsValidAsFilter.length === 0
                                    ? getAddFiltersButton()
                                    : <FiltersBar
                                        user={props.user}
                                        buttonType={FilterButtonType.ShowReportFilterModal}
                                        openModalToFilterPage={props.openModalToFilterPage}
                                    />
                                }
                                {shouldShowEntityInstanceSelector &&
                                    <DropdownSelector<EntityInstance>
                                        label="Select entity to break on"
                                        items={allMultiBreakInstanceOptions}
                                        selectedItem={getSelectedEntity()!}
                                        onSelected={(instance) => updateSelectedMultiBreakOptions(instance)}
                                        itemDisplayText={e => e.name}
                                        asButton={true}
                                        showLabel={false}
                                        itemKey={e => e.name + e.id}
                                    />
                                }
                                {canPickFilterInstances &&
                                    <ConfigureReportPartFilterInstance
                                        reportPart={props.reportPart}
                                        canPickFilterInstances={canPickFilterInstances}
                                        savePartChanges={(part: PartDescriptor) => {props.updatePart({...props.reportPart, part: part});}}
                                    />
                                }
                            </div>
                            <div className="right-items">
                                <ReportCardLowSampleWarning
                                    id={props.reportPart.part.spec2.toString()}
                                    isLowSample={report.highlightLowSample && isLowSample}
                                    shrink={false}
                                    isLineChart={props.reportPart.part.partType === PartType.ReportsCardLine} />
                                {props.canEditReport && !configureChartMenuVisible &&
                                    <button id="configure-chart-button" className="hollow-button configure-chart-button" onClick={() => setConfigureChartMenuVisible(true)}>
                                        <i className="material-symbols-outlined">edit</i>
                                        <div>Configure</div>
                                    </button>
                                }
                                {!isNonExportablePartType() &&
                                    <ReportPowerpointExportButton
                                        metrics={metricsForReports}
                                        curatedFilters={props.curatedFilters}
                                        overTimeFilters={props.overTimeFilters}
                                        reportPart={props.reportPart}
                                        isDataInSyncWithDatabase={props.isDataInSyncWithDatabase}
                                    />
                                }
                            </div>
                        </div>
                        {shouldShowEntityInstanceSelector && <WarningBanner message={"In order to view multiple breaks the question has been filtered to a single choice. You can switch in the dropdown above."} materialIconName={"info"} isClosable isFreestanding/>}
                    </div>
                    <CatchReportAndDisplayErrors applicationConfiguration={props.applicationConfiguration}
                        childInfo={{
                            "Part": props.reportPart.part.partType,
                            "Spec1": props.reportPart.part.spec1,
                            "Spec2": props.reportPart.part.spec2,
                            "Spec3": props.reportPart.part.spec3,
                            "Report": report.savedReportId.toString()
                        }}
                    >
                        <ReportsPageCard
                            reportPart={props.reportPart}
                            googleTagManager={props.googleTagManager}
                            pageHandler={props.pageHandler}
                            curatedFilters={props.curatedFilters}
                            overTimeFilters={props.overTimeFilters}
                            questionTypeLookup={props.questionTypeLookup}
                            reportOrder={report.reportOrder}
                            cardType={ReportsPageCardType.FullChart}
                            canEditReport={props.canEditReport}
                            removeFromReport={props.removeFromReport}
                            breaks={breaks}
                            waves={props.getPartWaves(props.reportPart.part)}
                            setIsLowSample={setIsLowSample}
                            showWeightedCounts={props.showWeightedCounts}
                            updateBreaks={(b) => updateBreaks(b)}
                            updateWave={(w) => updateWaves(w)}
                            duplicatePart={props.duplicatePart}
                            updatePart={(colours: string[]) => updatePart(colours)}
                            isUsingOverTime={isPartUsingOverTime}
                        />
                    </CatchReportAndDisplayErrors>
                </div>
                {props.canEditReport && configureChartMenuVisible &&
                    <ReportsPageConfigureMenu
                        reportPart={props.reportPart}
                        visible={configureChartMenuVisible}
                        questionTypeLookup={props.questionTypeLookup}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        user={props.user}
                        updatePart={props.updatePart}
                        closeMenu={closeConfigureChartMenu}
                    />
                }
            </div>
        </div>
    );
}

export default ReportsSingleChartPage;