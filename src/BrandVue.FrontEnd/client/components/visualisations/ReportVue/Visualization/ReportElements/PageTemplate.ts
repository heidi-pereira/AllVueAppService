import { ReportStructure } from "./ReportStructure";
import { PageZone } from "./PageZone";

export class PageTemplate {
   
    public Report: ReportStructure;

    public Name: string;
    public Zones: PageZone[];

    constructor(report: ReportStructure, json: any) {
        this.Report = report;
        this.Populate(json);
    }

    public Populate(json: any) {
        var me = this;
        me.Name = json.Name;
        me.Zones = [];
        json.Zones.forEach(pageZoneJson => {
            me.Zones.push(new PageZone(me, pageZoneJson));
        });
    }

    public static GetPageTemplates(report: ReportStructure, json: any): PageTemplate[] {
        var pageTemplates: PageTemplate[] = [];
        json.forEach(pageTemplateJson => {
            pageTemplates.push(new PageTemplate(report, pageTemplateJson));
        });
        return pageTemplates;
    }

}