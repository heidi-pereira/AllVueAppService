import { FontHelper, FontStyle } from "../Base/FontHelper";
import { Filter } from "./Filter";
import { ReportStructure } from "./ReportStructure";

export enum FormatTypes {
    XAxisTitle = 0,
    YAxisTitle = 1,
    XAxisText = 2,
    YAxisText = 3,
    DataLabels = 4,
    Legend = 5,
    MainTitle = 6,
    SlideTitle = 7,
    Scorecard = 8,
    Num = 8
}

export enum Orientation {
    Horizontal,
    Vertical
}

export class Format {

    public FormatType: FormatTypes;
    public FontName: string;
    public FontSize: number;
    public FontStyle: FontStyle;
    public FontColor: string;
    public AllCaps: boolean = false;
    public Orientation: Orientation;

    constructor(json: any) {
        this.Populate(json);
    }

    private Populate(json: any) {
        var me = this;
        me.FormatType = json.FormatType ?? FormatTypes.XAxisTitle;
        me.FontName = json.FontName ?? "Arial Narrow";
        me.FontSize = json.FontSize ?? 10;
        me.FontStyle = json.FontStyle ?? FontStyle.Regular;
        me.FontColor = json.FontColor ?? "#000000";
        me.AllCaps = json.AllCaps ?? false;
        me.Orientation = json.Orientation ?? Orientation.Horizontal;
    }

    public ApplyStyle(div: HTMLDivElement) {
        div.style.fontFamily = this.FontName;
        div.style.fontSize = this.FontSize + "px";
        div.style.color = this.FontColor;
        FontHelper.ApplyFontStyle(div, this.FontStyle);
    }

    public static GetFormats(reportStructure: ReportStructure, json: any): Format[] {
        var formats: Format[] = [];
        json.forEach(formatJson => {
            formats.push(new Format(formatJson));
        });
        return formats;
    }
}