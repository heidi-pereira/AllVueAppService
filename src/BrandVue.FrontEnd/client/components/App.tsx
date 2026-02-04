import React from 'react';
import { useState } from 'react';
import { dsession } from "../dsession";
import DashBoard from "./DashBoard";
import PageTitle from "./PageTitle";
import {
    AverageTotalRequestModel,
    IEntityType,
    PageDescriptor, EntityType
} from "../BrandVueApi";
import { IEntityConfiguration } from "../entity/EntityConfiguration";
import TopNav from './TopNav';
import Footer from './Footer';
import { IEntitySetFactory } from "../entity/EntitySetFactory";
import {
    doesPageHaveFilters,
    isBrandAnalysisPage,
    isBrandAnalysisSubPage,
    isCrosstabPage,
    isSurveyVueEntryPage,
    getStartPage,
    getCurrentPageInfo
} from './helpers/PagesHelper';
import {
    IQueryStringParam,
    QueryStringParamNames, useWriteVueQueryParams,
} from './helpers/UrlHelper';
import { Toaster } from 'react-hot-toast';
import SurveyVueEntryPage from './SurveyVueEntryPage';
import { isBarometer, isColourConfigurationEnabled } from './helpers/FeaturesHelper';
import { PaneType } from './panes/PaneType';
import BrandVueSidePanel from './BrandVueSidePanel';
import BrandVueSidePanelEntitySetSelector, { IBrandVueSidePanelEntitySetSelectorProps } from './BrandVueSidePanelEntitySetSelector';
import BrandVueSidePanelAbout, { IBrandVueSidePanelAboutProps } from './BrandVueSidePanelAbout';
import { getActiveAudienceBreaks } from './helpers/AudienceHelper';
import { useMetricStateContext } from '../metrics/MetricStateContext';
import { useSavedBreaksContext } from './visualisations/Crosstab/SavedBreaksContext';
import { ContentType, BrandVueSidePanelContent, defaultBrandVueSidePanelContent } from './helpers/PanelHelper';
import { MetricResultsSummary } from './helpers/MetricInsightsHelper';
import { ApplicationConfiguration } from '../ApplicationConfiguration';
import { ProductConfiguration } from '../ProductConfiguration';
import { analysisScorecardFooter } from './helpers/AnalysisHelper';
import { userCanEditAbouts } from './helpers/AboutHelper';
import { BaseVariablesForComparison } from './dropdown/BaseVariableSelector';
import { PageLoadIndication } from "../PageLoadIndication";
import { MixPanel } from './mixpanel/MixPanel';
import { useLocation, useNavigate } from "react-router-dom";
import { useAppSelector } from "../state/store";
import { useActiveBrandSetWithDefault, useActiveEntitySetWithDefaultOrNull, useSelectedBreaks } from "client/state/entitySelectionHooks";
import {useEntityConfigurationStateContext} from "../entity/EntityConfigurationStateContext";
import {useTagManager} from "../TagManagerContext";

interface IAppProps {
    session: dsession,
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
    entityConfiguration: IEntityConfiguration,
    entitySetFactory: IEntitySetFactory,
    activePage: PageDescriptor;
}

