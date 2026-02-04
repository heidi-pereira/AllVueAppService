import React from "react";
import style from "./ReportVueUser404Page.module.less";
import { ReportStructure } from "../Visualization/ReportElements/ReportStructure";

interface IReportVueUser404Page {
    report: ReportStructure;
    brandName: string|null,
    onSelectDefaultPage: () => void;
}


const ReportVueUser404Page = (props: IReportVueUser404Page) => {

    const page = props.report.GetDefaultPage();

    return (<div className={style.container}>
        <div className={style.error }>
            <div className={style.title}>Unable to locate <span className={style.brandName}>{props.report.BrandDefinition.Singular()} {props.brandName??"Unknown asset"}</span> </div>
            <div className={style.action}>Click <a onClick={props.onSelectDefaultPage}>here</a> to go to the home page</div>
        </div>
    </div>
    );
}

export default ReportVueUser404Page;
