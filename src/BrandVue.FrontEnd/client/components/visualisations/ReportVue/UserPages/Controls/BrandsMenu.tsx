import React from "react";
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from "reactstrap";
import SearchInput from "../../../../SearchInput";
import { BrandDefinition } from "../../Visualization/ReportElements/BrandDefinition";
import { BrandRecord } from "../../Visualization/ReportElements/BrandRecord";
import { ReportSection } from "../../Visualization/ReportElements/ReportSection";
import { ReportStructure } from "../../Visualization/ReportElements/ReportStructure";

import style from "./Menus.module.less";

interface IBrandsMenu {
    reportStructure: ReportStructure | undefined;
    reportSection: ReportSection;
    onSelectSection: (section: BrandRecord) => void;
    brandDefinition: BrandDefinition;
    defaultBrand: BrandRecord|undefined;
}

const BrandsMenu = (props: IBrandsMenu) => {

    const [isSectionDropdownOpen, setIsSectionDropdownOpen] = React.useState<boolean>(false);

    const [brands, setBrands] = React.useState<BrandRecord[]>([]);
    const [brand, setBrand] = React.useState<BrandRecord>();
    const [searchQuery, setSearchQuery] = React.useState<string>("");
    const [noBrandSelectorAvailable, setNoBrandSelectorAvailable] = React.useState<boolean>(false);

    React.useEffect(() => {
        let validBrands: BrandRecord[] = [];
        if (props.reportSection && props.reportStructure && props.reportStructure.BrandRecords) {
            const brandIndexes: number[] = [];

            props.reportSection.Report.PageContents.map(pageContent => {
                const relatedPage = props.reportSection.Pages.find(x => x.Id == pageContent.Id);
                if (relatedPage) {
                    if (props.reportStructure?.DoesPageContentExist(relatedPage, undefined, pageContent.BrandId)) {
                        brandIndexes.push(pageContent.BrandId);
                    }
                }
            });
            const uniqueBrands = brandIndexes.filter((x, i, a) => a.indexOf(x) == i);
            validBrands = [];
            const brandsFromIds = uniqueBrands.map(brandIndex => props.reportStructure!.BrandRecords.find(x => x.Id == brandIndex));
            brandsFromIds.forEach(x => {
                if (x) {
                    validBrands.push(x)
                }
            });
            const sectionName = props.reportSection.Name.toLocaleLowerCase();
            const sectionsToIgnore : string[] = ['welcome',
                'guidance',
                'aggregated reporting'];
            const hideBrandSelector = props.reportSection.IsUnrelatedToBrand ||
                brandsFromIds.length <= 1 && (sectionsToIgnore.includes(sectionName));

            setNoBrandSelectorAvailable(hideBrandSelector);
        }
        validBrands.sort((a, b) => {
            return BrandRecord.Compare(a, b);
        });
        setBrands(validBrands);
        if (validBrands.length > 0) {
            let useDefaultBrand = false;
            if (props.defaultBrand != undefined) {
                useDefaultBrand = validBrands.find(x => x.Id == props.defaultBrand!.Id) != undefined;
            }
            const selectedBrand = useDefaultBrand ? props.defaultBrand! : validBrands[0];
            setBrand(selectedBrand);
            props.onSelectSection(selectedBrand)
        }
    }, [props.reportStructure, props.reportSection]);


    React.useEffect(() => {
        window.requestAnimationFrame(() => {
            const selectedElement = document.getElementById("activeSetDropdownItem");
            if (selectedElement) {
                setTimeout(() => selectedElement.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "start" }), 50);
            }
        });
    }, [isSectionDropdownOpen, searchQuery]);


    if (noBrandSelectorAvailable) {
        return <></>
    }
    return (<div className={style.container}>
        <label className={style.label} >{props.brandDefinition.Plural()}:</label>
        <ButtonDropdown isOpen={isSectionDropdownOpen} toggle={() => setIsSectionDropdownOpen(!isSectionDropdownOpen)}
            className="calculation-type-dropdown">

            <DropdownToggle className={style.dropDownToggle + (brand ? "" : " " + style.disabled)}>
                <div className={style.dropDownItem} >{brand?.GetDisplayName() ?? `No ${props.brandDefinition.Singular()} available`}</div>
                    {brands.length > 1 &&
                    <div><i className="material-symbols-outlined">arrow_drop_down</i></div>
                    }
            </DropdownToggle>

            {brands.length > 1 &&
                <DropdownMenu>
                    <SearchInput id="metric-search-input" text={searchQuery} onChange={(text) => setSearchQuery(text)} autoFocus={true} className={style.search} />
                    <DropdownItem divider />
                    <div className={style.dropdownMenu}>
                        {brands.filter(x => { if (searchQuery.length) { return x.GetDisplayName().toLocaleLowerCase().includes(searchQuery.toLocaleLowerCase()); } return true; }
                        ).map((item, id) =>
                            <DropdownItem id={item.Id == brand?.Id ? "activeSetDropdownItem" : ""}
                                className={item.Id == brand?.Id ? style.dropDownItemActive : style.dropDownItem}
                                key={id}
                                onClick={() => { setBrand(item); props.onSelectSection(item); }}
                                active={item.Id == brand?.Id}>
                                {item.GetDisplayName()}
                                </DropdownItem>
                            )}
                        </div>
                </DropdownMenu>
                }
            </ButtonDropdown>
        </div>
        );
}

export default BrandsMenu;
