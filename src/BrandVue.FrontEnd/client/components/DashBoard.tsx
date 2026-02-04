import React, { ReactElement } from 'react';
import {useEffect, useState} from 'react';
import {dsession} from "../dsession";
import * as PageHandler from "./PageHandler";
import {useLocation} from "react-router-dom";
import DashBox, {legendPosition} from "./visualisations/DashBox";
import {CatchReportAndDisplayErrors} from "./CatchReportAndDisplayErrors";
import ProfileChart from "./visualisations/ProfileChart";
import ProfileChartOverTime from "./visualisations/ProfileChartOverTime";
import MultiMetrics from "./visualisations/MultiMetrics";
import MultiEntityCompetition from "./visualisations/MultiEntityCompetition";
import RankingTable from "./visualisations/RankingTable";
import Funnel from "./visualisations/Funnel";
import {viewBase} from "../core/viewBase";
import {CuratedFilters} from "../filter/CuratedFilters";
import moment from "moment";
import ScorecardPerformance from "./scorecards/ScorecardPerformance";
import ScorecardFilters from "./scorecards/ScorecardFilters";
import ScorecardVsPeers from "./scorecards/ScorecardVsKeyCompetitors";
import StandardSelectorsAndFilters from "./StandardSelectorsAndFilters";
import ScatterPlot from "./visualisations/ScatterPlot";
import {
    AbstractCommonResultsInformation,
    IAverageDescriptor,
    AverageTotalRequestModel,
    CrossMeasure,
    DataClient,
    IEntityType,
    Factory,
    IPaneDescriptor,
    LowSampleSummary,
    PaneDescriptor,
    PartDescriptor
} from "../BrandVueApi";
import BrandSample from "./visualisations/BrandSample";
import SplitMetricChart from "./visualisations/SplitMetricChart";
import StackedProfileChart from "./visualisations/StackedProfileChart";
import {MetricSet} from "../metrics/metricSet";
import WordleChart from "./visualisations/WordleChart";
import LowSampleWarning from "./LowSampleWarning";
import SidePanel, {SidePanelObject, SidePanelType} from "./SidePanel";
import FilterDescription from "./filters/FilterDescription";
import TrialWarning from './TrialWarning';
import MetricComparison from './visualisations/MetricComparison/MetricComparison';
import {Metric} from '../metrics/metric';
import {EntitySet} from "../entity/EntitySet";
import {FilterInstance} from "../entity/FilterInstance";
import {EntityInstance} from "../entity/EntityInstance";
import {PaneType} from "./panes/PaneType";
import FilterStrip from "./FilterStrip"
import PageLayout from "./PageLayout";
import PageInfo from "./PageInfo";
import {getEndOfLastMonthWithData} from "./helpers/DateHelper";
import {getBasePathByPageName} from './helpers/UrlHelper';
import {isEatingOutMarketMetric} from "../metrics/metricHelper";
import {getMarketMetricAverages} from "./helpers/AveragesHelper";
import ExploreDataButton from './buttons/ExploreDataButton';
import ColumnChart from './visualisations/ColumnChart';
import {getStartPage, isCrosstabPage} from './helpers/PagesHelper';
import {MetricResultsSummary} from './helpers/MetricInsightsHelper';
import {ApplicationConfiguration} from '../ApplicationConfiguration';
import { IGoogleTagManager } from '../googleTagManager';
import {ProductConfiguration} from '../ProductConfiguration';
import {AnalysisScorecardTitle} from './helpers/AnalysisHelper';
import BrandAnalysisScore from "./brandanalysis/BrandAnalysisScore";
import {getTypedPart, IPart} from "../parts/IPart";
import {filterSet as FilterSet} from "../filter/filterSet";
import {IEntityConfiguration} from '../entity/EntityConfiguration';
import {PartType} from './panes/PartType';
import {getCategoryComparisonColorByName, getCategoryComparisonSecondaryColorByName} from './helpers/ChromaHelper';
import {getFirstLegendDescription, getSecondLegendDescription} from '../helpers/CategoryComparisonHelper';
import {LocalStorageKeys, LocalStorageValues} from './helpers/LocalStorageHelper';
import NoSampleForFocusInstanceWarning from './NoSampleForFocusInstanceWarning';
import WarningBanner from './visualisations/WarningBanner';
import {useAppSelector} from "../state/store";
import { selectActiveEntityTypeOrNull } from "../state/entitySelectionSelectors";
import {
    useActiveEntitySetWithDefaultOrNull,
    useActiveInstanceOrBrandDefault,
    useFilterInstanceWithDefaultOrNull
} from "client/state/entitySelectionHooks";
import {EntityInstanceGroup} from "../entity/EntityInstanceGroup";
import { selectSubsetId } from 'client/state/subsetSlice';
import { selectAllAverages } from 'client/state/averageSlice';

