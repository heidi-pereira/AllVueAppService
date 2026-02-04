import React from "react";
import { PropsWithChildren } from "react";
import {
    IPaneDescriptor,
    IEntityType,
    IAverageDescriptor,
    WeightingMethod,
    IApplicationUser,
    CrossMeasure,
    AverageTotalRequestModel,
    ComparisonPeriodSelection,
    PermissionFeaturesOptions,
} from "../BrandVueApi";
import { PaneType } from "./panes/PaneType";
import IFrameContainer from "./visualisations/IFrameContainer";
import { dsession } from "../dsession";
import { EntitySet } from "../entity/EntitySet";
import { EntityInstance } from "../entity/EntityInstance";
import { DashPane } from "./DashBoard";
import CardPane from "./panes/CardPane";
import CrosstabPage from "./visualisations/Crosstab/CrosstabPage";
import { CuratedFilters } from "../filter/CuratedFilters";
import { UserContext } from "../GlobalContext";
import { useMetricStateContext } from "../metrics/MetricStateContext";
import { SavedReportsProvider } from "./visualisations/Reports/SavedReportsContext";
import SettingsPage from "./visualisations/Settings/SettingsPage";
import { FilterStateProvider } from "../filter/FilterStateContext";
import { AsyncExportProvider } from "./visualisations/Reports/Utility/AsyncExportContext";
import { MetricResultsSummary } from "./helpers/MetricInsightsHelper";
import { ApplicationConfiguration } from "../ApplicationConfiguration";
import { ProductConfiguration } from "../ProductConfiguration";
import { CrosstabPageStateProvider } from "./visualisations/Crosstab/CrosstabPageStateContext";
import { IGoogleTagManager } from "../googleTagManager";
import { Metric } from "../metrics/metric";
import { MetricSet } from "../metrics/metricSet";
import { IEntityConfiguration } from "../entity/EntityConfiguration";
import ReportVuePage from "./visualisations/ReportVue/ReportVuePage";
import { VariableProvider } from "./visualisations/Variables/VariableModal/Utils/VariableContext";
import {HtmlView} from "./HtmlView";
import AllVueWebPage from "./visualisations/WebPage/AllVueWebPage";
import { useActiveEntitySetWithBrandDefaultOrNull } from "client/state/entitySelectionHooks";
import { throwIfNullish } from "./helpers/ThrowHelper";
import { isCustomPeriodAverage } from "client/components/helpers/PeriodHelper";
import { useWriteVueQueryParams } from "./helpers/UrlHelper";
import { useLocation, useNavigate } from "react-router-dom";
import { selectSubsetId, updateSubset } from "client/state/subsetSlice";
import { useAppSelector } from "client/state/store";
import { selectAveragesForSubset } from "client/state/averageSlice";
import { useDispatch } from "react-redux";
import FeatureGuard from "./FeatureGuard/FeatureGuard";
import ReportsLayer from "./ReportsLayer";
import styles from "./PageLayout.module.less";

interface IProps {
    layout?: string;
    panesToRender: IPaneDescriptor[];
    session: dsession;
    enabledMetricSet: MetricSet;
    entityConfiguration: IEntityConfiguration;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
    breaks?: CrossMeasure;
    getAllInstancesForType(type: IEntityType): EntityInstance[];
    availableEntitySets: EntitySet[] | undefined;
    updateMetricResultsSummary(metricResultsSummary: MetricResultsSummary): void;
    updateAverageRequests(averageRequests: AverageTotalRequestModel[] | null): void;
    removeFromLowSample(brand: EntityInstance, metric: Metric): void;
    updateBaseVariableNames(firstName: string | undefined, secondName: string | undefined): void;
}