const App = (props: IAppProps) => {
    const activeEntityTypes = useAppSelector(x=>x.entitySelection.priorityOrderedEntityTypes);
    const activeEntityType = activeEntityTypes[0];
    const activeEntitySet = useActiveEntitySetWithDefaultOrNull();
    const activeBrandSet = useActiveBrandSetWithDefault();
    const [isSidePanelOpen, setSidePanelOpen] = useState(false);
    const [sidePanelEntityType, setSidePanelEntityType] = useState(activeEntityType);
    const [sidePanelContentType, setSidePanelContentType] = useState<ContentType>(ContentType.None);
    const [metricResultsSummary, setMetricResultsSummary] = useState<MetricResultsSummary>();
    const { enabledMetricSet, questionTypeLookup } = useMetricStateContext();
    const [averageRequests, setAverageRequests] = useState<AverageTotalRequestModel[] | null>(null);
    const { savedBreaks } = useSavedBreaksContext();
    const location = useLocation();
    const [headerText, setHeaderText] = useState<string>("");
    const pageHandler = props.session.pageHandler;
    const entityConfigurationContext = useEntityConfigurationStateContext();
    const availableEntitySets = activeEntityType ? props.entityConfiguration.getSetsFor(activeEntityType) : [];
    const googleTagManager = useTagManager(); 
    const { setQueryParameters } = useWriteVueQueryParams(useNavigate(), useLocation());
    const entityInstances = props.entityConfiguration.getAllEnabledInstancesOrderedAsSet(activeEntityType);        
    const allEntities = activeEntityType ? entityInstances : undefined;
    const activeBreaks = useSelectedBreaks();
    
    PageLoadIndication.instance.postLoadEvent = () => {
        let values = pageHandler.getApplicationStateForGoogleAnalytics(location);
        if (values != null && PageLoadIndication.instance.xhrStart != null) {
            values.pageLoadTiming = Date.now() - PageLoadIndication.instance.xhrStart;
            const pageName = props.activePage.name;
            const activeView = props.session.activeView;
            if (pageName && pageName.trim() !== '')
                MixPanel.trackPageLoadTime(
                    {
                        PageLoadTime: values.pageLoadTiming,
                        PageName: pageName,
                        EntitySet: activeEntitySet?.id,
                        Instances: activeEntitySet?.isCustomSet ? activeEntitySet.getInstances().getAll().map(x=>x.id).sort() : [],
                        Average: activeView.curatedFilters.average.averageId,
                        Metric: activeView.activeMetrics?.[0]?.name,
                        DateStart: activeView.curatedFilters.startDate.toISOString().substring(0, 10),
                        DateEnd: activeView.curatedFilters.endDate.toISOString().substring(0,10) },
                    activeView.curatedFilters.measureFilters);
        }
        googleTagManager.addEvent("xhrFinish", pageHandler, values)
    }

    React.useEffect(() => {
        setSidePanelOpen(false);

        // Assumption: The help text is the title for pages with layout
        const helpText = props.activePage.pageSubsetConfiguration.find(p => p.subset == props.session.pageHandler.session.selectedSubsetId)?.helpText ?? props.activePage.helpText;
        setHeaderText(props.activePage.layout && helpText?.replace("{{instance}}", activeEntitySet?.mainInstance?.name ?? "").trim());
    }, [props.activePage.name]);
    
    const availableActiveInstances = activeEntityType ? entityConfigurationContext.entityConfiguration.getAllEnabledInstancesForType(activeEntityType) : [];
    if (isSurveyVueEntryPage(props.activePage)) {
        return <SurveyVueEntryPage
            session={props.session}
            enabledMetricSet={enabledMetricSet}
            entityConfiguration={props.entityConfiguration}
            googleTagManager={googleTagManager}
            applicationConfiguration={props.applicationConfiguration}
            productConfiguration={props.productConfiguration}
            entitySet={allEntities}
            splitByType={activeEntityType}
            availableSplitByInstances={availableActiveInstances}
            updateAverageRequests={setAverageRequests}
        />
    }
    
    // Ensure the active entity set is part of the available entity sets - for temporary entity sets created from the URL this needs to be added so that the average line works.
    if (activeEntitySet && !availableEntitySets.find(s => s.id === activeEntitySet.id)) {
        availableEntitySets.push(activeEntitySet);
    }
    const audienceBreaks = getActiveAudienceBreaks(activeBreaks, savedBreaks, enabledMetricSet.metrics, questionTypeLookup);

    const shouldDisplayTopControls = doesPageHaveFilters(props.activePage);
    const isCrosstab = isCrosstabPage(props.activePage);
    const isBrandAnalysis = isBrandAnalysisPage(props.activePage);
    const isBrandAnalysisSub = isBrandAnalysisSubPage(props.activePage);

    const panesToRender = pageHandler.getPanesToRender();
    const renderSetSelector = panesToRender.some(p => PaneType.shouldShowEntitySetSelector(p.paneType));

    const closeSidePanel = () => {
        if (sidePanelContentType === ContentType.AboutInsights)
            MixPanel.track("aboutMetricClosed")
        setSidePanelContentType(ContentType.None);
        setSidePanelOpen(false);
    }

    const openSidePanel = (contentType: ContentType, entityType: EntityType) => {
        setSidePanelContentType(contentType);
        setSidePanelEntityType(entityType);
        setSidePanelOpen(true);
    }

    const toggleSidePanel = (contentType: ContentType, entityType: EntityType) => {
        if (contentType === ContentType.EntitySetSelector) {
            MixPanel.track("entitySelectorOpened")
        }
        if (contentType === ContentType.AboutInsights) {
            MixPanel.track("aboutMetricOpened")
        }
        if (isSidePanelOpen) {
            closeSidePanel();
            if (contentType !== sidePanelContentType || entityType?.identifier != sidePanelEntityType?.identifier) {
                openSidePanel(contentType, entityType);
            }
        }
        else if (!isSidePanelOpen) {
            openSidePanel(contentType, entityType);
        }
    }

    const shouldRenderSidePanel = () => {
        if (sidePanelContentType === ContentType.EntitySetSelector) {
            return renderSetSelector;
        }
        return true;
    }

    const sidePanelOpen = isSidePanelOpen && shouldRenderSidePanel();
    const getSidePanelContent = (contentType: ContentType, entityType: IEntityType): BrandVueSidePanelContent => {
        switch (contentType) {
            case ContentType.EntitySetSelector:
                const entitySetSelectorProps: IBrandVueSidePanelEntitySetSelectorProps = {
                    isOpen: sidePanelOpen,
                    close: closeSidePanel,
                    entityType: entityType,
                    entitySets: props.entityConfiguration.getSetsFor(entityType),
                    availableInstances: availableActiveInstances,
                    isBarometer: isBarometer(props.productConfiguration),
                    isColourConfigEnabled: isColourConfigurationEnabled(props.productConfiguration),
                    googleTagManager: googleTagManager,
                    pageHandler: props.session.pageHandler,
                    productConfiguration: props.productConfiguration
                };
                return BrandVueSidePanelEntitySetSelector(entitySetSelectorProps, activeBreaks);

            case ContentType.AboutInsights:
                const insightsProps: IBrandVueSidePanelAboutProps = {
                    isOpen: sidePanelOpen,
                    close: closeSidePanel,
                    metric: props.session.activeView.activeMetrics[0],
                    userCanEdit: userCanEditAbouts(props.productConfiguration),
                    brand: activeBrandSet?.getMainInstance()?.name ?? undefined,
                    activeEntitySet: activeEntitySet ?? undefined,
                    metricResultsSummary: metricResultsSummary,
                    page: props.activePage,
                    parts: pageHandler.getPanesToRender().flatMap(p => p.parts),
                    contentType: contentType,
                };
                return BrandVueSidePanelAbout(insightsProps, getCurrentPageInfo(location).viewMenuItem);

            case ContentType.PageAbout:
                const aboutProps: IBrandVueSidePanelAboutProps = {
                    isOpen: sidePanelOpen,
                    close: closeSidePanel,
                    metric: props.session.activeView.activeMetrics[0],
                    userCanEdit: userCanEditAbouts(props.productConfiguration),
                    brand: activeBrandSet?.getMainInstance()?.name ?? undefined,
                    activeEntitySet: activeEntitySet ?? undefined,
                    metricResultsSummary: metricResultsSummary,
                    page: props.activePage,
                    parts: pageHandler.getPanesToRender().flatMap(p => p.parts),
                    contentType: contentType,
                };
                return BrandVueSidePanelAbout(aboutProps, getCurrentPageInfo(location).viewMenuItem);
        }

        return defaultBrandVueSidePanelContent;
    }

    const updateBaseVariables = (baseVariables: BaseVariablesForComparison) => {
        const paramsToUpdate: IQueryStringParam[] = [
            { name: QueryStringParamNames.baseVariableId1, value: baseVariables.baseVariable1?.id },
            { name: QueryStringParamNames.baseVariableId2, value: baseVariables.baseVariable2?.id }
        ];
        setQueryParameters(paramsToUpdate);
    }

    return (
        <div className="content-scroll-template" id="content-scroll-template">
            <Toaster position='bottom-center' toastOptions={{ duration: 5000 }} />
            <TopNav startPage={getStartPage()} pages={props.session.pages} applicationConfiguration={props.applicationConfiguration} productConfiguration={props.productConfiguration}
                googleTagManager={googleTagManager} pageHandler={props.session.pageHandler} enabledMetricSet={enabledMetricSet} />
            <div className="page-content-disabled"></div>
            <div className="scrollable-content">
                <div className={`title-and-chart ${isCrosstab ? 'full-height' : ''} ${isBrandAnalysisSub ? 'full-width white' : ''}`}>
                    {shouldDisplayTopControls && <PageTitle session={props.session}
                        enabledMetricSet={enabledMetricSet}
                        entityConfiguration={props.entityConfiguration}
                        googleTagManager={googleTagManager}
                        productConfiguration={props.productConfiguration}
                        pageHandler={pageHandler}
                        availableEntitySets={availableEntitySets}
                        availableSplitByInstances={availableActiveInstances}
                        toggleSidePanel={toggleSidePanel}
                        averageRequests={averageRequests}
                        updateBaseVariables={updateBaseVariables}
                        setHeaderText={setHeaderText}
                        applicationConfiguration={props.applicationConfiguration}
                    />}
                    <DashBoard session={props.session}
                        enabledMetricSet={enabledMetricSet}
                        entityConfiguration={props.entityConfiguration}
                        averageRequests={averageRequests}
                        googleTagManager={googleTagManager}
                        applicationConfiguration={props.applicationConfiguration}
                        productConfiguration={props.productConfiguration}
                        pageHandler={pageHandler}
                        breaks={audienceBreaks}
                        getAllInstancesForType={(type: IEntityType) => props.entityConfiguration.getAllEnabledInstancesForType(type)}
                        shouldDisplayTopControls={shouldDisplayTopControls}
                        availableEntitySets={availableEntitySets}
                        availableSplitByInstances={availableActiveInstances}
                        focusInstance={activeEntitySet?.getMainInstance() ?? null}
                        updateMetricResultsSummary={setMetricResultsSummary}
                        updateAverageRequests={setAverageRequests}
                        headerText={headerText}
                    />
                </div>
                <BrandVueSidePanel
                    isOpen={sidePanelOpen}
                    close={closeSidePanel}
                    panelContent={getSidePanelContent(sidePanelContentType, sidePanelEntityType)}
                />
                {isBrandAnalysis && analysisScorecardFooter(props.productConfiguration.cdnAssetsEndpoint, props.productConfiguration.productName)}
                <Footer />
            </div>
        </div>
    );
}

export default App;