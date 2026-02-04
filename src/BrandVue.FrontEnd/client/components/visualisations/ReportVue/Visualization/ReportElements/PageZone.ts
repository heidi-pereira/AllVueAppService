import { PageTemplate } from "./PageTemplate";
import { Box } from "../Base/Box";
import { FontHelper } from "../Base/FontHelper";

export class PageZone {
   
    public Template: PageTemplate;

    public ContentType
    public Box: Box;
    public ChartName: string;
    public Text: string;
    public FontName: string;
    public FontSize: string;
    public FontStyle: string;
    public FontColor: string;

    public LineWidth: number;
    public LineColor: string;
    public FillColor: string;

    public DialPadAlignment: number;
    public AutoFitType: string;

    public MarginTop: number;
    public MarginBottom: number;
    public MarginLeft: number;
    public MarginRight: number;

    constructor(template: PageTemplate, json: any) {
        this.Template = template;
        this.Populate(json);
    }
    
    private Populate(json: any) {
        this.ContentType = json.ContentType;
        this.ChartName = json.ChartName;
        this.Text = json.Text;

        this.Box = Box.Get(json);

        this.FontName = json.FontName;
        this.FontSize = json.FontSize;
        this.FontStyle = json.FontStyle;
        this.FontColor = json.FontColor;

        this.LineWidth = +json.LineWidth;
        this.LineColor = json.LineColor;
        this.FillColor = json.FillColor;

        this.MarginTop = +json.MarginTop;
        this.MarginBottom = +json.MarginBottom;
        this.MarginLeft = +json.MarginLeft;
        this.MarginRight = +json.MarginRight;

        this.DialPadAlignment = +json.DialPadAlignment;
        this.AutoFitType = json.AutoFitType;
    }

    public ApplyStyle(div: HTMLDivElement, innerDiv: HTMLDivElement) {
        innerDiv.style.fontFamily = this.FontName;
        innerDiv.style.fontSize = this.FontSize + "px";
        innerDiv.style.color = this.FontColor;

        this.ApplyShadingAndBorders(div);

        FontHelper.ApplyFontStyle(innerDiv, this.FontStyle);
        switch (this.DialPadAlignment) {
            case 1:
                break;
            case 2:
                innerDiv.style.textAlign = "center";
                break;
            case 3:
                innerDiv.style.textAlign = "right";
                break;
            case 4:
                innerDiv.style.textAlign = "left";
                this.VerticalAlignCenter(innerDiv);
                break;
            case 5:
                innerDiv.style.textAlign = "center";
                this.VerticalAlignCenter(innerDiv);
                break;
            case 6:
                innerDiv.style.textAlign = "right";
                this.VerticalAlignCenter(innerDiv);
                break;
            case 7:
                innerDiv.style.textAlign = "left";
                this.VerticalAlignBottom(innerDiv);
                break;
            case 8:
                innerDiv.style.textAlign = "center";
                this.VerticalAlignBottom(innerDiv);
                break;
            case 9:
                innerDiv.style.textAlign = "right";
                this.VerticalAlignBottom(innerDiv);
                break;
        }

    }
    private ApplyShadingAndBorders(div: HTMLDivElement) {
        if (this.LineWidth > 0 && this.LineColor) {
            div.style.borderWidth = this.LineWidth + "px";
            div.style.borderColor = this.LineColor;
            div.style.borderStyle = "solid";
        }

        if (this.FillColor) {
            div.style.backgroundColor = this.FillColor;
        }

        if (this.MarginTop) {
            div.style.paddingTop = this.MarginTop + "px";
        }
        if (this.MarginBottom) {
            div.style.paddingBottom = this.MarginBottom + "px";
        }
        if (this.MarginLeft) {
            div.style.paddingLeft = this.MarginLeft + "px";
        }
        if (this.MarginRight) {
            div.style.paddingRight = this.MarginRight + "px";
        }
    }

    private VerticalAlignCenter(div: HTMLDivElement) {
        div.style.position = "relative";
        div.style.top = "50%";
        div.style.webkitTransform = "translateY(-50%)";
        div.style.transform = "translateY(-50%)";
    }

    private VerticalAlignBottom(div: HTMLDivElement) {
        div.style.position = "absolute";
        div.style.bottom = "0%";
    }

}