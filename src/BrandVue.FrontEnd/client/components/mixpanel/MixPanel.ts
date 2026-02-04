import {MixPanelProps, VueEventProps, VueEventName, MixPanelModel, PageLoadEventProps} from './MixPanelHelper';
import { IMixPanelClient } from './IMixPanelClient';
import {ApplicationUser, IMeasureFilterRequestModel} from 'client/BrandVueApi';
import { UserProfile } from './UserProfile';
import { DataSubsetManager } from '../../DataSubsetManager';

export class MixPanel {
    public static readonly Props: MixPanelProps = {
        selectedAudience: new VueEventProps("Audience", "Audience Breaks", "External"),
        selectedAll: new VueEventProps("Audience", "Audience Breaks", "External"),
        removeAudienceBreaks: new VueEventProps("Audience", "Audience Breaks", "External"),

        metricVsMetric: new VueEventProps("Audience", "Audience Page", "External"),
        metricVsAverage: new VueEventProps("Audience", "Audience Page", "External"),
        metric1Changed: new VueEventProps("Audience", "Audience Page", "External"),
        metric2Changed: new VueEventProps("Audience", "Audience Page", "External"),

        exploreDataSelected: new VueEventProps("Charts", "Buttons", "External"),
        filtersOpened: new VueEventProps("Charts", "Buttons", "External"),
        chartTypeChanged: new VueEventProps("Charts", "Chart Type", "External"),
        chartLoaded: new VueEventProps("Charts", "Chart Loaded", "External"),
        comparisonPeriodChanged: new VueEventProps("Charts", "Comparisons", "External"),

        entitySelectorOpened: new VueEventProps("Charts", "Entity selector", "External"),
        mainEntityChanged: new VueEventProps("Charts", "Entity selector", "External"),
        entitiesAdded: new VueEventProps("Charts", "Entity selector", "External"),
        entitiesRemoved: new VueEventProps("Charts", "Entity selector", "External"),
        averageAdded: new VueEventProps("Charts", "Entity selector", "External"),
        averageRemoved: new VueEventProps("Charts", "Entity selector", "External"),
        setSavedAsNew: new VueEventProps("Charts", "Entity selector", "External"),
        setSavedTo: new VueEventProps("Charts", "Entity selector", "External"),

        aboutMetricClosed: new VueEventProps("Charts", "Help", "External"),
        aboutMetricOpened: new VueEventProps("Charts", "Help", "External"),

        toggleOffLegend: new VueEventProps("Charts", "Legend Toggle", "External"),
        toggleOnLegend: new VueEventProps("Charts", "Legend Toggle", "External"),

        averageChanged: new VueEventProps("Charts", "Period", "External"),
        dateRangeChanged: new VueEventProps("Charts", "Period", "External"),

        addedAverageMentions: new VueEventProps("Crosstabs", "AllVue Averages", "External"),
        addedMeanAverage: new VueEventProps("Crosstabs", "AllVue Averages", "External"),
        addedMedianAverage: new VueEventProps("Crosstabs", "AllVue Averages", "External"),
        removedAverageMentions: new VueEventProps("Crosstabs", "AllVue Averages", "External"),
        removedMeanAverage: new VueEventProps("Crosstabs", "AllVue Averages", "External"),
        removedMedianAverage: new VueEventProps("Crosstabs", "AllVue Averages", "External"),

        addedCrosstabBreak: new VueEventProps("Crosstabs", "Breaks", "External"),
        removedCrosstabBreak: new VueEventProps("Crosstabs", "Breaks", "External"),
        nestedCrosstabBreak: new VueEventProps("Crosstabs", "Breaks", "External"),
        saveCrosstabBreak: new VueEventProps("Crosstabs", "Breaks", "External"),
        updateCrosstabBreak: new VueEventProps("Crosstabs", "Breaks", "External"),
        deleteCrosstabBreak: new VueEventProps("Crosstabs", "Breaks", "External"),

        enabledIncludeCounts: new VueEventProps("Crosstabs", "Settings", "External"),
        disabledIncludeCounts: new VueEventProps("Crosstabs", "Settings", "External"),
        enabledCalculateIndexScores: new VueEventProps("Crosstabs", "Settings", "External"),
        disabledCalculateIndexScores: new VueEventProps("Crosstabs", "Settings", "External"),
        crossTabMetricChanged: new VueEventProps("Crosstabs", "Metric", "External"),
        changedBaseTypeOverride: new VueEventProps("Crosstabs", "Settings", "External"),
        crossTabInResultOrderAscending: new VueEventProps("Crosstabs", "Settings", "External"),
        crossTabInResultOrderDescending: new VueEventProps("Crosstabs", "Settings", "External"),
        crossTabInScriptOrderAscending: new VueEventProps("Crosstabs", "Settings", "External"),
        crossTabInScriptOrderDescending: new VueEventProps("Crosstabs", "Settings", "External"),
        enabledHideTotalColumn: new VueEventProps("Crosstabs", "Settings", "External"),
        disabledHideTotalColumn: new VueEventProps("Crosstabs", "Settings", "External"),
        enabledShowMultipleTablesAsSingle: new VueEventProps("Crosstabs", "Settings", "External"),
        disabledShowMultipleTablesAsSingle: new VueEventProps("Crosstabs", "Settings", "External"),

        disabledHighlightLowSample: new VueEventProps("Crosstabs", "Settings", "External"),
        disabledHighlightSignificant: new VueEventProps("Crosstabs", "Settings", "External"),
        enabledHighlightSignificantVsTotal: new VueEventProps("Crosstabs", "Settings", "External"),
        enabledHighlightSignificantWithinEachGroup: new VueEventProps("Crosstabs", "Settings", "External"),
        enabledHighlightLowSample: new VueEventProps("Crosstabs", "Settings", "External"),
        toggleDisplaySignificantDifferences: new VueEventProps("Crosstabs", "Settings", "External"),
        sigConfidenceLevel: new VueEventProps("Crosstabs", "Settings", "External"),

        enabledDisplayMeanValues: new VueEventProps("Crosstabs", "Settings", "External"),
        disabledDisplayMeanValues: new VueEventProps("Crosstabs", "Settings", "External"),
        enabledDisplayStandardDeviation: new VueEventProps("Crosstabs", "Settings", "External"),
        disabledDisplayStandardDeviation: new VueEventProps("Crosstabs", "Settings", "External"),
        reportsEnabledDisplayMeanValues: new VueEventProps("Reports", "Settings", "External"),
        reportsDisabledDisplayMeanValues: new VueEventProps("Reports", "Settings", "External"),
        reportsEnabledDisplayStandardDeviation: new VueEventProps("Reports", "Settings", "External"),
        reportsDisabledDisplayStandardDeviation: new VueEventProps("Reports", "Settings", "External"),

        allFiltersRemoved: new VueEventProps("Filters", "Filters", "External"),
        closeFilterDialog: new VueEventProps("Filters", "Filters", "External"),
        filtersApplied: new VueEventProps("Filters", "Filters", "External"),
        filterAdded: new VueEventProps("Filters", "Filters", "External"),
        filterRemoved: new VueEventProps("Filters", "Filters", "External"),
        filtersClosed: new VueEventProps("Filters", "Filters", "External"),
        clearAllFilterInstances: new VueEventProps("Filters", "Filters", "External"),
        selectAllFilterInstances: new VueEventProps("Filters", "Filters", "External"),

        helpOpened: new VueEventProps("Navigation", "Help", "External"),

        myProductSelected: new VueEventProps("Navigation", "My Account", "External"),
        userLoggedOut: new VueEventProps("Navigation", "My Account", "External"),
        configurationOpened: new VueEventProps("Navigation", "My Account", "External"),
        manageUsersSelected: new VueEventProps("Navigation", "My Account", "External"),

        navigationOpened: new VueEventProps("Navigation", "Navigation Menu", "External"),
        navigationClosed: new VueEventProps("Navigation", "Navigation Menu", "External"),
        pageSelected: new VueEventProps("Navigation", "Navigation Menu", "External"),

        searchBoxSelected: new VueEventProps("Navigation", "Search Box", "External"),
        subsetChanged: new VueEventProps("Navigation", "Subset", "External"),

        pageLoaded: new VueEventProps("Performance", "Non Event", "Profiling"),

        testSurveyOpened: new VueEventProps("Surveys", "Test Survey", "External"),
        fieldVueLinkOpened: new VueEventProps("Surveys", "FieldVue", "External"),
        kimbleLinkOpened: new VueEventProps("Surveys", "Kimble", "External"),
        
        editQuestionName: new VueEventProps("Surveys", "Survey", ""),
        editQuestionNameFailed: new VueEventProps("Surveys", "Survey", ""),
        editQuestionHelpText: new VueEventProps("Surveys", "Survey", ""),
        editQuestionHelpTextFailed: new VueEventProps("Surveys", "Survey", ""),

        aiSummariseClicked: new VueEventProps("Crosstabs", "Question", "Internal"),
        aiSummariseRatingThumbsUp: new VueEventProps("Crosstabs", "Question", "Internal"),
        aiSummariseRatingThumbsDown: new VueEventProps("Crosstabs", "Question", "Internal"),
        aiSummariseFeedbackSubmit: new VueEventProps("Crosstabs", "Question", "Internal"),
        projectLoaded: new VueEventProps("Surveys", "Survey", ""),
        exportPowerpointRequested: new VueEventProps("Reporting", "PowerPoint", ""),
        exportExcelRequested: new VueEventProps("Reporting", "Excel", ""),
        exportCrossTabRequested: new VueEventProps("Reporting", "CrossTab", ""),

        forceReloadOfSurvey: new VueEventProps("Settings", "Reports", "Internal"),
        forceLoadReportData: new VueEventProps("Settings", "Reports", "Internal"),

        dashboardCopyToClipboard: new VueEventProps("Dashboard", "Report", "External"),
        dashboardFiltersApplied: new VueEventProps("Dashboard", "Filters", "External"),
        dashboardFiltersCleared: new VueEventProps("Dashboard", "Filters", "External"),

        swysOpened: new VueEventProps("BrandVue", "SayWhatYouSee", "Internal"),
        reportDuplicatePage: new VueEventProps("Reporting", "Page", "Internal"),
        reportsPageAddParts: new VueEventProps("Reporting", "Page", "Internal"),
        reportsPageViewChart: new VueEventProps("Reporting", "Page", "External"),
        reportsPageBackFromChart: new VueEventProps("Reporting", "Page", "External"),
        reportsPageRemovePart: new VueEventProps("Reporting", "Page", "Internal"),
        reportsPageReorderParts: new VueEventProps("Reporting", "Page", "Internal"),
        reportsPageModifyPart: new VueEventProps("Reporting", "Page", "Internal"),
        reportsPageViewReportSettings: new VueEventProps("Reporting", "Page", "Internal"),
        reportsPageLoaded: new VueEventProps("Reporting", "Page", ""),
        reportChartTypeChanged: new VueEventProps("Reporting", "Chart Type", ""),
        chartReportCreated: new VueEventProps("Reporting", "Chart", "Internal"),
        tableReportCreated: new VueEventProps("Reporting", "Table", "Internal"),

        datePickerRangeSelected: new VueEventProps("Dates", "Date Picker", "External"),
        datePickerCustomRangeSelected: new VueEventProps("Dates", "Date Picker", "External"),
        datePickerSavedRangeSelected: new VueEventProps("Dates", "Date Picker", "External"),
        datePickerSavedRangeDeleted: new VueEventProps("Dates", "Date Picker", "External"),
        datePickerCalendarDatesSelected: new VueEventProps("Dates", "Date Picker", "External"),

        reportTemplateCreated: new VueEventProps("Reporting", "Template", "External"),
        reportTemplateDeleted: new VueEventProps("Reporting", "Template", "External"),
        templateUsedToCreateNewReport: new VueEventProps("Reporting", "Template", "External"),

    };
    private static client: IMixPanelClient;
    private static isAllVue: boolean;
    private static productName: string;
    private static project: string | undefined;
    private static kimbleProposalId: string | undefined;

