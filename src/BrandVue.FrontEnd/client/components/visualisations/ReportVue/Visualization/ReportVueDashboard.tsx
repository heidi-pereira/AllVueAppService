import React from "react";
import { ReportPageContents } from "../Visualization/ReportElements/ReportPageContents";
import { ReportStructure } from "../Visualization/ReportElements/ReportStructure";
import { DashboardLayoutStyles, DashboardRepeatBehaviours, ReportPage } from "../Visualization/ReportElements/ReportPage";
import { PageTemplate } from "../Visualization/ReportElements/PageTemplate";
import { ZoneContents } from "../Visualization/ReportElements/ZoneContents";
import { PageZone } from "../Visualization/ReportElements/PageZone";
import { FormatTypes } from "../Visualization/ReportElements/Format";
import { Box } from "../Visualization/Base/Box";
import { Scorecard } from "./ReportElements/Scorecard/Scorecard";
import { CommentTable } from "./ReportElements/CommentTable/CommentTable";

const onError = (embeddedImage: HTMLImageElement, altText: string) => {
    embeddedImage.onerror = null;
    embeddedImage.alt = altText;
}
class ReportVueDashboard {
    static _scorecard: Scorecard | undefined = undefined;

    public Report: ReportStructure;
    public TransformUrl: (value: string) => string;
    public constructor(report: ReportStructure) {
        this.Report = report;
    }
    static Update() {
        if (ReportVueDashboard._scorecard) {
            ReportVueDashboard._scorecard.UpdateScorecard();
        }
    }

    private ShowBlankPage(container: HTMLDivElement, pageName: string) {
        var contentFrame = document.createElement("div");
        contentFrame.id = "contentframe-blank";
        contentFrame.className = "backBorderBlank"
        contentFrame.style.width = this.Report.SlideWidth + "px";
        contentFrame.style.height = this.Report.SlideHeight + "px";
        container.appendChild(contentFrame);

        this.AddPageTitle(contentFrame, pageName + " - No Data", false);
    }
    private AddLoadingMessage(container: HTMLDivElement, parent: HTMLDivElement): HTMLDivElement {
        var innerDiv = document.createElement("div");

        innerDiv.style.position = "absolute";
        innerDiv.style.left = parent.style.left;
        innerDiv.style.top = parent.style.top;
        innerDiv.style.width = parent.style.width;
        innerDiv.style.height = parent.style.height;
        innerDiv.style.animation = "fadein 4s";

        innerDiv.innerHTML = "Loading scorecard...";
        container.appendChild(innerDiv);
        return innerDiv;
    }

    private DeleteLoadingMessage(element: HTMLDivElement) {
        element.style.display = "none";
    }

    private debugPageDetails(page: ReportPage, container: HTMLDivElement, withDetailedInformation : boolean) {
        if (withDetailedInformation) {
            const debugDetails = document.createElement("div");
            debugDetails.className = "debugPage";

            const description = `Id: ${page.Id}<br> Title:${page.PageTitle}<br> PageTemplateName:${page.PageTemplateName}<br> SuppressTemplatePageTitle:${page.SuppressTemplatePageTitle}<br> FilterTemplateName:${page.FilterTemplateName}<br> DashboardLayoutStyle:${DashboardLayoutStyles[page.DashboardLayoutStyle]}<br> DashboardRepeatBehaviour:${DashboardRepeatBehaviours[page.DashboardRepeatBehaviour]}`;
            debugDetails.innerHTML = description;
            container.appendChild(debugDetails);
        }
    }

    private debugZoneDetails(zoneIsValid: boolean, pageZone: PageZone, zoneContents: ZoneContents, zoneHTMLDiv: HTMLDivElement, withDetailedInformation: boolean, extraDetails: string) {
        if (withDetailedInformation) {
            if (!zoneIsValid) {
                extraDetails = "Zone will be ignored";
            }
            const debugDetails = document.createElement("div");
            debugDetails.className = "debug";
            const zoneType = (zoneContents ? zoneContents.ContentType : "??");
            const zoneDescription = zoneType == pageZone.ContentType ? zoneType : `${zoneType}/${pageZone.ContentType}`;
            debugDetails.innerHTML = `${pageZone.Template.Name}-${zoneHTMLDiv.id} '${zoneDescription}' ${extraDetails} (${pageZone.Box.X}, ${pageZone.Box.Y}) ${pageZone.Box.Width} x ${pageZone.Box.Height}`
            zoneHTMLDiv.appendChild(debugDetails);
        }

    }

