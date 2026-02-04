export class filterItem {

    public caption: string;
    public spec: string; // Field name / Brand field name / Period spec
    public idList: string[] = []; // List of ids that match the filter
    public respIds: number[]; // All respondents matching filter
    public min: number;
    public max: number;

    public inFilter(value: number): boolean {
        if (value < this.min) return false;
        if (value > this.max) return false;
        return true;
    }
}


