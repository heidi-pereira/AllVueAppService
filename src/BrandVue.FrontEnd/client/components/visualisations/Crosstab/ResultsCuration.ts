import { CrosstabResults, ReportOrder, InstanceResult, CellResult, FieldOperation } from "client/BrandVueApi";
import { Metric } from "client/metrics/metric";
import _ from "lodash";


export interface CuratedCrosstabResult {
    crosstabResult: CrosstabResults;
    canIncludeCounts: boolean;
    curatedResults: InstanceResult[];
    numberOfEmptyRows: number;
}

export const CurateResults = (results: CrosstabResults, metric: Metric, showTop?: number, hideEmptyRows?: boolean, resultSortingOrder?: ReportOrder) => {


    const totalScoreColumn = "Total";

    const hasNonZeroData = (r: InstanceResult) => {
        const cellResults: CellResult[] = Object.values(r.values);
        return cellResults.some(data => data?.count != null && data.count > 0);
    };

    const getNumberOfEmptyRows = () => {
        if (hideEmptyRows) {
            const rowsOfNoData = results.instanceResults.filter(r => !hasNonZeroData(r));
            return rowsOfNoData.length;
        }
        return 0;
    };


    const getOrderedResults = () => {
        switch (resultSortingOrder) {
            case ReportOrder.ScriptOrderAsc:
                return [...results.instanceResults].reverse();
            case ReportOrder.ResultOrderDesc:
                return _.orderBy(results.instanceResults, x => x.values[totalScoreColumn].result, "desc");
            case ReportOrder.ResultOrderAsc:
                return _.orderBy(results.instanceResults, x => x.values[totalScoreColumn].result, "asc");
            default:
                return results.instanceResults;
        }
    };

    const getCuratedResults = () => {
        let orderedResults = getOrderedResults();
        if (hideEmptyRows) {
            orderedResults = orderedResults.filter(r => hasNonZeroData(r));
        }
        if (showTop) {
            orderedResults = orderedResults.slice(0, showTop);
        }
        return orderedResults;
    };


    const curatedResults = getCuratedResults();



    const allCounts = curatedResults.map(c => {
        for (const [key, value] of Object.entries(c.values)) {
            return value.count;
        }
    });

    const canIncludeCounts = metric.fieldOperation == FieldOperation.None && allCounts.every(f => f != undefined);
    return { canIncludeCounts, curatedResults, numberOfEmptyRows: getNumberOfEmptyRows() };
};
