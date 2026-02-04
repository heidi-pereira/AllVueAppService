import React from "react";
import { ActiveReport } from "../../../../../BrandVueApi";

import style from "./ReportCard.module.less";

interface IReportVueCard {
    report: ActiveReport;

    onSelect: () => void;
}

const ReportVueCard = (props: IReportVueCard) => {
    return (
        <div className={style.card}>
            <div className={style.lhhColumn }><i className="material-symbols-outlined">text_snippet</i></div>
            <div className={style.rhColumn }>
                <div onClick={props.onSelect} className={style.title}>{props.report.title ? props.report.title:"No title specified"}</div>
                <div className={style.about}>{props.report.username}</div>
                <div className={style.about}>{props.report.releaseDate?.toLocaleDateString()?? ""}</div>
                {/*{props.report.releaseDate}*/}
                
            </div>
        </div>
    );
}

export default ReportVueCard;
