import React from 'react';
import { dsession } from "../dsession";
import {
    IEntityType,
    AverageTotalRequestModel,
    CategorySortKey
} from "../BrandVueApi";
import { PageHandler } from "./PageHandler";
import ViewSelector from "./ViewSelector";
import { EntitySet } from "../entity/EntitySet";
import FilterInstanceDropdownSelector from "./FilterInstanceDropdownSelector";
import { EntityInstance } from "../entity/EntityInstance";
import PageTitleDropdown from "./PageTitleDropdown";
import DropdownSelector from "./dropdown/DropdownSelector";
import {
    getCurrentPageInfo,
    isBrandAnalysisPage,
    isBrandAnalysisSubPage,
    isHtmlImportPage,
    isMetricComparisonPage,
    isAudiencePage,
    isShowInstanceSelectorPage
} from "./helpers/PagesHelper";
import { ContentType } from './helpers/PanelHelper';
import { ProductConfiguration } from '../ProductConfiguration';
import BrandAnalysisSelector from './visualisations/brandanalysis/BrandAnalysisSelector';
import { IGoogleTagManager } from '../googleTagManager';
import { MetricSet } from '../metrics/metricSet';
import { IEntityConfiguration } from '../entity/EntityConfiguration';
import { userCanEditAbouts, pageHasPageAbouts, pageHasMetricAbouts, aboutLink } from './helpers/AboutHelper';
import BaseVariableSelector, { BaseVariablesForComparison } from './dropdown/BaseVariableSelector';
import { ApplicationConfiguration } from '../ApplicationConfiguration';
import { useLocation } from "react-router-dom";
import { getCategorySortKeysQueryString } from "./helpers/CategorySortKeyHelper";
import { selectActiveEntityTypeOrNull } from "client/state/entitySelectionSelectors";
import {
    useAllActiveEntitySetsWithDefault,
    useFilterInstanceWithDefaultOrNull,
    useActiveInstanceWithDefaultOrNull
} from "client/state/entitySelectionHooks";
import { useAppSelector} from "../state/store";
import { useDispatch} from "react-redux";
import { setCategorySortKey, setSplitBy } from "../state/entitySelectionSlice";
import { throwIfNullish } from './helpers/ThrowHelper';
interface IPageTitleProps {
    averageRequests: AverageTotalRequestModel[] | null;
    session: dsession,
    enabledMetricSet: MetricSet;
    entityConfiguration: IEntityConfiguration;
    googleTagManager: IGoogleTagManager;
    productConfiguration: ProductConfiguration;
    pageHandler: PageHandler,
    availableEntitySets: EntitySet[],
    availableSplitByInstances: EntityInstance[];
    toggleSidePanel(contentType: ContentType, entityType?: IEntityType): void;
    updateBaseVariables(variables: BaseVariablesForComparison): void;
    setHeaderText(header: string): void;
    applicationConfiguration: ApplicationConfiguration;
}

const hasAnyActiveMetrics = (session: dsession): boolean => session.activeView.activeMetrics && session.activeView.activeMetrics.length > 0;

const isAnyActiveMetricProfileMetric = (session: dsession): boolean => session.activeView.activeMetrics && session.activeView.activeMetrics.some(x => x.isProfileMetric());