    public static init(model: MixPanelModel) {
        this.client = model.client;
        this.client.init(model.projectId)
        this.client.identify(model.userId);
        this.isAllVue = model.isAllVue;
        this.productName = model.productName;
        this.project = model.project;
        this.kimbleProposalId = model.kimbleProposalId;
    }

    public static logout() {
        const userLoggedOut = "userLoggedOut";
        let props = MixPanel.Props[userLoggedOut];
        this.client.track(this.camelCaseToTitle(userLoggedOut), props);
    }

    public static trackWithContext(eventName: VueEventName,context: string, additionalProperties: Partial<VueEventProps> | null = null) {
        if (additionalProperties) {
            additionalProperties["Context"] = context;
            this.track(eventName, additionalProperties);
        }
        else {
            this.track(eventName, { Context: context });
        }
    }

    public static track(eventName: VueEventName,
                        additionalProperties: Partial<VueEventProps> | null = null) {
        let props = MixPanel.Props[eventName];
        if(this.isAllVue) props.Tag = undefined;
        let propsObj = this.addGlobalProps(props);
        if (additionalProperties) {
            propsObj = Object.assign({}, propsObj, additionalProperties);
        }
        this.client.track(this.camelCaseToTitle(eventName), propsObj);
    }

    public static trackPageLoadTime(eventProps: PageLoadEventProps, filters: IMeasureFilterRequestModel[] ) {
        if (!eventProps.PageName || eventProps.PageName.trim() === '') return;
        const eventName: VueEventName = "pageLoaded";
        let props = MixPanel.Props[eventName];
        props["Page Load Time"] = eventProps.PageLoadTime;
        props = this.addGlobalProps(props);
        this.client.track(`${this.camelCaseToTitle(eventProps.PageName)} ${this.camelCaseToTitle(eventName)}`, props);
        props = {...props, ...eventProps };
        props.Filters = filters.sort((x, y) => x.measureName.localeCompare(y.measureName)).map(x=>x.measureName);
        this.client.track(`${this.camelCaseToTitle(eventName)}`, props);
    }

