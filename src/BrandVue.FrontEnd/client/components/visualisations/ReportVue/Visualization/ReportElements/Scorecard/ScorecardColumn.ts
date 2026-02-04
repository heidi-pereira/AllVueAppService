import { BrandRecord } from "../BrandRecord";
import { Scorecard } from "./Scorecard";

export enum ScorecardColumnTypes {
    RowTitle = 1,
    RangeBar = 2,
    UpDown = 3,
    VsTarget = 4,
    PreviousWave = 5,
    Change = 6,
    Value = 7,
    Rank = 8,
    ColorIndicator = 9,
    HorizontalBar = 10,
    AutoRank = 11,
    StandardChart = 12,
    AdvancedChart = 13,
    MiniChart = 14,
    Gap = 15,
    ChangeWithArrow = 16,
    BrandLogo = 17,
    Image = 18,
    ColumnSpanTitle = 19,
    ValueWithArrow = 20
}

export class ScorecardColumn {

    public Id: number;
    public FilterId: number;
    public Width: number;
    public ColumnType: ScorecardColumnTypes;
    public AssociatedBrandRecord: BrandRecord;
    public AssociatedBrandId: string;

    constructor(scorecard: Scorecard, json: any, id: number) {
        this.Populate(json, id);
    }

    private Populate(json: any, id: number) {
        this.Id = id;
        this.FilterId = json.FilterId;
        this.Width = json.Width;
        this.ColumnType = json.ColumnType;
    }

    public static GetScorecardColumns(scorecard: Scorecard, json: any): ScorecardColumn[] {
        var scorecardColumns: ScorecardColumn[] = [];
        var id = 0;
        json.forEach(scorecardColumnJson => {
            scorecardColumns.push(new ScorecardColumn(scorecard, scorecardColumnJson, id));
        });
        return scorecardColumns;
    }

    public IsGap() {
        return this.ColumnType == ScorecardColumnTypes.Gap;
    }
}