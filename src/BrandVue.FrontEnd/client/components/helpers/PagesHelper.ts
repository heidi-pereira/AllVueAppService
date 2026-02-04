import { PaneType } from '../panes/PaneType';
import { PartType } from '../panes/PartType';
import { IAverageDescriptor, PageDescriptor, PaneDescriptor} from '../../BrandVueApi';
import {
     constructQueryString,
    getPathByPageName,
    IQueryParamModifiers, IReadVueQueryParams,
    parseBrandVuePath, QueryStringParamNames, replaceQueryParams
} from "./UrlHelper";
import { allViewTypes, getViewTypeByNameOrUrl, ViewType, ViewTypeEnum } from "./ViewTypeHelper";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { IPart } from "../../parts/IPart";
import { getStartDateForDefault13MonthPeriod } from "./PeriodHelper";
import { getEndOfLastMonthWithData } from "./DateHelper";
import { CompletePeriod } from "../../helpers/CompletePeriod";
import { IEntityConfiguration } from "../../entity/EntityConfiguration";
import { Location } from "react-router-dom";
import { IActiveBreaks } from "../../state/entitySelectionSlice";
import { isAudienceActive } from "./AudienceHelper";

let pages : PageDescriptor[] = [];
let activePage: PageDescriptor;

export interface IPageContext {
    page: PageDescriptor;
    viewMenuItem?: ViewType;
    pagePart: string;
    activeViews: ViewType[];
}

export interface IPageUrlOptions extends IQueryParamModifiers {
    viewTypeNameOrUrl?: string;
    ignoreQuery?: boolean;
}

/**
 * This method is called by dsession when we get the pages from the API.
 * This way we can gradually remove the dependency on session.pages and use (almost) pure helper functions in this file.
 */
export const initialisePages = (pagesInSession: PageDescriptor[]) => {
    pages = pagesInSession;
}

/**
 * This method is called by dsession whenever we change the active page.
 * This way we can gradually remove the dependency on session.activeDashPage.
 */
export const setActivePage = (newActivePage: PageDescriptor) => {
    activePage = newActivePage;
}

export const getActivePage = () => activePage;

export const getStartPage = (): PageDescriptor => {
    return pages.find(page => page.startPage) || pages[0];
};

export const getReportsPage = (): PageDescriptor | undefined => {
    return pages.find(page => page.panes.some(pane => pane.paneType === PaneType.reportsPage));
}

const findPageTree = (pages: PageDescriptor[], predicate: (page: PageDescriptor) => boolean): PageDescriptor[] => {
    let result: PageDescriptor[] = [];
    for (let parentPage of pages) {
        if (predicate(parentPage)) {
            result = [parentPage];
        } else {
            result = findPageTree(parentPage.childPages, predicate);
            if (result.length) {
                // Insert parent page before the child page
                result.unshift(parentPage);
            }
        }
        if (result.length) {
            break;
        }
    }
    return result;
}

/**
 * Finds the first page tree from parent page to the page containing competition view of the given metric.
 */
export const findPageTreeByMetricName = (pages: PageDescriptor[], metricName: string): PageDescriptor[] => {
    return findPageTree(pages, page => isPageTheMetricPage(page, metricName));
};

/**
 * Finds the first page tree from parent page to the page with the specified name.
 */
export const findPageTreeByPageName = (pages: PageDescriptor[], pageName: string): PageDescriptor[] => {
    return findPageTree(pages, page => page.name.toLowerCase() === pageName.toLowerCase());
};

/**
 * Finds the first page tree from parent page to the page with the specified display name.
 */
export const findPageTreeByPageDisplayName = (pages: PageDescriptor[], pageDisplayName: string, matchIfStartsWith: boolean): PageDescriptor[] => {
     return findPageTree(pages, page => doesPageMatchTheDisplayName(page, pageDisplayName, matchIfStartsWith));
};

/**
 * Finds the page by name in a hierarchy of pages
 */
export const findPageByPageName = (pages: PageDescriptor[], pageName: string): PageDescriptor => {
    const pageTree = findPageTreeByPageName(pages, pageName);
    return pageTree[pageTree.length - 1];
};

/**
 * The page is defined as 'being a metric page' if it contains precisely one Competition view with one part which describes this metric.
 */
