import {dsession} from "../dsession";
import QueryString from 'query-string';
import {DataSubsetManager} from "../DataSubsetManager";
import {IFilterStateCondensed, MetricFilterState} from '../filter/metricFilterState';
import {FilterValueMapping as MetricFilter, MetricSet} from "../metrics/metricSet";
import {Metric} from "../metrics/metric";
import {EntityInstance} from "../entity/EntityInstance";
import {CalculationType, PageDescriptor, PaneDescriptor} from "../BrandVueApi";
import {ICommonVariables} from "../googleTagManager";
import moment from "moment";
import {IEntityConfiguration} from "../entity/EntityConfiguration";
import {PaneType} from "./panes/PaneType";
import {getCurrentPageInfo, getMetricNamesForPanes, getPageInfo, setActivePage} from "./helpers/PagesHelper";
import {
    IQueryParamModifiers, IQueryStringParam, IReadVueQueryParams,
    QueryStringParamNames,
    replaceQueryParams
} from "./helpers/UrlHelper";
import { EntitySet } from "../entity/EntitySet";
import { isCustomPeriodAverage } from "./helpers/PeriodHelper";
import { allViewTypes } from "./helpers/ViewTypeHelper";
import { GroupFilterConfiguration } from "../filter/GroupFilterConfiguration";
import { Location } from "react-router-dom";
import { isAudienceActive } from "./helpers/AudienceHelper";
import { IActiveBreaks } from "../state/entitySelectionSlice";

export class PageHandler {
    session: dsession;
    // The active entity set is only needed for getApplicationStateForGoogleAnalytics and we'll hopefully get rid of it at some point.
    private activeEntitySet: EntitySet;
    constructor(session: dsession) {
        this.session = session;
    }

    public setActiveEntitySet(entitySet: EntitySet) : void {
        this.activeEntitySet = entitySet;
    }
    
    public getQueryStringParameter(name: string, location: Location): string[] {

        const parsed = QueryString.parse(location.search);
        let value = parsed[name.replace(/\+/g, " ")];
        if (value == undefined) {
            value = parsed[name];
        }

        // This should be a string, but it can be something else
        // the query parameter is repeated which can happen
        // if there's a bug elsewhere in the code.
        //
        if (typeof value === 'string') {
            value = value.replace(/\+/g, " ");
            value = value.split(".");
        }
        return value;
    }

    public updateActiveDashPage(enabledMetricSet: MetricSet, crossTabPageMetrics: Metric[], location: Location, getQueryParameter: <T = string>(parameterName: string, defaultValue?: T) => T | undefined) {
        const pageParts = getCurrentPageInfo(location);
        const activePage = pageParts.page;

        if (activePage) {
            setActivePage(activePage);
            const viewType = pageParts.viewMenuItem ? pageParts.viewMenuItem : allViewTypes[0];
            this.session.coreViewType = viewType.id;
            if (this.pageRespectsMetricUrlParam(activePage)) {
                this.session.activeView.activeMetrics = this.getActiveMetricsFromQueryParam(enabledMetricSet, crossTabPageMetrics, getQueryParameter);
            } else {
                this.session.activeView.activeMetrics = this.searchForMetricsOnPage(activePage, enabledMetricSet);
            }
            this.session.setCurrentAverageDescriptorFromURL(getQueryParameter);
            this.session.setCurrentPeriodSelectionFromURL(getQueryParameter);
        }
    }

    /*
        We use a query parameter to track what metric is currently in use when multiple metrics can be displayed
        At the moment this is only used for the Crosstab page
    */
    private pageRespectsMetricUrlParam(page: PageDescriptor | undefined) {
        return page && page.panes && page.panes.some(p => p.paneType === PaneType.crossTabPage);
    }

    private getActiveMetricsFromQueryParam(enabledMetricSet: MetricSet, crossTabPageMetrics: Metric[], getQueryParameter: <T = string>(parameterName: string, defaultValue?: T) => T | undefined) {
        const urlSafeMetricName = getQueryParameter<string>(QueryStringParamNames.urlSafeMetricName);
        return urlSafeMetricName && enabledMetricSet.getMetricsByUrlSafeName(urlSafeMetricName)
            || crossTabPageMetrics.filter(m => !m.disableMeasure && m.eligibleForCrosstabOrAllVue).slice(0, 1);
    }

