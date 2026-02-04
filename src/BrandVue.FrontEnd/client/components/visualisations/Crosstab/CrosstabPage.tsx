import React from "react";
import { useContext, useEffect, useMemo, useRef, useState } from "react";
import styles from "./CrosstabPage.module.less";
import { Metric } from "../../../metrics/metric";
import * as BrandVueApi from "../../../BrandVueApi";
import {
    IAverageDescriptor,
    BaseExpressionDefinition,
    CalculationType,
    CrossMeasure,
    IEntityType,
    IApplicationUser,
    PermissionFeaturesOptions,
} from "../../../BrandVueApi";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import { IGoogleTagManager } from "../../../googleTagManager";
import SearchableQuestionList from "./SearchableQuestionList";
import { useMetricStateContext } from "../../../metrics/MetricStateContext";
import { QueryStringParamNames, useReadVueQueryParams, useWriteVueQueryParams } from "../../helpers/UrlHelper";
import CrosstabsContainer from "./CrosstabsContainer";
import { SurveyVueQueryParams } from "../../helpers/SurveyVueQueryParams";
import { useSavedBreaksContext } from "./SavedBreaksContext";
import { updateFiltersWithSelectedProperies, getSplitByAndFilterByEntityTypesForMetric, handleError } from "../../helpers/SurveyVueUtils";
import { EntitySet } from "../../../entity/EntitySet";
import CrosstabOptionsPane from "./CrosstabOptionsPane";
import { Nav, NavItem, NavLink } from 'reactstrap';
import { ApplicationConfiguration } from "../../../ApplicationConfiguration";
import { useFilterStateContext } from "../../../filter/FilterStateContext";
import CrosstabTitle from "./CrosstabTitle";
import { EntityInstance } from "../../../entity/EntityInstance";
import { getTypesReferencedByBreaks, doBreaksMatch } from "./CrossMeasureUtils";
import ConvertCalculationTypeModal from "./ConvertCalculationTypeModal";
import TablePaginationControls from "../TablePaginationControls";
import { MAX_TABLES_PER_PAGE, usePaginationDict } from "../PaginationData";
import NoMetadataNotification from "../NoMetadataNotification";
import { getCrosstabRequestModel } from "./CrosstabHelper";
import { PageHandler } from "../../PageHandler";
import { getDefaultBaseStateForMetric, useCrosstabPageStateContext } from "./CrosstabPageStateContext";
import VariableContentModal from "../Variables/VariableModal/VariableContentModal";
import { VariableProvider } from "../Variables/VariableModal/Utils/VariableContext";
import TextCard from "../shared/TextCard";
import Separator from '../../helpers/Separator';
import { PageCardState } from '../shared/SharedEnums';
import { BaseVariableContext } from "../Variables/BaseVariableContext";
import { isCrosstabAdministrator, hasAllVuePermissionsOrSystemAdmin, isFeatureEnabled } from "../../helpers/FeaturesHelper";
import { ProductConfigurationContext } from "../../../ProductConfigurationContext";
import { FilterInstance } from "../../../entity/FilterInstance";
import { useEntityConfigurationStateContext } from "../../../entity/EntityConfigurationStateContext";
import { ViewHelper } from "../ViewHelper";
import { CatchReportAndDisplayErrors } from "../../CatchReportAndDisplayErrors";
import BreaksPicker from "../BreakPicker/BreaksPicker";
import { useAppDispatch, useAppSelector } from "../../../state/store";
import { selectEntitySelectionState, selectActiveEntityTypeOrNull } from "../../../state/entitySelectionSelectors";
import { setActiveBreaks, setSplitBy } from "../../../state/entitySelectionSlice";
import { selectPrimaryMetricWithDefaultOrNull } from "../../../state/applicationSlice";
import { useLocation, useNavigate } from "react-router-dom";
import HeatmapImageCard from "../shared/HeatmapImageCard";
import { defaultHeatMapOptions } from "../../helpers/HeatMapHelper";
import { BreakPickerParent } from "../BreakPicker/BreaksDropdownHelper";
import { selectSubsetId } from "../../../state/subsetSlice";
import { getActiveBreaksFromSelection, setBreaksAndPeriod } from "../../helpers/AudienceHelper";
import { useSelectedBreaks } from "../../../state/entitySelectionHooks";
import {FeatureGuard} from "../../../components/FeatureGuard/FeatureGuard";
import { selectTimeSelection } from "../../../state/timeSelectionStateSelectors";
import CreateNewReportModal from "../Reports/Modals/CreateNewReportModal";
import AddToReportModal from "../Reports/Modals/AddToReportModal";

