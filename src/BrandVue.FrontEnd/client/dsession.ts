import {PageHandler} from "./components/PageHandler";
import {NumberFormattingHelper as dashFormat} from "./helpers/NumberFormattingHelper";
import {DataSubsetManager} from "./DataSubsetManager";
import {viewBase} from "./core/viewBase";
import {filterSet as FilterSet} from "./filter/filterSet";
import {MetricFilterState} from "./filter/metricFilterState";
import {CuratedFilters } from "./filter/CuratedFilters";
import {ApplicationConfiguration} from './ApplicationConfiguration';
import Comparison from "./components/visualisations/MetricComparison/Comparison";
import { IReadVueQueryParams, IWriteVueQueryParams, QueryStringParamNames } from './components/helpers/UrlHelper';
import { findPageByPageName, getActivePage, initialisePages, setActivePage, getStartPage } from "./components/helpers/PagesHelper";
import { selectBestAverage } from "./components/helpers/AveragesHelper";
import { ComparisonPeriodSelection, Factory, PageDescriptor, IAverageDescriptor, AverageTotalRequestModel, MakeUpTo } from './BrandVueApi';
import { getStartEndDateUTCFromUrl } from "./components/helpers/DateHelper";
import { Location } from "react-router-dom";
import { throwIfNullish } from "./components/helpers/ThrowHelper";

export interface IImmutableSession {
    readonly averages: IAverageDescriptor[];
    readonly pageHandler: PageHandler;
    readonly pages: PageDescriptor[];
    readonly filters: FilterSet;
}

/// Ideally this object would contain only app services and information that doesn't change between full page refreshes
/// Currently it contains session state, which should be passed via props
/// Pass IImmutableSession where you're using it legitimately
export class dsession implements IImmutableSession {
    public averages: IAverageDescriptor[];

    public pageHandler: PageHandler;

    public pages: PageDescriptor[];
    public filters: FilterSet;
    public selectedSubsetId: string;

    /**
     * This method is only for backwards compatibility. Use getActivePage from PageHandler.tsx in new code.
     */
    public get activeDashPage() {
        return getActivePage();
    }

    /// Session state should be passed via props instead
    public activeView: viewBase;
    /// Session state should be passed via props instead
    public comparisons: Comparison[] = [];
    /// Session state should be passed via props instead
    public averageRequests: AverageTotalRequestModel[] | null;
    /// Session state should be passed via props instead
    public coreViewType: number;

    constructor() {

    }

    public async init(applicationConfiguration: ApplicationConfiguration, location: Location, readVueQueryParams: IReadVueQueryParams, writeQueryParams: IWriteVueQueryParams): Promise<any> 
    {
        this.selectedSubsetId = readVueQueryParams.getQueryParameter<string>("Subset")!;
        this.pageHandler = new PageHandler(this);
        const api = Factory.MetaDataClient(error => error());
        const response = await api.getSubsets();
        DataSubsetManager.Initialize(response, this.selectedSubsetId);
        await applicationConfiguration.loadConfig(readVueQueryParams, writeQueryParams);
        if (DataSubsetManager.selectedSubset) {
            this.selectedSubsetId = DataSubsetManager.selectedSubset.id;
        } else {
            throw new Error("You don't have access to any currently loaded, please contact an administrator.");
        }
        dashFormat.setLocale(DataSubsetManager.selectedSubset);
        return await this.loadAll(applicationConfiguration, location, readVueQueryParams, writeQueryParams);
    }
    
    private initFilters(): any {
        this.filters = new FilterSet();
        return this.filters.load(this.selectedSubsetId);
    }

    public getScoreCardsAverages(): IAverageDescriptor[] {
        return this.averages.filter(a => a.makeUpTo === MakeUpTo.WeekEnd ||
            a.makeUpTo === MakeUpTo.MonthEnd ||
            a.makeUpTo === MakeUpTo.QuarterEnd ||
            a.makeUpTo === MakeUpTo.CalendarYearEnd);
    }

    public getScoreCardAverageByIdOrDefault(averageId?: string): IAverageDescriptor {
        if (this.averages.length === 0) {
            throw new Error("There are no averages!");
        }
        return selectBestAverage(this.getScoreCardsAverages(), averageId);
    }

    public getAverageByIdOrDefault(averageId?: string): IAverageDescriptor {
        if (this.averages.length === 0) {
            throw new Error("There are no averages!");
        }
        return selectBestAverage(this.averages, averageId);
    }

