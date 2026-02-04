import { ReportPage } from "./ReportPage";
import { ReportStructure } from "./ReportStructure";

export class ReportSection {
    public Report: ReportStructure;
    public Pages: ReportPage[];
    public Name: string;
    public RepeatFilterTemplate: string;
    public MultiReportFilter: string;
    public IncludeInReport: boolean;
    public IsUnrelatedToBrand: boolean;
    public ArePagesHidden: boolean;
    constructor(report: ReportStructure, json: any) {
        this.Report = report;
        this.Populate(json);
    }

    public Populate(json: any) {
        this.Name = json.Name;
        this.RepeatFilterTemplate = json.RepeatFilterTemplate;
        this.MultiReportFilter = json.MultiReportFilter;
        this.IncludeInReport = json.IncludeInReport ?? true;
        this.IsUnrelatedToBrand = json.IsUnrelatedToBrand ?? false;
        this.ArePagesHidden = json.HidePages ?? false;
        this.Pages = ReportPage.GetReportPages(this, json.ReportPages);
    }
    
    public static GetReportSections(report: ReportStructure, json: any): ReportSection[] {
        var reportSections: ReportSection[] = [];
        json.forEach(reportSectionJson => {
            reportSections.push(new ReportSection(report, reportSectionJson));
        });
        return reportSections;
    }

}