    public static trackSurvey(surveyIds: number[], surveyNames: string [], surveyGroupedName: string, surveyUID: string) {
        const eventName: VueEventName = "projectLoaded";
        let props = MixPanel.Props[eventName];
        props["Survey Ids"] = surveyIds.join(",");
        props["Survey Names"] = surveyNames.join(",");
        props["Name"] = surveyGroupedName;
        props["Survey UID"] = surveyUID;
        props = this.addGlobalProps(props);
        this.client.track(`${this.camelCaseToTitle(eventName)}`, props);
    }

    public static trackPage(page: string) {
        const pageSelected = "pageSelected";
        let props = MixPanel.Props[pageSelected];
        props.Page = page;
        let propsObj = this.addGlobalProps(props);
        this.client.track(this.camelCaseToTitle(pageSelected), propsObj);
    }

    public static setPeople(user: ApplicationUser) {
        let userProfile = this.getUserProfileFromUser(user);
        this.client.setPeople(userProfile)
    }

    private static getUserProfileFromUser(user: ApplicationUser) {
        let result = new UserProfile();
        result.Roles = this.getRoleFromUser(user);
        result.$name = user.name + " " + user.surname;
        result.$email = user.userName;
        result.$organisation = this.getCompanyFromUserEmail(user);
        
        return result;
    }

