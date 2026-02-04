import * as BrandVueApi from "../../../../BrandVueApi";
import { getOvertimeColumnChartOptions } from './HighchartsOptions/ColumnOptions';
import { OverTimeResults } from '../../../../BrandVueApi';
import ReportsPageOvertimeChart, { IReportsPageOvertimeChartProps } from './ReportsPageOvertimeChart';
import { useAppSelector } from "client/state/store";
import { selectCurrentReport } from "client/state/reportSelectors";

const ReportsPageOvertimeColumnChart = (props: IReportsPageOvertimeChartProps) => {
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const getHighchartOptions = (results: OverTimeResults, colourMap: Map<string, string>) =>
        getOvertimeColumnChartOptions(
            results.entityWeightedDailyResults,
            props.reportPart.metric!,
            props.curatedFilters.average,
            colourMap,
            report.decimalPlaces,
            props.curatedFilters.average.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell,
            report.highlightLowSample,
            (a, b) => {},
            report.displaySignificanceDifferences
        );

    return (
        <ReportsPageOvertimeChart {...props} getChartOptions={getHighchartOptions} />
    );
};

export default ReportsPageOvertimeColumnChart;