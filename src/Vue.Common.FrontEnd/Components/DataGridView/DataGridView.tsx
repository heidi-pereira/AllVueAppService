import React from 'react';
import {
    DataGrid, GridRowsProp, GridColDef, GridSortModel, GridToolbarContainer,
    QuickFilter,
    QuickFilterControl,
    QuickFilterClear,
    QuickFilterTrigger,
    ToolbarButton
} from '@mui/x-data-grid';
import { styled } from '@mui/material/styles';
import Tooltip from '@mui/material/Tooltip';
import TextField from '@mui/material/TextField';
import SearchIcon from '@mui/icons-material/Search';
import InputAdornment from '@mui/material/InputAdornment';
import CancelIcon from '@mui/icons-material/Cancel';

import { KebabMenu, MenuOption } from '@shared/KebabMenu/KebabMenu';
import styles from './DataGridView.module.scss';
import { SHOW_ALL_ROWS_IN_DATAGRID } from '@shared/constants';
import { Box } from '@mui/material';
export interface DataGridViewProps {
    id?: string;
    rows: GridRowsProp;
    columns: GridColDef[];
    loading: boolean;
    defaultSort?: GridSortModel;
    perRowOptions?: MenuOption[] | ((row: any) => MenuOption[]);
    globalSearch?: boolean;
    autoPageSize?: boolean;
    defaultRowsPerPage?: number;
    maxPageHeight?: string;
    minPageHeight?: string;
    showFooter?: boolean;
    toolbar_lhs?: React.ReactNode;
    toolbar_rhs?: React.ReactNode;
}

const StyledQuickFilter = styled(QuickFilter)({
    display: 'flex',
    minWidth: 0,
    width: '100%',
    button: {
        display: 'none'
    }
});

const StyledToolbarButton = styled(ToolbarButton)<{ ownerState: OwnerState }>(
    ({ theme, ownerState }) => ({
        gridArea: '1 / 1',
        width: 'min-content',
        height: 'min-content',
        zIndex: 1,
        opacity: ownerState.expanded ? 0 : 1,
        pointerEvents: ownerState.expanded ? 'none' : 'auto',
        transition: theme.transitions.create(['opacity']),
    })
);

const StyledTextField = styled(TextField)<{
    ownerState: OwnerState;
}>(({ theme, ownerState }) => ({
    gridArea: '1 / 1',
    overflowX: 'clip',
    width: ownerState.expanded ? 260 : 'var(--trigger-width)',
    opacity: ownerState.expanded ? 1 : 0,
    transition: theme.transitions.create(['width', 'opacity']),
}));
function CustomToolbar({ toolbar_lhs, toolbar_rhs }: { toolbar_lhs: React.ReactNode, toolbar_rhs: React.ReactNode }) {
  return (
      <GridToolbarContainer sx={{ justifyContent: 'space-between', px: 1, py: 0.5 }}>
        <Box sx={{ display: 'flex', alignItems: 'flex-start', height: "45px", justifyContent: 'space-between'}}>
          {toolbar_lhs}
        <StyledQuickFilter>
            <QuickFilterTrigger
                render={(triggerProps, state) => (
                      <Tooltip title="Search" enterDelay={0}>
                          <StyledToolbarButton
                              {...triggerProps}
                              ownerState={{ expanded: true }}
                              color="default"
                              aria-disabled={state.expanded}
                          >
                              <SearchIcon fontSize="small" />
                          </StyledToolbarButton>
                      </Tooltip>
                  )}
            />
            <QuickFilterControl
                render={({ ref, ...controlProps }, state) => (
                      <StyledTextField
                          {...controlProps}
                          ownerState={{ expanded: true }}
                          inputRef={ref}
                          aria-label="Search"
                          placeholder="Search..."
                          size="small"
                          slotProps={{
                              input: {
                                  startAdornment: (
                                      <InputAdornment position="start">
                                          <SearchIcon fontSize="small" />
                                      </InputAdornment>
                                  ),
                                  endAdornment: state.value ? (
                                      <InputAdornment position="end">
                                          <QuickFilterClear
                                              edge="end"
                                              size="small"
                                              aria-label="Clear search"
                                              material={{ sx: { marginRight: -0.75 } }}
                                          >
                                              <CancelIcon fontSize="small" />
                                          </QuickFilterClear>
                                      </InputAdornment>
                                  ) : null,
                                  ...controlProps.slotProps?.input,
                              },
                              ...controlProps.slotProps,
                          }}
                      />
                  )}
              />
              </StyledQuickFilter>
        </Box>
        {toolbar_rhs && <Box sx={{ display: 'flex', alignItems: 'flex-end', height: "45px", justifyContent: 'space-between'}}>{toolbar_rhs}</Box>}
    </GridToolbarContainer>
  );
}

export const DataGridView = ({ 
    rows,
    columns,
    ...props
}: DataGridViewProps) => {

    const defaultSort: GridSortModel = props.defaultSort?? [{ field: columns[0].field, sort: 'asc' }];
    const defaultRowsPerPage = props.defaultRowsPerPage ?? SHOW_ALL_ROWS_IN_DATAGRID;
    const pageSize = props.autoPageSize ?? defaultRowsPerPage;
    const [sortModel, setSortModel] = React.useState<GridSortModel>(defaultSort);
    const globalSearch = props.globalSearch ?? false;

    const columnsWithActions = React.useMemo(() => {
        if (props.perRowOptions) {
            const perRowColumnAction: GridColDef = {
                field: 'actions',
                headerName: '',
                width: 50,
                sortable: false,
                filterable: false,
                disableColumnMenu: true,
                renderCell: (params) => {
                    const options = props.perRowOptions(params.row);
                    if (options.length === 0) {
                        return (<></>);
                    }
                    return (
                    <div className={styles["kebab-menu-container"]} >
                        <KebabMenu options={props.perRowOptions!} rowData={params.row} />
                    </div>
                )},
            };
            return [...columns, perRowColumnAction];
        }
        return columns;
    }, [columns, props.perRowOptions]);

    const style: React.CSSProperties = {display: 'flex', flexDirection: 'column', flexGrow: 1}
    if (props.maxPageHeight) {
        style.maxHeight = props.maxPageHeight;
    }
    if (props.minPageHeight) {
        style.minHeight = props.minPageHeight;
    }

    return (
        <div style={{
            ...style
        }}>
            <DataGrid
                ignoreDiacritics
                rows={rows}
                columns={columnsWithActions}
                {...props}
                hideFooter={props.loading || rows.length < defaultRowsPerPage || !props.showFooter}
                sortModel={sortModel}
                onSortModelChange={(newSortModel) => setSortModel(newSortModel)}
                slots={{
                    toolbar: () => <CustomToolbar toolbar_lhs={props.toolbar_lhs} toolbar_rhs={props.toolbar_rhs} />
                }}
                showToolbar={globalSearch}
                disableColumnFilter
                disableColumnSelector
                initialState={{
                    pagination: { paginationModel: { pageSize: pageSize } },
                    filter: {
                        filterModel: {
                            items: [],
                            quickFilterExcludeHiddenColumns: false,
                        },
                    },
                }}
                {...(props.autoPageSize ?
                    {
                        autoPageSize:true,
                    }
                    :
                    {
                        pageSize: pageSize,
                        pageSizeOptions: [10, 20, 50, 100, { value: SHOW_ALL_ROWS_IN_DATAGRID, label: 'All' }]
                    }
                )}
            />
        </div>
        );
}
