import { FilterTemplate } from "./FilterTemplate";

export class Filter {
    public FilterTemplate: FilterTemplate;

    public Id: number;
    public SectionName: string;
    public SectionName2: string;
    public SectionName3: string;
    public QName: string;
    public BandName: string;
    public QName2: string;
    public BandName2: string;
    public QName3: string;
    public BandName3: string;
    public ExtraFilters: string;
    public NoSort: boolean;
    public IgnoreReportFilter: boolean;
    public IgnoreSectionFilter: boolean;
    public IgnorePageFilter: boolean;
    public ShowAsAverage: boolean;
    public WeightingQuestion: string;
    public Categories: string;
    public ForceMultiReportFilter: string;
    public CustomFieldData: string[];
    constructor(filterTemplate: FilterTemplate, json: any) {
        this.FilterTemplate = filterTemplate;
        this.Populate(json);
    }
    
    private Populate(json: any) {
        this.Id = json.Id;
        this.SectionName = json.SectionName;
        this.SectionName2 = json.SectionName2;
        this.SectionName3 = json.SectionName3;
        this.QName = json.QName;
        this.BandName = json.BandName;
        this.QName2 = json.QName2;
        this.BandName2 = json.BandName2;
        this.QName3 = json.QName3;
        this.BandName3 = json.BandName3;
        this.ExtraFilters = json.ExtraFilters;
        this.NoSort = json.NoSort;
        this.IgnoreReportFilter = json.IgnoreReportFilter;
        this.IgnoreSectionFilter = json.IgnoreSectionFilter;
        this.IgnorePageFilter = json.IgnorePageFilter;
        this.ShowAsAverage = json.ShowAsAverage;
        this.WeightingQuestion = json.WeightingQuestion;
        this.Categories = json.Categories;
        this.ForceMultiReportFilter = json.ForceMultiReportFilter;
        if (this.FilterTemplate.CustomFieldHeadings) {
            this.PopulateCustomFields(json.CustomFieldData);
        }
    }
    private PopulateCustomFields(customFieldData: string) {
        var nHeadings = this.FilterTemplate.CustomFieldHeadings.length;
        if (customFieldData) {
            this.CustomFieldData = customFieldData.split("|");
        }
    }

    public CustomField(index: number): string {
        if (!this.CustomFieldData)
            return "";

        if (this.CustomFieldData.length < index)
            return "";

        return this.CustomFieldData[index];
    }

    public get DisplayName(): string {
        if (this.IsGap) {
            return "";
        }

        if (!this.SectionName2 && !this.SectionName3) {
            return this.SectionName;
        } else if (!this.SectionName3) {
            return `${this.SectionName}:${this.SectionName2}`;
        } else {
            return `${this.SectionName}:${this.SectionName2}:${this.SectionName3}`;
        }
    }

    public get IsGap(): boolean {
        if ((this.QName ?? "")=="" && ((this.SectionName ?? "") == "GAP" || (this.SectionName ?? "") == "BLANK" || (this.SectionName == "" && this.SectionName2 == "" && this.SectionName3 == ""))) {
            return true;
        } else {
            return false;
        }
    }

    public GetCategories(): string[] {
        if (this.Categories?.trim() === '') {
            return [];
        }

        return this.Categories.split('|').filter(category => category.trim() !== '');
    }

}