    private searchForMetricsOnPage(page: PageDescriptor, enabledMetricSet: MetricSet): Metric[] {
         const metrics = page.panes
            .flatMap(pane => pane.parts)
            .flatMap(part => enabledMetricSet.getMetrics(part.spec1))
            .filter(metric => metric);
        return [...new Set(metrics)];
    };

    public getPageQuery(pageUrl: string, currentQuery: string, enabledMetricSet: MetricSet, readVueQueryParams: IReadVueQueryParams): string {
        const pageParts = getPageInfo(pageUrl);
        if (pageParts != null && pageParts.page != null && pageParts.page.panes[0] != null) {
            const pane = pageParts.page.panes[0];
            const part = pane.parts[0];
            if (part) {
                const activeMetrics = enabledMetricSet.getMetrics(part.spec1);
                if (activeMetrics != null && activeMetrics.length > 0 && activeMetrics[0] != null) {
                    const canSetAverageForPane = ![ PaneType.partColumn, PaneType.partGrid ].includes(pane.paneType);
                    const paramModifiers: IQueryParamModifiers = {
                        averageId: canSetAverageForPane ? part.defaultAverageId : undefined,
                    }
                    return replaceQueryParams(currentQuery, paramModifiers, readVueQueryParams);
                }
            }
        }
        return currentQuery;
    }

    public getPanesToRender() {
        const pageType = this.session.coreViewType;
        const page = this.session.activeDashPage;
        return page.panes.filter(pane => pane.view < 0 || pane.view === pageType);
    }

    public hasScorecardFilters(paneDescriptors?: PaneDescriptor[]): boolean | null {
        if (!paneDescriptors) {
            paneDescriptors = this.getPanesToRender();
        }
        if (paneDescriptors.some(p => p.paneType === PaneType.scorecard || p.paneType === PaneType.brandSample)) {
            return true;
        }
        else if (paneDescriptors.some(p => p.paneType === PaneType.standard || p.paneType === PaneType.metricComparison || p.paneType === PaneType.partGrid || p.paneType === PaneType.audienceProfile || PaneType.BrandAnalysisPanes.some(pn => pn === p.paneType))) {
            return false;
        }
        return null;
    }

    public hasCustomerProfilingDropdowns(): boolean {
        return this.getPanesToRender().some(p => p.paneType === PaneType.audienceProfile)
    }

    public getDisplayedMetrics(): string[] {
        return getMetricNamesForPanes(this.getPanesToRender());
    }
    
    public metricUsesOldGroupFilterFormat(filterValueMapping: MetricFilter[], filterGroup: string): boolean {
        return filterValueMapping.length > 0  && filterGroup != null
    }

    currentPanesCanShowPeriodOnPeriod(): boolean {

        if (isCustomPeriodAverage(this.session.activeView.curatedFilters.average)) {
            return false;
        }

        return this.getPanesToRender()
            .filter(p => p.parts.filter(part =>
                part.partType === "MultiMetrics" ||
                part.partType === "MultiEntityCompetition" ||
                part.partType === "ProfileChart" ||
                part.partType === "ScatterPlot" ||
                part.partType === "ColumnChart" ||
                part.partType === "RankingTable" ||
                part.partType === "BoxOnly")
                .length >
                0)
            .length >
            0;
    }

    currentPanesCanToggleDataLabels(): boolean {
        return this.getPanesToRender()
            .some(p => p.parts.some(part => part.partType === "ProfileChart"));
    }

    currentPanesShouldRestrictToCurrentPeriod(activeAudience: IActiveBreaks | undefined): boolean {
        return activeAudience != null && isAudienceActive(activeAudience) &&
            this.getPanesToRender().some(pane => pane.parts.some(part => part.partType === "ColumnChart"));
    }

