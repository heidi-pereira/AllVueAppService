import { getOvertimeBarChartOptions } from './HighchartsOptions/BarOptions';
import { OverTimeResults } from '../../../../BrandVueApi';
import ReportsPageOvertimeCard, { IReportsPageOvertimeCardProps } from './ReportsPageOvertimeCard';
import { useAppSelector } from 'client/state/store';
import { selectCurrentReport } from 'client/state/reportSelectors';

const ReportsPageOvertimeBarCard = (props: IReportsPageOvertimeCardProps) => {

    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const getHighchartOptions = (results: OverTimeResults, colourMap: Map<string, string>) =>
        getOvertimeBarChartOptions(
            results.entityWeightedDailyResults,
            props.reportPart.metric!,
            props.curatedFilters.average,
            colourMap,
            report.decimalPlaces,
            report.highlightLowSample,
            report.isDataWeighted,
            report.displaySignificanceDifferences
        );

    return (
        <ReportsPageOvertimeCard {...props} getChartOptions={getHighchartOptions} />
    );
};

export default ReportsPageOvertimeBarCard;