import { MaterialReactTable, MRT_TableOptions, MRT_RowData } from 'material-react-table';

export type StyledTableRowData = MRT_RowData & {
    isSummaryRow?: boolean;
};

function StyledTable<TData extends StyledTableRowData>(props: MRT_TableOptions<TData>) {
    return (
        <MaterialReactTable<TData>
            {...props}
            enableTopToolbar={false}
            enableBottomToolbar={false}
            enablePagination={false}
            enableColumnActions={false}
            enableColumnFilterModes={false}
            enableColumnDragging={false}
            enableHiding
            enableGrouping
            enableStickyHeader
            enableStickyFooter
            enableColumnPinning
            muiTablePaperProps={{
                elevation: 0,
                sx: {
                    backgroundColor: 'transparent',
                    boxShadow: 'none',
                    display: "flex"
                }
            }}
            displayColumnDefOptions={{
                'mrt-row-expand': {
                    size: 0,
                    minSize: 0,
                    muiTableHeadCellProps: { sx: { display: 'none' } },
                    muiTableBodyCellProps: { sx: { display: 'none' } },
                },
            }}
            muiTableHeadProps={{
                sx: {
                    //sticky header doesnt work properly with nested headers, so only make bottom row sticky
                    '& tr:not(:last-of-type)': {
                        position: 'static',
                        '& th': {
                            position: 'static',
                        },
                    },
                }
            }}
            muiTableBodyRowProps={({ row }) => ({
                sx: {
                    backgroundColor: row.original.isSummaryRow ? '#f2f2f2' :
                        row.getIsGrouped() ? '#ececf0' :
                        'transparent',
                    flex: 1,
                    overflow: 'auto',
                }
            })}
            muiTableHeadCellProps={{
                sx: {
                    backgroundColor: '#ececf0',
                    '&[data-pinned="true"]': {opacity: 1 },
                    '&[data-pinned="true"]::before': { backgroundColor: '#ececf0 !important' },
                }
            }}
            muiTableBodyCellProps={({ cell }) => ({
                sx: {
                    '&[data-pinned="true"]': { opacity: 1 },
                    '&[data-pinned="true"]::before': {
                        backgroundColor: cell.row.original.isSummaryRow ? '#f2f2f2 !important' :
                            cell.row.getIsGrouped() ? '#ececf0 !important' :
                            '#F8F9F9 !important',
                    },
                }
            })}
            muiTableFooterCellProps={{
                sx: {
                    backgroundColor: '#ececf0',
                    '&[data-pinned="true"]': { opacity: 1 },
                    '&[data-pinned="true"]::before': { backgroundColor: '#ececf0 !important' },
                }
            }}
        />
    );
}
export default StyledTable;