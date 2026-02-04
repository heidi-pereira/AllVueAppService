import { MappedTableGrouping, TableItem, TableOptions } from "../TableBuilderTypes";
import { ReactNode, useEffect, useState } from "react";
import { useAppSelector } from "../../../state/store";
import { selectSubsetId } from "../../../state/subsetSlice";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import { CrosstabulatedResults, Factory, SigConfidenceLevel, Significance, WeightedDailyResult } from "../../../BrandVueApi";
import { getSignificanceLevelText, mapTableGroupings, tableItemsToTemporaryVariableRequestModel } from "../TableBuilderUtils";
import StyledTable from "./StyledTable";
import styles from './StyledTable.module.less';
import { MRT_ColumnDef, MRT_SortingFn } from "material-react-table";
import { getFormattedValueText } from "../../../components/helpers/SurveyVueUtils";
import { Stack, Typography } from "@mui/material";
import { SortingState } from "@tanstack/table-core";
import { selectTimeSelection } from "../../../state/timeSelectionStateSelectors";
import { Metric } from "../../../metrics/metric";
import { calculateIndexScore } from "./TableDataAnalysis";
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';

interface Props {
    rows: TableItem[];
    columns: TableItem[];
    options: TableOptions;
    curatedFilters: CuratedFilters;
}

//we copy the rows/cols into state so that there isn't a disconnect between these and the results while waiting for network request
interface TableData {
    groupedRows: MappedTableGrouping[];
    groupedCols: MappedTableGrouping[];
    results: CrosstabulatedResults[];
}

interface DataRow {
    rowGrouping: number;
    name: string;
    results: WeightedDailyResult[];
    isSummaryRow?: boolean;
}

const defaultConfidenceLevel = SigConfidenceLevel.NinetyFive;

const DataTable = (props: Props) => {
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [tableData, setTableData] = useState<TableData>({
        groupedRows: mapTableGroupings(props.rows, true),
        groupedCols: mapTableGroupings(props.columns, false),
        results: []
    });
    const [sorting, setSorting] = useState<SortingState>([]);

    const timeSelection = useAppSelector(selectTimeSelection);
    const subsetId = useAppSelector(selectSubsetId);
    const sampleSize = hasEqualSampleSizesForTotals(tableData.results) ?
        tableData.results[0].data[0].weightedDailyResults[0].weightedSampleSize?.toString() ?? "-" :
        undefined;

    useEffect(() => {
        let isCancelled = false;

        const groupedRows = mapTableGroupings(props.rows, true);
        const groupedCols = mapTableGroupings(props.columns, false);
        setTableData({ groupedRows: groupedRows, groupedCols: groupedCols, results: [] });
        if (groupedRows.length > 0 && groupedCols.length > 0) {
            setIsLoading(true);

            const requestModel = tableItemsToTemporaryVariableRequestModel(groupedRows, groupedCols, subsetId, props.curatedFilters, timeSelection);

            const dataClient = Factory.DataClient(throwError => throwError());
            dataClient.crosstabResultsFromTemporaryVariables(requestModel)
                .then(results => {
                    if (!isCancelled) {
                        setTableData({
                            groupedRows: groupedRows,
                            groupedCols: groupedCols,
                            results: results
                        });
                    }
                })
                .finally(() => {
                    if (!isCancelled) {
                        setIsLoading(false);
                    }
                });
        }

        return () => { isCancelled = true; }
    }, [props.rows, props.columns, props.curatedFilters, timeSelection, subsetId]);

    return (
        <>
            <StyledTable<DataRow>
                columns={createHeaders(tableData.groupedRows, tableData.groupedCols, props.options, sorting)}
                data={getDataRows(tableData.results, tableData.groupedRows)}
                onSortingChange={setSorting}
                state={{
                    sorting: sorting,
                    grouping: ['rowGrouping'],
                    columnVisibility: {
                        rowGrouping: false,
                        total: props.options.showTotalColumn
                    },
                    expanded: true,
                    columnPinning: { left: ['name'] },
                    isLoading
                }}
            />
            {sampleSize &&
                <Typography variant="body2" color="text.secondary">Sample size: n = {sampleSize}</Typography>
            }
            <Stack direction="row" spacing={2}>
                <Typography variant="body2" color="text.secondary">
                    Row breaks: {tableData.groupedRows.length}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    Column breaks: {tableData.groupedCols.length}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    Total columns: {1 + tableData.groupedCols.reduce((sum, col) =>
                        sum + col.items.instances.length, 0
                    )}
                </Typography>
            </Stack>
            {props.options.highlightSignificance &&
                <Typography variant="body2" color="text.secondary">
                    Statistical significance testing applied at {getSignificanceLevelText(defaultConfidenceLevel)} confidence level
                </Typography>
            }
        </>
    );
};

