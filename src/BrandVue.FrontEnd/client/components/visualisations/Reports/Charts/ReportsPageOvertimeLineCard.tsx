import {
    OverTimeResults
} from '../../../../BrandVueApi';
import { getOvertimeLineChartOptions } from './HighchartsOptions/LineOptions';
import ReportsPageOvertimeCard, { IReportsPageOvertimeCardProps } from './ReportsPageOvertimeCard';
import { useAppSelector } from 'client/state/store';
import { selectCurrentReport } from 'client/state/reportSelectors';

const ReportsPageOvertimeLineCard = (props: IReportsPageOvertimeCardProps) => {

    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const getHighchartOptions = (results: OverTimeResults, colourMap: Map<string, string>) =>
        getOvertimeLineChartOptions(
            results,
            props.reportPart.metric!,
            props.curatedFilters.average,
            colourMap,
            report.decimalPlaces,
            report.highlightLowSample,
            report.isDataWeighted,
            false,
            true,
            report.highlightSignificance,
            () => { },
            report.displaySignificanceDifferences
        );

    return (
        <ReportsPageOvertimeCard {...props}
            handleHeight
            noScroll
            getChartOptions={getHighchartOptions}
            resizeElementClass="reports-page-line-card" />
    );
};

export default ReportsPageOvertimeLineCard;