interface IDashboardProps {
    session: dsession;
    enabledMetricSet: MetricSet;
    entityConfiguration: IEntityConfiguration;
    averageRequests: AverageTotalRequestModel[] | null;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
    pageHandler: PageHandler.PageHandler;
    breaks?: CrossMeasure;
    getAllInstancesForType(type: IEntityType): EntityInstance[];
    shouldDisplayTopControls: boolean;
    availableEntitySets: EntitySet[];
    availableSplitByInstances: EntityInstance[];
    focusInstance: EntityInstance | null;
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void;
    updateAverageRequests(averageRequests: AverageTotalRequestModel[] | null): void;
    headerText: string;
}

export default (props: IDashboardProps) => {
    return (
        <CatchReportAndDisplayErrors applicationConfiguration={props.applicationConfiguration} childInfo={{ "Component": "DashBoard" }}>
            <Content {...props} />
        </CatchReportAndDisplayErrors>
    );
}

interface IContextButtonsProps {
    session: dsession;
    filters: FilterSet;
    averageRequests: AverageTotalRequestModel[] | null;
    enabledMetricSet: MetricSet;
    entityConfiguration: IEntityConfiguration;
    googleTagManager: IGoogleTagManager;
    productConfiguration: ProductConfiguration;
    filterInstance?: FilterInstance;
    panesToRender: PaneDescriptor[];
    entitySet: EntitySet;
    breaks?: CrossMeasure;
    metrics: Metric[];
    showFilterButton: boolean;
    saveButtonText?: string;
    applicationConfiguration: ApplicationConfiguration;
}

class ContextButtons extends React.Component<IContextButtonsProps, {}> {
    constructor(props) {
        super(props);
    }

    render() {
        const crosstabPage = this.props.session.pages.find(page => isCrosstabPage(page))
        return <div id="contextButtons">
            {this.props.metrics.length === 1 &&
                <ExploreDataButton metrics={this.props.metrics} crosstabPage={crosstabPage} pageHandler={this.props.session.pageHandler} />}
            <FilterStrip
                filters={this.props.session.filters}
                averageRequests={this.props.averageRequests}
                metrics={this.props.enabledMetricSet}
                entityConfiguration={this.props.entityConfiguration}
                activeView={this.props.session.activeView}
                activeDashPage={this.props.session.activeDashPage}
                coreViewType={this.props.session.coreViewType}
                overridingPaneType={this.props.session.getOverridingPaneType()!}
                googleTagManager={this.props.googleTagManager}
                pageHandler={this.props.session.pageHandler}
                entitySet={this.props.entitySet}
                filterInstance={this.props.filterInstance}
                showFilterButton={this.props.showFilterButton}
                breaks={this.props.breaks}
                saveImageButtonText={this.props.saveButtonText}
                applicationConfiguration={this.props.applicationConfiguration}
            />
        </div>;

    }
}

interface IContentProps {
    session: dsession;
    enabledMetricSet: MetricSet;
    entityConfiguration: IEntityConfiguration;
    averageRequests: AverageTotalRequestModel[] | null;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
    pageHandler: PageHandler.PageHandler;
    breaks?: CrossMeasure;
    getAllInstancesForType(type: IEntityType): EntityInstance[];
    shouldDisplayTopControls: boolean;
    availableEntitySets: EntitySet[]
    availableSplitByInstances: EntityInstance[];
    focusInstance: EntityInstance | null;
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void;
    updateAverageRequests(averageRequests: AverageTotalRequestModel[] | null): void;
    headerText: string;
}