const PageLayout: React.FunctionComponent<IProps> = (props: PropsWithChildren<IProps>) => {
    const dispatch = useDispatch();
    const subsetId = useAppSelector(selectSubsetId);
    const availableAveragesForSubset = useAppSelector(state => selectAveragesForSubset(state, subsetId));
    const activeEntitySet = useActiveEntitySetWithBrandDefaultOrNull();
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());

    const unchangingFilters = React.useMemo(() => {
        const demographicFilter = props.session.activeView.curatedFilters.demographicFilter;
        const newFilters = props.productConfiguration.isSurveyVue() ? CuratedFilters.createWithOptions({
            startDate: props.applicationConfiguration.dateOfFirstDataPoint,
            endDate: props.applicationConfiguration.dateOfLastDataPoint,
            average: availableAveragesForSubset.find(a => isCustomPeriodAverage(a) && a.weightingMethod === WeightingMethod.None),
            comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly,
        }, props.entityConfiguration) : CuratedFilters.createWithOptions({
            startDate: props.session.activeView.curatedFilters.startDate,
            endDate: props.session.activeView.curatedFilters.endDate,
            average: props.session.activeView.curatedFilters.average,
            comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly,
        }, props.entityConfiguration);
        newFilters.demographicFilter.ageGroups = demographicFilter.ageGroups;
        newFilters.demographicFilter.genders = demographicFilter.genders;
        newFilters.demographicFilter.regions = demographicFilter.regions;
        newFilters.demographicFilter.socioEconomicGroups = demographicFilter.socioEconomicGroups;
        if (!props.productConfiguration.isSurveyVue()) {
            newFilters.measureFilters = props.session.activeView.curatedFilters.measureFilters;
        }
        return newFilters;
    }, [
        props.session.activeView.curatedFilters.demographicFilter.ageGroups,
        props.session.activeView.curatedFilters.demographicFilter.genders,
        props.session.activeView.curatedFilters.demographicFilter.regions,
        props.session.activeView.curatedFilters.demographicFilter.socioEconomicGroups,
        availableAveragesForSubset,
        props.applicationConfiguration.dateOfFirstDataPoint,
        props.applicationConfiguration.dateOfLastDataPoint
    ]);
    
    const getPaneComponent = (pane: IPaneDescriptor, index: number) => {
        switch (pane.paneType) {
            case PaneType.import:
                return <HtmlView key={pane.id} 
                                 productConfiguration={props.productConfiguration} 
                                 googleTagManager={props.googleTagManager} 
                                 setQueryParameter={setQueryParameter}
                                 pageHandler={props.session.pageHandler} fileName={pane.spec}/>;
            case PaneType.iFrame:
                return <IFrameContainer key={pane.id} url={pane.spec} productConfiguration={props.productConfiguration} height={pane.height} />;
            case PaneType.partGrid:
                if(activeEntitySet && props.availableEntitySets){
                    return <CardPane key={pane.id}
                        paneIndex={index}
                        layout={"PartGrid"}
                        session={props.session}
                        enabledMetricSet={props.enabledMetricSet}
                        entityConfiguration={props.entityConfiguration}
                        applicationConfiguration={props.applicationConfiguration}
                        pane={pane}
                        entitySet={activeEntitySet}
                        averages={availableAveragesForSubset}
                        availableEntitySets={props.availableEntitySets}
                        updateBaseVariableNames={props.updateBaseVariableNames}
                    />;
                }
            case PaneType.partColumn:
            case PaneType.audienceProfile:
                if(activeEntitySet && props.availableEntitySets){
                    return <CardPane key={pane.id}
                        paneIndex={index}
                        layout={"PartColumn"}
                        session={props.session}
                        enabledMetricSet={props.enabledMetricSet}
                        entityConfiguration={props.entityConfiguration}
                        applicationConfiguration={props.applicationConfiguration}
                        pane={pane}
                        entitySet={activeEntitySet}
                        averages={availableAveragesForSubset}
                        availableEntitySets={props.availableEntitySets}
                        updateBaseVariableNames={props.updateBaseVariableNames}
                    />;
                }
            case PaneType.crossTabPage:
                if(subsetId !== props.session.selectedSubsetId) {
                    dispatch(updateSubset(props.session.selectedSubsetId));
                }
                return (
                    <AsyncExportProvider key={pane.id}>
                        <CrosstabPageStateProvider productConfiguration={props.productConfiguration}>
                            <UserContext.Consumer>
                                {(user) =>
                                    <SavedReportsProvider session={props.session} key={pane.id} user={user}>
                                        <CrosstabWithFilterContext
                                            session={props.session}
                                            googleTagManager={props.googleTagManager}
                                            applicationConfiguration={props.applicationConfiguration}
                                            user={user}
                                            averages={availableAveragesForSubset}
                                            unchangingFilters={unchangingFilters}
                                            entitySet={activeEntitySet ?? undefined}
                                            focusInstance={activeEntitySet?.getMainInstance()}
                                            productConfiguration={props.productConfiguration}
                                        />
                                    </SavedReportsProvider>
                                }
                            </UserContext.Consumer>
                        </CrosstabPageStateProvider>
                    </AsyncExportProvider>
                    );
            case PaneType.reportsPage:
            case PaneType.reportSubPage:
                return (
                    <UserContext.Consumer>
                        {(user) => (
                            <FeatureGuard permissions={[PermissionFeaturesOptions.ReportsView]} fallback={<div className={styles.noAccessMessage}>You do not have permission to view this page.</div>}>
                                <ReportsLayer
                                    pane={pane}
                                    session={props.session}
                                    googleTagManager={props.googleTagManager}
                                    applicationConfiguration={props.applicationConfiguration}
                                    averages={availableAveragesForSubset}
                                    curatedFilters={unchangingFilters}
                                    productConfiguration={props.productConfiguration}
                                    user={user}
                                />
                            </FeatureGuard>
                        )}
                    </UserContext.Consumer>
                );

            case PaneType.settingsPage:
                return (
                    <UserContext.Consumer key={pane.id}>
                        {(user) => {
                            return (
                            <FeatureGuard permissions={[PermissionFeaturesOptions.SettingsAccess]}
                                fallback={<div className="error-message"><h3>You're not authorised to access this page.</h3></div>}>

                                <VariableProvider
                                    user={user}
                                    nonMapFileSurveys={props.productConfiguration.nonMapFileSurveys}
                                    googleTagManager={props.googleTagManager}
                                    pageHandler={props.session.pageHandler}
                                    shouldSetQueryParamOnCreate={true}
                                    isSurveyGroup={props.productConfiguration.isSurveyGroup}>
                                        <SettingsPage
                                            key={pane.id}
                                            googleTagManager={props.googleTagManager}
                                            pageHandler={props.session.pageHandler}
                                            entityConfiguration={props.entityConfiguration}
                                            applicationConfiguration={props.applicationConfiguration}
                                            productConfiguration={props.productConfiguration}
                                            metrics={props.enabledMetricSet}
                                            averages={availableAveragesForSubset}
                                            canAccessRespondentLevelDownload={user?.canAccessRespondentLevelDownload ?? false}
                                        />
                                </VariableProvider>
                            </FeatureGuard>
                            );
                        }}
                    </UserContext.Consumer>
                );
            case PaneType.analysisScorecard:
                if (!activeEntitySet || !props.availableEntitySets) {
                    throw new Error("No entity sets defined");
                }

                return (
                    <>
                        <DashPane
                            key={pane.id}
                            session={props.session}
                            enabledMetricSet={props.enabledMetricSet}
                            entityConfiguration={props.entityConfiguration}
                            googleTagManager={props.googleTagManager}
                            applicationConfiguration={props.applicationConfiguration}
                            productConfiguration={props.productConfiguration}
                            pane={pane}
                            entitySet={activeEntitySet}
                            availableEntitySets={props.availableEntitySets}
                            breaks={props.breaks}
                            getAllInstancesForType={props.getAllInstancesForType}
                            updateMetricResultsSummary={props.updateMetricResultsSummary}
                            updateAverageRequests={props.updateAverageRequests}
                            removeFromLowSample={props.removeFromLowSample}
                        />
                    </>
                );

            case PaneType.allVueWebPage:
                return (<UserContext.Consumer>
                    {(user) =>
                        <AllVueWebPage key={pane.id}
                            applicationConfiguration={props.applicationConfiguration}
                            productConfiguration={props.productConfiguration}
                            name={pane.pageName}
                            user={user }
                        />}</UserContext.Consumer>);

            case PaneType.reportVuePage:
                return (
                    <ReportVuePage
                        key={pane.id}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.session.pageHandler}
                        applicationConfiguration={props.applicationConfiguration}
                        productConfiguration={props.productConfiguration}
                    />
                );

            default:
                return <DashPane
                    key={pane.id}
                    session={props.session}
                    enabledMetricSet={props.enabledMetricSet}
                    entityConfiguration={props.entityConfiguration}
                    googleTagManager={props.googleTagManager}
                    applicationConfiguration={props.applicationConfiguration}
                    productConfiguration={props.productConfiguration}
                    pane={pane}
                    entitySet={throwIfNullish(activeEntitySet, "Active entity set is null")}
                    availableEntitySets={props.availableEntitySets ?? []}
                    breaks={props.breaks}
                    getAllInstancesForType={props.getAllInstancesForType}
                    updateMetricResultsSummary={props.updateMetricResultsSummary}
                    updateAverageRequests={props.updateAverageRequests}
                    removeFromLowSample={props.removeFromLowSample}
                />;
        }
    };

    const getClassName = (layout: string | undefined) => {
        if (!layout) {
            return "chart"
        }

        return layout
    }

    const className = getClassName(props.layout);
    return (
        <div className={className}>
            {
                props.panesToRender.map((pane, index) => getPaneComponent(pane, index))
            }
        </div>
    );
}