    getGroupedMetricFilterFromArgs(m: Metric, entityConfiguration: IEntityConfiguration, location: Location): GroupFilterConfiguration {
        let val = this.getQueryStringParameter(m.name, location);
        let mf: GroupFilterConfiguration = new GroupFilterConfiguration();
        mf.metric = m;
        mf.name = m.name === "AgeUS" ? "Age" : m.name;
        mf.isAdvanced = m.name !== "Age" && m.name !== "AgeUS";
        mf.isRange = m.calcType === CalculationType.Average &&
            m.filterValueMapping.length > 0 &&
            m.filterValueMapping[0].text === "Range";
                mf.state = { entityInstances: {}, values: [], invert: false, treatPrimaryValuesAsRange: false};
        if (val != null && val.length === 2) {
            let brandId = +val[0];
            let value = val[1];
            const invert = value.startsWith("!");
            const valuesString = invert ? value.substring(1) : value;
            let treatPrimaryValuesAsRange = false
            let values = valuesString.split(",").map(x => +x);

            //Attempt to extract range if it is of that format
            const filterRangeValues = GroupFilterConfiguration.getFilterRangeValues(valuesString);
            if (filterRangeValues) {
                values = filterRangeValues;
                treatPrimaryValuesAsRange = true;
            }
            if (!m.isBrandMetric() && !m.isProfileMetric()) {
                if (values.length) {
                    brandId = values[0];
                }
            }
            mf.state = {
                entityInstances: {"brand": [brandId]},
                values: values ?? [],
                invert: invert,
                treatPrimaryValuesAsRange: treatPrimaryValuesAsRange,
            };
        }
        return mf;
    }

    public getGroupMetricFilters(metrics: MetricSet, entityConfiguration: IEntityConfiguration, location: Location): GroupFilterConfiguration[] {
        let result: GroupFilterConfiguration[] = [];
        {
            metrics.metrics.filter(m =>  this.metricUsesOldGroupFilterFormat(m.filterValueMapping, m.filterGroup) &&
                !m.disableFilter && DataSubsetManager.supportsDataSubset(DataSubsetManager.selectedSubset, m.subset)).map(m => {
                    result.push(this.getGroupedMetricFilterFromArgs(m, entityConfiguration, location));
                }
            );
        }
        return result;
    }
    
    getMetricFiltersFromArgs(m: Metric, urlArgs: URLSearchParams, location: Location): MetricFilterState {
        let mNameLocal = m.name === "AgeUS" ? "Age" : m.name;
        let mf: MetricFilterState = new MetricFilterState();
        mf.metric = m;
        mf.name = mNameLocal;
        mf.entityInstances = {};

        mf.isAdvanced = mNameLocal !== "Age";
        mf.isRange = m.calcType === CalculationType.Average &&
            m.filterValueMapping.length > 0 &&
            m.filterValueMapping[0].text === "Range";
        mf.values = [];
        
        const argValue = urlArgs.get("f" + mNameLocal);
        if(argValue) {
            let val: IFilterStateCondensed = JSON.parse(argValue);
            mf.metric = m;
            mf.name = mNameLocal;
            mf.entityInstances = val.e;
            mf.invert = val.i;
            let values = val.v;
            mf.treatPrimaryValuesAsRange = val.r;
            mf.values = values;
        }
        else {
            let oldStyleVal = this.getQueryStringParameter(m.name, location);
            if (oldStyleVal != null && oldStyleVal.length === 2) {
                let entityId = +oldStyleVal[0];
                let parsedValues = PageHandler.getValuesFromFilterString(oldStyleVal[1]);
                

                
                if (!m.isBrandMetric() && !m.isProfileMetric()) {
                    if (parsedValues.values.length) {
                        entityId = parsedValues.values[0];
                    }
                }
                if (m.primaryFieldEntityCombination.length || !EntityInstance.isAllBrands(entityId)) {
                    const entityType = m.primaryFieldEntityCombination.length ? m.primaryFieldEntityCombination[0].identifier : "brand";
                    mf.entityInstances = { [entityType]: [entityId] };
                }
                mf.values = parsedValues.values;
                mf.invert = parsedValues.invert;
                mf.treatPrimaryValuesAsRange = parsedValues.treatPrimaryValuesAsRange;
            }
        }
        return mf;
    }
    