const PageTitle = (props: IPageTitleProps) => {
    const [hasPageAbouts, setHasPageAbouts] = React.useState<boolean>(false);
    const [hasMetricAbouts, setHasMetricAbouts] = React.useState<boolean>(false);
    const location = useLocation();
    const splitByType = useAppSelector(selectActiveEntityTypeOrNull);
    const categorySortKey = useAppSelector((state) => state.entitySelection.categorySortKey);
    const entitySelection = useAllActiveEntitySetsWithDefault(); 
    const mainInstance = useActiveInstanceWithDefaultOrNull();
    const filterInstance = useFilterInstanceWithDefaultOrNull(props.session.activeView.getEntityCombination());
    const dispatch = useDispatch();
    const updateActiveProfileSortedBy = (newValue: string) => {
        dispatch(setCategorySortKey(newValue));
    }

    const activeDashPage = props.session.activeDashPage;
    const activeMetrics = props.session.activeView.activeMetrics;

        React.useEffect(() => {
        pageHasPageAbouts(activeDashPage).then(setHasPageAbouts);
    }, [activeDashPage]);

    React.useEffect(() => {
        if (activeMetrics.length === 1) {
            pageHasMetricAbouts(activeMetrics[0]).then(setHasMetricAbouts);
        }
    }, [activeMetrics[0]]);

    // Hide menu for HTML import screens (i.e. the About page)
    if (isHtmlImportPage(activeDashPage))
        return null;
        
    const isStandardMetricPage = hasAnyActiveMetrics(props.session)
        && !isAnyActiveMetricProfileMetric(props.session)
        && !isMetricComparisonPage(activeDashPage)
        && !isBrandAnalysisPage(activeDashPage)
        && !isBrandAnalysisSubPage(activeDashPage);
    const currentPageInfo = getCurrentPageInfo(location);
    const hasManyViews = currentPageInfo.activeViews.length > 1;

    const entityCombination = props.session.activeView.getEntityCombination();

    const showBrandAnalysisSelector = isBrandAnalysisPage(activeDashPage) || isBrandAnalysisSubPage(activeDashPage);
    const showEntityTypeSelector = entityCombination.length > 1 && !isAudiencePage(activeDashPage);

    const getEntitySetSelector = () => {
        if (isStandardMetricPage) {
            //for now, we just render the first entity selector
            return [entitySelection[0]].map(x=> <div className='page-title-menu' key={x.type.identifier}>
                <div className="page-title-label">{x.type.displayNamePlural}</div>
                <button className="entity-set-toggle-btn" onClick={() => props.toggleSidePanel(ContentType.EntitySetSelector, x.type)}>
                    <div className='entity-set-btn-name'>{x.name}</div>
                    <i className='material-symbols-outlined'>edit</i>
                </button>
            </div>);
        }
    }

    const renderStandardSelectors = () => <>
        {showEntityTypeSelector ? <DropdownSelector<IEntityType>
            label="Split by"
            items={entityCombination}
            selectedItem={throwIfNullish(splitByType, "Split by type")}
            onSelected={setSplitByType}
            itemDisplayText={e => e.displayNameSingular}
            asButton={false}
            showLabel={true}
            itemKey={e => e.identifier}
        /> : null }
        {getEntitySetSelector()}
        {(isStandardMetricPage && isShowInstanceSelectorPage(activeDashPage) && filterInstance) ?
            <FilterInstanceDropdownSelector
                filterInstance={filterInstance}
                activeEntityTypes={props.session.activeView.getEntityCombination()}
            /> : null }
        {hasManyViews && <ViewSelector pageContext={currentPageInfo} />}
        {getAboutLink()}
    </>
    
    const isValidForPageAbouts = () => {
        return userCanEditAbouts(props.productConfiguration) || hasPageAbouts;
    }

    const isValidForMetricAbouts = () => {
        return (userCanEditAbouts(props.productConfiguration) || hasMetricAbouts) && activeMetrics.length === 1 && !isBrandAnalysisSubPage(activeDashPage);
    }

    const getAboutLink = () => {
        if (!props.productConfiguration.isSurveyVue()) {
            if (isValidForMetricAbouts()) {
                return aboutLink("metric", () => props.toggleSidePanel(ContentType.AboutInsights));
            }

            return isValidForPageAbouts() && aboutLink("page", () => props.toggleSidePanel(ContentType.PageAbout));
        }
    }

    const setSplitByType = (splitByEntityType: IEntityType) => {
        dispatch(setSplitBy(splitByEntityType));
    }

    const allCategorySortKeys = Object.keys(CategorySortKey)
        .filter(c => CategorySortKey[c] != CategorySortKey.None)
        .map(s => getCategorySortKeysQueryString(CategorySortKey[s]));
    
    return (
        <div className="not-exported">
            <div className={`page-title ${isBrandAnalysisSubPage(activeDashPage) ? 'full-width' : ''}`}>
                {!isBrandAnalysisSubPage(activeDashPage) || isBrandAnalysisPage(activeDashPage) ? <div className="page-title-menu">
                    <div className="page-title-label">{isStandardMetricPage || hasManyViews ? "Metric" : isBrandAnalysisPage(activeDashPage) ? "\u00A0" : ""}</div>
                    <div className="pointer-and-title">
                        <span className="main-title">{activeDashPage.displayName}</span>
                    </div>
                </div> : null}
                {props.pageHandler.hasCustomerProfilingDropdowns() ?
                    <PageTitleDropdown
                        itemList={allCategorySortKeys}
                        title="Show"
                        activeOption={categorySortKey.toString()}
                        updateSession={updateActiveProfileSortedBy} /> : null
                }
                {showBrandAnalysisSelector ?
                    <BrandAnalysisSelector
                        googleTagManager={props.googleTagManager}
                        focusInstance={throwIfNullish(mainInstance, "Main instance")}
                        availableEntityInstances={props.availableSplitByInstances}
                        pageHandler={props.pageHandler}
                        activeDashPage={activeDashPage}
                        coreViewType={props.session.coreViewType}
                        metrics={props.enabledMetricSet}
                        filters={props.session.filters}
                        comparisons={props.session.comparisons}
                        averageRequests={props.averageRequests}
                        selectedSubsetId={props.session.selectedSubsetId}
                        viewBase={props.session.activeView}
                        splitByEntityType={throwIfNullish(splitByType, "Split by type")}
                        entityConfiguration={props.entityConfiguration}
                        availableEntitySets={props.availableEntitySets}
                        toggleSidePanel={props.toggleSidePanel}
                        productConfiguration={props.productConfiguration}
                /> : renderStandardSelectors()}
            </div>
            {isAudiencePage(activeDashPage) &&
                <BaseVariableSelector
                    focusInstance={throwIfNullish(mainInstance, "Main instance")}
                    updateBaseVariables={props.updateBaseVariables}
                    setHeaderText={props.setHeaderText}
                    defaultBaseVariableIdentifier={activeDashPage.defaultBase}
                />}
        </div>
    );
}

export default PageTitle;