function createHeaders(groupedRows: MappedTableGrouping[], groupedCols: MappedTableGrouping[], options: TableOptions, sorting: SortingState): MRT_ColumnDef<DataRow>[] {
    let resultIndex = 1;

    type RowType = { original: DataRow }
    function getSortingFn(compare: (rowA: RowType, rowB: RowType) => number): MRT_SortingFn<DataRow> {
        return (rowA, rowB, columnId) => {
            //due to how MRT asc/desc works, we need to reverse in some cases to preserve group/summary row order
            const sortState = sorting.find(s => s.id === columnId);
            const multiplier = sortState?.desc ? -1 : 1;

            if (rowA.original.rowGrouping !== rowB.original.rowGrouping) {
                return multiplier * (rowA.original.rowGrouping - rowB.original.rowGrouping);
            }
            const aIsSummary = rowA.original.isSummaryRow === true;
            const bIsSummary = rowB.original.isSummaryRow === true;
            if (aIsSummary !== bIsSummary) {
                return multiplier * (aIsSummary ? 1 : -1);
            }
            return compare(rowA, rowB);
        }
    };

    const mappedHeaders = groupedCols.map(col => {
        const groupedHeader: MRT_ColumnDef<DataRow> = {
            header: col.groupedLabel,
            muiTableHeadCellProps: { className: styles.headerParent, align: 'center' },
            columns: col.items.instances.map(instance => {
                const colIndex = resultIndex;
                resultIndex++;
                return {
                    id: resultIndex.toString(),
                    header: instance.label,
                    size: 1,
                    accessorFn: row => row.results[colIndex].weightedResult,
                    Cell: ({ row }) =>
                        getDataCell(row.original.results, colIndex, col.metric, options, false, row.original.isSummaryRow),
                    sortingFn: getSortingFn((rowA, rowB) =>
                        rowA.original.results[colIndex].weightedResult - rowB.original.results[colIndex].weightedResult
                    ),
                    muiTableHeadCellProps: { align: 'center' }
                };
            })
        };
        return groupedHeader;
    });

    return [
        {
            accessorKey: 'rowGrouping',
            header: 'Question (hidden)',
            enableGrouping: true,
            enableHiding: true,
            enableSorting: false,
            size: 1,
        },
        {
            accessorKey: 'name',
            header: 'Rows',
            AggregatedCell: ({ cell, table }) => {
                const rowGroup = groupedRows[cell.row.original.rowGrouping];
                return (<span>{rowGroup.groupedLabel}</span>);
            },
            Cell: ({ row }) => {
                return row.original.isSummaryRow ?
                    <i>{row.original.name}</i> : <span>{row.original.name}</span>;
            },
            sortingFn: getSortingFn((rowA, rowB) =>
                rowA.original.name.localeCompare(rowB.original.name)
            ),
        },
        {
            id: 'total',
            header: 'Total',
            accessorFn: row => row.results[0].weightedResult,
            Cell: ({ row }) => {
                const rowGroup = groupedRows[row.original.rowGrouping];
                return getDataCell(row.original.results, 0, rowGroup.metric, options, true, row.original.isSummaryRow);
            },
            sortingFn: getSortingFn((rowA, rowB) =>
                rowA.original.results[0].weightedResult - rowB.original.results[0].weightedResult
            ),
            size: 1,
            muiTableHeadCellProps: { align: 'center' }
        },
        ...mappedHeaders
    ];
}

function getDataCell(results: WeightedDailyResult[], resultIndex: number, metric: Metric, options: TableOptions, isTotalColumn: boolean, isSummaryRow: boolean | undefined): ReactNode {
    const cellResult = results[resultIndex];
    if (isSummaryRow) {
        return <Typography align="center">{cellResult.weightedSampleSize}</Typography>;
    }
    const significance = options.highlightSignificance ? cellResult.significance : Significance.None;
    const lime = '#19CD13';
    const lava = '#CD1319';
    const downColour = metric.downIsGood ? lime : lava;
    const upColour = metric.downIsGood ? lava : lime;
    //this uses a nbsp for index score in total column to maintain cell height consistency
    return (
        <Stack direction="column" alignItems="center">
            {options.showValues &&
                <Stack direction="row" alignItems="center" sx={{
                    color: significance === Significance.Down ? downColour :
                        significance === Significance.Up ? upColour :
                        undefined
                }}>
                    <Typography>{getFormattedValueText(cellResult.weightedResult, metric, options.decimalPlaces)}</Typography>
                    {
                        significance === Significance.Down ? <ArrowDownwardIcon />
                        : significance === Significance.Up ? <ArrowUpwardIcon />
                        : null
                    }
                </Stack>
            }
            {options.showCounts &&
                <Typography color="text.secondary">
                    ({cellResult.weightedValueTotal})
                </Typography>
            }
            {options.showIndexScores &&
                <Typography color="#1376cd">
                    {!isTotalColumn ? calculateIndexScore(cellResult, results[0]) : '\u00A0'}
                </Typography>
            }
        </Stack>
    );
}

function getDataRows(results: CrosstabulatedResults[], groupedRows: MappedTableGrouping[]): DataRow[] {
    return results.flatMap((result, rowIndex) => {
        const matchingRow = groupedRows[rowIndex];

        const dataRows: DataRow[] = result.data.map((instanceResult, instanceIndex) => ({
            rowGrouping: rowIndex,
            name: matchingRow.items.instances[instanceIndex].label,
            results: instanceResult.weightedDailyResults
        }));
        if (hasEqualSampleSizesForColumns(result)) {
            dataRows.push({
                rowGrouping: rowIndex,
                name: `Subtotal - ${matchingRow.groupedLabel}`,
                results: result.data[0].weightedDailyResults,
                isSummaryRow: true
            });
        }
        return dataRows;
    });
}

function hasEqualSampleSizesForTotals(results: CrosstabulatedResults[]): boolean {
    const samples = results.flatMap(r => r.data.map(ir => ir.weightedDailyResults[0].weightedSampleSize));
    return new Set(samples).size === 1;
}

function hasEqualSampleSizesForColumns(results: CrosstabulatedResults): boolean {
    for (let col = 0; col < results.data[0].weightedDailyResults.length; col++) {
        const firstSample = results.data[0].weightedDailyResults[col].weightedSampleSize;
        for (let row = 1; row < results.data.length; row++) {
            if (results.data[row].weightedDailyResults[col].weightedSampleSize !== firstSample) {
                return false;
            }
        }
    }
    return true;
}

export default DataTable;