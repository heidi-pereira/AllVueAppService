import { OverTimeResults } from '../../../../BrandVueApi';
import { getStackedOvertimeBarChartOptions } from './HighchartsOptions/StackedBarOptions';
import ReportsPageOvertimeCard, { IReportsPageOvertimeCardProps } from './ReportsPageOvertimeCard';
import { ReportOrder } from '../../../../BrandVueApi';
import { useAppSelector } from 'client/state/store';
import { selectCurrentReport } from 'client/state/reportSelectors';

type LocalOvertimeProps = IReportsPageOvertimeCardProps & { order: ReportOrder };

const ReportsPageOvertimeStackedBarCard = (props: LocalOvertimeProps) => {

    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const getHighchartOptions = (results: OverTimeResults, colourMap: Map<string, string>) =>
        getStackedOvertimeBarChartOptions(
            results,
            props.reportPart.metric!,
            props.curatedFilters.average,
            colourMap,
            report.decimalPlaces,
            report.highlightLowSample,
            report.isDataWeighted,
            props.order,
            report.displaySignificanceDifferences
        );

    return (
        <ReportsPageOvertimeCard {...props} getChartOptions={getHighchartOptions} />
    );
};

export default ReportsPageOvertimeStackedBarCard;