const Content: React.FC<IContentProps> = (props) => {
    const [isRegisteredForDataUpdates, setIsRegisteredForDataUpdates] = useState(false);
    // State declarations
    const [sidePanel, setSidePanel] = useState<{showLowSampleSidePanel: boolean, showNoSampleSidePanel: boolean}>({
        showLowSampleSidePanel: localStorage.getItem(LocalStorageKeys.ShowLowSampleSidePanel) ? true : false,
        showNoSampleSidePanel: localStorage.getItem(LocalStorageKeys.ShowNoSampleSidePanel) ? true : false
    });
    const [lowSampleSummaries, setLowSampleSummaries] = useState<any[]>([]);
    const [baseVariable1Name, setBaseVariable1Name] = useState<string | undefined>(undefined);
    const [baseVariable2Name, setBaseVariable2Name] = useState<string | undefined>(undefined);
    const [focusInstanceNoSample, setFocusInstanceNoSample] = useState<boolean>(false);
    const [currentRequestFilters, setCurrentRequestFilters] = useState<string>('');
    const [currentRequestEntities, setCurrentRequestEntities] = useState<string>('');
    const [currentRequestFocusInstance, setCurrentRequestFocusInstance] = useState<string>('');
    const entitySet = useActiveEntitySetWithDefaultOrNull();
    const splitByType = useAppSelector(selectActiveEntityTypeOrNull);
    const filterInstance = useFilterInstanceWithDefaultOrNull(props.session.activeView.getEntityCombination()) ?? undefined;
    const location = useLocation();
    const subsetId = useAppSelector(selectSubsetId);
    const allAverages = useAppSelector(selectAllAverages);

    // Side panel objects definition
    const sidePanelObjects = [
        {
            sidePanelType: SidePanelType.LowSample,
            localStorageKey: LocalStorageKeys.ShowLowSampleSidePanel,
            getToggleValue: () => sidePanel.showLowSampleSidePanel
        },
        {
            sidePanelType: SidePanelType.NoSampleForFocusInstance,
            localStorageKey: LocalStorageKeys.ShowNoSampleSidePanel,
            getToggleValue: () => sidePanel.showNoSampleSidePanel
        }
    ];

    useEffect(() => {
        const nextFilters = JSON.stringify(props.session.activeView.curatedFilters);
        const nextRequestEntities = JSON.stringify(entitySet?.getInstances());
        const nextRequestFocusInstance = JSON.stringify(entitySet?.mainInstance);

        const requestHasChanged =
            (currentRequestFilters !== nextFilters) ||
            (nextRequestEntities !== currentRequestEntities) ||
            (nextRequestFocusInstance !== currentRequestFocusInstance);

        if (requestHasChanged) {
            // Reset no sample side panel
            resetNoSampleSidePanel();

            // Update all relevant state
            setSidePanel(prev => ({
                ...prev,
                showNoSampleSidePanel: false
            }));
            setCurrentRequestFilters(nextFilters);
            setCurrentRequestEntities(nextRequestEntities);
            setCurrentRequestFocusInstance(nextRequestFocusInstance);
            setLowSampleSummaries([]);
            setFocusInstanceNoSample(false);
        }
    }, [
        props.session.activeView.curatedFilters,
        entitySet?.getInstances(),
        entitySet?.mainInstance,
        location
    ]);

    useEffect(() => {
        Factory.RegisterGlobalResponseHandler(DataClient, updateCommonResults);
        setIsRegisteredForDataUpdates(true);
        return () => Factory.UnregisterGlobalResponseHandler(DataClient, updateCommonResults);
    }, []);

    if (!isRegisteredForDataUpdates) return null;
    
    function resetNoSampleSidePanel() {
        localStorage.removeItem(LocalStorageKeys.ShowNoSampleSidePanel);
    }
    
    function removeFromLowSample(brand: EntityInstance, metric: Metric): void {
        const removeBrandMetricPredicate = (s: LowSampleSummary) => (s.entityInstanceId !== brand.id && s.name !== brand.name) || s.metric !== metric.name;
        const newLowSampleSummaries = lowSampleSummaries.filter(removeBrandMetricPredicate);
        setLowSampleSummaries(newLowSampleSummaries);
    }

    function updateCommonResults(commonResultsInformation: AbstractCommonResultsInformation) {
        if (commonResultsInformation) {
            if (commonResultsInformation.lowSampleSummary && commonResultsInformation.lowSampleSummary.length > 0) {
                const data = lowSampleSummaries.concat(commonResultsInformation.lowSampleSummary);
                setLowSampleSummaries(data);
            }

            if (commonResultsInformation.sampleSizeMetadata?.sampleSize !== undefined) {
                const focusInstanceNoSample = commonResultsInformation.sampleSizeMetadata.sampleSize.unweighted === 0;
                setFocusInstanceNoSample(focusInstanceNoSample);
            }
        }
    }

    function updateBaseVariableNames(firstName: string | undefined, secondName: string | undefined) {
        setBaseVariable1Name(firstName);
        setBaseVariable2Name(secondName);
    }

    function toggleSidePanel(sidePanelObject: SidePanelObject) {
        const currentToggleValue = sidePanelObject.getToggleValue();

        if (currentToggleValue) {
            localStorage.removeItem(sidePanelObject.localStorageKey);
        } else {
            sidePanelObjects.filter(o => o !== sidePanelObject).forEach(key => localStorage.removeItem(key.localStorageKey));
            localStorage.setItem(sidePanelObject.localStorageKey, LocalStorageValues.Yes);
        }

        const newSidePanelToggleValues = sidePanelObjects.map(o => [o.localStorageKey, o.sidePanelType === sidePanelObject.sidePanelType ? !currentToggleValue : false ]);
        setSidePanel({
            ...Object.fromEntries(newSidePanelToggleValues),
        });
    }

    function getChartFilterSelectorsForPanes(panesToRender: PaneDescriptor[], availableAveragesForSubset: IAverageDescriptor[]): JSX.Element | null {
        if (panesToRender.every(p => PaneType.hasFixedDate(p.paneType))) {
            return null;
        }

        let control: JSX.Element | null;

        const filterOptions = props.pageHandler.hasScorecardFilters(panesToRender);
        if (filterOptions == null) {
            return (null);
        }

        if (filterOptions) {
            control = <ScorecardFilters session={props.session}
                                        applicationConfiguration={props.applicationConfiguration}
                                        pageHandler={props.session.pageHandler}
                                        averages={availableAveragesForSubset} />;
        } else {
            control = <StandardSelectorsAndFilters
                pageHandler={props.session.pageHandler}
                session={props.session}
                googleTagManager={props.googleTagManager}
                applicationConfiguration={props.applicationConfiguration}
                panesToRender={panesToRender}
                averages={availableAveragesForSubset} />;
        }

        return control ? <div className="chart-control-buttons">{control}</div> : null;
    }

    function getChartButtons(panesToRender: PaneDescriptor[], metrics: Metric[], showFilters: boolean): JSX.Element | null {
        const filterOptions = props.pageHandler.hasScorecardFilters(panesToRender);
        if (filterOptions == null) {
            return null;
        }

        let saveText: string = "Save chart";
        if (panesToRender.every(p => PaneType.BrandAnalysisPanes.some(pn => pn === p.paneType))) {
            saveText = "Save page";
        }

        return <ContextButtons
            session={props.session}
            enabledMetricSet={props.enabledMetricSet}
            entityConfiguration={props.entityConfiguration}
            filters={props.session.filters}
            filterInstance={filterInstance}
            averageRequests={props.averageRequests}
            googleTagManager={props.googleTagManager}
            productConfiguration={props.productConfiguration}
            panesToRender={panesToRender}
            entitySet={entitySet!}
            metrics={metrics}
            breaks={props.breaks}
            showFilterButton={showFilters}
            saveButtonText={saveText}
            applicationConfiguration={props.applicationConfiguration}/>;
    }

    function getLowSampleWarning(): JSX.Element | null {
        const showLowSampleWarning = !sidePanel.showLowSampleSidePanel;

        return showLowSampleWarning
            ? <LowSampleWarning toggleVisibility={() => toggleSidePanel(sidePanelObjects[SidePanelType.LowSample])}
                                activeEntityType={splitByType ?? props.session.activeView.getEntityCombination()[0]}
                                lowSampleSummaries={lowSampleSummaries} />
            : null;
    }

    function getNoSampleWarning(): JSX.Element | null {
        const showNoSampleWarning = !sidePanel.showNoSampleSidePanel;

        return showNoSampleWarning
            ? <NoSampleForFocusInstanceWarning toggleVisibility={() => toggleSidePanel(sidePanelObjects[SidePanelType.NoSampleForFocusInstance])}
                                               activeEntityType={props.session.activeView.getEntityCombination()[0]}
                                               focusInstanceNoSample={focusInstanceNoSample} />
            : null;
    }

    function getFilterDescription(): JSX.Element | null {
        return (
            <div className="filters-and-warning">
                <FilterDescription session={props.session}
                                   enabledMetricSet={props.enabledMetricSet}
                                   entityConfiguration={props.entityConfiguration}
                                   googleTagManager={props.googleTagManager} />
            </div>
        );
    }

    function getPageHeading(headerText: string) {
        return (
            <div>
                <h1 className="page-heading">{headerText} <span className="date">{moment.utc(getEndOfLastMonthWithData(props.applicationConfiguration.dateOfLastDataPoint)).format("MMMM YYYY")}</span></h1>
            </div>
        )
    }

    function getCategoryComparisonPageLegend(): JSX.Element | null {
        if (props.session.activeDashPage.panes.some(pn => pn.parts.some(p => p.partType == PartType.CategoryComparison && p.spec3 == "detailed"))) {
            return <div className="inline-legend">
                <div className="legend-container">
                    <div>
                        <div className="legend-icon" style={{ backgroundColor: getCategoryComparisonColorByName("LightBlue") }} />
                        {getFirstLegendDescription(baseVariable1Name, entitySet?.mainInstance?.name)}
                    </div>
                    <div>
                        <div className="legend-icon" style={{ backgroundColor: getCategoryComparisonSecondaryColorByName("LightGrey") }} />
                        {getSecondLegendDescription(baseVariable1Name, baseVariable2Name)}
                    </div>
                </div>
            </div>;
        };
        return null;
    }

    function getFirstPaneType(): PaneType | undefined {
        const panes = props.session.activeDashPage.panes;
        return panes && panes.length > 0
            ? panes[0].paneType
            : undefined;
    }

    function renderHeading(panesToRender: PaneDescriptor[],
        pageHeading: "" | string,
        availableAveragesForSubset: IAverageDescriptor[]): ReactElement | null {
        const metricNames = props.session.pageHandler.getDisplayedMetrics();
        const metrics = metricNames.map(mn => props.enabledMetricSet.getMetrics(mn)).flat();
        const firstPaneType = getFirstPaneType();
        const showFilters = metrics.every(m => m.isMetricFilterable())
            && firstPaneType !== PaneType.audienceProfile;

        let fullWidth: boolean = false;
        if (panesToRender.every(p => PaneType.BrandAnalysisSubPanes.some(pn => pn === p.paneType))) {
            fullWidth = true;
        }

        return (
            <div id="chartControls" className={`chart-controls ${fullWidth ? 'full-width' : ''}`}>
                <div className="date-and-buttons">
                    <div className="chart-date-filters">
                        {
                            pageHeading ? getPageHeading(pageHeading)
                                : (props.shouldDisplayTopControls 
                                    ? getChartFilterSelectorsForPanes(panesToRender, availableAveragesForSubset) : null)
                        }
                        {getCategoryComparisonPageLegend()}
                    </div>
                    <div className="chart-control-buttons">
                        {
                            !pageHeading && props.shouldDisplayTopControls && getLowSampleWarning()
                        }
                        {
                            !pageHeading && props.shouldDisplayTopControls && getNoSampleWarning()
                        }
                        {
                            (firstPaneType === PaneType.audienceProfile || !pageHeading) 
                            && props.shouldDisplayTopControls 
                            && getChartButtons(panesToRender, metrics, showFilters)
                        }
                    </div>
                </div>
                {location.pathname.toString().startsWith("/audience") &&
                    <WarningBanner message={"This page requires a large number of calculations and therefore will likely be slow to load"} materialIconName={'info'} />
                }
                {
                    !pageHeading && props.shouldDisplayTopControls && showFilters && getFilterDescription()
                }
            </div>
        )
    }

    function getIntroText(session: dsession) {
        const panesToRender = session.pageHandler.getPanesToRender();
        let introText = session.activeDashPage.pageSubsetConfiguration.find(p => p.subset == session.selectedSubsetId)?.helpText ?? session.activeDashPage.helpText;
        if (!introText) {
            return "";
        }
        if (panesToRender.length === 0) {
            introText = "There is no summary to show at this level. Please select another option.";
        }
        return introText;
    }

    // Render logic
    const panesToRender = props.pageHandler.getPanesToRender();
    const activeSidePanelToRender = panesToRender.some(p => PaneType.usesSidePanel(p.paneType)) &&
        sidePanelObjects.find(o => o.getToggleValue() === true);
    const activeEntityType = splitByType ?? props.session.activeView.getEntityCombination()[0];
    const activeDashPage = props.session.activeDashPage;
    const sidePanelEntitySet = activeEntityType ?
        props.entityConfiguration.getAllEnabledInstancesOrderedAsSet(activeEntityType) : undefined;

    const allMetrics = panesToRender
        .reduce((parts, pane) => parts.concat(pane.parts), [] as PartDescriptor[])
        .reduce((metrics, part) => metrics.concat(props.enabledMetricSet.getMetrics(part.spec1)), [] as Metric[]);

    let availableAveragesForSubset = allAverages
        .filter(a => !a.isHiddenFromUsers 
            && (a.subset == undefined
            || a.subset?.some(s => s.id == subsetId)
            || a.subset?.length == 0));
    if (allMetrics.some(m => isEatingOutMarketMetric(m))) {
        availableAveragesForSubset = getMarketMetricAverages(availableAveragesForSubset);
    }

    const isCrosstab = isCrosstabPage(activeDashPage);
    return (
        <>
            <div className="chartContent">
                <div className={`chartGrid ${isCrosstab ? 'crosstab' : ''}`}>
                    {renderHeading(panesToRender, props.headerText, availableAveragesForSubset)}
                    <PageLayout
                        layout={activeDashPage.layout}
                        panesToRender={panesToRender}
                        session={props.session}
                        enabledMetricSet={props.enabledMetricSet}
                        entityConfiguration={props.entityConfiguration}
                        googleTagManager={props.googleTagManager}
                        applicationConfiguration={props.applicationConfiguration}
                        productConfiguration={props.productConfiguration}
                        breaks={props.breaks}
                        getAllInstancesForType={props.getAllInstancesForType}
                        availableEntitySets={props.availableEntitySets}
                        updateMetricResultsSummary={props.updateMetricResultsSummary}
                        updateAverageRequests={props.updateAverageRequests}
                        removeFromLowSample={removeFromLowSample}
                        updateBaseVariableNames={updateBaseVariableNames}
                    />
                    <TrialWarning applicationConfiguration={props.applicationConfiguration} />
                </div>
                {activeSidePanelToRender && (
                    <SidePanel
                        activeEntityType={activeEntityType}
                        currentAverage={props.session.activeView.curatedFilters.average}
                        entityInstances={sidePanelEntitySet ?
                            sidePanelEntitySet.getInstances() :
                            entitySet?.getInstances() ?? new EntityInstanceGroup([])}
                        toggleVisibility={() => toggleSidePanel(activeSidePanelToRender)}
                        lowSampleSummaries={lowSampleSummaries}
                        sidePanelType={activeSidePanelToRender.sidePanelType}
                        noSampleInstanceName={props.focusInstance?.name}
                    />
                )}
            </div>
            {!props.headerText && <PageInfo helpText={getIntroText(props.session)} />}
        </>
    );
};