interface ICrosstabWithFilterContext {
    session: dsession;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    user: IApplicationUser | null;
    averages: IAverageDescriptor[];
    unchangingFilters: CuratedFilters;
    entitySet: EntitySet | undefined;
    focusInstance: EntityInstance | undefined;
    productConfiguration: ProductConfiguration;
}

//extra component so we can access the metric context immediately
const CrosstabWithFilterContext = (props: ICrosstabWithFilterContext) => {
    const { selectableMetricsForUser: metrics } = useMetricStateContext();

    return (
        <FilterStateProvider initialFilters={[]} metrics={metrics} googleTagManager={props.googleTagManager} pageHandler={props.session.pageHandler}>
            <CrosstabPage
                curatedFilters={props.unchangingFilters}
                isSurveyVue={props.productConfiguration.isSurveyVue()}
                googleTagManager={props.googleTagManager}
                applicationConfiguration={props.applicationConfiguration}
                user={props.user}
                averages={props.averages}
                entitySet={props.entitySet}
                focusInstance={props.focusInstance}
                pageHandler={props.session.pageHandler}
            />
        </FilterStateProvider>
    );
}

export default PageLayout;

function dispatch(arg0: { payload: string; type: "subset/updateSubset"; }) {
    throw new Error("Function not implemented.");
}