    public async ShowPage(container: HTMLDivElement,
        page: ReportPage | null,
        pageContents: ReportPageContents | null,
        transformUrl: (value: string) => string,
        withDetailedInformation: boolean) {
        ReportVueDashboard._scorecard = undefined;
        this.TransformUrl = transformUrl;
        if (page == null) {
            this.ShowBlankPage(container, "No Page");
        }
        else if (pageContents == null) {
            this.ShowBlankPage(container, page.PageTitle);
            this.debugPageDetails(page, container, withDetailedInformation);
        }
        else {
            this.debugPageDetails(page, container, withDetailedInformation);
            const me = this;
            const pageTemplate: PageTemplate = this.Report.PageTemplateLookup[page.PageTemplateName];
            const backBorder = document.createElement("div");
            backBorder.id = "background" + page.Id;
            if (page.DashboardLayoutStyle == DashboardLayoutStyles.FitMainPlaceholder) {
                backBorder.style.width = "100%";
                backBorder.style.height = "100%";
            } else {
                backBorder.className = "backBorder";
                backBorder.style.width = this.Report.SlideWidth + "px";
                backBorder.style.height = this.Report.SlideHeight + "px";
            }

            if (pageContents.BackdropSvg) {
                const url2Backdrop = transformUrl(pageContents.BackdropSvg);
                this.AddSvgBackDrop(page.PageTitle, backBorder, url2Backdrop, pageContents, page.DashboardLayoutStyle == DashboardLayoutStyles.FitMainPlaceholder);
            }
            container.appendChild(backBorder);
            const pageTitle: string = page.PageTitle && !page.SuppressTemplatePageTitle ? pageContents.ReplaceTags(page.PageTitle) : "";
            // Title if present
            if (pageTitle.length) {
                this.AddPageTitle(container, pageTitle, withDetailedInformation);
            }
            if (pageTemplate && pageTemplate.Zones) {
                pageTemplate.Zones.forEach(async (z, zoneIndex) => {

                    const zone = document.createElement("div");
                    zone.className = withDetailedInformation ? "zoneWithDetails" : "zone";
                    zone.style.left = z.Box.X + "px";
                    zone.style.top = z.Box.Y + "px";
                    zone.style.width = z.Box.Width + "px";
                    zone.style.height = z.Box.Height + "px";
                    zone.style.right = "0px";
                    if (page.DashboardLayoutStyle == DashboardLayoutStyles.FitMainPlaceholder) {
                        zone.style.right = "20px";
                        zone.style.display = "flex";
                        zone.style.flexDirection = "column";
                        zone.style.width = `calc(100% - ${zone.style.left} - ${zone.style.right} )`;
                        zone.style.height = `calc(100% - ${zone.style.top} - 10px)`;
                    }
                    zone.id = `Zone(${zoneIndex})`

                    const zoneContents: ZoneContents = pageContents?.ZoneContentsLookup[zoneIndex];
                    let extraDetails = "";
                    let zoneIsValid = true;

                    if (zoneContents) {
                        switch (zoneContents.ContentType) {

                            case "Text":
                                zoneIsValid = me.AddTextZone(zone, zoneIndex, z, zoneContents, pageContents);
                                break;
                            case "Svg":
                                extraDetails = me.AddSvgZone(page.PageTitle, zone, zoneIndex, z, zoneContents, transformUrl);
                                break;
                            case "Jpg":
                            case "Png":
                                extraDetails = me.AddImageZone(page.PageTitle, zone, zoneIndex, z, zoneContents, page, transformUrl);
                                break;
                            case "Chart":
                                extraDetails = me.AddChartZone(zone, zoneIndex, z, zoneContents);
                                break;
                            case "Scorecard":
                                extraDetails = await me.AddScorecardZone(this.AddLoadingMessage(container, zone), zone, zoneIndex, z, zoneContents, page, transformUrl);
                                break;
                            case "CommentTable":
                                extraDetails = await me.AddCommentTableZone(this.AddLoadingMessage(container, zone), zone, zoneIndex, z, zoneContents, page);
                                break;
                            default:
                                zone.innerHTML = `Unsupported Zone: ZoneContentType'${zoneContents.ContentType}' : ContentType'${z.ContentType}' : '${z.ChartName}' Value:'${zoneContents.Value}''`;
                                break;
                        }
                        this.debugZoneDetails(zoneIsValid, z, zoneContents, zone, withDetailedInformation, extraDetails);
                        if (zoneIsValid || withDetailedInformation) {
                            container.appendChild(zone);
                        }
                    }
                    else {
                        console.log(`ReportVue: ${page.PageTitle} Not rendering ${zone.id} (no content)`)
                    }
                });
            }
            else {
                if (!pageTemplate) {
                    console.log(`ReportVue: ${page.PageTitle} Not rendering - unable to find template '${page.PageTemplateName}'`);
                }
                else if (!pageTemplate.Zones) {
                    console.log(`ReportVue: ${page.PageTitle} Not rendering - Template '${page.PageTemplateName}' has NULL Zones`);
                }
            }
        }
    }

