import { IMixPanelClient } from "./IMixPanelClient";

export type VueEventName = "selectedAudience"
    | "selectedAll" 
    | "removeAudienceBreaks" 
    | "metricVsMetric" 
    | "metricVsAverage" 
    | "metric1Changed" 
    | "metric2Changed"
    | "exploreDataSelected"
    | "filtersOpened" 
    | "chartTypeChanged" 
    | "chartLoaded" 
    | "comparisonPeriodChanged" 
    | "entitySelectorOpened" 
    | "mainEntityChanged" 
    | "entitiesAdded" 
    | "entitiesRemoved" 
    | "averageAdded" 
    | "averageRemoved" 
    | "setSavedAsNew" 
    | "setSavedTo" 
    | "aboutMetricOpened" 
    | "aboutMetricClosed" 
    | "toggleOnLegend"  
    | "toggleOffLegend" 
    | "averageChanged" 
    | "dateRangeChanged" 
    | "addedMeanAverage" 
    | "addedMedianAverage" 
    | "addedAverageMentions" 
    | "removedMeanAverage" 
    | "removedMedianAverage" 
    | "removedAverageMentions" 
    | "saveCrosstabBreak"
    | "updateCrosstabBreak"
    | "deleteCrosstabBreak"
    | "addedCrosstabBreak" 
    | "removedCrosstabBreak" 
    | "nestedCrosstabBreak"
    | "enabledIncludeCounts" 
    | "crossTabMetricChanged" 
    | "changedBaseTypeOverride" 
    | "crossTabInResultOrderAscending" 
    | "crossTabInResultOrderDescending" 
    | "crossTabInScriptOrderDescending" 
    | "crossTabInScriptOrderAscending" 
    | "disabledHighlightLowSample" 
    | "disabledHighlightSignificant"  
    | "disabledIncludeCounts" 
    | "enabledCalculateIndexScores" 
    | "disabledCalculateIndexScores"
    | "enabledHighlightSignificantWithinEachGroup" 
    | "enabledHighlightSignificantVsTotal" 
    | "toggleDisplaySignificantDifferences"
    | "enabledHighlightLowSample"
    | "enabledDisplayMeanValues"
    | "disabledDisplayMeanValues"
    | "enabledDisplayStandardDeviation"
    | "disabledDisplayStandardDeviation"
    | "enabledHideTotalColumn"
    | "disabledHideTotalColumn"
    | "enabledShowMultipleTablesAsSingle"
    | "disabledShowMultipleTablesAsSingle"
    | "reportsEnabledDisplayMeanValues"
    | "reportsDisabledDisplayMeanValues"
    | "reportsEnabledDisplayStandardDeviation"
    | "reportsDisabledDisplayStandardDeviation"
    | "allFiltersRemoved" 
    | "closeFilterDialog" 
    | "filtersApplied" 
    | "filterAdded" 
    | "filterRemoved" 
    | "filtersClosed"
    | "clearAllFilterInstances"
    | "selectAllFilterInstances"
    | "helpOpened" 
    | "myProductSelected" 
    | "userLoggedOut" 
    | "configurationOpened" 
    | "manageUsersSelected" 
    | "navigationOpened" 
    | "navigationClosed" 
    | "pageSelected" 
    | "searchBoxSelected" 
    | "subsetChanged" 
    | "pageLoaded" 
    | "testSurveyOpened" 
    | "fieldVueLinkOpened" 
    | "kimbleLinkOpened" 
    | "aiSummariseClicked" 
    | "aiSummariseRatingThumbsUp" 
    | "aiSummariseRatingThumbsDown" 
    | "aiSummariseFeedbackSubmit"
    | "editQuestionName" 
    | "editQuestionNameFailed" 
    | "editQuestionHelpText" 
    | "editQuestionHelpTextFailed" 
    | "projectLoaded"
    | "exportPowerpointRequested"
    | "exportExcelRequested"
    | "exportCrossTabRequested"
    | "forceReloadOfSurvey"
    | "forceLoadReportData"
    | "dashboardCopyToClipboard"
    | "dashboardFiltersApplied"
    | "dashboardFiltersCleared"
    | "swysOpened"
    | "reportDuplicatePage"
    | "reportsPageAddParts"
    | "reportsPageViewChart"
    | "reportsPageBackFromChart"
    | "reportsPageRemovePart"
    | "reportsPageReorderParts"
    | "reportsPageModifyPart"
    | "reportsPageViewReportSettings"
    | "reportsPageLoaded"
    | "reportChartTypeChanged"
    | "sigConfidenceLevel"
    | "datePickerRangeSelected"
    | "datePickerCustomRangeSelected"
    | "datePickerSavedRangeSelected"
    | "datePickerSavedRangeDeleted"
    | "datePickerCalendarDatesSelected"
    | "reportTemplateCreated"
    | "reportTemplateDeleted"
    | "templateUsedToCreateNewReport"
    | "chartReportCreated"
    | "tableReportCreated"
    ;

export interface PageLoadEventProps extends Pick<VueEventProps,
    'PageLoadTime' |
    'PageName' |
    'Metric' |
    'EntitySet' |
    'Instances' |
    'Average' |
    'DateStart' |
    'DateEnd'
> {}

export class VueEventProps {
    Category: string;
    SubCategory: string;
    Tag?: string | undefined;
    Part?: string;
    Page?: string;
    PageLoadTime?: any;
    PageName?: string;
    Metric?: string;
    EntitySet?: number;
    Instances?: number[];
    Average?: string;
    DateStart?: string;
    DateEnd?: string;
    Filters?: string[];
    Context?: string;
    Subset?: string;
    Product?: string;
    Feedback?: string;
    ActionType?: string;
    ReportName?: string;
    SavedReportId?: number;
    UseGenerativeAi?: boolean;
    Question?: string;
    ChartType?: string;
    Custom?: boolean;
    Path?: string;
    Project?: string;
    KimbleProposalId?: string;
    ReportChartType?: string;

    constructor(Category: string, subCategory: string, tag?: string | undefined, page?: string) {
        this.Category = Category;
        this.SubCategory = subCategory;
        this.Tag = tag;
        this.Page = page;
    }
}

export type MixPanelProps = { [TKey in VueEventName]: VueEventProps };

export type MixPanelModel = {
    userId: string; 
    projectId: string; 
    client: IMixPanelClient; 
    isAllVue: boolean;
    productName: string;
    project: string | undefined;
    kimbleProposalId: string | undefined;
}
