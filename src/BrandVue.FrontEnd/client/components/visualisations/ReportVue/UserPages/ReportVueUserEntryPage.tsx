import React from "react";
import { ActiveReport} from "../../../../BrandVueApi";
import SearchInput from "../../../SearchInput";
import style from "./ReportVueUserEntryPage.module.less";
import ReportVueCard from "./Controls/ReportCard";

interface IReportVueUserEntryPage {
    setSelectedReport: (report: ActiveReport) => void;
    reports: ActiveReport[];
}


const ReportVueUserEntryPage = (props: IReportVueUserEntryPage) => {

    const [searchText, setSearchText] = React.useState<string>("");

    const filtered = props.reports.filter(filter => {

        if (searchText && filter.title.includes(searchText, 0) == false) {
            return false;
        }
        return true;
    });

    return (
        <div className={style.settingsPage }>
            <aside className={style.settingsSidePanel}>

                <div className={style.reportsListContainer }>
                    <div className={style.reportsSearchContainer} >
                        <SearchInput id="report-search" onChange={(text) => setSearchText(text)} text={searchText} className={style.reportSearchInputGroup} autoFocus={true} />
                    </div>

                    <div className={style.reportList} >
                        {props.reports == null || props.reports.length == 0 &&
                            <div>No reports published yet</div>
                        }
                        {filtered.map(report => { return <ReportVueCard key={report.id} report={report} onSelect={() => props.setSelectedReport(report)} /> })}
                    </div>
                </div>

        </aside>
        </div>
    );
}

export default ReportVueUserEntryPage;