const isPageTheMetricPage = (page: PageDescriptor, metricName: string): boolean => {
    return page.panes.filter(p => p.view === ViewTypeEnum.Competition && p.paneType != "PartColumn" && p.parts.length === 1 && p.parts[0].spec1 === metricName).length === 1;
}

const doesPageMatchTheDisplayName = (page: PageDescriptor, pageDisplayName: string, matchIfStartsWith: boolean): boolean => {
    let foundMatch = page.displayName.toLowerCase() === pageDisplayName.toLowerCase();
    if (!foundMatch && matchIfStartsWith) {
        foundMatch = page.displayName.toLowerCase().startsWith(pageDisplayName.toLowerCase());
    }
    return foundMatch;
}

/**
 * Page always has filters unless all of its panes don't have filters.
 */
export const doesPageHaveFilters = (page: PageDescriptor) => {
    if (!page.panes || page.panes.length === 0) {
        return true;
    }

    return page.panes.some(p => PaneType.needsFilters(p.paneType));
}

export const isCrosstabPage = (page: PageDescriptor) => {
    if (!page.panes || page.panes.length === 0) {
        return false;
    }

    return page.panes.some(p => p.paneType === PaneType.crossTabPage);
}

export const isSurveyVueEntryPage = (page: PageDescriptor) => {
    const viewType = page.panes[0]?.view;
    return viewType === ViewTypeEnum.SingleSurveyNav;
}

export const pageListToUrl = (pages: PageDescriptor[]): string => {
    return pages.map(p => getPathByPageName(p.name)).join('');
}

export const dataPageUrl = (urlSafeMetricName?: string): string => {
    const base = getPathByPageName("Crosstabbing");

    if (!urlSafeMetricName) {
        return base;
    }
    //todo: test
    const search = constructQueryString("", [{name: QueryStringParamNames.urlSafeMetricName, value: urlSafeMetricName}]);
    return `${base}${search}`;
}

export const settingsPageUrl = (): string => {
    return getPathByPageName("Settings");
}

export const reportVuePageUrl = (urlPart: string): string => {
    return getPathByPageName(urlPart)
}

export const allVueWebPageUrl = (urlPart: string): string => {
    return getPathByPageName(urlPart)
}

export const usersPageUrl = (location, readVueQueryParams: IReadVueQueryParams,): string => {
    return getUrlForPageName("Users", location, readVueQueryParams, { ignoreQuery: true });
}

export const weightingPageUrl = (location, readVueQueryParams: IReadVueQueryParams,): string => {
    return getUrlForPageName("Weighting",location, readVueQueryParams, { ignoreQuery: true });
}

export const exportsPageUrl = (location, readVueQueryParams: IReadVueQueryParams,): string => {
    return getUrlForPageName("Exports", location, readVueQueryParams,{ ignoreQuery: true });
}

export const productConfigurationPageUrl = (location, readVueQueryParams: IReadVueQueryParams,): string => {
    return getUrlForPageName("Configuration", location, readVueQueryParams,{ ignoreQuery: true });
}
export const surveyConfigurationPageUrl = (location, readVueQueryParams: IReadVueQueryParams,): string => {
    return getUrlForPageName("SurveyConfiguration", location, readVueQueryParams, { ignoreQuery: true });
}

export const featuresPageUrl = (location, readVueQueryParams: IReadVueQueryParams,): string => {
    return getUrlForPageName("Features", location, readVueQueryParams,{ ignoreQuery: true });
}

export const getActiveViewsForPage = (p: PageDescriptor | null | undefined): ViewType[] => {
    const activeViews: ViewType[] = [];
    if (p) {
        for (let menuIndex = 0; menuIndex < allViewTypes.length; menuIndex++) {
            if (p.panes.filter(pane => pane.view === allViewTypes[menuIndex].id).length > 0) {
                activeViews.push(allViewTypes[menuIndex]);
            }
        }
    }

    return activeViews;
}

export const getPageFromUrl = (url: string): PageDescriptor | null => {

    let pageDescriptor = new PageDescriptor();
    pageDescriptor.disabled = true;
    pageDescriptor.childPages = pages;
    for (let part of url.split('/')) {
        if (!part.length) {
            continue;
        }

        if (!pageDescriptor.childPages) {
            break;
        }

        const foundPageDescriptor = pageDescriptor.childPages.find(page => getPathByPageName(page.name) === "/" + part);
        if (!foundPageDescriptor) {
            break;
        }
        pageDescriptor = foundPageDescriptor;
    }

    return pageDescriptor.disabled ? null : pageDescriptor;
}

