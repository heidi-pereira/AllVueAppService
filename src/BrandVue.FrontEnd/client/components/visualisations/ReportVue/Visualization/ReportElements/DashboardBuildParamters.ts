import moment from 'moment';

export class DashboardBuildParamters {
    public DesktopToolsVersion: string;
    public PowerpointTemplateFile: string;
    public LastUpdateTemplateDateTime: Date | undefined;
    public LastUpdateDataVueDateTime: Date | undefined;
    public DashboardGenerationDateTime: Date | undefined;
    constructor(json: any) {
        this.Populate(json);
    }

    private StringToDateTime(text: string): Date | undefined {
        var result = moment(text, 'MM/DD/YYYY hh:mm:ss', false);
        if (result.isValid()) { 
            return result.toDate();
        }
        return undefined;
    }

    private Populate(json: any) {
        var me = this;
        if (json) {
            me.DesktopToolsVersion = json.DesktopToolsVersion;
            me.PowerpointTemplateFile = json.Template;
            me.LastUpdateTemplateDateTime = this.StringToDateTime(json.LastUpdateTemplateDateTime);
            me.DashboardGenerationDateTime = this.StringToDateTime(json.GenerationDateTime);
            me.LastUpdateDataVueDateTime = this.StringToDateTime(json.LastUpdateDataVueDateTime);
        }
    }
}