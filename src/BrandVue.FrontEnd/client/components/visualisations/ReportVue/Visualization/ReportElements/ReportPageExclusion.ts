export class ReportPageExclusion
{
    public ReportFilterId: number;
    public BrandIds: number[];
    constructor(json: any) {
        this.Populate(json);
    }
    
    private Populate(json: any) {
        var me = this;
        me.ReportFilterId = json.ReportFilterId;
        if (json.BrandIds) {
            me.BrandIds = json.BrandIds;
        }
        else {
            me.BrandIds = [];
        }
    }

    public static GetExclusions(json: any): ReportPageExclusion[] {
        var reportPageExclusion: ReportPageExclusion[] = [];
        json.forEach(reportPageExclusionJson => {
            reportPageExclusion.push(new ReportPageExclusion(reportPageExclusionJson));
        });
        return reportPageExclusion;
    }
}