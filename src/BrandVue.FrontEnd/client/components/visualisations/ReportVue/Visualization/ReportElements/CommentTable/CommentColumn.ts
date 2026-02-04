import { CommentTable } from "./CommentTable";
export enum DialPadAlignment {
    TopLeft = 1,
    TopCenter = 2,
    TopRight = 3,
    MiddleLeft = 4,
    MiddleCenter = 5,
    MiddleRight = 6,
    BottomLeft = 7,
    BottomCenter = 8,
    BottomRight = 9,

}
export class CommentColumn {
    public Id: number;
    public Title: string;
    public Width: number;
    public DialPadAlignment: DialPadAlignment;
    constructor(commentTable: CommentTable, json: any, id: number) {
        this.Populate(json, id);
    }

    private Populate(json: any, id: number) {
        this.Id = id;
        this.Title = json.Title;
        this.Width = json.Width;
        this.DialPadAlignment = json.DialPadAlignment;
    }

    public static GetCommentColumns(commentTable: CommentTable, json: any): CommentColumn[] {
        var commentColumns: CommentColumn[] = [];
        var id = 0;
        json.forEach(commentColumnJson => {
            commentColumns.push(new CommentColumn(commentTable, commentColumnJson, id));
        });
        return commentColumns;
    }
}