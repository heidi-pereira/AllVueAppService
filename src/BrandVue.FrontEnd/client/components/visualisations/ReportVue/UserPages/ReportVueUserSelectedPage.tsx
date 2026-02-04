import React from "react";
import {ActiveReport, Factory, ReportVueProjectRelease} from "../../../../BrandVueApi";
import Throbber from "../../../throbber/Throbber";
import {BrandDefinition} from "../Visualization/ReportElements/BrandDefinition";
import {BrandRecord} from "../Visualization/ReportElements/BrandRecord";
import {DashboardLayoutStyles, ReportPage} from "../Visualization/ReportElements/ReportPage";
import {ReportSection} from "../Visualization/ReportElements/ReportSection";
import {ReportStructure} from "../Visualization/ReportElements/ReportStructure";
import ReportVueRender from "../Visualization/ReportVueDashboard";
import BrandsMenu from "./Controls/BrandsMenu";
import SectionsMenu from "./Controls/SectionsMenu";
import style from "./ReportVueUserSelectedPage.module.less";
import Tooltip from "../../../Tooltip";
import ReportVueUser404Page from "./ReportVueUser404Page";
import {UserContext} from "../../../../GlobalContext";
import {useLocation, useNavigate, useSearchParams} from "react-router-dom";
import {useWriteVueQueryParams} from "../../../helpers/UrlHelper";


interface IReportVueUserSelectedPage {
    selectedReport: ActiveReport;
    cancelSelection: () => void;
    skipInitialPage: boolean;
}


