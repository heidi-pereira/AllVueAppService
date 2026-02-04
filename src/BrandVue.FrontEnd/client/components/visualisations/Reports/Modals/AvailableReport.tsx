import style from './AvailableReport.module.less';
import { PageDescriptor, Report, ReportType } from '../../../../BrandVueApi';
import FuzzyDate from '../../../helpers/FuzzyDate';

interface IAvailableReport {
    report: Report;
    page: PageDescriptor | undefined;
    selected: boolean;
    onSelect: () => void;
}

const AvailableReport = (props: IAvailableReport) => {
    const parts = props.page?.panes[0].parts ?? [];

    return (
        <li className={`${props.selected ? style.itemSelected : style.item} ${style.itemWrapper}`} onClick={props.onSelect}>
            <div className={style.itemTopRow}>
                <div className={style.left}>
                    <div className={style.itemIcon}>
                        {props.report.reportType === ReportType.Chart ?
                            <i className="material-symbols-outlined rotate">bar_chart</i>
                            : <i className="material-symbols-outlined">table_chart</i>
                        }
                    </div>
                    <div className={style.itemMain}>
                        <div className={style.itemName}>{props.page?.displayName}</div>
                    </div>
                </div>
            </div>
            <div className={style.itemMetaRow}>
                <div className={style.itemMetaLeft}>
                    {parts.length ? parts.length : 0} item{parts.length === 1 ? '' : 's'}
                </div>
                <div className={style.itemMetaRight}>
                    <i className={`${style.calendar} material-symbols-outlined`}>calendar_month</i>
                    <FuzzyDate date={props.report.modifiedDate} lowerCase includePastFuture />
                </div>
            </div>
        </li>
    );
};

export default AvailableReport;