export const getCurrentPageInfo = (location: Location): IPageContext => {
    return getPageInfo(location.pathname);
}

export const getPageInfo = (location: string): IPageContext => {
    const parsedPath = parseBrandVuePath(location);
    const page = getPageFromUrl(parsedPath.RequestedPageUrl) || getStartPage();
    const allValidViewsForPage = getActiveViewsForPage(page);
    const activeView = allValidViewsForPage.find(v => v.url === parsedPath.RequestedViewUrl)
        || allValidViewsForPage.find(v => v.id == page.defaultPaneViewType)
        || allValidViewsForPage[0]
        || allViewTypes[0];

    return {
        page: page,
        viewMenuItem: activeView,
        pagePart: parsedPath.RequestedPageUrl,
        activeViews: getActiveViewsForPage(page)
    }
}

export const getActiveViewUrl = (linkLocation: string, location: Location, viewTypeNameOrUrl?: string) => {
    const currentPageInfo = getCurrentPageInfo(location);
    const newPageInfo = getPageInfo(linkLocation);
    const activeViews = getActiveViewsForPage(newPageInfo.page);

    const requestedViewType = viewTypeNameOrUrl ? getViewTypeByNameOrUrl(viewTypeNameOrUrl) : currentPageInfo.viewMenuItem;

    let activeViewUrlPart: string;
    if (requestedViewType && activeViews.indexOf(requestedViewType) >= 0) {
        activeViewUrlPart = requestedViewType.url;
    }
    else {
        activeViewUrlPart = activeViews.length ? activeViews[0].url : "";
    }

    return linkLocation + activeViewUrlPart;
}

const getUrlFromPageTree = (pageTree: PageDescriptor[], location: Location, readVueQueryParams: IReadVueQueryParams, pageUrlModifiers?: IPageUrlOptions): string | undefined => {
    const modifiers = pageUrlModifiers || {};
    if (!pageTree.length) {
        return undefined;
    }
    const url = getActiveViewUrl(pageListToUrl(pageTree), location,modifiers.viewTypeNameOrUrl);
    if (modifiers.ignoreQuery)
        return url;

    const query = replaceQueryParams(window.location.search, modifiers, readVueQueryParams);
    return url + query;
}


export const getCuratedFiltersForAverageId = (average: IAverageDescriptor | null, defaultFilters: CuratedFilters, dateOfLastDataPoint: Date, entityConfiguration: IEntityConfiguration) => {
    if (average == null) {
        return defaultFilters;
    }
    return CuratedFilters.createWithOptions({
        endDate: CompletePeriod.getLastDayInLastCompletePeriod(dateOfLastDataPoint, average.makeUpTo),
        average: average,
        comparisonPeriodSelection: defaultFilters.comparisonPeriodSelection,
    }, entityConfiguration);
}
export const convertToUrl = (filters: CuratedFilters, dateOfFirstDataPoint: Date, dateOfLastDataPoint: Date, entityConfiguration: IEntityConfiguration) =>
    CuratedFilters.createWithOptions({
        endDate: filters.endDate,
        average: filters.average,
        startDate: getStartDateForDefault13MonthPeriod(getEndOfLastMonthWithData(dateOfLastDataPoint), getEndOfLastMonthWithData(dateOfFirstDataPoint)),
        comparisonPeriodSelection: filters.comparisonPeriodSelection,
    }, entityConfiguration);

export const getCardLinkByMetricOrPageName = (nextPageName: string, partConfig: IPart, location: Location, readVueQueryParams: IReadVueQueryParams, curatedFilters?: CuratedFilters, viewTypeNameOrUrl?: string): string => {
    if (nextPageName == null || nextPageName.length == 0) {
        throw new Error(`Could not get next page url for ${partConfig.descriptor.paneId} - check that spec1/spec3 are properly set`)
    }
    return getUrlForMetricOrPageDisplayName(nextPageName, location,readVueQueryParams,
        {
            filters: curatedFilters,
            averageId: partConfig.descriptor.defaultAverageId,
            viewTypeNameOrUrl: viewTypeNameOrUrl,
        });
}