    private AddSvgZone(pageTitle: string, div: HTMLDivElement, zoneIndex: number, pageZone: PageZone, zoneContents: ZoneContents, transformUrl: (string: any) => string): string {
        this.SetZoneMargins(div, pageZone);
        let extraDetails = "Unknown";
        if (zoneContents.Value.startsWith("<")) {
            div.innerHTML = zoneContents.Value;
            extraDetails = `Embedded HTML ${zoneContents.Value.substring(0, 5)}...`;
        } else {
            var embeddedImage = document.createElement("img");
            div.appendChild(embeddedImage);
            embeddedImage.className = "svgZone";
            embeddedImage.onerror = function () { onError(embeddedImage, `${pageTitle} - Image for zone ${zoneIndex + 1} ${pageZone.Text}`) };
            const urlToImage = transformUrl(zoneContents.Value);
            embeddedImage.src = urlToImage;
            extraDetails = `<a target="_blank" href=${urlToImage}>${zoneContents.Value}</a>`;
        }
        return extraDetails;
    }
    

    private AddImageZone(pageTitle : string, div: HTMLDivElement, zoneIndex: number, pageZone: PageZone, zoneContents: ZoneContents, page: ReportPage, transformUrl: (string: any) => string): string {
        this.SetZoneMargins(div, pageZone);
        div.className += " " + "pngZone";
        const url = transformUrl(zoneContents.Value);

        const innerDiv = document.createElement("div");
        div.appendChild(innerDiv);

        const embeddedImage = document.createElement("img");
        innerDiv.appendChild(embeddedImage);
        embeddedImage.className = "pngImageInZone";
        embeddedImage.src = url;
        embeddedImage.onerror = function () { onError(embeddedImage, `${pageTitle} Image for zone ${zoneIndex + 1} ${pageZone.Text}`) };

        if (page.DashboardLayoutStyle == DashboardLayoutStyles.FitMainPlaceholder) {
            div.style.scrollBehavior = "";
            innerDiv.style.overflow = "auto";
            innerDiv.style.border = "1px solid #dddddd";
            embeddedImage.style.position = "unset";
            embeddedImage.style.width = "auto";
            embeddedImage.style.height = "auto";
        }

        return `<a target="_blank" href=${url}>${zoneContents.Value}</a>`;
    }

    private AddChartZone(div: HTMLDivElement, zoneIndex: number, pageZone: PageZone, zoneContents: ZoneContents): string {
        this.SetZoneMargins(div, pageZone);
        return `Ignoring Chart ${zoneContents.Value}`;
    }

    private async AddScorecardZone(loadingDiv: HTMLDivElement, div: HTMLDivElement, zoneIndex: number, pageZone: PageZone, zoneContents: ZoneContents, page: ReportPage, transformUrl: (string: any) => string): Promise<string> {
        this.SetZoneMargins(div, pageZone);
        var scorecard = await Scorecard.LoadScorecardFromJson(this.TransformUrl(zoneContents.Value));
        this.DeleteLoadingMessage(loadingDiv);
        if (scorecard) {
            scorecard.ReportStructure = this.Report;
            scorecard.ReportPage = page;
            scorecard.Render(div, page.DashboardLayoutStyle == DashboardLayoutStyles.FitMainPlaceholder);
            ReportVueDashboard._scorecard = scorecard;
        }
        else {
        }
        return `Scorecard: <a target="_blank" href=${transformUrl(zoneContents.Value)}>${zoneContents.Value}</a>`;
    }

    private async AddCommentTableZone(loadingDiv: HTMLDivElement, div: HTMLDivElement, zoneIndex: number, pageZone: PageZone, zoneContents: ZoneContents, page: ReportPage): Promise<string> {
        this.SetZoneMargins(div, pageZone);
        var commentTable = await CommentTable.LoadCommentTableFromJson(this.TransformUrl(zoneContents.Value));
        this.DeleteLoadingMessage(loadingDiv);
        if (commentTable) {
            commentTable.ReportStructure = this.Report;
            commentTable.Render(div);
        }
        return `CommentTable: ${zoneContents.Value}`;
    }

