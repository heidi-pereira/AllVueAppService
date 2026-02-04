import { ReportPageExclusion } from "./ReportPageExclusion";
import { ReportStructure } from "./ReportStructure";
import { ZoneContents } from "./ZoneContents";

export class ReportPageContents {
 
    public Id: number;
    public ReportFilterId: number;
    public SectionFilterId: number;
    public BrandId: number;
    public Tags: any;
    public ZoneContentsLookup: any;
    public BackdropSvg: string;
    public Exclusions: ReportPageExclusion[];

    constructor(json: any) {
        this.Populate(json);
    }
    
    private Populate(json: any) {
        var me = this;
        me.Tags = {};
        me.ZoneContentsLookup = {}
        me.Id = json.Id;
        me.ReportFilterId = json.ReportFilterId;
        me.SectionFilterId = json.SectionFilterId;
        me.BrandId = json.BrandId; 
        me.BackdropSvg = json.BackdropSvg;
        json.Tags.forEach(tagJson => {
            me.Tags[tagJson.Name] = tagJson.Value;
        });
        json.ZoneContents.forEach(zJson => {
            var zoneContents = new ZoneContents(zJson);
            me.ZoneContentsLookup[zoneContents.ZoneIndex] = zoneContents;
        });
        if (json.Exclusions) {
            me.Exclusions = ReportPageExclusion.GetExclusions(json.Exclusions);
        };
    }

    public static GetPageContents(reportStructure: ReportStructure, json: any): ReportPageContents[] {
        var pageContents: ReportPageContents[] = [];
        json.forEach(reportPageContentsJson => {
            pageContents.push(new ReportPageContents(reportPageContentsJson));
        });
        return pageContents;
    }

    public ReplaceTags(text: string): string {
        var result = text;
        for (var key in this.Tags) {
            result = result.replace(key, this.Tags[key]);
        }
        return result;
    }
}