export const getUrlForMetricOrPageDisplayName = (metricName: string, location: Location, readVueQueryParams: IReadVueQueryParams, pageUrlModifiers?: IPageUrlOptions): string => {
    const pageTree = findPageTreeByMetricName(pages, metricName);
    return getUrlFromPageTree(pageTree, location,readVueQueryParams, pageUrlModifiers) || 
        getUrlForPageDisplayName(metricName, location, readVueQueryParams, pageUrlModifiers);
}

export const getUrlForPageName = (pageName: string, location: Location, readVueQueryParams: IReadVueQueryParams, pageUrlModifiers?: IPageUrlOptions): string => {
    const pageTree = findPageTreeByPageName(pages, pageName);
    return getUrlFromPageTree(pageTree, location, readVueQueryParams, pageUrlModifiers) || "/";
}

export const getUrlForPageDisplayName = (pageDisplayName: string, location: Location, readVueQueryParams: IReadVueQueryParams, pageUrlModifiers?: IPageUrlOptions): string => {
    const pageTree = getPageTreeForDisplayName(pageDisplayName);
    return getUrlFromPageTree(pageTree,location, readVueQueryParams, pageUrlModifiers) || "/";
}

export const getMetricNamesForPanes = (panes: PaneDescriptor[]): string[] => {
    return panes.reduce((paneMetricArray, pane) => {
            const partMetricArray = pane.parts.reduce(
                (partMetricArray, part) => partMetricArray.concat(part.spec1.split('|')),
                ([] as string[]));
            return paneMetricArray.concat(partMetricArray);
        },
        ([] as string[]));
}


export const getPageTreeForDisplayName = (pageDisplayName: string): PageDescriptor[] => {
    let pageTree = findPageTreeByPageDisplayName(pages, pageDisplayName, false);
    if (pageTree.length == 0) {
        pageTree = findPageTreeByPageDisplayName(pages, pageDisplayName, true);
    }
    return pageTree;
}

const doesPageContainPaneType = (page: PageDescriptor, paneType: string) => {
    return page.panes && page.panes.some(p => p.paneType === paneType);
}

const doesPageContainPartType = (page: PageDescriptor, partType: string) => {
    return page.panes && page.panes.some(p => p.parts.some(q => q.partType === partType));
}

const doesPageContainAnyPaneTypes = (page: PageDescriptor, paneTypes: string[]) => {
    return paneTypes.some(pt => doesPageContainPaneType(page, pt));
}

const doesPageContainAllPaneType = (page: PageDescriptor, paneType: string) => {
    return page.panes.length > 0 && page.panes.every(p => p.paneType === paneType);
}

export const shouldPanesRestrictToCurrentPeriod = (activeBreaks: IActiveBreaks, panes: PaneDescriptor[]): boolean => {
    return isAudienceActive(activeBreaks) &&
        panes.some(pane => pane.parts.some(part => part.partType === "ColumnChart"));
}

export const isMetricComparisonPage = (page: PageDescriptor) : boolean => doesPageContainPaneType(page, PaneType.metricComparison);

export const isBrandAnalysisPage = (page: PageDescriptor): boolean => doesPageContainPaneType(page, PaneType.analysisScorecard);

export const isBrandAnalysisSubPage = (page: PageDescriptor): boolean => doesPageContainAnyPaneTypes(page, [PaneType.brandAdvocacy, PaneType.brandBuzz, PaneType.brandLove, PaneType.brandUsage]);

export const isHtmlImportPage = (page: PageDescriptor): boolean => doesPageContainPaneType(page, PaneType.import);

export const isAudiencePage = (page: PageDescriptor): boolean => doesPageContainAllPaneType(page, PaneType.audienceProfile);

export const isShowInstanceSelectorPage = (page: PageDescriptor): boolean => !doesPageContainPartType(page, PartType.MultiEntityCompetition);

/*
 * Multi-tile pages are e.g. Home, Brand Performance and Audience pages.
 * They are special because they can combine different: metrics, entity sets, filters, periods, etc.
 * They rely more on their pages/panes/parts config, rather than current query params.
 */
export const isActivePageMultiTilePage = (): boolean => doesPageContainAnyPaneTypes(activePage, [PaneType.audienceProfile, PaneType.partColumn, PaneType.partGrid]);