const ReportVueUserSelectedPage = (props: IReportVueUserSelectedPage) => {
    const [reportStructure, setReportStructure] = React.useState<ReportStructure | undefined>();
    const [reportPage, setReportPage] = React.useState<ReportPage | undefined>();
    const [reportFilterId, setReportFilterId] = React.useState<number>(1);
    const [brandId, setBrandId] = React.useState<number>(1);
    const [defaultBrand, setDefaultBrand] = React.useState<BrandRecord|undefined>(undefined);

    const [selectedSection, setSelectedSection] = React.useState<ReportSection>();
    const [brandDefinition, setBrandDefinition] = React.useState<BrandDefinition>(new BrandDefinition(undefined));
    const [pages, setPages] = React.useState<ReportPage[]>([]);
    const [fetchingData, setFetchingData] = React.useState<boolean>(false);
    const [displayThrobber, setDisplayThrobber] = React.useState<boolean>(false);
    const [activeRelease, setActiveRelease] = React.useState<ReportVueProjectRelease | undefined>(undefined);
    const queryParamForBrand: string = "item";
    const queryParamForSection: string = "section";
    const queryParamForPage: string = "page";
    const [searchParams] = useSearchParams();
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());
    
    const getValueFromURL = (param: string):string|null => {
        const valueFromQueryString = searchParams.get(param);
        return valueFromQueryString??null;
    }
    const pageNumberToURL = (index: number): string => {
        return (index + 1).toString();
    }

    const URLPageNumberToNumber = (urlNumber: number): number => {
        return urlNumber - 1;
    }

    const getPageNumberFromURL = (): number => {
        const pageNumber = getValueFromURL(queryParamForPage);
        if (pageNumber === null) {
            return 0;
        }
        const result = +pageNumber;
        if (Number.isNaN(result)) {
            return 0;
        }
        return URLPageNumberToNumber(result);
    }

    React.useEffect(() => {
        if (props.selectedReport.reportFile.length) {
            setFetchingData(true);
            var defaultPageId = getPageNumberFromURL();
            fetch(props.selectedReport.reportFile)
                .then(response => response.text())
                .then(data => {
                    setFetchingData(false);
                    setDisplayThrobber(false);
                    const jsonData = JSON.parse(data);
                    const reportStructure = new ReportStructure(jsonData);
                    setReportStructure(reportStructure);
                    let reportFilterId: number = 1;
                    let foundBrand = false;
                    const defaultBrandUniqueIdText = getValueFromURL(queryParamForBrand);
                    const defaultBrandUniqueId = defaultBrandUniqueIdText ? +defaultBrandUniqueIdText: null;
                    const defaultBrandName = getValueFromURL(queryParamForBrand);
                    const defaultSectionName = getValueFromURL(queryParamForSection);
                    if (defaultBrandUniqueIdText != undefined) {
                        let brand = reportStructure.BrandRecords.find(brand => brand.Id === defaultBrandUniqueId);
                        if (brand === undefined) {
                            brand = reportStructure.BrandRecords.find(brand => brand.BrandName === defaultBrandName);
                        }
                        if (brand !== undefined) {
                            const validPageIds = reportStructure.Sections.filter(section => !section.IsUnrelatedToBrand)
                                .flatMap(reportSection => reportSection.Pages.map(y => y.Id));
                            const firstMatchingPageContent = reportStructure.PageContents.find(
                                pageContent => pageContent.BrandId === brand!.Id &&
                                validPageIds.includes(pageContent.Id));
                            if (firstMatchingPageContent != undefined) {
                                reportFilterId = firstMatchingPageContent.ReportFilterId;
                                const reportPages = reportStructure.Sections.map(
                                    section => section.Pages.find(page => page.Id == firstMatchingPageContent.Id));
                                const validReportPages = reportPages.filter(x => x != undefined);
                                if (validReportPages.length > 0) {
                                    setDefaultBrand(brand);
                                    if (validReportPages[0] &&
                                        defaultPageId >= 0 &&
                                        defaultPageId < validReportPages[0].Section.Pages.length) {
                                        setReportPage(validReportPages[0]?.Section.Pages[defaultPageId]);
                                    } else {
                                        setReportPage(validReportPages[0]);
                                    }
                                    foundBrand = true;
                                    setBrand(brand);
                                }
                            }
                        }
                    } else if (defaultSectionName != undefined) {
                        const section = reportStructure.Sections.find(section => section.Name == defaultSectionName);
                        if (section != undefined) {
                            const selectedPage = section.Pages[defaultPageId];
                            const firstPage =
                                reportStructure.PageContents.find(section => section.Id == selectedPage.Id);
                            if (firstPage != undefined) {
                                var brand = reportStructure.BrandRecords.find(brand => brand.Id == firstPage?.BrandId);
                                if (brand != undefined) {
                                    setDefaultBrand(brand);
                                    setReportPage(selectedPage);
                                    foundBrand = true;
                                    setBrand(brand);
                                }
                            }
                        }
                    }
                    if (!foundBrand) {
                        const page = reportStructure.GetDefaultPage();
                        if (!defaultBrandUniqueIdText) {
                            setReportPage(page);
                        }
                        reportFilterId = (reportStructure.PageContents && reportStructure.PageContents.length > 0)
                            ? reportStructure.PageContents[0].ReportFilterId
                            : 1;
                    }
                    setReportFilterId(reportFilterId);
                    setBrandDefinition(reportStructure.BrandDefinition);

                    const client = Factory.ReportVueClient(error => error());
                    client.getActivePublishedStats(props.selectedReport.title).then(
                        (releases) => {
                            setActiveRelease(releases[0]);
                        });

                }).catch(() => {
                    setFetchingData(false);
                    setDisplayThrobber(false);
                });
        }
    }, [props.selectedReport]);

    React.useEffect(() => {
            const handler = setTimeout(() => {
                    if (fetchingData) {
                        setDisplayThrobber(true);
                    }
                },
                50);
            return () => { clearTimeout(handler) }
        },
        [fetchingData]);


    const validPages = (section: ReportSection, brandId): ReportPage[] => {
        const pages: ReportPage[] = [];
        section.Pages.forEach(page => {
            if (reportStructure?.DoesPageContentExist(page, undefined, brandId)) {
                pages.push(page);
            }
        });
        return pages;
    }
    const transformUrl = (value: string): string => {
        var parts = props.selectedReport.path.split("\\");
        const path = parts.join("/");
        const transformedUrl = `${path}/${value}`
        return (transformedUrl);
    }

    const setSection = (section: ReportSection) => {
        setSelectedSection(section);
    }

    const setBrand = (brand: BrandRecord) => {
        if (brand) {
            const uniqueBrandId = brand.Id;
            let pageIndex = getPageNumberFromURL();
            if (reportStructure) {
                reportStructure.ActiveBrandId = brand.Id;
            }
            setBrandId(brand.Id);
            if (selectedSection) {
                const pages = validPages(selectedSection, brand.Id);
                setPages(pages);
                const oldReportPage = reportPage;
                if (oldReportPage) {
                    const matchingPageIndex = pages.findIndex(x => x.PageTitle == oldReportPage.PageTitle);
                    if (matchingPageIndex != -1) {
                        pageIndex = matchingPageIndex;
                    }
                }
                if (pageIndex< 0 || pageIndex >= pages.length) {
                    pageIndex = 0;
                }
                setQueryParameter(queryParamForPage, pageNumberToURL(pageIndex));
                setReportPage(pages[pageIndex]);
            }
            if (selectedSection != undefined) {
                if (selectedSection.IsUnrelatedToBrand) {
                    setQueryParameter(queryParamForSection, selectedSection.Name);
                    setQueryParameter(queryParamForPage, pageNumberToURL(pageIndex));

                    setQueryParameter(queryParamForBrand, "");
                }
                else {
                    setQueryParameter(queryParamForBrand, uniqueBrandId);
                    setQueryParameter(queryParamForSection, "");
                    setQueryParameter(queryParamForPage, pageNumberToURL(getPageNumberFromURL()));
                }
            }
        }
    }
    const interceptedSetReportPage = (page: ReportPage, index: number) => {
        setReportPage(page);
        setQueryParameter(queryParamForPage, pageNumberToURL(index));
    }

    const getPage = (page: ReportPage, index: number) => {
        return (<div key={"page" + page.Id} className={(page.Id === reportPage?.Id) ? style.active : style.inactive} onClick={() => interceptedSetReportPage(page, index)}>
            <div className={style.image}> <i className={"material-symbols-outlined "}>article</i></div>
            <div className={style.text}>{page.PageTitle}</div>
        </div>);

    }

    const isFullWidthReport = (reportPage: ReportPage | undefined): boolean => {
        return reportPage != undefined && reportPage.DashboardLayoutStyle == DashboardLayoutStyles.FitMainPlaceholder;
    }

    const getTip = (selectedReport: ActiveReport) => {
        return <div className={style.tooltip} >
            <div className={style.header}>{selectedReport.title}</div>
            {reportStructure && reportStructure.DashboardBuildParamters.DashboardGenerationDateTime &&
                <>
                <div className={style.row}>
                    Generation date:
                    {reportStructure.DashboardBuildParamters.DashboardGenerationDateTime.toLocaleDateString()}
                </div>
                <div className={style.row}>
                    Time:
                    {reportStructure.DashboardBuildParamters.DashboardGenerationDateTime.toLocaleTimeString()}
                </div>
                </>
            }
            <div className={style.row}>
                Release date: {selectedReport.releaseDate.toLocaleDateString()}
            </div>
            <div className={style.row}>
                Time: {selectedReport.releaseDate.toLocaleTimeString()}
            </div>
            <div className={style.row}>
                User: {selectedReport.username}
            </div>
            {activeRelease &&  
                <div className={style.row}>
                    Version: {activeRelease.versionOfRelease}
                </div>
            }
            {reportStructure && reportStructure.DashboardBuildParamters.DesktopToolsVersion &&
                <div className={style.row}>
                    Desktop Tools Version:
                    {reportStructure.DashboardBuildParamters.DesktopToolsVersion}
                </div>
            }
            {reportStructure && reportStructure.DashboardBuildParamters.PowerpointTemplateFile &&
                <div className={style.row}>
                    Powerpoint Template File:
                    {reportStructure.DashboardBuildParamters.PowerpointTemplateFile}
                </div>
            }
            {reportStructure && reportStructure.DashboardBuildParamters.LastUpdateDataVueDateTime &&
                <>
                    <div className={style.row}>
                        Generation date:
                        {reportStructure.DashboardBuildParamters.LastUpdateDataVueDateTime.toLocaleDateString()}
                    </div>
                    <div className={style.row}>
                        Time:
                        {reportStructure.DashboardBuildParamters.LastUpdateDataVueDateTime.toLocaleTimeString()}
                    </div>
                </>
            }
            {reportStructure && reportStructure.DashboardBuildParamters.LastUpdateTemplateDateTime &&
                <>
                <div className={style.row}>
                    Powerpoint Template last update date:
                    {reportStructure.DashboardBuildParamters.LastUpdateTemplateDateTime.toLocaleDateString()}
                </div>
                <div className={style.row}>
                    Time:
                    {reportStructure.DashboardBuildParamters.LastUpdateTemplateDateTime.toLocaleTimeString()}
                </div>
            </>
            }

        </div>;
    }

    const displayTitle = !props.skipInitialPage;
    return (<div className={style.settingsPage }>
        <aside className={style.settingsSidePanel}>
            {displayTitle &&
            <div className={style.title}>
                    <div className={style.image}><i className="material-symbols-outlined ">text_snippet</i>
                        <span className={style.text}>{props.selectedReport.title} </span>
                    </div>
                    <div className={style.click} onClick={props.cancelSelection}><i className="material-symbols-outlined">menu_open</i></div>
            </div>
            }
            <div className={style.title}><SectionsMenu reportStructure={reportStructure} reportPage={reportPage!} onSelectSection={setSection} /> </div>
            <div className={style.title}><BrandsMenu brandDefinition={brandDefinition} reportStructure={reportStructure} reportSection={selectedSection!} onSelectSection={setBrand} defaultBrand={defaultBrand} /> </div>
            {selectedSection == undefined || !selectedSection.ArePagesHidden &&
                <div className={style.reportsListContainer} >
                    {pages.map(getPage)}
                </div>
            }
            <UserContext.Consumer>
                {(user) => {
                    if (user?.isAdministrator) {
                        return (<div className={style.help} >
                            <Tooltip placement="bottom" title={getTip(props.selectedReport)}>
                                <i className="material-symbols-outlined ">info</i>
                            </Tooltip>
                            </div>);
                        }
                    }
                }
            </UserContext.Consumer>
        </aside>
        {displayThrobber &&
            <div className={style.throbberCentral }>
                <Throbber />
             </div>
        }
        <div className={isFullWidthReport(reportPage) ? style.reportVueFullWidth : style.reportVue}>
            {reportStructure && reportPage &&
                <ReportVueRender
                reportPage={reportPage}
                reportPageContents={reportStructure.GetPageContents(reportPage, reportFilterId, brandId)}
                report={reportStructure}
                transformUrl={transformUrl}
                withDetailedInformation={false}
                brandId={brandId}
                />
            }
            {reportStructure && !reportPage &&
                <ReportVueUser404Page
                report={reportStructure}
                brandName={getValueFromURL(queryParamForBrand)}
                onSelectDefaultPage={() => {
                    const page = reportStructure.GetDefaultPage();
                    setReportPage(page);
                }}
                />
                }

        </div>
    </div>
    );
}

export default ReportVueUserSelectedPage;