interface IDashPaneProps {
    session: dsession;
    enabledMetricSet: MetricSet;
    entityConfiguration: IEntityConfiguration;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
    pane: IPaneDescriptor;
    entitySet: EntitySet;
    availableEntitySets: EntitySet[];
    breaks?: CrossMeasure;
    getAllInstancesForType(type: IEntityType): EntityInstance[];
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void;
    updateAverageRequests(averageRequests: AverageTotalRequestModel[] | null): void;
    removeFromLowSample(brand: EntityInstance, metric: Metric): void;
}
export const DashPane = (props: IDashPaneProps) => {
    const startPage = getStartPage();
    const startPageUrl = props.productConfiguration.appBasePath + getBasePathByPageName(startPage.name);
    const mainInstance = useActiveInstanceOrBrandDefault();
    const filterInstance = useFilterInstanceWithDefaultOrNull(props.session.activeView.getEntityCombination()) ?? undefined;
    return (
        <>
            {props.pane.parts
                .sort((a, b) => parseInt(a.ordering[0] ?? 0) - parseInt(b.ordering[0] ?? 0))
                .map((p, i) => (
                    <CatchReportAndDisplayErrors
                        key={i}
                        applicationConfiguration={props.applicationConfiguration}
                        childInfo={{
                            "Part": p.partType,
                            "Spec1": p.spec1,
                            "Spec2": p.spec2,
                            "Spec3": p.spec3
                        }}
                        startPagePath={startPageUrl}
                        startPageName={startPage.displayName}
                    >
                        <DashPart
                            paneHeight={props.pane.height}
                            key={i}
                            partConfig={getTypedPart(p)}
                            activeView={props.session.activeView}
                            curatedFilters={props.session.activeView.curatedFilters}
                            enabledMetricSet={props.enabledMetricSet}
                            entityConfiguration={props.entityConfiguration}
                            pageHandler={props.session.pageHandler}
                            session={props.session}
                            googleTagManager={props.googleTagManager}
                            entitySet={props.entitySet}
                            availableEntitySets={props.availableEntitySets}
                            mainInstance={mainInstance}
                            filterInstance={filterInstance}
                            breaks={props.breaks}
                            getAllInstancesForType={props.getAllInstancesForType}
                            updateMetricResultsSummary={props.updateMetricResultsSummary}
                            removeFromLowSample={props.removeFromLowSample}
                            updateAverageRequests={props.updateAverageRequests}
                        />
                    </CatchReportAndDisplayErrors>
                ))}
        </>
    );
};

