import { useAppSelector } from 'client/state/store';
import {
    OverTimeResults
} from '../../../../BrandVueApi';
import { getOvertimeLineChartOptions } from './HighchartsOptions/LineOptions';
import ReportsPageOvertimeChart, { IReportsPageOvertimeChartProps } from './ReportsPageOvertimeChart';
import { selectCurrentReport } from 'client/state/reportSelectors';

const ReportsPageOvertimeLineChart = (props: IReportsPageOvertimeChartProps) => {
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
            report.includeCounts,
            true,
            false,
            report.highlightSignificance,
            (selectedWaveName) => {},
            report.displaySignificanceDifferences
        );

    return (
        <ReportsPageOvertimeChart {...props} getChartOptions={getHighchartOptions} />
    );
}

export default ReportsPageOvertimeLineChart;