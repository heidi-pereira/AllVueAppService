import React from "react";
import { ReportStructure } from "../../Visualization/ReportElements/ReportStructure";
import { ReportPage } from "../../Visualization/ReportElements/ReportPage";
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from "reactstrap";
import { ReportSection } from "../../Visualization/ReportElements/ReportSection";
import { FilterTemplate } from "../../Visualization/ReportElements/FilterTemplate";
import { BrandRecord } from "../../Visualization/ReportElements/BrandRecord";
import style from "./PreviewReportVueMenus.module.less";
import { BrandDefinition,SectionDefinition } from "../../Visualization/ReportElements/BrandDefinition";
import AdvancedDiagnostics from "./AdvancedDiagnostics";


interface IPreviewReportVueMenus {
    reportStructure: ReportStructure | undefined;
    reportPage: ReportPage | undefined;
    setReportPage: (newReportPage: ReportPage) => void;
    filterTemplateId: number,
    setFilterTemplateId: (filterId: number) => void;
    brandId: number,
    setBrandId: (brandId: number) => void;

    debuggingMode: boolean;
    setDebuggingMode: (boolean) => void;


    publishCurrentFile: (title:string) => void;
    transformUrl: (value: string) => string;
    brandDefinition: BrandDefinition;
    sectionDefinition: SectionDefinition;
}