    private static getCompanyFromUserEmail(user: ApplicationUser): string {
        let userEmail = user.userName; // user names are ALWAYS emails

        if (userEmail) {
            let index = userEmail.indexOf('@');
            if (index < 0) {
                return 'no domain';
            } else if (index === userEmail.length - 1) {
                return 'domain truncated';
            } else {
                return userEmail.substring(index + 1);
            }
        }

        return "";
    }

    private static getRoleFromUser(user: ApplicationUser): string[] {
        const result: string[] = [];
        if (user.isAdministrator) {
            result.push("Administrator");
        }
        if (user.isReportViewer) {
            result.push("Report Viewer");
        }
        if (user.isSystemAdministrator) {
            result.push("System Administrator");
        }
        if (user.isTrialUser) {
            result.push("Trial User");
        }

        return result;
    }

    private static camelCaseToTitle(camelCase: string): string {
        // Split the string at each uppercase letter and capitalize the first letter of each word
        const words = camelCase.split(/(?=[A-Z])/).map(word => {
            return word.charAt(0).toUpperCase() + word.slice(1).toLowerCase();
        });

        // Join the words with spaces
        return words.join(' ');
    }

    private static addGlobalProps(props: VueEventProps) : VueEventProps{
        props.Subset = DataSubsetManager.selectedSubset?.displayName;
        props.Product = this.productName;
        props.Project = this.project;
        props.KimbleProposalId = this.kimbleProposalId;
        return props;
    }
}