    public getMetricFilters(metrics: MetricSet, location: Location): MetricFilterState[] {
        let result: MetricFilterState[] = [];

        var args = new URLSearchParams(location.search);
        metrics.metrics.filter(m => (m.filterValueMapping.length > 0) &&
            !m.disableFilter &&
            DataSubsetManager.supportsDataSubset(DataSubsetManager.selectedSubset, m.subset)).map(m => {
                result.push(this.getMetricFiltersFromArgs(m, args, location));
            });

        return result;
    }

    static getValuesFromFilterString = (value: string) => {
        const invert = value.startsWith("!");
        const valuesString = invert ? value.substring(1) : value;
        let values = valuesString.split(",").map(x => +x);
        let treatPrimaryValuesAsRange = false;
        //Attempt to extract range if it is of that format
        const filterRangeValues = MetricFilterState.getFilterRangeValues(valuesString);
        if (filterRangeValues) {
            values = filterRangeValues;
            treatPrimaryValuesAsRange = true;
        }

        return {
            invert: invert,
            treatPrimaryValuesAsRange,
            values: values,
        }
    }

    getApplicationStateForGoogleAnalytics(location: Location): ICommonVariables | null {
        const session = this.session;
        const activeView = session.activeView;
        if (!activeView) {
            return null;
        }

        const metricFilters = this.getFilterDescriptions(location);
        const demoFilters = this.session.activeView.curatedFilters.demographicFilter;
        const entityFilterKeys = Object.keys(demoFilters);
        const entityFilters = entityFilterKeys.map(k => ({ name: k, filter: demoFilters[k].join(',') }));
        const filters = entityFilters.concat(metricFilters).map(f => `${f.name}|${f.filter}`);

        return {
            value: "",
            parentComponent: "",
            activeBrand: this.activeEntitySet?.mainInstance?.name,
            filters: filters,
            startDate: moment.utc(activeView.curatedFilters.startDate).format("YYYY-MM-DD"),
            endDate: moment.utc(activeView.curatedFilters.endDate).format("YYYY-MM-DD"),
            highlighted: this.activeEntitySet?.getInstances().getAll().map(b => b.name),
            peer: this.activeEntitySet?.getInstances().getAll().map(b => b.name),
            comparisonPeriod: activeView.curatedFilters.comparisonPeriodSelection?.toString()
        };
    }

    clearFilterState(location: Location, setQueryParameters: (params: IQueryStringParam[]) => void) {
        const filterDescriptions = this.getFilterDescriptions(location);

        const toClear = filterDescriptions.flatMap(d => [{ name: d.name, value: "" }, { name: "f"+d.name, value: "" }]);

        setQueryParameters(toClear);

        // Careful to iterate on a copy of the collection while modifying it
        for (let measureFilter of [...this.session.activeView.curatedFilters.measureFilters]) {
            this.session.activeView.curatedFilters.removeMeasureFilter(measureFilter.measureName);
        }

        for (let filter of this.session.filters.filters) {
            if (filter.displayName.length) {
                this.session.activeView.curatedFilters.update(filter.field, filter.getDefaultValue(), undefined);
            }
        }
    }

    getFilterDescriptions(location: Location): { name: string, filter: string }[] {
        var descriptions: { name: string, filter: string }[] = [];
        for (let measureFilter of this.session.activeView.curatedFilters.measureFilters) {
            const description = this.session.activeView.curatedFilters.filterDescriptions[measureFilter.measureName];
            descriptions.push({ name: measureFilter.measureName, filter: description.filter });
        }

        for (let filter of this.session.filters.filters) {
            if (filter.displayName.length) {
                let val = this.getQueryStringParameter(filter.name, location);
                if (val != null) {
                    let description = filter.filterItems.filter(f => val!.indexOf(f.idList.join(",")) >= 0).map(f => f.caption).join(", ");
                    descriptions.push({ name: filter.name, filter: description });
                }
            }
        }
        return descriptions;
    }
}