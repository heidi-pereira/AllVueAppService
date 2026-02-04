import { useState } from 'react';
import Stack from '@mui/material/Stack';
import VisualisationControlsHeader from './VisualisationControlsHeader';
import EmptyVisualisation from './EmptyVisualisation';
import { TableItem, TableOptions } from '../TableBuilderTypes';
import { Metric } from '../../../metrics/metric';
import QuestionPreviewVisualisation from './QuestionPreviewVisualisation';
import TableVisualisation from './TableVisualisation';
import { CuratedFilters } from '../../../filter/CuratedFilters';

export enum VisualisationMode {
    Table = 'table',
    Chart = 'chart'
}

interface TableBuilderVisualisationProps {
    title: string;
    rows: TableItem[];
    columns: TableItem[];
    options: TableOptions;
    addToRows: (metric: Metric) => void;
    addToColumns: (metric: Metric) => void;
    transposeRowsAndColumns: () => void;
    clearRowsAndColumns: () => void;
    selectedMetric?: Metric;
    clearSelectedMetric: () => void;
    curatedFilters: CuratedFilters;
}

const TableBuilderVisualisation = (props: TableBuilderVisualisationProps) => {
    const [mode, setMode] = useState<VisualisationMode>(VisualisationMode.Table);

    const getVisualisation = () => {
        if (props.rows.length === 0 && props.columns.length === 0) {
            if (props.selectedMetric) {
                return (
                    <QuestionPreviewVisualisation
                        selectedMetric={props.selectedMetric}
                        curatedFilters={props.curatedFilters}
                        addToRows={props.addToRows}
                        addToColumns={props.addToColumns}
                        clearSelectedMetric={props.clearSelectedMetric}
                    />
                );
            }
            return <EmptyVisualisation />;
        }
        return (
            <TableVisualisation
                title={props.title}
                rows={props.rows}
                columns={props.columns}
                options={props.options}
                curatedFilters={props.curatedFilters}
                transpose={props.transposeRowsAndColumns}
                clearAll={props.clearRowsAndColumns}
            />
        );
    }

    return (
        <Stack direction="column" spacing={2} sx={{ flex: 1, minHeight: 0 }}>
            <VisualisationControlsHeader mode={mode} onModeChange={setMode} />
            {getVisualisation()}
        </Stack>
    );
};

export default TableBuilderVisualisation;