    private SetZoneMargins(div: HTMLDivElement, pageZone) {
        if (pageZone.MarginTop) {
            div.style.marginTop = pageZone.MarginTop + "px"
        }
        if (pageZone.MarginBottom) {
            div.style.marginBottom = pageZone.MarginBottom + "px"
        }
        if (pageZone.MarginLeft) {
            div.style.marginLeft = pageZone.MarginLeft + "px"
        }
        if (pageZone.MarginRight) {
            div.style.marginRight = pageZone.MarginRight + "px"
        }
    }

    private AddSvgBackDrop(pageTitle: string, containerDiv: HTMLDivElement, svgFile: string, pageContents: ReportPageContents, reSize: boolean) {
        var embeddedImage = document.createElement("img");
        containerDiv.appendChild(embeddedImage);
        embeddedImage.className = reSize ? "svgBackDropResize": "svgBackDrop";
        embeddedImage.src = svgFile;
        embeddedImage.onerror = function () { onError(embeddedImage, `${pageTitle} - background image`) };

        var html = embeddedImage.innerHTML;
        html = pageContents.ReplaceTags(html);
        embeddedImage.innerHTML = html;
    }

    private AddTextZone(div: HTMLDivElement, zoneIndex: number, pageZone: PageZone, zoneContents: ZoneContents, pageContents: ReportPageContents | null): boolean {

        if (!pageContents) {
            return false;
        }

        var innerDiv = document.createElement("div");
        div.appendChild(innerDiv);
        pageZone.ApplyStyle(div, innerDiv);
        var text = pageContents.ReplaceTags(zoneContents.Value);
        innerDiv.innerHTML = text;
        return (text != null && text.length > 0);
    }

    private AddPageTitle(div: HTMLDivElement, title: string, withDetailedInformation: boolean) {
        var innerDiv = document.createElement("div");
        innerDiv.style.position = "absolute";
        var format = this.Report.Formats[FormatTypes.SlideTitle];
        format.ApplyStyle(innerDiv);
        this.ApplySizeFromBox(innerDiv, this.Report.SlideTitleBox);
        innerDiv.innerHTML = format.AllCaps ? title.toUpperCase() : title;
        div.appendChild(innerDiv);

        if (withDetailedInformation) {
            innerDiv.className = "zoneWithDetails";
            const debugDetails = document.createElement("div");
            debugDetails.className = "debug";
            debugDetails.innerHTML = `Page Title '${title}' (${this.Report.SlideTitleBox.X}, ${this.Report.SlideTitleBox.Y}) ${this.Report.SlideTitleBox.Width} x ${this.Report.SlideTitleBox.Height}`
            innerDiv.appendChild(debugDetails);

        }
    }

    private ApplySizeFromBox(div: HTMLDivElement, box: Box) {
        div.style.left = box.X + "px";
        div.style.top = box.Y + "px";
        div.style.width = box.Width + "px";
        div.style.height = box.Height + "px";
    }
}

interface IReportVueRender {
    report: ReportStructure;
    reportPage: ReportPage | null;
    reportPageContents: ReportPageContents | null;
    brandId: number,

    transformUrl: (value: string) => string;
    withDetailedInformation: boolean;
}
const ReportVueRender = (props: IReportVueRender) => {

    const contentValueRef = React.useRef<HTMLDivElement>(null)
    const className = `${props.withDetailedInformation ? "reportVueDebugContainer" : "reportVueContainer"} ${props.reportPage?.DashboardLayoutStyle == DashboardLayoutStyles.FitMainPlaceholder ? "fitMainPlaceholder" : ""}`

    React.useEffect(() => {
        const handler = setTimeout(() => {
            if (contentValueRef.current) {
                contentValueRef.current.innerHTML = "";
                var dashboard = new ReportVueDashboard(props.report);
                dashboard.ShowPage(contentValueRef.current, props.reportPage, props.reportPageContents, props.transformUrl, props.withDetailedInformation);
            }
        }, 50)
        return () => { clearTimeout(handler) }
    }, [props.reportPage?.Id, props.withDetailedInformation, props.reportPageContents])
    React.useEffect(() => {
        if (props.reportPage) {
            ReportVueDashboard.Update();
        }
    }, [props.brandId])
    return (<div id="previewPage" className={className} ref={contentValueRef}></div>)
}
export default ReportVueRender;
