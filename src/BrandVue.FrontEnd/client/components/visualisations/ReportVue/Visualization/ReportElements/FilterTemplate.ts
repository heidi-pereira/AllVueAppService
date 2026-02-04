import { Filter } from "./Filter";
import { ReportStructure } from "./ReportStructure";

export class FilterTemplate {
    
    public Id: number;
    public Name: string;
    public CustomFieldHeadings: string[];
    public Filters: Filter[];

    constructor(json: any) {
        this.Filters = [];
        this.Populate(json);
    }
    
    private Populate(json: any) {
        var me = this;
        me.Id = json.Id;
        me.Name = json.Name;
        if (json.CustomFieldHeadings) {
            me.CustomFieldHeadings = json.CustomFieldHeadings.split("|");
        }
        me.Filters = [];
        json.Filters.forEach(filterJson => {
            me.Filters.push(new Filter(me, filterJson));
        });
    }

    public static GetFilterTemplates(reportStructure: ReportStructure, json: any): FilterTemplate[] {
        var filterTemplates: FilterTemplate[] = [];
        json.forEach(filterTemplateJson => {
            filterTemplates.push(new FilterTemplate(filterTemplateJson));
        });
        return filterTemplates;
    }
}