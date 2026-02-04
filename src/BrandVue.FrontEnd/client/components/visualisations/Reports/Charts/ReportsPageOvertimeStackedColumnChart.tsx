import { OverTimeResults } from '../../../../BrandVueApi';
import { getStackedOvertimeColumnChartOptions } from './HighchartsOptions/StackedColumnOptions';
import ReportsPageOvertimeChart, { IReportsPageOvertimeChartProps } from './ReportsPageOvertimeChart';
import { useAppSelector } from 'client/state/store';
import { selectCurrentReport } from 'client/state/reportSelectors';

const ReportsPageOvertimeStackedColumnChart = (props: IReportsPageOvertimeChartProps) => {
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const getHighchartOptions = (results: OverTimeResults, colourMap: Map<string, string>) =>
        getStackedOvertimeColumnChartOptions(
            results,
            props.reportPart.metric!,
            props.curatedFilters.average,
            colourMap,
            report.decimalPlaces,
            report.highlightLowSample,
            report.isDataWeighted,
            props.order,
            (e) => {}, //not supported in this chart type
            props.reportPart.part.displayMeanValues,
            report.displaySignificanceDifferences,
        );

    return (
        <ReportsPageOvertimeChart {...props} getChartOptions={getHighchartOptions} />
    );
};

export default ReportsPageOvertimeStackedColumnChart;