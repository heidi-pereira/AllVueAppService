import React, { useMemo, useCallback } from "react";
import { useState } from "react";
import {
    IAverageDescriptor,
    BaseDefinitionType,
    BaseExpressionDefinition,
    CrossMeasure,
    FeatureCode,
    IApplicationUser,
    ICustomPeriod,
    MainQuestionType,
    PageDescriptor,
    PartDescriptor,
    Report,
    ReportType,
    ReportWaveConfiguration,
    ReportWavesOptions,
} from "../../../BrandVueApi";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import { IGoogleTagManager } from "../../../googleTagManager";
import { Metric } from "../../../metrics/metric";
import ReportsPageSideNav from "./Components/ReportsPageSideNav";
import _ from "lodash";
import { clonePart, getPartFromMetric } from "./Utility/ReportPageBuilder";
import { useSavedReportsContext } from "./SavedReportsContext";
import { useEffect } from "react";
import { selectCurrentReport, selectCurrentReportOrNull, selectReportIsSelected } from 'client/state/reportSelectors';
import toast from "react-hot-toast";
import ReportsSingleChartPage from "./ReportsSingleChartPage";
import ReportsPageTableDisplay from "./Tables/ReportsPageTableDisplay";
import ReportsPageChartsDisplay from "./Charts/ReportsPageChartsDisplay";
import ReportsPageTopBar from "./Components/ReportsPageTopBar";
import { ApplicationConfiguration } from "../../../ApplicationConfiguration";
import { debounce } from 'lodash';
import NoPages from "./Components/NoPages";
import {
    getAvailableCrossMeasureFilterInstances, getDateRangeLookup, getDefaultOverTimeSettings, getDefaultWave,
    getSplitByAndFilterByEntityTypesForPart,
    getUserVisibleAverages,
    updateFiltersWithSelectedProperies
} from "../../../components/helpers/SurveyVueUtils";
import { useFilterStateContext } from "../../../filter/FilterStateContext";
import ReportSettingsModal, { ReportSettingsModalTabSelection } from "./Modals/ReportSettingsModal";
import { EntitySet } from "../../../entity/EntitySet";
import { MAX_TABLES_PER_PAGE, PaginationData, usePaginationDict } from "../PaginationData";
import { getEntityInstanceGroupFromIds } from "../../../entity/EntityInstanceGroup";
import EntitySetBuilder from "../../../entity/EntitySetBuilder";
import { EntityInstanceColourRepository } from "../../../entity/EntityInstanceColourRepository";
import { PageHandler } from "../../PageHandler";
import { useEntityConfigurationStateContext } from "../../../entity/EntityConfigurationStateContext";
import { useMetricStateContext } from "../../../metrics/MetricStateContext";
import { MixPanel } from '../../mixpanel/MixPanel';
import { getDefaultBaseStateForMetric, IBaseState, useCrosstabPageStateContext } from "../Crosstab/CrosstabPageStateContext";
import {useLocation, useNavigate} from "react-router-dom";
import { PartType } from "../../panes/PartType";
import { isFeatureEnabled } from "../../helpers/FeaturesHelper";
import { isUsingOverTime } from "client/components/visualisations/Reports/Charts/ReportsChartHelper";
import { selectSubsetId } from "client/state/subsetSlice";
import { useAppSelector } from "client/state/store";
import { selectAllAverages } from "client/state/averageSlice";
import { setReportsPageOverride } from "client/state/reportSlice";
import { useDispatch } from "react-redux";

interface IReportsPageDisplayProps {
    metricsForReports: Metric[];
    dataWaves: ICustomPeriod[];
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    curatedFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: MainQuestionType };
    canEditReports: boolean;
    user: IApplicationUser | null;
    reportsPageUrl: string;
    applicationConfiguration: ApplicationConfiguration;
    averages: IAverageDescriptor[];
    isDataInSyncWithDatabase: boolean;
    setIsDataInSyncWithDatabase(areInSync: boolean): void;
}

export type PartWithExtraData = {
    part: PartDescriptor;
    metric: Metric | undefined;
    ref: React.RefObject<HTMLDivElement>;
    selectedEntitySet: EntitySet | undefined;
}

enum ReportsPageDisplayType {
    AllCharts,
    SingleChart,
}

