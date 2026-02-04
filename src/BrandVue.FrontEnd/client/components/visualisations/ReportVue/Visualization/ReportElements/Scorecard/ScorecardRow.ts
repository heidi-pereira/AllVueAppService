import { Scorecard } from "./Scorecard";
import { ScorecardCell } from "./ScorecardCell";

export enum ScorecardRowTypes {
    Unknown = 0,
    Main = 1,
    Title = 2,
    Gap = 3,
    BrandTitle = 4,
    Group = 5,
    GroupTitle = 6,
    GroupSubTitle = 7,
    Image = 8,
    ImageFile = 10,
    SpanTitle = 11,
    DataTitle = 12,
    ColumnRank = 13,
    RowGroupTitle = 14,
    SimpleTitle = 15,
    VariableTitle = 16,
    DifferenceDescription = 17
}

export class ScorecardRow {

    public RowType: ScorecardRowTypes;
    public Cells: ScorecardCell[];

    constructor(scorecard: Scorecard, json: any) {
        this.Populate(scorecard, json);
    }

    private Populate(scorecard: Scorecard, json: any) {
        this.RowType = json.RowType ?? ScorecardRowTypes.Main;
        this.Cells = ScorecardCell.GetScorecardCells(this, json.Cells);
        // Map cells -> Columns
        for (var i = 0; i < scorecard.Columns.length; i++) {
            if (this.Cells[i]) {
                this.Cells[i].Column = scorecard.Columns[i];
            }
        }
    }

    public GetSpan(columnIndex: number): number {
        if (this.RowType !== ScorecardRowTypes.SpanTitle) {
            return 1;
        }

        // Don't add cell if same as previous
        let cellText = this.Cells[columnIndex].DisplayText
        if (columnIndex > 0 && cellText == this.Cells[columnIndex - 1].DisplayText) {
            return 0;
        }

        // Look forward
        let lastMatchIndex = columnIndex + 1;
        while (lastMatchIndex < this.Cells.length && this.Cells[lastMatchIndex]?.DisplayText == cellText) {
            lastMatchIndex++;
        }
        return lastMatchIndex - columnIndex;
    }

    public IsTitle() {
        return this.RowType == ScorecardRowTypes.DataTitle
            || this.RowType == ScorecardRowTypes.VariableTitle
            || this.RowType == ScorecardRowTypes.ColumnRank
            || this.RowType == ScorecardRowTypes.BrandTitle
            || this.RowType == ScorecardRowTypes.Group
            || this.RowType == ScorecardRowTypes.GroupSubTitle
            || this.RowType == ScorecardRowTypes.ImageFile
            || this.RowType == ScorecardRowTypes.SpanTitle
            || this.RowType == ScorecardRowTypes.Title
            || this.RowType == ScorecardRowTypes.SimpleTitle
    }

    public static GetScorecardRows(scorecard: Scorecard, json: any): ScorecardRow[] {
        var scorecardRows: ScorecardRow[] = [];
        json.forEach(scorecardRowJson => {
            scorecardRows.push(new ScorecardRow(scorecard, scorecardRowJson));
        });
        return scorecardRows;
    }
}