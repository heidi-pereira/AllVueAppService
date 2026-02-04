export class ZoneContents {
    public ZoneIndex: number;
    public ContentType: string;
    public Value: string;
    
    constructor(json: any) {
        this.Populate(json);
    }
    
    private Populate(json: any) {
        this.ZoneIndex = json.ZoneIndex ? json.ZoneIndex : 0;
        this.ContentType = json.ContentType;
        this.Value = json.Value;
    }
}