interface IProps {
    pageHandler: PageHandler;
    curatedFilters: CuratedFilters;
    isSurveyVue: boolean;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    user: IApplicationUser | null;
    averages: IAverageDescriptor[];
    entitySet: EntitySet | undefined;
    focusInstance: EntityInstance | undefined;
}

enum ConfigurationPage {
    Question = "question",
    Breaks = "breaks",
    Options = "options"
}

const CrosstabPage: React.FunctionComponent<IProps> = (props: IProps) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const isCurrentUserCrosstabAdmin = isCrosstabAdministrator(productConfiguration);
    const isCurrentUserCrosstabAdminCreate = hasAllVuePermissionsOrSystemAdmin(productConfiguration, [PermissionFeaturesOptions.VariablesCreate]);
    const { baseVariables } = useContext(BaseVariableContext);
    const [isSurveyVueDataWeightable, setIsSurveyVueDataWeightable] = React.useState<boolean>(false);
    const { selectableMetricsForUser: metrics, crosstabPageMetrics, enabledMetricSet, metricsDispatch, questionTypeLookup } = useMetricStateContext();
    const { savedBreaks } = useSavedBreaksContext();
    const { filters } = useFilterStateContext();
    const { crosstabPageState, crosstabPageDispatch } = useCrosstabPageStateContext();
    const { categories } = crosstabPageState;
    const { getQueryParameter } = useReadVueQueryParams();
    const [weightingStatus, setWeightingStatus] = useState<BrandVueApi.WeightingStatus>(BrandVueApi.WeightingStatus.NoWeightingConfigured);
    const [canDownload, setCanDownload] = useState(false);
    const [convertCalculationTypeModalVisible, setConvertCalculationTypeModalVisible] = React.useState<boolean>(false);
    const [isLowSample, setIsLowSample] = useState(false);
    const [canIncludeCounts, setCanIncludeCounts] = React.useState(true);
    const [isVariableModalOpen, setIsVariableModalOpen] = React.useState<boolean>(false);
    const [dataState, setDataState] = React.useState(PageCardState.Show);
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const metric = useAppSelector(state => selectPrimaryMetricWithDefaultOrNull(state, crosstabPageMetrics)) || undefined;
    const baseExpressionOverride = getBaseExpressionOverride();
    const [heatMapOptions, setHeatMapOptions] = React.useState<BrandVueApi.HeatMapOptions>(crosstabPageState.heatMapOptions ?? defaultHeatMapOptions());
    const entitySelectionState = useAppSelector(selectEntitySelectionState);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const [isCreateNewReportModalVisible, SetCreateNewReportModalVisible] = React.useState<boolean>(false);
    const [preSelectedMetric, setPreSelectedMetric] = React.useState<Metric | undefined>(undefined);
    const [showAddToReportModal, setShowAddToReportModal] = React.useState<boolean>(false);

    const splitByType = useAppSelector(state => selectActiveEntityTypeOrNull(state)) ?? getSplitByAndFilterByEntityTypesForMetric(metric, entityConfiguration).splitByEntityType;
    const dispatch = useAppDispatch();
    const navigate = useNavigate();
    const location = useLocation();
    const { setQueryParameter } = useWriteVueQueryParams(navigate, location);
    const activeBreaks = useSelectedBreaks();
    const activeEntitySet = props.entitySet;

    const getDefaultFilterInstance = (metric: Metric | undefined, entitySet: EntitySet | undefined): FilterInstance[] | undefined => {
        if (metric && entitySet && metric.calcType == CalculationType.Text && metric.entityCombination.length == 1) {
            return [new FilterInstance(metric.entityCombination[0], entitySet.getMainInstance())];
        }
        return undefined;
    }

    const filterInstances = useMemo(
        () => getDefaultFilterInstance(metric, activeEntitySet),
        [metric, activeEntitySet]
    );

    const calculateSecondaryEntitySets = (categories: CrossMeasure[]) => {
        const getEntitySetForType = (entityType: IEntityType): EntitySet => {
            if (entityType.identifier === props.entitySet?.type.identifier) {
                return props.entitySet;
            }
            return entitySelectionState[entityType.identifier] ?? entityConfiguration.getDefaultEntitySetFor(entityType);
        }

        if (!props.isSurveyVue && activeEntitySet) {
            const splitByType = activeEntitySet.type;
            const filterByTypes = metric?.entityCombination.filter(et => et.identifier !== splitByType.identifier) ?? [];
            const allTypes = filterByTypes.concat(getTypesReferencedByBreaks(categories, enabledMetricSet));
            const distinctTypes = allTypes.filter((type, index) => type.identifier !== splitByType.identifier &&
                allTypes.findIndex(t => t.identifier === type.identifier) === index);
            const newSecondarySets = distinctTypes.map(t => getEntitySetForType(t));
            return newSecondarySets;
        }
        return [];
    }

    const secondaryEntitySets = useMemo(() => {
        return calculateSecondaryEntitySets(categories);
    }, [categories, metric, activeEntitySet, entityConfiguration]);

    const paginationDict = usePaginationDict(Object.assign({}, ...crosstabPageMetrics.map(m => ({ [m.name]: { currentPageNo: 1, noOfTablesPerPage: MAX_TABLES_PER_PAGE, totalNoOfTables: 1 } }))),
        metric?.name,
        metric,
        activeEntitySet,
        secondaryEntitySets,
        entityConfiguration);

    const onMetricChange = (changedMetric: Metric | null) => {
        if (changedMetric) {
            props.googleTagManager.addEvent("changeCrosstabMetric", props.pageHandler, { value: changedMetric?.name });
            setQueryParameter(QueryStringParamNames.urlSafeMetricName, changedMetric.urlSafeName);
        }
    };

    const updatedFilters = useMemo(() =>
        updateFiltersWithSelectedProperies(props.curatedFilters,
            props.averages,
            isSurveyVueDataWeightable && crosstabPageState.weightingEnabled,
            filters,
            props.isSurveyVue
        ), [isSurveyVueDataWeightable, filters, crosstabPageState.weightingEnabled]);

    useEffect(() => setDataState(PageCardState.Show), [filterInstances, updatedFilters, JSON.stringify(baseExpressionOverride)]);

    useEffect(() => {
        const allowUserToSelectWeightedUnWeighted = props.isSurveyVue;
        if (allowUserToSelectWeightedUnWeighted) {
            const weightingPlansConfigClient = BrandVueApi.Factory.WeightingPlansClient(error => error());
            setWeightingStatus(BrandVueApi.WeightingStatus.NoWeightingConfigured);
            weightingPlansConfigClient.isWeightingPlanDefinedAndValid(subsetId).then(
                details => {
                    setIsSurveyVueDataWeightable(details.isValid);
                    setWeightingStatus(details.status);
                }
            ).catch((e: Error) => handleError(() => { throw e }));
        }
    }, [subsetId]);

    useEffect(() => {
        if (savedBreaks.length === 0) return;
        
        const selectedAudience = savedBreaks.find(b => b.id === activeBreaks?.audienceId);
        if (selectedAudience) {
            const breaksFromAudience = selectedAudience?.breaks ?? [];
            if (!doBreaksMatch(categories, breaksFromAudience)) {
                crosstabPageDispatch({ type: "SET_CATEGORIES", data: { categories: breaksFromAudience } });
            }
        }
    }, [activeBreaks?.audienceId, savedBreaks, categories]);

    useEffect(() => {
        setHeatMapOptions(crosstabPageState.heatMapOptions);
    }, [crosstabPageState]);

    const configPageStringToEnum = (page: string | undefined): ConfigurationPage => {
        if (Object.values(ConfigurationPage).some(p => p == page)) {
            return page as ConfigurationPage;
        }
        return ConfigurationPage.Question;
    }

    const initialConfigurationPage = getQueryParameter<string>(SurveyVueQueryParams.CrosstabPage);
    const [configurationPage, setConfigurationPage] = useState<ConfigurationPage>(configPageStringToEnum(initialConfigurationPage));

    const tableRef = useRef<HTMLTableElement>(null);

    const setMetricEnabled = (m: Metric, isEnabled: boolean): Promise<void> => {
        return metricsDispatch({ type: "SET_METRIC_ENABLED", data: { metric: m, isEnabled: isEnabled } });
    }

    const setFilterForMetricEnabled = (m: Metric, isEnabled: boolean): Promise<void> => {
        return metricsDispatch({ type: "SET_METRIC_FILTER_ENABLED", data: { metric: m, isEnabled: isEnabled } });
    }

    const setEligibleForCrosstabOrAllVue = (m: Metric, isEligible: boolean): Promise<void> => {
        return metricsDispatch({ type: "SET_ELIGIBLE_FOR_CROSSTAB_OR_ALLVUE", data: { metric: m, isEligible: isEligible } });
    }

    const updateMetricDefaultSplitBy = (m: Metric, defaultSplitBy: IEntityType): Promise<void> => {
        return metricsDispatch({
            type: "UPDATE_METRIC_DEFAULT_SPLIT_BY",
            data: { metric: m, splitByEntityTypeName: defaultSplitBy.identifier }
        }
        ).then(() => {
            dispatch(setSplitBy(defaultSplitBy));
        });
    }

    const getCreateVariableButton = () => {
            return (
                <FeatureGuard permissions={[BrandVueApi.PermissionFeaturesOptions.VariablesCreate]} 
                 customCheck={(_, __) => (isCurrentUserCrosstabAdminCreate)
                }>
                    <button id="new-variable-button" className={`hollow-button new-variable-button`} onClick={() => setIsVariableModalOpen(true)}>
                        <i className="material-symbols-outlined">add</i>
                        <div className="new-variable-button-text">Create new variable</div>
                    </button>
                </FeatureGuard>
            );
    }

    const updateUrlForBreaks = (categories: CrossMeasure[]) => {
        const matchingSavedBreak = savedBreaks.find((b) => doBreaksMatch(b.breaks, categories));
        const breaks = getActiveBreaksFromSelection(matchingSavedBreak, undefined, undefined, questionTypeLookup);
        setBreaksAndPeriod(breaks, setQueryParameter, props.curatedFilters, props.pageHandler, dispatch);
    }

    const onCategoriesChange = (categories: CrossMeasure[]) => {
        crosstabPageDispatch({ type: "SET_CATEGORIES", data: { categories } });
        updateUrlForBreaks(categories);
    }

    const getCrosstabControlsBody = (user: IApplicationUser | null, splitByEntityType: IEntityType | undefined) => {
        switch (configurationPage) {
            case (ConfigurationPage.Question): {
                return (
                    <SearchableQuestionList
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        metrics={crosstabPageMetrics}
                        selectedMetric={metric}
                        selectedMetricSplitByType={splitByEntityType}
                        setEligibleForCrosstabOrAllVue={setEligibleForCrosstabOrAllVue}
                        setMetricEnabled={setMetricEnabled}
                        setMetricDefaultSplitBy={updateMetricDefaultSplitBy}
                        selectMetric={onMetricChange}
                        buttonGroup={getCreateVariableButton()}
                        groupCustomVariables={props.isSurveyVue || isCurrentUserCrosstabAdmin}
                        showHamburger={props.isSurveyVue || isCurrentUserCrosstabAdmin}
                        setConvertCalculationTypeModalVisible={() => setConvertCalculationTypeModalVisible(true)}
                        setFilterForMetricEnabled={setFilterForMetricEnabled}
                        subsetId={subsetId}
                    />
                );
            }

            case (ConfigurationPage.Breaks): {
                return (
                    <div className={"crosstabsBreakPickerContainer"}>
                        <BreaksPicker
                            reportType={undefined}
                            selectedBreaks={categories}
                            setSelectedBreaks={onCategoriesChange}
                            user={user}
                            canSaveAndLoad={props.isSurveyVue}
                            groupCustomVariables={props.isSurveyVue || isCurrentUserCrosstabAdmin}
                            isPrimaryAction
                            googleTagManager={props.googleTagManager}
                            pageHandler={props.pageHandler}
                            supportMultiBreaks={true}
                            displayBreakInstanceSelector={props.isSurveyVue}
                            parentComponent={BreakPickerParent.Crosstab}
                        />
                    </div>
                )
            }

            case (ConfigurationPage.Options): {
                return (
                    <CrosstabOptionsPane googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        productConfiguration={productConfiguration}
                        metric={metric}
                        weightingStatus={weightingStatus}
                        isSurveyVueDataWeightable={isSurveyVueDataWeightable}
                        canIncludeCounts={canIncludeCounts}
                        activeEntitySet={activeEntitySet}
                        filterInstances={filterInstances}
                        canCreateNewBase={isCurrentUserCrosstabAdminCreate}
                        subsetId={subsetId}
                        hasBreaksApplied={categories.length > 0}
                    />
                );
            }
        }
    }

    const getConfigurationOptionClassName = (page: ConfigurationPage) => page == configurationPage ? 'tab-active' : 'tab-item';

    const updateConfigurationPage = (page: ConfigurationPage) => {
        setConfigurationPage(page);
        setQueryParameter(SurveyVueQueryParams.CrosstabPage, page);
    }

    function getBaseExpressionOverride(): BaseExpressionDefinition | undefined {
        if (metric && !metric.hasCustomBase && productConfiguration.isSurveyVue()) {
            let baseState = crosstabPageState.metricBaseLookup[metric.name] ?? getDefaultBaseStateForMetric(metric);

            if (baseState.baseVariableId && !baseVariables.some(b => b.id == baseState.baseVariableId)) {
                baseState = getDefaultBaseStateForMetric(metric);
            }

            return new BaseExpressionDefinition({
                baseType: baseState.baseType,
                baseVariableId: baseState.baseVariableId,
                baseMeasureName: metric.name,
            });
        }
    }

    const questionTypeText = (metric: Metric): string => {
        if (metric.isBasedOnCustomVariable) {
            return "Custom variable";
        }

        return metric.calcType.toString();
    };

    if (!metric) {
        return (
            <NoMetadataNotification />
        );
    }


    const getTextDescription = () => {
        const metricName = metric.displayName;
        const questionType = questionTypeText(metric);
        return (
            <div>
                <div className="name-and-options">
                    <div className="name-and-type">
                        <div className="question-name-text" title={metricName}>{metricName}</div>
                        <Separator />
                        <div className="question-type-text">{questionType}</div>
                    </div>
                </div>
                {filterInstances != null &&
                    <div className="filter-instance-container">
                        <div className="filter-instance" key={filterInstances[0].instance.name}>
                            {filterInstances[0].type.displayNameSingular}: {filterInstances[0].instance.name}
                        </div>
                    </div>
                }
            </div>
        );
    }

    const isMetricUndefined = metric === undefined;
    const currentPaginationData = paginationDict.getCurrentPaginationData();
    const isText = metric?.calcType === CalculationType.Text;

    const getCrossTabTextContainer = () => {
        const mainQuestionType = questionTypeLookup[metric.name];
        if (mainQuestionType == BrandVueApi.MainQuestionType.HeatmapImage) {
            return <div className="page-card-full table-card-full">
                <HeatmapImageCard
                    metric={metric}
                    curatedFilters={updatedFilters}
                    setDataState={setDataState}
                    baseExpressionOverride={baseExpressionOverride}
                    filterInstances={filterInstances ? filterInstances : []}
                    setCanDownload={setCanDownload}
                    displayFooter={true}
                    decimalPlaces={crosstabPageState.decimalPlaces}
                    heatmapOptions={heatMapOptions}
                    user={props.user}
                />
            </div>
        }

        switch (dataState) {
            case PageCardState.NoData:
                return <div className="page-error no-data">
                    <div>No results</div>
                </div>;
            case PageCardState.Error:
                return <div className="page-error error">
                    <i className="material-symbols-outlined no-symbol-fill">info</i>
                    <div>There was an error loading results</div>
                </div>;
            case PageCardState.NotSupportedOverlap:
                return <div className="page-error error">
                    <i className="material-symbols-outlined no-symbol-fill">warning</i>
                    <div>More than one overlap between net results or more than one overlap between net and non-net results are not supported</div>
                </div>;
            case PageCardState.Show:
                return <div className="page-card-full table-card-full">
                    <TextCard
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        metric={metric}
                        getDescriptionNode={getTextDescription}
                        curatedFilters={updatedFilters}
                        setDataState={setDataState}
                        baseExpressionOverride={baseExpressionOverride}
                        filterInstances={filterInstances ? filterInstances : []}
                        setIsLowSample={setIsLowSample}
                        setCanDownload={setCanDownload}
                        lowSampleThreshold={crosstabPageState.lowSampleThreshold}
                        fullWidth
                    />
                </div>
        }
    }

    function getRequestModel() {
        const hasActiveEntitySetOrIsNPS = activeEntitySet || metric?.calcType === CalculationType.NetPromoterScore;
        if (metric && hasActiveEntitySetOrIsNPS) {
            if (metric.calcType === CalculationType.Text) {
                const entityInstanceIds = filterInstances ? filterInstances.map(f => f.instance.id) : [];
                return ViewHelper.createCuratedRequestModel(
                    entityInstanceIds,
                    [metric],
                    updatedFilters,
                    0,
                    {},
                    subsetId,
                    timeSelection // Add timeSelection as the last argument
                );
            }
            return getCrosstabRequestModel(metric,
                metrics,
                categories,
                activeEntitySet,
                secondaryEntitySets,
                updatedFilters,
                entityConfiguration,
                currentPaginationData,
                props.isSurveyVue,
                crosstabPageState.highlightSignificance,
                crosstabPageState.displaySignificanceDifferences,
                crosstabPageState.significanceType,
                isSurveyVueDataWeightable && crosstabPageState.weightingEnabled,
                false,
                props.focusInstance,
                baseExpressionOverride,
                subsetId,
                crosstabPageState.sigConfidenceLevel,
                crosstabPageState.showMultipleTablesAsSingle,
                timeSelection,
                crosstabPageState.calculateIndexScores
            );
        }
    }

    return (
        <FeatureGuard permissions={[BrandVueApi.PermissionFeaturesOptions.DataAccess]} fallback={<div className={styles.noAccessMessage}>You do not have permission to view this page.</div>}>
            <VariableProvider
                user={props.user}
                nonMapFileSurveys={productConfiguration.nonMapFileSurveys}
                googleTagManager={props.googleTagManager}
                pageHandler={props.pageHandler}
                shouldSetQueryParamOnCreate={true}
                isSurveyGroup={productConfiguration.isSurveyGroup}
            >
                <div className="question-page">
                    <div className={`left-pane ${isMetricUndefined ? "disable-page" : ""}`} >
                        <Nav tabs>
                            <NavItem>
                                <NavLink className={getConfigurationOptionClassName(ConfigurationPage.Question)}
                                    onClick={e => updateConfigurationPage(ConfigurationPage.Question)}>
                                    Question
                                </NavLink>
                            </NavItem>
                            <NavItem>
                                <NavLink className={getConfigurationOptionClassName(ConfigurationPage.Breaks)}
                                    onClick={e => updateConfigurationPage(ConfigurationPage.Breaks)}>
                                    Breaks
                                </NavLink>
                            </NavItem>
                            <NavItem>
                                <NavLink className={getConfigurationOptionClassName(ConfigurationPage.Options)}
                                    onClick={e => updateConfigurationPage(ConfigurationPage.Options)}>
                                    Options
                                </NavLink>
                            </NavItem>
                        </Nav>
                        {getCrosstabControlsBody(props.user, splitByType)}
                    </div>
                    <VariableContentModal
                        isOpen={isVariableModalOpen}
                        setIsOpen={setIsVariableModalOpen}
                        subsetId={subsetId}
                    />
                    {metric &&
                        <ConvertCalculationTypeModal
                            isVisible={convertCalculationTypeModalVisible}
                            setIsModalVisible={setConvertCalculationTypeModalVisible}
                            googleTagManager={props.googleTagManager}
                            pageHandler={props.pageHandler}
                            selectedMetric={metric}
                            user={props.user}
                        />
                    }
                    <div className={`right-pane ${isMetricUndefined || metric.eligibleForCrosstabOrAllVue ? "" : "hidden"}`}>
                        <CrosstabTitle
                            selectedMetric={metric}
                            curatedFilters={updatedFilters}
                            splitByEntityType={splitByType}
                            user={props.user}
                            applicationConfiguration={props.applicationConfiguration}
                            resultSortingOrder={crosstabPageState.resultSortingOrder}
                            includeCounts={crosstabPageState.includeCounts}
                            highlightLowSample={crosstabPageState.highlightLowSample}
                            decimalPlaces={crosstabPageState.decimalPlaces}
                            isLowSample={isLowSample}
                            isSurveyVue={props.isSurveyVue}
                            googleTagManager={props.googleTagManager}
                            activeEntitySet={activeEntitySet}
                            focusInstance={props.focusInstance}
                            secondaryEntitySets={secondaryEntitySets}
                            numTables={currentPaginationData.totalNoOfTables}
                            getRequestModel={getRequestModel}
                            canExportData={canDownload}
                            activeMetrics={crosstabPageMetrics}
                            averageTypes={crosstabPageState.selectedAverages}
                            averages={props.averages}
                            pageHandler={props.pageHandler}
                            displayMeanValues={crosstabPageState.displayMeanValues}
                            hideTotalColumn={crosstabPageState.hideTotalColumn}
                            showMultipleTablesAsSingle={crosstabPageState.showMultipleTablesAsSingle}
                            calculateIndexScores={crosstabPageState.calculateIndexScores}
                            setCreateNewReportModalVisibility={(isVisible: boolean, metric: Metric) => {
                                SetCreateNewReportModalVisible(isVisible)
                                setPreSelectedMetric(metric);
                            }}
                            showAddToReportModal={() => setShowAddToReportModal(true)}
                            lowSampleThreshold={crosstabPageState.lowSampleThreshold}
                        />
                        <CatchReportAndDisplayErrors applicationConfiguration={props.applicationConfiguration}
                            childInfo={{
                                "Component": "CrosstabsContainer",
                                "Metric": metric?.name,
                                "Breaks": JSON.stringify(categories),
                                "Base": JSON.stringify(baseExpressionOverride)
                            }}
                        >
                            {isText ? getCrossTabTextContainer() : (
                                <>
                                    <div className={`question-tables-container ${isMetricUndefined || metric.entityCombination.length <= 1 ? 'single fit-to-page' : 'multi'}`} ref={tableRef}>
                                        <CrosstabsContainer metric={metric}
                                            activeEntitySet={activeEntitySet}
                                            secondaryEntitySets={secondaryEntitySets}
                                            curatedFilters={updatedFilters}
                                            categories={categories}
                                            includeCounts={crosstabPageState.includeCounts}
                                            highlightLowSample={crosstabPageState.highlightLowSample}
                                            highlightSignificance={crosstabPageState.highlightSignificance}
                                            displaySignificanceDifferences={crosstabPageState.displaySignificanceDifferences}
                                            significanceType={crosstabPageState.significanceType}
                                            resultSortingOrder={crosstabPageState.resultSortingOrder}
                                            baseExpressionOverride={baseExpressionOverride}
                                            decimalPlaces={crosstabPageState.decimalPlaces}
                                            setCanDownload={setCanDownload}
                                            hideEmptyRows={false}
                                            hideEmptyColumns={false}
                                            showTop={undefined}
                                            isUserAdmin={props.user?.isAdministrator ?? false}
                                            setIsLowSample={setIsLowSample}
                                            allMetrics={metrics}
                                            isSurveyVue={props.isSurveyVue}
                                            setCanIncludeCounts={setCanIncludeCounts}
                                            isDataWeighted={isSurveyVueDataWeightable && crosstabPageState.weightingEnabled && props.isSurveyVue}
                                            focusInstance={props.focusInstance}
                                            currentPaginationData={currentPaginationData}
                                            averageTypes={crosstabPageState.selectedAverages}
                                            displayMeanValues={crosstabPageState.displayMeanValues}
                                            splitBy={splitByType}
                                            sigConfidenceLevel={crosstabPageState.sigConfidenceLevel}
                                            hideTotalColumn={crosstabPageState.hideTotalColumn}
                                            showMultipleTablesAsSingle={crosstabPageState.showMultipleTablesAsSingle}
                                            calculateIndexScores={crosstabPageState.calculateIndexScores}
                                            lowSampleThreshold={crosstabPageState.lowSampleThreshold}
                                            displayStandardDeviation={crosstabPageState.displayStandardDeviation}
                                        />
                                    </div>
                                    <div>
                                        <TablePaginationControls
                                            currentPaginationData={currentPaginationData}
                                            setPagination={paginationDict.setCurrentPaginationData}
                                            maxNoOfTablesPerPage={MAX_TABLES_PER_PAGE}
                                        />
                                    </div>
                                </>
                            )}
                        </CatchReportAndDisplayErrors>
                    </div>
                    <CreateNewReportModal
                        user={props.user}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        isCreateNewReportModalVisible={isCreateNewReportModalVisible}
                        setCreateNewReportModalVisibility={(isVisible) => SetCreateNewReportModalVisible(isVisible)}
                        questionTypeLookup={questionTypeLookup}
                        applicationConfiguration={props.applicationConfiguration}
                        averages={props.averages}
                        preSelectedMetric={preSelectedMetric}
                    />
                    <AddToReportModal isOpen={showAddToReportModal}
                        setIsOpen={() => setShowAddToReportModal(false)}
                        preSelectedMetric={metric}
                    />
                </div>
            </VariableProvider>
        </FeatureGuard>
    );
};

export default CrosstabPage;

