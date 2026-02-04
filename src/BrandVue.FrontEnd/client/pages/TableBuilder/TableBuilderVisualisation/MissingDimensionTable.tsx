import { TableItem } from "../TableBuilderTypes";
import { MRT_ColumnDef } from 'material-react-table';
import { Stack, Typography } from '@mui/material';
import StyledTable from "./StyledTable";
import TableBuilderTip from "../TableBuilderTip";
import styles from './StyledTable.module.less';
import { mapTableGroupings } from "../TableBuilderUtils";

interface Props {
    rows: TableItem[];
    columns: TableItem[];
}

interface EmptyColumnsRowData {
    rowGrouping: number;
    name: string;
}

interface EmptyRowsRowData {}

const MissingDimensionTable = (props: Props) => {

    if (props.rows.length === 0 && props.columns.length === 0) {
        return (null);
    }

    //No columns selected
    if (props.columns.length === 0) {
        const groupedRows = mapTableGroupings(props.rows, true);
        const columns: MRT_ColumnDef<EmptyColumnsRowData>[] = [
            {
                accessorKey: 'rowGrouping',
                header: 'Question (hidden)',
                enableGrouping: true,
                enableHiding: true,
                size: 1,
            },
            {
                accessorKey: 'name',
                header: 'Rows',
                AggregatedCell: ({ cell, table }) => {
                    const rowGroup = groupedRows[cell.row.original.rowGrouping];
                    return (<span>{rowGroup.groupedLabel}</span>);
                },
            },
            {
                header: 'Columns',
                Cell: () => (
                    <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                        Add column questions to see data
                    </Typography>
                ),
            },
        ];
        const data: EmptyColumnsRowData[] = groupedRows.flatMap((group, groupIndex) =>
            group.items.instances.map(instance => ({
                rowGrouping: groupIndex,
                name: instance.label
            }))
        );
        return (
            <>
                <StyledTable<EmptyColumnsRowData>
                    key="no-columns" //key is necessary to ensure we use initialState when transposing between the two tables
                    columns={columns}
                    data={data}
                    state={{
                        grouping: ['rowGrouping'],
                        columnVisibility: { rowGrouping: false },
                        expanded: true,
                        columnPinning: { left: ['name'] }
                    }}
                />
                <Stack direction="row" spacing={2} justifyContent="space-between">
                    <Typography variant="body2" color="text.secondary">
                        Row breaks: {groupedRows.length}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                        Total row options: {groupedRows.reduce((sum, row) =>
                            sum + row.items.instances.length, 0
                        )}
                    </Typography>
                </Stack>
                <TableBuilderTip text="Add column questions from the sidebar to populate this table with data" />
            </>
        );
    }

    //No rows selected
    let resultIndex = 0;

    const groupedCols = mapTableGroupings(props.columns, false);
    const mappedHeaders = groupedCols.map(col => {
        const groupedHeader: MRT_ColumnDef<EmptyRowsRowData> = {
            header: col.groupedLabel,
            muiTableHeadCellProps: { className: styles.headerParent },
            columns: col.items.instances.map(instance => {
                resultIndex++;
                return {
                    id: resultIndex.toString(),
                    header: instance.label,
                    accessorFn: _ => "-"
                };
            })
        };
        return groupedHeader;
    });

    const columns: MRT_ColumnDef<EmptyRowsRowData>[] = [
        {
            id: 'rows',
            header: 'Rows',
            Cell: () => (
                <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                    Add row questions to see data
                </Typography>
            )
        },
        {
            id: 'total',
            header: 'Total',
            accessorFn: _ => "-",
        },
        ...mappedHeaders
    ];

    return (
        <>
            <StyledTable<EmptyRowsRowData>
                key="no-rows" //key is necessary to ensure we use initialState when transposing between the two tables
                columns={columns}
                data={[{}]}
                state={{ columnPinning: { left: ['rows'] } }}
            />
            <Stack direction="row" spacing={2} justifyContent="space-between">
                <Typography variant="body2" color="text.secondary">
                    Column breaks: {groupedCols.length}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    Total columns: {1 + groupedCols.reduce((sum, col) =>
                        sum + col.items.instances.length, 0
                    )}
                </Typography>
            </Stack>
            <TableBuilderTip text="Add row questions from the sidebar to populate this table with data" />
        </>
    );
};
export default MissingDimensionTable;