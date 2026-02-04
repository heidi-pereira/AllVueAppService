import React, { useState } from 'react';
import Footer from '../../components/Footer';
import { ProductConfiguration } from '../../ProductConfiguration';
import Stack from '@mui/material/Stack';
import Box from '@mui/material/Box';
import QuestionsVariablesList from './QuestionsVariablesList/QuestionsVariablesList';
import TableConfiguration from './TableConfiguration/TableConfiguration';
import TableBuilderVisualisation from './TableBuilderVisualisation/TableBuilderVisualisation';
import { Toaster } from 'react-hot-toast';
import { TableItem, TableOptions } from './TableBuilderTypes';
import { Metric } from '../../metrics/metric';
import { useEntityConfigurationStateContext } from '../../entity/EntityConfigurationStateContext';
import { getTableItemForMetric } from './TableBuilderUtils';
import { CuratedFilters } from '../../filter/CuratedFilters';

interface ITableBuilderPageProps {
    nav: React.ReactNode;
    productConfiguration: ProductConfiguration;
    curatedFilters: CuratedFilters;
}

const TableBuilderPage = (props: ITableBuilderPageProps) => {
    return (
        <Stack direction="column" sx={{ height: '100vh', flex: 1 }}>
            {props.nav}
            <TableBuilderContent curatedFilters={props.curatedFilters} />
            <Footer />
        </Stack>
    );
};

const defaultTableOptions: TableOptions = {
    showValues: true,
    showCounts: false,
    showIndexScores: false,
    showTotalColumn: true,
    highlightSignificance: false,
    decimalPlaces: 0,
};

const TableBuilderContent = (props: { curatedFilters: CuratedFilters; }) => {
    const [title, setTitle] = useState<string>("Cross-tabulation Analysis");
    const [rows, setRows] = useState<TableItem[]>([]);
    const [columns, setColumns] = useState<TableItem[]>([]);
    const [options, setOptions] = useState<TableOptions>(defaultTableOptions);
    const [selectedMetric, setSelectedMetric] = useState<Metric | undefined>(undefined);

    const { entityConfiguration } = useEntityConfigurationStateContext();

    const addToRows = (metric: Metric) => {
        const newRow = getTableItemForMetric(metric, entityConfiguration);
        setRows(prevRows => [...prevRows, newRow]);
        setSelectedMetric(undefined);
    };

    const addToColumns = (metric: Metric) => {
        const newColumn = getTableItemForMetric(metric, entityConfiguration);
        setColumns(prevColumns => [...prevColumns, newColumn]);
        setSelectedMetric(undefined);
    };

    const updateTableItem = (
        setItems: React.Dispatch<React.SetStateAction<TableItem[]>>,
        newItem: TableItem
    ) => {
        setItems(prevItems =>
            prevItems.map(item =>
                item.metric.name === newItem.metric.name ? newItem : item
            )
        );
    };

    const updateRow = (newItem: TableItem) => {
        updateTableItem(setRows, newItem);
    };

    const updateColumn = (newItem: TableItem) => {
        updateTableItem(setColumns, newItem);
    };

    const removeRow = (item: TableItem) =>
        setRows(prevRows => prevRows.filter(r => r.metric.name !== item.metric.name));

    const removeColumn = (item: TableItem) =>
        setColumns(prevColumns => prevColumns.filter(c => c.metric.name !== item.metric.name));

    const transposeRowsAndColumns = () => {
        const newColumns = rows;
        const newRows = columns;
        setRows(newRows);
        setColumns(newColumns);
    };

    const clearRowsAndColumns = () => {
        setRows([]);
        setColumns([]);
    };

    return (
        <Stack direction="row" sx={{ flex: 1, minHeight: 0 }}>
            <Toaster position='bottom-center' toastOptions={{duration: 2000}} />
            <Box sx={{ p: 2, width: 280, borderRight: '1px solid #ddd', overflowY: 'auto', scrollbarGutter: 'stable' }}>
                <QuestionsVariablesList
                    rows={rows}
                    columns={columns}
                    addToRows={addToRows}
                    addToColumns={addToColumns}
                    selectedMetric={selectedMetric}
                    selectMetric={setSelectedMetric}
                />
            </Box>
            <Stack sx={{ flex: 1, p: 2, minWidth: 0 }}>
                <TableBuilderVisualisation
                    title={title}
                    rows={rows}
                    columns={columns}
                    options={options}
                    addToRows={addToRows}
                    addToColumns={addToColumns}
                    transposeRowsAndColumns={transposeRowsAndColumns}
                    clearRowsAndColumns={clearRowsAndColumns}
                    selectedMetric={selectedMetric}
                    clearSelectedMetric={() => setSelectedMetric(undefined)}
                    curatedFilters={props.curatedFilters}
                />
            </Stack>
            <Box  sx={{ p: 2, width: 336, borderLeft: '1px solid #ddd', overflowY: 'auto', scrollbarGutter: 'stable' }}>
                <TableConfiguration
                    title={title}
                    setTitle={setTitle}
                    rows={rows}
                    columns={columns}
                    options={options}
                    updateRow={updateRow}
                    updateColumn={updateColumn}
                    removeRow={removeRow}
                    removeColumn={removeColumn}
                    updateOptions={setOptions}
                />
            </Box>
        </Stack>
    );
}

export default TableBuilderPage;