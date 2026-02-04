import { ReportSection } from "./ReportSection";

export enum DashboardLayoutStyles {
    Default = 0,
    FitMainPlaceholder = 1
}

export enum DashboardRepeatBehaviours {
    Default = 0,
    None = 1,
    ByBrandOnly = 2,
    ByFilterOnly = 3
}

export class ReportPage {
 
    public Section: ReportSection;

    public Id: number;
    public PageTitle: string;
    public PageTemplateName: string;
    public SuppressTemplatePageTitle: boolean;
    public FilterTemplateName: string;
    public DashboardLayoutStyle: DashboardLayoutStyles;
    public DashboardRepeatBehaviour: DashboardRepeatBehaviours;

    constructor(reportSection: ReportSection, json: any) {
        this.Section = reportSection;
        this.Populate(json);
    }

    private Populate(json: any) {
        this.Id = json.Id;
        this.PageTitle = json.PageTitle;
        this.PageTemplateName = json.PageTemplateName;
        this.SuppressTemplatePageTitle = json.SuppressTemplatePageTitle ?? false;
        this.FilterTemplateName = json.FilterTemplateName;
        this.DashboardLayoutStyle = json.DashboardLayoutStyle ?? DashboardLayoutStyles.Default;
        this.DashboardRepeatBehaviour = json.DashboardRepeatBehaviour ?? DashboardRepeatBehaviours.Default;
    }

    static GetReportPages(reportSection: ReportSection, json: any): ReportPage[] {
        var reportPages: ReportPage[] = [];
        json.forEach(reportPageJson => {
            reportPages.push(new ReportPage(reportSection, reportPageJson));
        });
        return reportPages;
    }

}