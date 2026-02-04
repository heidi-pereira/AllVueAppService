import { FontHelper } from "../../Base/FontHelper";
import { ScorecardColumn } from "./ScorecardColumn";
import { ScorecardRow } from "./ScorecardRow";

export class ScorecardCell {
    RowId: number;
    ColumnId: number;
    DisplayText: string;
    Value: number;
    Sample: number;
    FontName: string;
    FontSize: string;
    FontStyle: string;
    FontColor: string;
    FontFillColor: string;
    HorizontalPadding: number;
    VerticalPadding: number;
    DialPadAlignment: number;
    Rotation: number;
    BackColor: string;
    Height: string;

    Column: ScorecardColumn;

    constructor(scorecardRow: ScorecardRow|undefined, json: any) {
        this.Populate(json);
    }

    private Populate(json: any) {
        this.DisplayText = json.DisplayText;
        this.Value = json.Value;
        this.Sample = json.Sample;
        this.FontName = json.FontName;
        this.FontStyle = json.FontStyle;
        this.FontSize = json.FontSize;
        this.FontColor = json.FontColor;
        this.FontFillColor = json.FontFillColor;
        this.HorizontalPadding = json.HorizontalPadding;
        this.VerticalPadding = json.VerticalPadding;
        this.DialPadAlignment = json.DialPadAlignment;
        this.Rotation = json.Rotation;
        this.BackColor = json.BackColor;
    }

    public static GetScorecardCells(scorecardRow: ScorecardRow, json: any): ScorecardCell[] {
        var scorecardCells: ScorecardCell[] = [];
        json.forEach(scorecardCellJson => {
            scorecardCells.push(new ScorecardCell(scorecardRow, scorecardCellJson));
        });
        return scorecardCells;
    }

    public CloneForGap(): ScorecardCell {
        const clone = new ScorecardCell(undefined, JSON.stringify(this));
        clone.DisplayText = " ";
        clone.Height = "10px";
        return clone;
    }

    public ApplyStyle(cell: HTMLElement) {
        cell.style.fontFamily = this.FontName;
        cell.style.fontSize = this.FontSize + "px";
        cell.style.color = this.FontColor;
        FontHelper.ApplyFontStyle(cell, this.FontStyle);
        this.ApplyShadingAndBorders(cell);
        if (this.Height) {
            cell.style.height = this.Height;
        }
        
        switch (this.DialPadAlignment) {
            case 1:
                break;
            case 2:
                cell.style.textAlign = "center";
                break;
            case 3:
                cell.style.textAlign = "right";
                break;
            case 4:
                cell.style.textAlign = "left";
                break;
            case 5:
                cell.style.textAlign = "center";
                break;
            case 6:
                cell.style.textAlign = "right";
                break;
            case 7:
                cell.style.textAlign = "left";
                break;
            case 8:
                cell.style.textAlign = "center";
                break;
            case 9:
                cell.style.textAlign = "right";
                break;
        }
    }

    private ApplyShadingAndBorders(div: HTMLElement) {

        if (this.BackColor) {
            div.style.backgroundColor = this.BackColor;
        } else {
            div.style.backgroundColor = "#FFFFFF";
        }
        

        if (this.VerticalPadding) {
            div.style.paddingTop = this.VerticalPadding + "px";
            div.style.paddingBottom = this.VerticalPadding + "px";
        }

        if (this.HorizontalPadding) {
            div.style.paddingLeft = this.HorizontalPadding + "px";
            div.style.paddingRight = this.HorizontalPadding + "px";
        }
    }
   

}