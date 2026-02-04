import { ReportStructure } from "./ReportStructure";

export class BrandRecord {
   
    
    public Id: number;
    public BrandName: string;
    public SolidFillColor: string;
    public TransparentFillColor: string;
    public SolidLineColor: string;
    public TransparentLineColor: string;
    public LogoFileName: string;
    public IsMainBrand: boolean;
    public IsKeyCompetitor: boolean;
    public IsExcluded: boolean;
    public Categories: string[];
    public DisplayName: string;
    public AutoAssigned: boolean;

    constructor(json: any, index: number) {
        this.Populate(json, index);
    }
    
    private Populate(json: any, index: number) {
        var me = this;
        me.Id = json.Id;
        me.BrandName = json.BrandName;
        me.DisplayName = json.DisplayName;
        me.SolidFillColor = json.SolidFillColor;
        me.TransparentFillColor = json.TransparentFillColor;
        me.SolidLineColor = json.SolidLineColor;
        me.TransparentLineColor = json.TransparentLineColor;
        me.LogoFileName = json.LogoFileName;
        me.IsMainBrand = json.IsMainBrand;
        me.IsKeyCompetitor = json.IsKeyCompetitor;
        me.IsExcluded = json.IsExcluded;
        if (json.Categories) {
            me.Categories = [];
            json.Categories.forEach(category => {
                me.Categories.push(category);
            });
        }
        me.AutoAssigned = me.Id == undefined; 
        if (me.AutoAssigned) {
            me.Id = index;
            console.log(`Auto assigning ${me.BrandName} to ${me.Id}`)
        }

    }
    public GetDisplayName(): string {
        return this.DisplayName ?? this.BrandName;
    }

    public static Compare(a: BrandRecord, b: BrandRecord): number {

        const aNameParts = a.GetDisplayName().split("-");
        const bNameParts = b.GetDisplayName().split("-");
        for (let index = 0; index < aNameParts.length; index++) {
            if (index >= bNameParts.length) {
                return 1;
            }
            const stringCompare = aNameParts[index].localeCompare(bNameParts[index]);
            if (stringCompare != 0) {
                const aAsInteger = parseInt(aNameParts[index]);
                const bAsInteger = parseInt(bNameParts[index]);
                const numericCompare = (isNaN(aAsInteger) || isNaN(bAsInteger)) ? 0: aAsInteger - bAsInteger;
                if (numericCompare) {
                    return numericCompare;
                }
                return stringCompare;
            }
        }
        return 0;
    }

    public static GetBrandRecords(reportStructure: ReportStructure, json: any): BrandRecord[] {
        var brandRecords: BrandRecord[] = [];
        json.forEach((brandRecordJson, index) => {
            brandRecords.push(new BrandRecord(brandRecordJson, index));
        });
        return brandRecords;
    }

    public InCategories(activeCategories: string[]): boolean {
        if (!this.Categories)
            return true;

        return activeCategories.every(category => this.Categories.includes(category));
    }

}