export interface IDashPartProps {
    paneHeight: number;
    partConfig: IPart;
    activeView: viewBase;
    curatedFilters: CuratedFilters;
    enabledMetricSet: MetricSet;
    entityConfiguration: IEntityConfiguration;
    pageHandler: PageHandler.PageHandler;
    session: dsession;
    googleTagManager: IGoogleTagManager;
    entitySet: EntitySet;
    mainInstance: EntityInstance;
    availableEntitySets: EntitySet[];
    filterInstance?: FilterInstance;
    breaks?: CrossMeasure;
    updateAverageRequests: (averageRequests: AverageTotalRequestModel[]) => void;
    getAllInstancesForType(type: IEntityType): EntityInstance[];
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void;
    removeFromLowSample(brand: EntityInstance, metric: Metric): void;
}

class DashPart extends React.Component<IDashPartProps, {}> {
    constructor(props) {
        super(props);
        this.state = { results: [] }
    }

    private currentPropsJson: string = "";

    private static withoutCircularDependenciesReplacer = (key, value) => {
        if (typeof key === 'string' || key instanceof String) {
            if (key.startsWith("__reactInternal")) { //For WGSN we seem to be getting __reactInternalInstance$sdipdk9bse with cyclic dependencies
                return undefined;
            }
        }
        if (value instanceof MetricSet) {
            return JSON.stringify(value, (key, value) => {
                if (key === 'parent') {
                    return undefined
                }
                return value;
            });
        }
        if (value instanceof dsession) {
            return JSON.stringify(value, Object.keys(value).filter(k => k !== 'pageHandler'));
        }
        return value;
    };

