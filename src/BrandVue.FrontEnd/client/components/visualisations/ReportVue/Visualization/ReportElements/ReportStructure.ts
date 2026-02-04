import { ReportSection } from "./ReportSection";
import { PageTemplate } from "./PageTemplate";
import { FilterTemplate } from "./FilterTemplate";
import { BrandRecord } from "./BrandRecord";
import { ReportPageContents } from "./ReportPageContents";
import { Box } from "../Base/Box";
import { Format } from "./Format";
import { BrandDefinition, SectionDefinition } from "./BrandDefinition";
import { DashboardRepeatBehaviours, ReportPage } from "./ReportPage";
import { DashboardBuildParamters } from "./DashboardBuildParamters";

export type FilterLookup = Record<string, FilterTemplate>;
export class ReportStructure {


    public DashboardTitle: string;
    public Sections: ReportSection[];
    public PageTemplates: PageTemplate[];
    public FilterTemplates: FilterTemplate[];
    public BrandRecords: BrandRecord[];

    public PageTemplateLookup: any;
    public FilterTemplateLookup: FilterLookup;
    public PageContents: ReportPageContents[];

    public SlideWidth: number;
    public SlideHeight: number;
    public ReportFilterTemplateName: string;
    public MultiReportFilter: string;
    public SlideTitleBox: Box;
    public Formats: Format[];
    public FileVersion: string;
    public BrandDefinition: BrandDefinition;
    public SectionDefinition: SectionDefinition;
    public ScorecardDropDownStates: { [Name: string]: string };
    public ApplicationUIStates: { [Name: string]: string };
    public DashboardBuildParamters: DashboardBuildParamters;
    constructor(json: any) {
        this.Populate(json);
    }

    private Populate(json: any) {
        if (json.FileVersion === "1.0" || json.FileVersion === undefined) {
            this.DashboardTitle = json.DashboardTitle;
            this.PageTemplates = PageTemplate.GetPageTemplates(this, json.PageTemplates);
            this.PageTemplateLookup = this.PageTemplates.reduce((lookup, pageTemplate) => ({ ...lookup, [pageTemplate.Name]: pageTemplate, }), {});
            this.FilterTemplates = FilterTemplate.GetFilterTemplates(this, json.FilterTemplates);
            this.FilterTemplateLookup = this.FilterTemplates.reduce((lookup, filterTemplate) => ({ ...lookup, [filterTemplate.Name]: filterTemplate, }), {});
            this.BrandRecords = BrandRecord.GetBrandRecords(this, json.BrandRecords).filter(x => !x.AutoAssigned);
            this.PageContents = ReportPageContents.GetPageContents(this, json.PageContents);
            this.Formats = Format.GetFormats(this, json.Formats);
            this.BrandDefinition = new BrandDefinition(json.BrandSettings);
            this.SectionDefinition = new SectionDefinition(json.SectionSettings);
            this.Sections = ReportSection.GetReportSections(this, json.ReportSections);

            this.SlideWidth = json.SlideWidth;
            this.SlideHeight = json.SlideHeight;
            this.SlideTitleBox = Box.GetFromDimensions(json.SlideTitleX, json.SlideTitleY, json.SlideTitleWidth, json.SlideTitleHeight);
            this.ReportFilterTemplateName = json.ReportFilterTemplateName;
            this.MultiReportFilter = json.MultiReportFilter ?? "";
            this.DashboardBuildParamters = new DashboardBuildParamters(json);
        }
        else {
            console.error("ReportVue unknown version number")
        }
    }

    public DoesPageContentExist(reportPage: ReportPage, reportFilterId?: number, brandId?: number) {
        return this.GetPageContents(reportPage, reportFilterId, brandId) != null;
    }

    private HackToGetTheCurrentFilterId(hintId: number): number {
        const activeFilterName = this.ReportFilterTemplateName;
        const currentFilterTemplate = this.FilterTemplateLookup[activeFilterName];
        const currentFilterId = currentFilterTemplate?.Filters && currentFilterTemplate.Filters.length == 1 ? currentFilterTemplate.Filters[0].Id : hintId;
        return currentFilterId;
    }

    private BrandInExclusionFilters(pageContents: ReportPageContents, reportFilterId?: number, brandId?: number): boolean {
        if (pageContents.Exclusions) {

            const reportFilterIdToLookFor = reportFilterId == undefined ? this.HackToGetTheCurrentFilterId(pageContents.Exclusions[0].ReportFilterId) : reportFilterId;
            const reportFilterExclusion = pageContents.Exclusions.filter(e => e.ReportFilterId == reportFilterIdToLookFor);

            if (reportFilterExclusion == null || reportFilterExclusion.length == 0) {
                return false;
            }
            if (brandId == null) {
                return true;
            }
            if (reportFilterExclusion.length === 1) {
                return reportFilterExclusion[0].BrandIds.includes(brandId);
            }
            console.log("***Error multiple matching filters****");
            return false;
        }
        return false;
    }

    public GetValidSections(): ReportSection[] {
        const validSections: ReportSection[] = [];
        if (this.Sections) {
            this.Sections.forEach(section => {
                let isValidSection = false;
                section.Pages.forEach(page => {
                    if (this?.DoesPageContentExist(page)) {
                        isValidSection = true;
                    }
                });
                if (isValidSection) {
                    validSections.push(section);
                }
            });
        }
        return validSections;
    }

    public GetDefaultPage(): ReportPage {
        const validSections = this.GetValidSections();
        const page = validSections.length > 0 ? validSections[0].Pages[0] : this.Sections[0].Pages[0];
        return page;
    }
    public GetPageContents(reportPage: ReportPage, reportFilterId?: number, brandId?: number): ReportPageContents | null {
        for (var i = 0; i < this.PageContents.length; i++) {
            var pageContents = this.PageContents[i];
            const brandMatch = (brandId == undefined) ? true : pageContents.BrandId == brandId;
            if (pageContents.Id == reportPage.Id) {
                const reportFilterMatch = (reportFilterId == undefined) ? true : pageContents.ReportFilterId == reportFilterId;

                if (this.BrandInExclusionFilters(pageContents, reportFilterId, brandId)) {
                    return null;
                }
                switch (reportPage.DashboardRepeatBehaviour) {
                    case DashboardRepeatBehaviours.None:
                        return pageContents;

                    case DashboardRepeatBehaviours.ByFilterOnly:
                        if (reportFilterMatch) {
                            return pageContents;
                        };

                    case DashboardRepeatBehaviours.ByBrandOnly:
                        if (brandMatch) {
                            return pageContents;
                        }

                    case DashboardRepeatBehaviours.None:
                        return pageContents;

                    default:
                        if (reportFilterMatch && brandMatch) {
                            return pageContents;
                        }
                        break;
                }
            }
        }
        return null;
    }



    public ActiveBrandId: number;
}