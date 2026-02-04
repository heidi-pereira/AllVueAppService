import { CommentColumn } from "./CommentColumn";
import { CommentRow } from "./CommentRow";

export class CommentCell {
   
    public Text: string;
    public Column: CommentColumn
        
    constructor(json: any) {
        this.Populate(json);
    }

    private Populate(json: any) {
        this.Text = json.Text;
    }

    public static GetCommentCells(commentRow: CommentRow, json: any): CommentCell[] {
        let commentCells: CommentCell[] = [];
        json.forEach(commentCellJson => {
            commentCells.push(new CommentCell(commentCellJson));
        });
        return commentCells;
    }
}