import { CommentCell } from "./CommentCell";
import { CommentTable } from "./CommentTable";

export class CommentRow {

    public Cells: CommentCell[];

    constructor(commentTable: CommentTable, json: any) {
        this.Populate(commentTable, json);
    }

    private Populate(commentTable: CommentTable, json: any) {
        this.Cells = CommentCell.GetCommentCells(this, json.Cells);
        for (var i = 0; i < commentTable.Columns.length; i++) {
            if (this.Cells[i]) {
                this.Cells[i].Column = commentTable.Columns[i];
            }
        }
    }

    public static GetCommentRows(commentTable: CommentTable, json: any): CommentRow[] {
        let commentRows: CommentRow[] = [];
        json.forEach(commentRowJson => {
            commentRows.push(new CommentRow(commentTable, commentRowJson));
        });
        return commentRows;
    }
}