const PreviewReportVueMenus = (props: IPreviewReportVueMenus) => {

    const [isPagesDropdownOpen, setIsPagesDropdownOpen] = React.useState<boolean>(false);
    const [isSectionDropdownOpen, setIsSectionDropdownOpen] = React.useState<boolean>(false);
    const [isFiltersDropdownOpen, setIsFiltersDropdownOpen] = React.useState<boolean>(false);
    const [isBrandDropdownOpen, setIsBrandDropdownOpen] = React.useState<boolean>(false);
    const [section, setSection] = React.useState<ReportSection | undefined>(props.reportPage?.Section);

    const [localCopyOfFilter, setLocalCopyOfFilter] = React.useState<FilterTemplate | undefined>(undefined)
    const [localCopyOfBrand, setLocalCopyOfBrand] = React.useState<BrandRecord | undefined>(undefined)

    React.useEffect(() => {
        const currentBrand = props.reportStructure?.BrandRecords.find(x => x.Id == props.brandId);
        setSection(props.reportPage?.Section);
        setLocalCopyOfFilter(props.reportStructure?.FilterTemplates.find(x => x.Id == props.filterTemplateId))
        setLocalCopyOfBrand(currentBrand);

    }, [props.reportPage, props.filterTemplateId, props.brandId]);

    React.useEffect(() => {
        if (section?.Name != props.reportPage?.Section.Name) {
            if (section?.Name) {
                const originalTitle = props.reportPage?.PageTitle
                const bestPage = section.Pages.find(x => x.PageTitle == originalTitle);

                props.setReportPage(bestPage ?? section.Pages[0]);
            }
        }
    }, [section]);

    const renderSectionMenu = () => {
        const validSections: ReportSection[] = props.reportStructure ? props.reportStructure.GetValidSections() : [];
        return (<div className={style.container}>
            <label className={style.label} >{props.sectionDefinition.Plural()}:</label>
            <ButtonDropdown isOpen={isSectionDropdownOpen} toggle={() => setIsSectionDropdownOpen(!isSectionDropdownOpen)} className={"calculation-type-dropdown"}>
                <DropdownToggle className={"toggle-button"}>
                    <div>{section?.Name??"NoSection"}</div>
                    {validSections.length > 1 &&
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    }
                </DropdownToggle>
                {validSections.length > 1 &&
                    <DropdownMenu>
                        <div className={style.dropdownMenu} >
                            {validSections.map((validSection, id) =>
                                <DropdownItem className={validSection.Name == section?.Name ? style.dropDownItemActive : style.dropDownItem} key={id} onClick={() => setSection(validSection)} active={validSection.Name == section?.Name}>
                                    {validSection.Name}
                                </DropdownItem>
                            )}
                        </div>
                    </DropdownMenu>
                }
            </ButtonDropdown>
        </div>
);
    }

    const renderPagesMenu = () => {

        const validPages: ReportPage[] = [];
        if (section && props.reportStructure) {
            section.Pages.filter(x => props.reportStructure!.DoesPageContentExist(x)).forEach(page=> validPages.push(page));
        }
        
        return (<div className={style.container}>
            <label className={style.label}>Pages:</label>
            <ButtonDropdown isOpen={isPagesDropdownOpen} toggle={() => setIsPagesDropdownOpen(!isPagesDropdownOpen)} className={"calculation-type-dropdown"}>
                <DropdownToggle className={"toggle-button"}>
                    <div className={style.itemName }>{props.reportPage?.PageTitle}</div>
                    <i className="material-symbols-outlined">arrow_drop_down</i>
                </DropdownToggle>
                <DropdownMenu>
                    <div className={style.dropdownMenu}>
                        {validPages.map((page) =>
                            <DropdownItem className={page.Id == props.reportPage?.Id ? style.dropDownItemActive : style.dropDownItem } key={page.Id} onClick={() => props.setReportPage(page)} active={page.Id == props.reportPage?.Id}>
                                {page.PageTitle}
                            </DropdownItem>
                        )}
                    </div>
                </DropdownMenu>
            </ButtonDropdown>
        </div>
        );
    }


    const renderFiltersMenu = () => {

        const validFilters: FilterTemplate[] = [];
        if (props.reportPage != undefined) {
            if (props.reportStructure && props.reportStructure.FilterTemplates) {
                props.reportStructure.FilterTemplates.forEach(filterTemplate => {
                    if (props.reportStructure!.DoesPageContentExist(props.reportPage!, filterTemplate.Id)) {
                        validFilters.push(filterTemplate);
                    }
                });
            }
        }

        return (<div className={style.container}>
            <label className={style.label}>Filters:</label>
            <ButtonDropdown isOpen={isFiltersDropdownOpen} toggle={() => setIsFiltersDropdownOpen(!isFiltersDropdownOpen)} className={"calculation-type-dropdown"}>
                <DropdownToggle className={"toggle-button " + (validFilters.length <=1 ? "disabled" : "")}>
                    <div>{localCopyOfFilter?.Name}</div>
                    {validFilters.length > 1 && <i className="material-symbols-outlined">arrow_drop_down</i>}
                </DropdownToggle>
                {validFilters.length > 1 &&
                    <DropdownMenu>
                        <div className={style.dropdownMenu}>
                            {validFilters.map((validFilter) =>
                                <DropdownItem className={validFilter.Id == localCopyOfFilter?.Id ? style.dropDownItemActive : style.dropDownItem} key={validFilter.Id} onClick={() => props.setFilterTemplateId(validFilter.Id)} active={validFilter.Id == localCopyOfFilter?.Id}>
                                    {validFilter.Name}
                                </DropdownItem>
                            )}
                        </div>
                    </DropdownMenu>
                }
            </ButtonDropdown>
        </div>
        );
    }


    const renderBrandsMenu = () => {

        const validBrands: BrandRecord[] = [];
        const pageId = props.reportPage?.Id;
        if (props.reportStructure && props.reportStructure.PageContents) {
            var brandIds: number[] = [];
            for (var id = 0; id < props.reportStructure.PageContents.length; id++) {
                var pageContents = props.reportStructure.PageContents[id];
                if (pageContents.Id == pageId && pageContents.ReportFilterId == props.filterTemplateId) {
                    brandIds.push(pageContents.BrandId);
                }
            }
            const vals = brandIds.map(i => props.reportStructure!.BrandRecords.find(x => x.Id == i));
            vals.forEach(x => {
                if (x) {
                    validBrands.push(x)
                }
            });
        }
        validBrands.sort((a, b) => {
            return BrandRecord.Compare(a, b);
        }
        );

        if (props.reportStructure && validBrands.length > 0) {
            props.reportStructure.ActiveBrandId = validBrands[0].Id
        }

        return (<div className={style.container}>
            <label className={style.label}>{props.brandDefinition.Plural()}:</label>
            <ButtonDropdown isOpen={isBrandDropdownOpen} toggle={() => setIsBrandDropdownOpen(!isBrandDropdownOpen)} className={"calculation-type-dropdown"}>
                <DropdownToggle className={"toggle-button " + (validBrands.length <= 1 ? "disabled" : "")}>
                    <div>{localCopyOfBrand ? localCopyOfBrand.GetDisplayName(): "Not selected"}</div>
                    {validBrands.length > 1 && <i className="material-symbols-outlined">arrow_drop_down</i>}
                </DropdownToggle>
                <DropdownMenu>
                    <div className={ style.dropdownMenu}>
                        {validBrands.map((validBrand, id) =>
                            <DropdownItem className={validBrand.Id == localCopyOfBrand?.Id ? style.dropDownItemActive : style.dropDownItem} key={id} onClick={() => props.setBrandId(validBrand.Id)} active={validBrand.Id == localCopyOfBrand?.Id} >
                                {validBrand.GetDisplayName()}
                            </DropdownItem>
                        )}
                    </div>
                </DropdownMenu>
            </ButtonDropdown>
        </div>
        );
    }
    const handleSettingOfInteractiveDebugging = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.setDebuggingMode(e.target.checked);
    }
    const renderHighlightSelector = () => {
        return (<div className={style.container}>
            <input type="checkbox"
                className="checkbox" id="heighlighting-checkbox"
                checked={props.debuggingMode} onChange={handleSettingOfInteractiveDebugging} />
            <label className={style.label} htmlFor="heighlighting-checkbox">
                Highlight
            </label>
        </div>);
    }
    const page = props.reportStructure && props.reportPage ? props.reportStructure.GetPageContents(props.reportPage, props.filterTemplateId, props.brandId) : undefined;

    return (
        <div className={style.menuBar}>
            <div className={style.menuBarTop}>
            {renderSectionMenu()}
            {renderPagesMenu()}
            {renderFiltersMenu()}
            {renderBrandsMenu()}
            {renderHighlightSelector()}
                {props.debuggingMode &&
                    <>
                    <div className={style.text}>Page size: ({props.reportStructure?.SlideWidth}px x {props.reportStructure?.SlideHeight}px)</div>
                    <div>Template Name: {props.reportPage?.PageTemplateName}</div>
                    <div>SuppressTemplatePageTitle:{props.reportPage?.SuppressTemplatePageTitle? "True": "False"}</div>
                    {page && page.BackdropSvg && page.BackdropSvg.length >0 &&
                        <div>Background Image: <a target="_blank" href={props.transformUrl(page.BackdropSvg) }>{page.BackdropSvg}</a></div>
                    }
                    
                </>
                }
            </div>
            {props.reportStructure &&
                <div className={style.menuBarBottom}>
                    <button type="button" className="hollow-button" onClick={() => props.publishCurrentFile(props.reportStructure!.DashboardTitle)}>Publish</button>
                    <AdvancedDiagnostics reportStructure={props.reportStructure} transformUrl={props.transformUrl} />
                </div>
            }
        </div>
    );
}


export default PreviewReportVueMenus;
