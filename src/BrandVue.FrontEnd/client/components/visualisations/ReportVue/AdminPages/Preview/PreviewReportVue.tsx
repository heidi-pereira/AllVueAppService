import React from "react";
import { ReportStructure } from "../../Visualization/ReportElements/ReportStructure";
import { ReportPage } from "../../Visualization/ReportElements/ReportPage";
import { ReportSection } from "../../Visualization/ReportElements/ReportSection";

import PreviewReportVueMenus from "./PreviewReportVueMenus";
import ReportVueRender  from "../../Visualization/ReportVueDashboard";
import style from "./PreviewReportVue.module.less";
import { BrandDefinition, SectionDefinition } from "../../Visualization/ReportElements/BrandDefinition";


interface IPreviewReportVue {
    fileToPreview: string;
    urlOfFileToPreview: string;
    publishCurrentFile: (title:string) => void;
    setTitle: (title: string) => void;
    cancelPreview: () => void;
}

const PreviewReportVue = (props: IPreviewReportVue) => {

    const [reportPage, setReportPage] = React.useState<ReportPage|undefined>();
    const [reportFilterId, setReportFilterId] = React.useState<number>(1);
    const [brandId, setBrandId] = React.useState<number>(1);
    const [reportStructure, setReportStructure] = React.useState<ReportStructure | undefined>();
    const [debuggingMode, setDebuggingMode] = React.useState<boolean>(false);
    const [brandDefinition, setBrandDefinition] = React.useState<BrandDefinition>(new BrandDefinition(undefined));
    const [sectionDefinition, setSectionDefinition] = React.useState<SectionDefinition>(new SectionDefinition(undefined));

    React.useEffect(() => {
        if (props.urlOfFileToPreview.length) {
            fetch(props.urlOfFileToPreview)
                .then(response => response.text())
                .then(data => {
                    const jsonData = JSON.parse(data);
                    const reportStructure = new ReportStructure(jsonData);
                    setReportStructure(reportStructure);
                    const page = reportStructure.GetDefaultPage();

                    setReportPage(page);
                    props.setTitle(reportStructure.DashboardTitle);
                    setBrandDefinition(reportStructure.BrandDefinition);
                    setSectionDefinition(reportStructure.SectionDefinition);
                    if (reportStructure.BrandRecords && reportStructure.BrandRecords.length > 0) {
                        const pageContent = reportStructure.GetPageContents(page);
                        if (pageContent) {
                            setBrandId(pageContent.BrandId);
                            setReportFilterId(pageContent?.ReportFilterId)
                        }
                        else {
                            setBrandId(reportStructure.BrandRecords[0].Id);
                            if (reportStructure.PageContents && reportStructure.PageContents.length > 0) {
                                setReportFilterId(reportStructure.PageContents[0].ReportFilterId);
                            }
                        }
                    }
                })
        }
    }, [props.urlOfFileToPreview]);

    const setThisReportPage = (value: ReportPage): void => {
        setReportPage(value);
        if (!reportStructure?.DoesPageContentExist(value, reportFilterId, brandId)) {
            const result = reportStructure?.GetPageContents(value, reportFilterId, brandId);
            if (result) {
                setBrandId(result.BrandId);
                if (reportStructure) {
                    reportStructure.ActiveBrandId = result.BrandId;
                }
                setReportFilterId(result.ReportFilterId);
            }
        }
    }

    const setFilterForPage = (filterId: number): void => {
        if (reportPage) {
            if (!reportStructure?.DoesPageContentExist(reportPage, filterId, brandId)) {
                const defaultPage = reportStructure?.GetPageContents(reportPage, filterId, brandId);
                if (defaultPage) {
                    setBrandId(defaultPage.BrandId);
                }
            }
        }
        setReportFilterId(filterId);

    }
    const transformUrl = (value: string): string => {
        var parts = props.urlOfFileToPreview.split("\\");
        parts.splice(parts.length - 1, 1)
        const path = parts.join("/");
        const transformedUrl = `${path}/${value}`
        return (transformedUrl)
    }

    const titleBar = () => {
        const parts = props.fileToPreview.split("\\");
        const folder = parts.splice(0, parts.length - 1).join("\\");
        return <div className={style.title}>
            <div className={style.titleText}>Preview ({folder}-<strong>{reportStructure?.DashboardTitle}</strong>)</div>
            <div className={style.closeIcon}>
                <button type="button" className="btn btn-close" onClick={() => props.cancelPreview()}>
                </button>
            </div>
        </div>
    }
    return (<div className={style.container }>
        <aside className={style.settingsSidePanel}>
            <PreviewReportVueMenus
                reportStructure={reportStructure}
                setReportPage={setThisReportPage}
                reportPage={reportPage}
                filterTemplateId={reportFilterId}
                setFilterTemplateId={setFilterForPage}
                brandId={brandId}
                setBrandId={setBrandId}
                debuggingMode={debuggingMode}
                setDebuggingMode={setDebuggingMode}
                publishCurrentFile={props.publishCurrentFile}
                transformUrl={transformUrl}
                brandDefinition={brandDefinition}
                sectionDefinition={sectionDefinition!}
            />
        </aside>
        {reportStructure != undefined && reportPage &&
            < div className={style.previewContainter }>
            { titleBar() }
                <ReportVueRender
                    reportPage={reportPage}
                    reportPageContents={reportStructure.GetPageContents(reportPage, reportFilterId, brandId)}
                    report={reportStructure}
                    transformUrl={transformUrl}
                    withDetailedInformation={debuggingMode}
                    brandId={brandId }
            />
            </div>
            }
        </div>
    );
}

export default PreviewReportVue;