    private initializeDefaultView(filterSet: FilterSet, 
            applicationConfiguration: ApplicationConfiguration,
            readVueQueryParams: IReadVueQueryParams, 
            writeVueQueryParams: IWriteVueQueryParams) {
        const metricFilters: MetricFilterState[] = [];
        const curatedFilters = new CuratedFilters(filterSet, null,metricFilters);
        curatedFilters.average = this.getAverageByIdOrDefault();
        const {start, end} = getStartEndDateUTCFromUrl(applicationConfiguration.dateOfFirstDataPoint, applicationConfiguration.dateOfLastDataPoint, false, readVueQueryParams, writeVueQueryParams);
        curatedFilters.setDates(start.toDate(), end.toDate());
        this.activeView = new viewBase(curatedFilters);
        this.setCurrentPeriodSelectionFromURL(readVueQueryParams.getQueryParameter);
        this.setCurrentAverageDescriptorFromURL(readVueQueryParams.getQueryParameter);
    }

    private loadAverages() {
        return Factory.MetaDataClient(throwErr => throwErr())
            .getAllAverages();
    }

    private loadAll(applicationConfiguration: ApplicationConfiguration, location: Location, readVueQueryParams: IReadVueQueryParams, writeQueryParams: IWriteVueQueryParams) {

        return Promise.all([this.loadPagesPanesParts(), this.initFilters(), this.loadAverages()])
            .then(([_, __, averages]) => {
                this.averages = averages;
                this.initializeFilters(location); // Pre-render setup
                
                this.initializeDefaultView(this.filters, applicationConfiguration, readVueQueryParams, writeQueryParams);
            }).catch((error) => {
                    console.error(
                        `Fatal error or errors loading dashboard: ${error} Attempting to initPage before rethrow`);
                    throw error;
            });
    }

    public loadPagesPanesParts(activePageNameOverride?: string): any {
        this.pages = [];
        return Factory.PagesClient(throwErr => throwErr()).getPages(this.selectedSubsetId).then(
            pages => {
                this.pages = pages;
                initialisePages(pages);
                if (this.activeDashPage) {
                    const pageName = activePageNameOverride ?? this.activeDashPage.name;
                    const matchedPage = findPageByPageName(pages, pageName);
                    const newActiveDashPage = matchedPage ?? getStartPage();
                    setActivePage(newActiveDashPage);
                } else {
                    const startPage = throwIfNullish(getStartPage(), "Start page");
                    setActivePage(startPage);
                }
            }
        );
    }

    public setCurrentAverageDescriptorFromURL(getQueryParameter: <T = string>(parameterName: string, defaultValue?: T) => T | undefined) {
        const averageId = getQueryParameter<string>("Average");
        const average = this.getAverageByIdOrDefault(averageId);
        this.activeView.curatedFilters.average = average;
    }

    public setCurrentPeriodSelectionFromURL(getQueryParameter: <T = string>(parameterName: string, defaultValue?: T) => T | undefined) {
        const renderPreviousPeriodToggle = this.pageHandler.currentPanesCanShowPeriodOnPeriod();

        var comparisonPeriodSelection = getQueryParameter<ComparisonPeriodSelection>(QueryStringParamNames.period);
        if (comparisonPeriodSelection) {
            this.activeView.curatedFilters.comparisonPeriodSelection = comparisonPeriodSelection;
        } else {
            if (renderPreviousPeriodToggle) {
                this.activeView.curatedFilters.comparisonPeriodSelection = ComparisonPeriodSelection.CurrentAndPreviousPeriod;
            } else {
                this.activeView.curatedFilters.comparisonPeriodSelection = ComparisonPeriodSelection.CurrentPeriodOnly;
            }
        }
    }

    public getOverridingPaneType() {
        const panes = this.activeDashPage.panes;
        return panes && panes.length > 0
            ? panes[0].paneType
            : undefined;
    }

    initializeFilters(location: Location) {

        for (let filter of this.filters.filters) {
            let val = this.pageHandler.getQueryStringParameter(filter.name, location);
            if (val != null) {
                filter.initialValue = val;
                filter.initialDescription = filter.filterItems.filter(f => val!.indexOf(f.idList.join(",")) >= 0)
                    .map(f => f.caption).join(", ");;
            } else {

                filter.initialValue = filter.getDefaultValue();
            }
        }
    }
}
