import React from "react";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from "reactstrap";
import ButtonThrobber from "../../../../throbber/ButtonThrobber";
import { PageTemplate } from "../../Visualization/ReportElements/PageTemplate";
import { ReportStructure } from "../../Visualization/ReportElements/ReportStructure";
import { ZoneContents } from "../../Visualization/ReportElements/ZoneContents";
import { ProductConfigurationContext } from "../../../../../ProductConfigurationContext";


import style from "./PreviewReportVueMenus.module.less";


interface IAdvancedDiagnostics {
    reportStructure: ReportStructure | undefined;
    transformUrl: (value: string) => string;
}

const AdvancedDiagnostics = (props: IAdvancedDiagnostics) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);


    const [isSectionDropdownOpen, setIsSectionDropdownOpen] = React.useState<boolean>(false);
    const [lookupOfUrls, setLookupOfUrls] = React.useState<{}>({});
    const [isBusy, setIsBusy] = React.useState<boolean>(false);
    function fileNameToDownload(part: string): string {
        const fileName = `${productConfiguration.subProductId}-${props.reportStructure?.DashboardTitle??"NoTitle"}-${part}-private.csv`
        return fileName;
    }
    function download(filename, text) {
        var element = document.createElement('a');
        element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(text));
        element.setAttribute('download', filename);

        element.style.display = 'none';
        document.body.appendChild(element);

        element.click();

        document.body.removeChild(element);
    }
    const countNumberOfPages = (pageId: number, reportFilterId: number, brandId: number): number => {
        const rs = props.reportStructure;
        let count = 0;
        if (rs) {
            for (var i = 0; i < rs.PageContents.length; i++) {
                var pageContents = rs.PageContents[i];
                if (pageContents.Id == pageId) {
                    const reportFilterMatch = (reportFilterId == undefined) ? true : pageContents.ReportFilterId == reportFilterId;
                    const brandFilterMatch = (brandId == undefined) ? true : pageContents.BrandId == brandId;
                    if (reportFilterMatch && brandFilterMatch)
                        count++;
                }
            }
        }
        return count;
    }

    
    const getContentReport = () : string => {
        let output = "";
        const rs = props.reportStructure;
        if (rs) {
            if (rs.BrandRecords.length == 0) {
                output += "CAUTION: NO BRANDS defined\n";
            }
            if (rs.FilterTemplates.length == 0) {
                output += "CAUTION: NO FilterTemplates defined\n";
            }
            output += "pageId,brandId,FilterId,Section,Page Title,Brand Name,Filter,PageTemplate,exists,Zones,Duplicate";
            for (let sIndex = 0; sIndex < rs.Sections.length; sIndex++) {
                const section = rs.Sections[sIndex];

                for (let pIndex = 0; pIndex < section.Pages.length; pIndex++) {
                    const page = section.Pages[pIndex];
                    for (var ftIndex = 0; ftIndex < rs.FilterTemplates.length; ftIndex++) {
                        const filterTemplate = rs.FilterTemplates[ftIndex];
                        for (let bIndex = 0; bIndex < rs.BrandRecords.length; bIndex++) {
                            const brand = rs.BrandRecords[bIndex];

                            if (rs.DoesPageContentExist(page, filterTemplate.Id, brand.Id)) {
                                const pageContent = rs.GetPageContents(page, filterTemplate.Id, brand.Id);
                                const pageTemplate: PageTemplate = rs.PageTemplateLookup[page.PageTemplateName];
                                output += (`\r\n${page.Id},${brand.Id},${filterTemplate.Id},"${section.Name}","${page.PageTitle}","${brand.GetDisplayName()}","${filterTemplate.Name}"`);
                                output += `,"${page.PageTemplateName}",${pageTemplate ? "true" : "false"}`
                                if (pageTemplate && pageTemplate.Zones) {
                                    output += `,${pageTemplate.Zones.length}`
                                }
                                else {
                                    output += ',ERROR: NO ZONES'
                                }
                                const matches = countNumberOfPages(page.Id, filterTemplate.Id, brand.Id);
                                output += `,${matches == 1 ? '' : (`ERROR: ${matches - 1} Duplicates`)}`
                            }
                        }
                    }
                }
            }
        }
        else {
            output = "Report is not set";
        }
        return output;
    }

    const downloadData = () => {
        if (props.reportStructure) {
            download(fileNameToDownload("layout"), getContentReport())
        }
    }

    async function doesImageExist(fileName: string) {
        if (fileName == null || fileName == undefined || fileName.length == 0)
            return false;

        if (lookupOfUrls[fileName] != undefined) {
            return lookupOfUrls[fileName];
        }
        const urlToVerify = props.transformUrl(fileName);
        XMLHttpRequest 

        try {
            const response = await fetch(urlToVerify);
            if (response.status !== 200) {
                lookupOfUrls[fileName] = false;
                return false;
            }
            lookupOfUrls[fileName] = true;
            return true;
        }
        catch (error) { }
        lookupOfUrls[fileName] = false;
        return false;
    }

    async function runImageReport() {
        setLookupOfUrls({});
        let output = "pageId,brandId,FilterId,fileName,url,location,exists";
        for (let index = 0; index < props.reportStructure!.PageContents.length; index++) {
            const pageContents = props.reportStructure!.PageContents[index];
            if (pageContents.BackdropSvg && pageContents.BackdropSvg.length > 0) {
                output += `\r\n${pageContents.Id}, ${pageContents.BrandId}, ${pageContents.ReportFilterId},"${pageContents.BackdropSvg}","${props.transformUrl(pageContents.BackdropSvg)}","Bckdrop page", ${await doesImageExist(pageContents.BackdropSvg)}`;
            }
            let page = props.reportStructure?.Sections[0].Pages[0];
            props.reportStructure?.Sections.forEach(section => {
                section.Pages.forEach(myPage => {
                    if (myPage.Id == pageContents.Id) {
                        page = myPage;
                    }
                })
            });
            const urls: string[] = [];
            const zone: string[] = [];
            const pageTemplate: PageTemplate = props.reportStructure!.PageTemplateLookup[page!.PageTemplateName];
            if (pageTemplate && pageTemplate.Zones) {
                pageTemplate.Zones.forEach((z, index) => {
                    const zoneContents: ZoneContents = pageContents?.ZoneContentsLookup[index];
                    if (zoneContents) {
                        switch (zoneContents.ContentType) {
                            case "Svg":
                                if (!zoneContents.Value.startsWith("<")) {
                                    urls.push(zoneContents.Value);
                                    zone.push(`SVG ${index}`)
                                }
                                break;
                            case "Jpg":
                                urls.push(zoneContents.Value);
                                zone.push(`JPG ${index}`)
                                break;

                            case "Png":
                                urls.push(zoneContents.Value);
                                zone.push(`PNG ${index}`)
                                break;
                        }
                    }
                });
            }
            for (let i = 0; i < urls.length; i++) {
                output += `\r\n${pageContents.Id}, ${pageContents.BrandId}, ${pageContents.ReportFilterId},"${urls[i]}","${props.transformUrl(urls[i])}","${zone[i]}", ${await doesImageExist(urls[i])}`;
            }
        }
        return output;
    }
    const imageReport = () => {
        if (props.reportStructure) {
            setIsBusy(true);
            runImageReport().then(output => { download(fileNameToDownload("image"), output); setIsBusy(false); });
        }
    }


    return (<div className={style.container}>
        <ButtonDropdown isOpen={isSectionDropdownOpen}  toggle={() => setIsSectionDropdownOpen(!isSectionDropdownOpen)} className={"calculation-type-dropdown " + style.advancedDropDown}>
            <DropdownToggle disabled={isBusy}>
                <div className={style.advancedButton} >
                    <i className="material-symbols-outlined">file_download</i>
                    {isBusy && <div className={style.throbberContainer }><ButtonThrobber /></div>}
                </div>
                 </DropdownToggle>

                 <DropdownMenu>
                    <DropdownItem key={1} onClick={() => downloadData()} >Advanced  - Section/Page Layout</DropdownItem>
                    <DropdownItem key={2} onClick={() => imageReport() } >Advanced - Image Report</DropdownItem>
                  </DropdownMenu>
            </ButtonDropdown>
        </div>
    );

}
export default AdvancedDiagnostics;