const ReportsPageDisplay = (props: IReportsPageDisplayProps) => {
    const { reportsDispatch } = useSavedReportsContext();
    const dispatch = useDispatch();
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { enabledMetricSet, questionTypeLookup } = useMetricStateContext();
    const { crosstabPageState } = useCrosstabPageStateContext();
    const reportAndPage = useAppSelector(selectCurrentReport);
        
    const report = reportAndPage.report;
    const page = reportAndPage.page;
    
    const getMetric = useCallback((metricName: string): Metric | undefined => {
        if (metricName) {
            return enabledMetricSet.getMetric(metricName);
        }
    }, [enabledMetricSet]);

    const buildEntitySetFromPart = useCallback((part: PartDescriptor, metric: Metric | undefined): EntitySet | undefined => {
        const entityTypes = getSplitByAndFilterByEntityTypesForPart(part, metric, entityConfiguration);
        if (entityTypes?.splitByEntityType) {
            if (part.selectedEntityInstances) {
                const availableInstances = entityConfiguration.getAllEnabledInstancesForType(entityTypes.splitByEntityType);
                const instances = getEntityInstanceGroupFromIds(part.selectedEntityInstances.selectedInstances, availableInstances);

                const entitySetBuilder = new EntitySetBuilder(EntityInstanceColourRepository.empty());
                return entitySetBuilder.asType(entityTypes.splitByEntityType)
                    .withName("Selected")
                    .withInstanceGroup(instances)
                    .build();
            }

            return entityConfiguration.getAllEnabledInstancesOrderedAsSet(entityTypes.splitByEntityType);
        }
    }, [entityConfiguration]);

    const getPartWithExtraData = useCallback((part: PartDescriptor): PartWithExtraData => {
        const metric = getMetric(part.spec1);
        const entitySet = buildEntitySetFromPart(part, metric);

        return {
            part: part,
            metric: metric,
            ref: React.createRef<HTMLDivElement>(),
            selectedEntitySet: entitySet,
        };
    }, [getMetric, buildEntitySetFromPart]);

    const reportParts = useMemo(() => 
        page.panes[0].parts.map(p => getPartWithExtraData(p)), 
        [page.panes[0].parts, getPartWithExtraData]
    );
    
    const location = useLocation();
    const navigate = useNavigate();
    /**
     * @deprecated Use useSearchParams hook directly instead. Example:
     * const [searchParams] = useSearchParams();
     * const value = searchParams.get('paramName') ?? defaultValue;
     */
    const getFocussedPart = () => {
        if(location?.state){
            return reportParts.find(p => p.part.id === location?.state["id"]) ?? reportParts[0];
        }
        return reportParts[0];
    }
    const [focusedPart, setFocusedPart] = useState<PartWithExtraData | undefined>(getFocussedPart());
    const [isReportSettingsModalVisible, setIsReportSettingsModalVisible] = React.useState(false);
    const [reportSettingsModalActiveTab, setReportSettingsModalActiveTab] = React.useState<ReportSettingsModalTabSelection>(ReportSettingsModalTabSelection.Details);
    const [displayType, setDisplayType] = useState<ReportsPageDisplayType>(ReportsPageDisplayType.AllCharts);
    const [canDownload, setCanDownload] = useState<boolean>(false);
    const [isLowSample, setIsLowSample] = useState<boolean>(false);

    const defaultDataWave = getDefaultWave(props.curatedFilters);
    const [selectedWave, setSelectedWave] = React.useState<ICustomPeriod>(defaultDataWave);

    const isDataWeighted = report?.isDataWeighted ?? false;
    const { filters } = useFilterStateContext();
    
    const [currentScrollY, setCurrentScrollY] = useState<number | undefined>(0);
    const [previousScrollY, setPreviousScrollY] = useState<number | undefined>(0);
    const [sideNavCurrentScrollY, setSideNavCurrentScrollY] = useState<number | undefined>(0);
    const [sideNavPreviousScrollY, setSideNavPreviousScrollY] = useState<number | undefined>(0);
    const baseState = crosstabPageState.metricBaseLookup[focusedPart?.metric?.name ?? ''] ?? getDefaultBaseStateForMetric(focusedPart?.metric);
    const baseVariableId = baseState.baseVariableId;

    const [startDate, setStartDate] = useState<Date | undefined>();
    const [endDate, setEndDate] = useState<Date | undefined>();
    const [overTimeAverage, setOverTimeAverage] = useState<IAverageDescriptor | undefined>();
    const subsetId = useAppSelector(selectSubsetId);
    const allAverages = useAppSelector(selectAllAverages);
        
    const dateRangeLookup = useMemo(() => getDateRangeLookup(props.applicationConfiguration),
    [
        props.applicationConfiguration.dateOfFirstDataPoint.getTime(),
        props.applicationConfiguration.dateOfLastDataPoint.getTime()
    ]);
    const isReportUsingOverTime = isFeatureEnabled(FeatureCode.Overtime_data) && report.overTimeConfig != undefined;
    const isReportPartUsingOvertime = isFeatureEnabled(FeatureCode.Overtime_data) && reportParts.some(p => isUsingOverTime(report, p));
    const isOverTimeInUse = isReportUsingOverTime || isReportPartUsingOvertime;
    const userVisibleAverages = getUserVisibleAverages(props.applicationConfiguration,
        allAverages,
        isDataWeighted,
        subsetId);

    const getBaseExpressionDefinition = (type: BaseDefinitionType | undefined, baseVariableId: number | undefined): BaseExpressionDefinition | undefined => {
        if ((type || baseVariableId) && focusedPart?.metric) {
            return new BaseExpressionDefinition({
                baseType: type ?? BaseDefinitionType.SawThisQuestion,
                baseVariableId: baseVariableId,
                baseMeasureName: focusedPart.metric.name
            });
        }
    }

    const paginationDict = usePaginationDict(Object.assign({}, ...reportParts.map(p => ({ [p.part.spec2]: { currentPageNo: 1, noOfTablesPerPage: MAX_TABLES_PER_PAGE, totalNoOfTables: 1 } }))),
        focusedPart?.part.spec2,
        focusedPart?.metric,
        focusedPart?.selectedEntitySet,
        [],
        entityConfiguration)

    useEffect(() => {
        if (location.state && location.state["id"]) {
            setFocusedPart(reportParts.find(p => p.part.id === location.state.id));
            setDisplayType(ReportsPageDisplayType.SingleChart);
            setPreviousScrollY(location.state["scroll"])
            setSideNavPreviousScrollY(location.state["sideNavScroll"])
        } else {
            setFocusedPart(undefined);
            setDisplayType(ReportsPageDisplayType.AllCharts);
            setCurrentScrollY(previousScrollY)
            setSideNavCurrentScrollY(sideNavPreviousScrollY)
        }
    }, [location])

    const onDragEnd = () => {
        setPreviousScrollY(currentScrollY)
        setSideNavPreviousScrollY(sideNavCurrentScrollY)
    }

    const updatedFilters = React.useMemo(() =>
        updateFiltersWithSelectedProperies(props.curatedFilters, props.averages, isDataWeighted, filters, true, selectedWave, startDate, endDate)
        , [
            props.curatedFilters,
            props.averages,
            isDataWeighted,
            selectedWave,
            filters,
            startDate,
            endDate
        ]);

    const overTimeFilters = React.useMemo(() =>
        updateFiltersWithSelectedProperies(updatedFilters, props.averages, isDataWeighted, filters, true, selectedWave, startDate, endDate, overTimeAverage)
        , [updatedFilters, overTimeAverage]);

    useEffect(() => {
        resetFocusedPart(page);
    }, [page]);

    useEffect(() => {
        if (focusedPart) {
            updateFocusedPartWithBaseExpression(focusedPart, baseState);
        }
    }, []);

    useEffect(() => {
        loadDefaultOverTimeSettings();
    }, [report.savedReportId, isOverTimeInUse, isDataWeighted]);

    function loadDefaultOverTimeSettings() {
        if (isOverTimeInUse) {
            const overTimeDefaults = getDefaultOverTimeSettings(report.overTimeConfig, userVisibleAverages, dateRangeLookup, props.applicationConfiguration);
            setStartDate(overTimeDefaults.startDate);
            setEndDate(overTimeDefaults.endDate);
            setOverTimeAverage(overTimeDefaults.average);
        } else {
            setStartDate(undefined);
            setEndDate(undefined);
            setOverTimeAverage(undefined);
        }
    }

    const setSelectedDates = (startDate: Date, endDate: Date) => {
        setStartDate(startDate);
        setEndDate(endDate);
    };

    function updateFocusedPartWithBaseExpression(focusedPart: PartWithExtraData, baseState: IBaseState) {
        if ((focusedPart.part.baseExpressionOverride?.baseType !== baseState.baseType || focusedPart.part.baseExpressionOverride?.baseVariableId !== baseVariableId)) {
            const modifiedPart = new PartDescriptor(focusedPart.part);
            modifiedPart.baseExpressionOverride = getBaseExpressionDefinition(baseState.baseType, baseVariableId);
            focusedPart.part = modifiedPart;
        }
    }

    function resetFocusedPart(page: PageDescriptor) {
        const partToFocus = focusedPart?.part.id === 0 ?
            page.panes[0].parts.find(p => p.spec2 == focusedPart?.part.spec2) :
            page.panes[0].parts.find(p => p.id == focusedPart?.part.id);
        if (partToFocus) {
            const partWithExtraData = getPartWithExtraData(partToFocus)
            setFocusedPart(partWithExtraData);
        }
    }

    const getPartBreaks = (reportPart: PartWithExtraData | undefined): CrossMeasure[] => {
        if (reportPart && reportPart.part.overrideReportBreaks) {
            return reportPart.part.breaks ?? [];
        }

        const isFunnelWithPartLevelWaves = reportPart && reportPart.part.partType === PartType.ReportsCardFunnel && reportPart.part.waves?.waves !== undefined;

        return isFunnelWithPartLevelWaves ? [] : report.breaks;
    }

    const getReportWaves = (): CrossMeasure | undefined => {
        return getWaves(report.waves);
    }

    const getPartWaves = (part: PartDescriptor): CrossMeasure | undefined => {
        if (part.waves) {
            return getWaves(part.waves);
        }

        const isFunnelWithPartLevelBreaks = part.partType === PartType.ReportsCardFunnel && part.breaks?.length > 0;

        return isFunnelWithPartLevelBreaks ? undefined : getReportWaves();
    }

    const getWaves = (waves: ReportWaveConfiguration | undefined): CrossMeasure | undefined => {
        if (waves?.waves) {
            const updatedWaves = new CrossMeasure({
                ...waves.waves
            });
            if (waves.wavesToShow === ReportWavesOptions.AllWaves) {
                //CrosstabFilterModelFactory includes all instances if there are no filter instances
                updatedWaves.filterInstances = [];
            } else if (waves.wavesToShow === ReportWavesOptions.MostRecentNWaves) {
                const metric = props.metricsForReports.find(m => m.name === updatedWaves.measureName);
                if (!metric || waves.numberOfRecentWaves <= 0) {
                    return undefined;
                }
                const isBasedOnSingleChoice = questionTypeLookup[metric.name] == MainQuestionType.SingleChoice;
                updatedWaves.filterInstances = getAvailableCrossMeasureFilterInstances(metric, entityConfiguration, updatedWaves.multipleChoiceByValue, isBasedOnSingleChoice).slice(-1 * waves.numberOfRecentWaves);
            } else if (waves.wavesToShow === ReportWavesOptions.SelectedWaves) {
                if (updatedWaves.filterInstances.length === 0) {
                    return undefined;
                }
            }
            return updatedWaves;
        }
    }

    const getStorePartChangesDebouncer = React.useCallback(
        //todo - we might need to think about how the deboucner handles the forcereload
        _.memoize((partId: number) => debounce((part: PartDescriptor, forceReload: boolean) => storePartChanges([part], forceReload), 2000)),
        [report]
    );

    const storePartChanges = (parts: PartDescriptor[], forceReload: boolean) => {
        //This is using a ref for the report as it is called from within memoized charts
        reportsDispatch({ type: "UPDATE_PARTS", data: { report: report, parts: parts, forceReload: forceReload } })
            .catch(error => toast.error("Saving report changes failed, please try again"));
    }

    const deletePartAndStoreChanges = (partIdToDelete: number, partsToUpdate: PartDescriptor[]) => {
        reportsDispatch({ type: "DELETE_PART", data: { report: report, partIdToDelete: partIdToDelete, partsToUpdate: partsToUpdate } })
            .catch(error => toast.error("Saving report changes failed, please try again"));
    }

    const storeNewParts = (parts: PartDescriptor[]) => {
        reportsDispatch({ type: "ADD_PARTS", data: { report: report, parts: parts } })
            .catch(error => toast.error("Saving report changes failed, please try again"));
    }

    const updatePart = (reportPart: PartWithExtraData, forceReload: boolean) => {
        if (!props.canEditReports) {
            throw new Error("Cannot update report");
        }

        const newPage = new PageDescriptor(page);
        const parts = [...newPage.panes[0].parts];
        const partIndex = parts.findIndex(p => p.id === reportPart.part.id);
        if (partIndex < 0) {
            throw new Error("Couldn't update report");
        }
        parts.splice(partIndex, 1, reportPart.part);
        newPage.panes[0].parts = parts;

        dispatch(setReportsPageOverride(newPage));

        props.setIsDataInSyncWithDatabase(false);
        resetFocusedPart(newPage);
        const debouncedSave = getStorePartChangesDebouncer(reportPart.part.id);
        debouncedSave(reportPart.part, forceReload);
        MixPanel.track("reportsPageModifyPart");
    }

    const saveReordering = (sourceIndex: number, destinationIndex: number) => {
        if (!props.canEditReports) {
            throw new Error("Cannot update report");
        }

        const reorderedListItems: PartDescriptor[] = reorder(
            sourceIndex,
            destinationIndex
        );

        let clonedPage: PageDescriptor = new PageDescriptor(page);
        clonedPage.panes[0].parts = reorderedListItems;
        dispatch(setReportsPageOverride(clonedPage));
        resetFocusedPart(clonedPage);
        storePartChanges(clonedPage.panes[0].parts.slice(Math.min(sourceIndex, destinationIndex)), false);
        MixPanel.track("reportsPageReorderParts");
    }

    const reorder = (sourceIndex: number, destinationIndex: number) => {
        const parts = Array.from(page.panes[0].parts);
        const [reorderedItem] = parts.splice(sourceIndex, 1);
        parts.splice(destinationIndex, 0, reorderedItem);
        const partsPagination: { [partId: string]: PaginationData } = {}
        parts.forEach((p, index) => {
            const newId = index.toString();
            partsPagination[newId] = { ...paginationDict.paginationDict[p.spec2] };
            p.spec2 = newId;
        });
        paginationDict.setPaginationDict(partsPagination)
        return parts;
    };

    const removePart = (reportPart: PartWithExtraData) => {
        if (!props.canEditReports) {
            throw new Error("Cannot update report");
        }

        const isCurrentlyFocusedPart = focusedPart?.part.id === reportPart.part.id;
        const newPage = new PageDescriptor(page);
        const matchingPartIndex = newPage.panes[0].parts.findIndex(p => p.id == reportPart.part.id);
        if (matchingPartIndex < 0) {
            throw new Error("Couldn't update report");
        }
        newPage.panes[0].parts.splice(matchingPartIndex, 1);
        newPage.panes[0].parts.forEach((p, index) => p.spec2 = index.toString());
        resetFocusedPart(newPage);
        deletePartAndStoreChanges(reportPart.part.id, newPage.panes[0].parts.slice(matchingPartIndex));
        MixPanel.track("reportsPageRemovePart", { Part: reportPart.part.partType, Question: reportPart.part.spec1 });

        if (isCurrentlyFocusedPart) {
            const partToFocus = matchingPartIndex > 0 ? newPage.panes[0].parts[matchingPartIndex - 1] : newPage.panes[0].parts[0];
            if (report.reportType === ReportType.Table && partToFocus) {
                const partWithExtraData = getPartWithExtraData(partToFocus);
                setFocusedPart(partWithExtraData);
            } else {
                setFocusedPart(undefined);
            }
        }
    }

    const viewChartPage = (chartToView: PartWithExtraData) => {
        MixPanel.track("reportsPageViewChart");
        navigate(location.pathname, {state:{ id: chartToView.part.id, scroll: currentScrollY, sideNavScroll: sideNavCurrentScrollY }} );
    }

    const viewAllCharts = () => {
        MixPanel.track("reportsPageBackFromChart");
        navigate(location.pathname,  {state:{"id": undefined, "scroll": undefined, "sideNavScroll": undefined}});
    }

    const viewTablePart = (tableToView: PartWithExtraData) => {
        navigate(location.pathname, {state:{ "id": tableToView.part.id, "scroll": currentScrollY, "sideNavScroll": sideNavCurrentScrollY }} );
    }

    const handleReportSideNavPartClick = (reportPart: PartWithExtraData) => {
        if (report.reportType === ReportType.Table) {
            viewTablePart(reportPart);
        } else {
            setFocusedPart(reportPart);
        }
    }

    const addPartsToReport = (metrics: Metric[]) => {
        const indexOffset = reportParts.length;
        const reportHasWaves = !!report.waves;
        const partsToAdd = metrics.map((m, i) =>
            getPartFromMetric(m, page.panes[0].id, indexOffset + i, entityConfiguration, props.questionTypeLookup, report.reportType, reportHasWaves));
        const newPage = new PageDescriptor(page);
        newPage.panes[0].parts = newPage.panes[0].parts.concat(partsToAdd);
        resetFocusedPart(newPage);
        storeNewParts(partsToAdd);
        MixPanel.track("reportsPageAddParts");
        partsToAdd.map(reportPart => {

            MixPanel.track("reportsPageAddParts", { Part: reportPart.partType, Question: reportPart.spec1});

        });
        if (report.reportType === ReportType.Table) {
            const partWithExtraData = getPartWithExtraData(partsToAdd[0]);
            setFocusedPart(partWithExtraData);
        }
    }

    const duplicatePart = (partDescriptor: PartDescriptor) => {
        const newPage = new PageDescriptor(page);
        const index = page.panes[0].parts.findIndex(x => x == partDescriptor);

        const partToAdd = clonePart(partDescriptor);
        const parts = newPage.panes[0].parts;
        const modifiedParts = index == 0 ? parts : parts.slice(index);

        if (index == 0) {
            newPage.panes[0].parts = [partToAdd, ...parts]
        }
        else {
            newPage.panes[0].parts = [...parts.slice(0, index), partToAdd, ...parts.slice(index)]
        }
        const partsPagination: { [partId: string]: PaginationData } = {}
        newPage.panes[0].parts.forEach((p, index) => {
            const newId = index.toString();
            partsPagination[newId] = { ...paginationDict.paginationDict[p.spec2] };
            p.spec2 = newId;
        });
        paginationDict.setPaginationDict(partsPagination)

        resetFocusedPart(newPage);
        storeNewParts([partToAdd]);
        storePartChanges(modifiedParts, false)
        MixPanel.track("reportDuplicatePage", { Part: partToAdd.partType, Question: partToAdd.spec1 });
        if (report.reportType === ReportType.Table) {
            const partWithExtraData = getPartWithExtraData(partToAdd);
            setFocusedPart(partWithExtraData);
        }
    }

    const getPrimaryButtonText = (metrics: Metric[]) => {
        const selectedMetricCount = metrics.length;
        const typeToAdd = report.reportType === ReportType.Chart ? "chart" : "table";
        return `Add ${selectedMetricCount > 0 ? selectedMetricCount : ""} ${selectedMetricCount === 1 ? typeToAdd : typeToAdd + "s"}`;
    }

    const openModalToFilterPage = () => {
        setReportSettingsModalActiveTab(ReportSettingsModalTabSelection.Filters);
        openReportSettingsModal();
    }

    const openReportSettingsModal = () => {
        setIsReportSettingsModalVisible(true);
        MixPanel.track("reportsPageViewReportSettings");
    }

    const modalHeaderText = `Add ${report.reportType === ReportType.Chart ? "charts" : "tables"} to report`;

    const getContentToDisplay = () => {
        if (!report.userHasAccess) {
            return (
                <div id="reports-page">
                    <ReportsPageTopBar
                        metricsForReports={props.metricsForReports}
                        questionTypeLookup={props.questionTypeLookup}
                        curatedFilters={updatedFilters}
                        overTimeFilters={overTimeFilters}
                        canEditReports={props.canEditReports}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        user={props.user}
                        reportsPageUrl={props.reportsPageUrl}
                        canExportData={canDownload}
                        showReportSettingsModal={openReportSettingsModal}
                        openModalToFilterPage={openModalToFilterPage}
                        highlightLowSample={false}
                        isLowSample={false}
                        isDataInSyncWithDatabase={props.isDataInSyncWithDatabase}
                        applicationConfiguration={props.applicationConfiguration}
                        averages={props.averages}
                        userVisibleAverages={userVisibleAverages}
                        isReportUsingOverTime={isReportUsingOverTime}
                        isReportPartUsingOvertime={isReportPartUsingOvertime}
                        startDate={startDate}
                        endDate={endDate}
                        overTimeAverage={overTimeAverage}
                        setDates={setSelectedDates}
                        setOverTimeAverage={setOverTimeAverage}
                        selectedPart={focusedPart}
                        updatePart={(p: PartWithExtraData) => updatePart(p, false)}
                    />
                <div className="no-pages">
                        <div className="text">
                            You cannot access this report because you don't have permission to view one or more of its questions.
                        </div>
                    </div>
                </div>
            )
        }


        if (displayType === ReportsPageDisplayType.SingleChart && report.reportType === ReportType.Chart && focusedPart) {
            return (
                <ReportsSingleChartPage
                    reportPart={focusedPart}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.pageHandler}
                    user={props.user}
                    curatedFilters={updatedFilters}
                    overTimeFilters={overTimeFilters}
                    questionTypeLookup={props.questionTypeLookup}
                    canEditReport={props.canEditReports}
                    updatePart={(p => updatePart(p, false))}
                    viewAllCharts={viewAllCharts}
                    removeFromReport={() => removePart(focusedPart)}
                    getPartBreaks={getPartBreaks}
                    getPartWaves={getPartWaves}
                    applicationConfiguration={props.applicationConfiguration}
                    openModalToFilterPage={openModalToFilterPage}
                    showWeightedCounts={isDataWeighted}
                    isDataInSyncWithDatabase={props.isDataInSyncWithDatabase}
                    duplicatePart={duplicatePart}
                    userVisibleAverages={userVisibleAverages}
                    isReportUsingOverTime={isReportUsingOverTime}
                    startDate={startDate}
                    endDate={endDate}
                    overTimeAverage={overTimeAverage}
                    setDates={setSelectedDates}
                    setOverTimeAverage={setOverTimeAverage}
                />
            );
        }

        if (page.panes[0].parts.length == 0) {
            return (
                <div id="reports-page">
                    <ReportsPageTopBar
                        metricsForReports={props.metricsForReports}
                        questionTypeLookup={props.questionTypeLookup}
                        curatedFilters={updatedFilters}
                        overTimeFilters={overTimeFilters}
                        canEditReports={props.canEditReports}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        user={props.user}
                        reportsPageUrl={props.reportsPageUrl}
                        canExportData={canDownload}
                        showReportSettingsModal={openReportSettingsModal}
                        openModalToFilterPage={openModalToFilterPage}
                        highlightLowSample={false}
                        isLowSample={false}
                        isDataInSyncWithDatabase={props.isDataInSyncWithDatabase}
                        applicationConfiguration={props.applicationConfiguration}
                        averages={props.averages}
                        userVisibleAverages={userVisibleAverages}
                        isReportUsingOverTime={isReportUsingOverTime}
                        isReportPartUsingOvertime={isReportPartUsingOvertime}
                        startDate={startDate}
                        endDate={endDate}
                        overTimeAverage={overTimeAverage}
                        setDates={setSelectedDates}
                        setOverTimeAverage={setOverTimeAverage}
                        selectedPart={focusedPart}
                        updatePart={(p: PartWithExtraData) => updatePart(p, false)}
                    />
                    <NoPages metricsForReports={props.metricsForReports}
                        questionTypeLookup={props.questionTypeLookup}
                        getPrimaryButtonText={getPrimaryButtonText}
                        modalHeaderText={modalHeaderText}
                        chartsPaneId={page.panes[0].id}
                        addPartsToReport={(metrics: Metric[]) => addPartsToReport(metrics)}
                        reportType={report.reportType}
                    />
                </div>
            )
        }

        return (
            <div id="reports-page">
                <ReportsPageTopBar
                    metricsForReports={props.metricsForReports}
                    questionTypeLookup={props.questionTypeLookup}
                    curatedFilters={updatedFilters}
                    overTimeFilters={overTimeFilters}
                    canEditReports={props.canEditReports}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.pageHandler}
                    user={props.user}
                    reportsPageUrl={props.reportsPageUrl}
                    canExportData={canDownload}
                    showReportSettingsModal={openReportSettingsModal}
                    openModalToFilterPage={openModalToFilterPage}
                    highlightLowSample={report.highlightLowSample}
                    isLowSample={isLowSample}
                    isDataInSyncWithDatabase={props.isDataInSyncWithDatabase}
                    applicationConfiguration={props.applicationConfiguration}
                    averages={props.averages}
                    userVisibleAverages={userVisibleAverages}
                    isReportUsingOverTime={isReportUsingOverTime}
                    isReportPartUsingOvertime={isReportPartUsingOvertime}
                    startDate={startDate}
                    endDate={endDate}
                    overTimeAverage={overTimeAverage}
                    setDates={setSelectedDates}
                    setOverTimeAverage={setOverTimeAverage}
                    selectedPart={focusedPart}
                    updatePart={(p: PartWithExtraData) => updatePart(p, false)}
                />
                <ReportsPageSideNav metricsForReports={props.metricsForReports}
                    reportParts={reportParts}
                    canEditReport={props.canEditReports}
                    defaultDataWave={defaultDataWave}
                    dataWaves={props.dataWaves}
                    setSelectedWave={setSelectedWave}
                    addPartsToReport={(metrics: Metric[]) => addPartsToReport(metrics)}
                    saveReordering={(source: number, destination: number) => saveReordering(source, destination)}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.pageHandler}
                    removeFromReport={removePart}
                    setFocusedPart={handleReportSideNavPartClick}
                    reportType={report.reportType}
                    getPrimaryButtonText={getPrimaryButtonText}
                    modalHeaderText={modalHeaderText}
                    focusedPart={focusedPart}
                    scrollY={sideNavCurrentScrollY}
                    setScrollY={setSideNavCurrentScrollY}
                    onDragEnd={onDragEnd}
                    duplicatePart={duplicatePart}
                />
                {report.reportType === ReportType.Table &&
                    <ReportsPageTableDisplay
                        canEditReport={props.canEditReports}
                        focusedPart={focusedPart}
                        breaks={getPartBreaks(focusedPart)}
                        curatedFilters={updatedFilters}
                        questionTypeLookup={props.questionTypeLookup}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        user={props.user}
                        setCanDownload={setCanDownload}
                        updatePart={(p) => updatePart(p, false)}
                        applicationConfiguration={props.applicationConfiguration}
                        setIsLowSample={(isLowSample) => setIsLowSample(isLowSample)}
                        isDataWeighted={isDataWeighted}
                        paginationData={paginationDict.getCurrentPaginationData()}
                        setPagination={paginationDict.setCurrentPaginationData}
                        maxNoOfTablesPerPage={MAX_TABLES_PER_PAGE}
                    />
                }
                {report.reportType === ReportType.Chart &&
                    <ReportsPageChartsDisplay
                        applicationConfiguration={props.applicationConfiguration}
                        canEditReport={props.canEditReports}
                        reportParts={reportParts}
                        curatedFilters={updatedFilters}
                        overTimeFilters={overTimeFilters}
                        questionTypeLookup={props.questionTypeLookup}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        getPartBreaks={getPartBreaks}
                        getPartWaves={getPartWaves}
                        removePartFromReport={removePart}
                        viewChartPage={viewChartPage}
                        showWeightedCounts={isDataWeighted}
                        scrollY={currentScrollY}
                        setScrollY={setCurrentScrollY}
                        duplicatePart={duplicatePart}
                    />
                }
            </div>
        );
    }

    return (
        <>
            {getContentToDisplay()}
            {props.canEditReports &&
                <ReportSettingsModal
                    isOpen={isReportSettingsModalVisible}
                    setIsOpen={(isOpen) => setIsReportSettingsModalVisible(isOpen)}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.pageHandler}
                    user={props.user}
                    questionTypeLookup={props.questionTypeLookup}
                    reportsPageUrl={props.reportsPageUrl}
                    activeTab={reportSettingsModalActiveTab}
                    setActiveTab={setReportSettingsModalActiveTab}
                    applicationConfiguration={props.applicationConfiguration}
                    averages={props.averages}
                    currentReportPage={reportAndPage}
                    reportPartsHaveBreaks={reportParts.some(p => p.part.overrideReportBreaks && (p.part.breaks ?? []).length > 0)}
                />
            }
        </>
    )
}

export default ReportsPageDisplay;
