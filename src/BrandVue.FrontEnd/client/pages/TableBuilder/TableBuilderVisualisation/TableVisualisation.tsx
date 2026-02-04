import { Button, Typography } from "@mui/material";
import Stack from "@mui/material/Stack";
import CloseIcon from "@mui/icons-material/Close";
import SyncIcon from '@mui/icons-material/Sync';
import { TableItem, TableOptions } from "../TableBuilderTypes";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import MissingDimensionTable from "./MissingDimensionTable";
import DataTable from "./DataTable";

interface Props {
    title: string;
    rows: TableItem[];
    columns: TableItem[];
    options: TableOptions;
    curatedFilters: CuratedFilters;
    transpose: () => void;
    clearAll: () => void;
}

const TableVisualisation = (props: Props) => {

    const getTableDescription = () => {
        if (props.rows.length > 0 && props.columns.length > 0) {
            return "Cross-tabulation Analysis";
        } else if (props.rows.length > 0) {
            return "Row Structure Preview";
        } else if (props.columns.length > 0) {
            return "Column Structure Preview";
        }
        return "";
    };

    const getItemSummary = (name: string, items: TableItem[]) => {
        if (items.length === 0) {
            return `Add ${name.toLowerCase()} to see data`;
        }
        const itemNames = items.map(item => item.label);
        return `${name}: ${itemNames.join(" | ")}`;
    };

    return (
        <Stack direction="column" spacing={1} sx={{ p: 2, border: '1px solid #ddd', flex: 1, minHeight: 0 }}>
            <Stack direction="row" spacing={1} justifyContent="space-between">
                <Typography variant="h6">{props.title}</Typography>
                <Stack direction="row" spacing={1}>
                    <Button
                        variant="outlined"
                        startIcon={<SyncIcon />}
                        onClick={props.transpose}
                    >
                        Transpose
                    </Button>
                    <Button
                        variant="contained"
                        startIcon={<CloseIcon />}
                        onClick={props.clearAll}
                    >
                        Clear All
                    </Button>
                </Stack>
            </Stack>
            <Typography variant="subtitle1">{getTableDescription()}</Typography>
            <Typography variant="body2" color="text.secondary">
                {getItemSummary("Rows", props.rows)} | {getItemSummary("Columns", props.columns)}
            </Typography>
            {(props.rows.length === 0 || props.columns.length === 0) ?
                <MissingDimensionTable rows={props.rows} columns={props.columns} />
                :
                <DataTable
                    rows={props.rows}
                    columns={props.columns}
                    options={props.options}
                    curatedFilters={props.curatedFilters} />
            }
        </Stack>
    );
};
export default TableVisualisation;