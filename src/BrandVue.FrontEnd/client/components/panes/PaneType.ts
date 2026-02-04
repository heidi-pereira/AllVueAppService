export class PaneType {
    public static readonly standard = "Standard";
    public static readonly scorecard = "Scorecard";
    public static readonly brandSample = "BrandSample";
    public static readonly metricComparison = "MetricComparison";
    public static readonly partGrid = "PartGrid";
    public static readonly partColumn = "PartColumn";
    public static readonly audienceProfile = "AudienceProfile";
    public static readonly iFrame = "IFrame";
    public static readonly import = "Import";
    public static readonly crossTabPage = "CrosstabPage";
    public static readonly reportVuePage = "ReportVuePage";
    public static readonly allVueWebPage = "WebPage";
    public static readonly reportsPage = "ReportsPage";
    public static readonly reportSubPage = "ReportSubPage";
    public static readonly settingsPage = "SettingsPage";
    public static readonly analysisScorecard = "AnalysisScorecard";
    public static readonly brandAdvocacy = "BrandAdvocacy";
    public static readonly brandBuzz = "BrandBuzz";
    public static readonly brandLove = "BrandLove";
    public static readonly brandUsage = "BrandUsage";

    public static readonly BrandAnalysisPanes = [
        PaneType.analysisScorecard,
        PaneType.brandAdvocacy,
        PaneType.brandBuzz,
        PaneType.brandLove,
        PaneType.brandUsage
    ]

    public static readonly BrandAnalysisSubPanes = [
        PaneType.brandAdvocacy,
        PaneType.brandBuzz,
        PaneType.brandLove,
        PaneType.brandUsage
    ]

    private static readonly panesWithoutSidePanel = [
        PaneType.import,
        PaneType.partGrid,
        PaneType.iFrame,
        PaneType.partColumn,
        PaneType.audienceProfile
    ];

    private static readonly panesWithNoFilters = [
        PaneType.partGrid,
        PaneType.partColumn,
        PaneType.crossTabPage,
    ];

    private static readonly panesWithoutEntitySetSelector = [
        PaneType.import,
        PaneType.partGrid,
        PaneType.iFrame,
        PaneType.partColumn,
    ]

    private static readonly panesWithFixedDate = [
        PaneType.partGrid,
        PaneType.partColumn,
        PaneType.audienceProfile
    ];

    public static shouldShowEntitySetSelector(paneType: string): boolean {
        return !this.panesWithoutEntitySetSelector.includes(paneType);
    }

    public static usesSidePanel(paneType: string): boolean {
        return !this.panesWithoutSidePanel.includes(paneType);
    }

    public static needsFilters(paneType: string): boolean {
        return !this.panesWithNoFilters.includes(paneType);
    }

    public static hasFixedDate(paneType:string): boolean {
        return this.panesWithFixedDate.includes(paneType);
    }
};