    componentDidUpdate(prevProps: Readonly<IDashPartProps>, prevState: Readonly<{}>, snapshot?: any): void {
        try {
            this.currentPropsJson = JSON.stringify(this.props, DashPart.withoutCircularDependenciesReplacer);
        }
        catch (e) {
            this.currentPropsJson = "";
            let result = (e as Error).message;
            console.log(`Error componentDidUpdate ${result}`);
        }
    }

    shouldComponentUpdate(nextProps: Readonly<IDashPartProps>, nextState: Readonly<{}>, nextContext: any): boolean {
        //
        //only re - render if the props change, not if the parents props or other state changes
        //
        let json = "";
        try {
            json = JSON.stringify(nextProps, DashPart.withoutCircularDependenciesReplacer);
        }
        catch (e) {
            let result = (e as Error).message;
            console.log(`Error shouldComponentUpdate ${result}`);
        }
        return this.currentPropsJson !== json;
    }

    render() {
        const mainInstance = this.props.entitySet.getMainInstance();
        let dashPart: ReactElement | null = this.props.partConfig.getPartComponent(this.props);
        if (dashPart == null) {
        const metrics = this.props.enabledMetricSet.getMetrics(this.props.partConfig.descriptor.spec1);

            switch (this.props.partConfig.descriptor.partType) {
                case "BoxChartTall":
                    dashPart =
                        <DashBox
                            googleTagManager={this.props.googleTagManager}
                            height={640}
                            metrics={metrics}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeBrand={mainInstance}
                            partId={this.props.partConfig.descriptor.id}
                            legendPosition={legendPosition.Bottom}
                            entitySet={this.props.entitySet}
                            availableEntitySets={this.props.availableEntitySets}
                            filterInstance={this.props.filterInstance}
                            breaks={this.props.breaks}
                            updateMetricResultsSummary={this.props.updateMetricResultsSummary}
                            updateAverageRequests={this.props.updateAverageRequests}
                        />;
                    break;
                case "ColumnChart":
                    dashPart =
                        <ColumnChart
                            height={640}
                            metrics={metrics}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeBrand={mainInstance}
                            entitySet={this.props.entitySet}
                            filterInstance={this.props.filterInstance}
                            breaks={this.props.breaks}
                            updateMetricResultsSummary={this.props.updateMetricResultsSummary}
                            availableEntitySets={this.props.availableEntitySets}
                            updateAverageRequests={this.props.updateAverageRequests}
                            partId={this.props.partConfig.descriptor.id}
                        />;
                    break;
                case "ProfileChart":
                    dashPart =
                        <ProfileChart
                            googleTagManager={this.props.googleTagManager}
                            height={640}
                            entitySet={this.props.entitySet}
                            availableEntitySets={this.props.availableEntitySets}
                            filterInstance={this.props.filterInstance}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeBrand={mainInstance}
                            metric={metrics[0]}
                            updateMetricResultsSummary={this.props.updateMetricResultsSummary}
                            updateAverageRequests={this.props.updateAverageRequests}
                            partId={this.props.partConfig.descriptor.id}
                        />;
                    break;
                case "ProfileChartOverTime":
                    dashPart =
                        <ProfileChartOverTime
                            googleTagManager={this.props.googleTagManager}
                            height={640}
                            keyBrands={this.props.entitySet.getInstances()}
                            metric={metrics[0]}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeBrand={mainInstance}
                            updateMetricResultsSummary={this.props.updateMetricResultsSummary}
                            updateAverageRequests={this.props.updateAverageRequests}
                        />;
                    break;
                case "RankingTable":
                    dashPart =
                        <RankingTable
                            metric={metrics[0]}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeBrand={mainInstance}
                            entitySet={this.props.entitySet}
                            filterInstance={this.props.filterInstance}
                            updateMetricResultsSummary={this.props.updateMetricResultsSummary}
                            partId={this.props.partConfig.descriptor.id}
                        />;
                    break;
                case "MultiMetrics":
                    dashPart =
                        <MultiMetrics
                            googleTagManager={this.props.googleTagManager}
                            title={this.props.partConfig.descriptor.spec2}
                            height={640}
                            metrics={metrics}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeBrand={mainInstance}
                            availableEntitySets={this.props.availableEntitySets}
                            entitySet={this.props.entitySet}
                            maxNumberOfEntries={Number(this.props.partConfig.descriptor.spec3)}
                            updateAverageRequests={this.props.updateAverageRequests} />;
                    break;
                case "MultiEntityCompetition":
                    dashPart =
                        <MultiEntityCompetition
                            googleTagManager={this.props.googleTagManager}
                            title={this.props.partConfig.descriptor.spec2}
                            height={640}
                            metric={metrics[0]}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeInstance={mainInstance}
                            availableEntitySets={this.props.availableEntitySets}
                            entitySet={this.props.entitySet}
                            filterInstance={this.props.filterInstance}
                            maxNumberOfEntries={Number(this.props.partConfig.descriptor.spec3)}
                            updateMetricResultsSummary={this.props.updateMetricResultsSummary}
                            updateAverageRequests={this.props.updateAverageRequests}
                            partId={this.props.partConfig.descriptor.id}
                        />;
                    break;
                case "Funnel":
                    dashPart =
                        <Funnel
                            title={this.props.partConfig.descriptor.spec2}
                            height={640}
                            metrics={metrics}
                            availableEntitySets={this.props.availableEntitySets}
                            entitySet={this.props.entitySet}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeBrand={mainInstance}
                            showAdoptionSignatureIfAvailable={this.props.partConfig.descriptor.spec3 === 'ShowSignature'}  
                            partId={this.props.partConfig.descriptor.id}/>;
                    break;
                case "ScorecardPerformance":
                    dashPart =
                        <ScorecardPerformance
                            title={this.props.partConfig.descriptor.spec2}
                            height={640}
                            metrics={metrics}
                            curatedFilters={this.props.activeView.curatedFilters}
                            mainInstance={mainInstance}
                            nextSteps={this.props.partConfig.descriptor.spec3}
                            pageHandler={this.props.pageHandler}
                            entitySet={this.props.entitySet}
                            availableEntitySets={this.props.availableEntitySets}
                            partId={this.props.partConfig.descriptor.id}
                        />
                    break;
                case "ScorecardVsPeers":
                    dashPart =
                        <ScorecardVsPeers
                            title={this.props.partConfig.descriptor.spec2}
                            height={640}
                            metrics={metrics}
                            curatedFilters={this.props.activeView.curatedFilters}
                            entitySet={this.props.entitySet}
                            nextSteps={this.props.partConfig.descriptor.spec3}
                            pageHandler={this.props.pageHandler} />;
                    break;
                case "ScatterPlot":
                    dashPart =
                        <ScatterPlot
                            googleTagManager={this.props.googleTagManager}
                            height={640}
                            metrics={metrics}
                            curatedFilters={this.props.activeView.curatedFilters}
                            entitySet={this.props.entitySet}
                            activeBrand={mainInstance}
                            pageHandler={this.props.pageHandler}
                            xAxisRange={this.props.partConfig.descriptor.xAxisRange}
                            yAxisRange={this.props.partConfig.descriptor.yAxisRange}
                            sections={this.props.partConfig.descriptor.sections} />;
                    break;
                case "BrandSample":
                    dashPart =
                        <BrandSample
                            height={640}
                            metrics={metrics}
                            curatedFilters={this.props.activeView.curatedFilters}
                            keyBrands={this.props.entitySet.getInstances()}
                            activeBrand={mainInstance}
                            metricDescriptions={this.props.partConfig.descriptor.spec2}
                            metricGroups={this.props.partConfig.descriptor.spec3} />;
                    break;
                case "SplitMetricChart":
                    dashPart =
                        <SplitMetricChart
                            googleTagManager={this.props.googleTagManager}
                            title={this.props.partConfig.descriptor.spec2}
                            height={640}
                            metrics={metrics}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeBrand={mainInstance}
                            entitySet={this.props.entitySet}
                            filterInstance={this.props.filterInstance}
                            filters={this.props.partConfig.descriptor.filters}
                            colours={this.props.partConfig.descriptor.colours} />;
                    break;
                case "StackedProfileChart":
                    dashPart =
                        <StackedProfileChart
                            googleTagManager={this.props.googleTagManager}
                            height={640}
                            metrics={metrics}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeBrand={mainInstance}
                            colours={this.props.partConfig.descriptor.colours} />;
                    break;
                case "Wordle":
                    dashPart =
                        <WordleChart
                            height={640}
                            metrics={metrics}
                            curatedFilters={this.props.activeView.curatedFilters}
                            activeBrand={mainInstance} />;
                    break;
                case "MetricComparison":
                    dashPart =
                        <MetricComparison
                            googleTagManager={this.props.googleTagManager}
                            brands={this.props.getAllInstancesForType(this.props.entityConfiguration.defaultEntityType)}
                            metrics={this.props.enabledMetricSet.metrics.filter(m => m.eligibleForMetricComparison)}
                            height={640}
                            curatedFilters={this.props.activeView.curatedFilters}
                            session={this.props.session}
                            removeFromLowSample={this.props.removeFromLowSample}
                        />
                    break;
                case "BrandAnalysisScore":
                    dashPart = <BrandAnalysisScore
                        brand={mainInstance}
                        activeEntitySet={this.props.entitySet}
                        curatedFilters={this.props.curatedFilters}
                        primaryMetric={metrics[0]}
                        entityConfiguration={this.props.entityConfiguration}
                        title={AnalysisScorecardTitle[this.props.partConfig.descriptor.spec2]}
                        pageMetricConfiguration={JSON.parse(this.props.partConfig.descriptor.spec3)}
                    />;
                    break;
                default:
                    console.log("Unsupported part type: " + this.props.partConfig.descriptor.partType);
            }
        }

        return dashPart;
    }
}
