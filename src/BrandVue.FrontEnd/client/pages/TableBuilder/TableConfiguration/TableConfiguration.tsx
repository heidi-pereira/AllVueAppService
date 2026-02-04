import { useState } from "react";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import TextField from "@mui/material/TextField";
import Divider from "@mui/material/Divider";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import { TableItem, TableOptions } from "../TableBuilderTypes";
import ConfiguredTableItem from "./ConfiguredTableItem";
import ManageTableItemDialog from "./ManageTableItemDialog";
import EditLabelsDialog from "./EditLabelsDialog";
import { FormControl, FormControlLabel, FormGroup, MenuItem, Switch } from "@mui/material";

enum TableConfigTab {
    Data = 'data',
    Options = 'options'
}

interface TableConfigurationProps {
    title: string;
    setTitle: (title: string) => void;
    rows: TableItem[];
    columns: TableItem[];
    options: TableOptions;
    updateRow(newItem: TableItem): void;
    removeRow(item: TableItem): void;
    updateColumn(newItem: TableItem): void;
    removeColumn(item: TableItem): void;
    updateOptions(newOptions: TableOptions): void;
}

const TableConfiguration = (props: TableConfigurationProps) => {
    const [tab, setTab] = useState<TableConfigTab>(TableConfigTab.Data);

    const handleTabChange = (event: React.MouseEvent, newTab: TableConfigTab | null) => {
        if (newTab !== null) {
            setTab(newTab);
        }
    };

    return (
        <Stack spacing={3} sx={{ height: '100%' }}>
            <Stack spacing={0.5}>
                <Typography variant="subtitle1">Table Configuration</Typography>
                <Typography variant="body2" color="text.secondary">
                    Manage your table structure and labels
                </Typography>
            </Stack>

            <Stack spacing={1}>
                <Typography variant="subtitle2">Table Title</Typography>
                <TextField
                    size="small"
                    fullWidth
                    value={props.title}
                    onChange={(e) => props.setTitle(e.target.value)} />
            </Stack>

            <Divider />

            <ToggleButtonGroup
                value={tab}
                exclusive
                onChange={handleTabChange}
                size="small"
                sx={{ width: '100%' }}
            >
                <ToggleButton value={TableConfigTab.Data} fullWidth>Data</ToggleButton>
                <ToggleButton value={TableConfigTab.Options} fullWidth>Options</ToggleButton>
            </ToggleButtonGroup>

            {tab === TableConfigTab.Data && <DataSection {...props} />}
            {tab === TableConfigTab.Options && <OptionsSection {...props} />}
        </Stack>
    );
}

interface EditedTableItem {
    tableItem: TableItem;
    isRow: boolean;
}
const DataSection = (props: TableConfigurationProps) => {
    const [manageItem, setManageItem] = useState<EditedTableItem | undefined>(undefined);
    const [labelItem, setLabelItem] = useState<EditedTableItem | undefined>(undefined);

    const EmptySection = (props: { type: string }) => (
        <Stack
            sx={{
                p: 1,
                border: '2px dashed #ddd',
                height: "47px",
                alignItems: 'center',
                justifyContent: 'center'
            }}
            direction="row"
        >
            <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
                No {props.type} configured.
            </Typography>
        </Stack>
    );

    return (
        <>
        {manageItem && (
            <ManageTableItemDialog
                open={!!manageItem}
                tableItem={manageItem.tableItem}
                onClose={() => setManageItem(undefined)}
                updateItem={(newItem) => {
                    if (manageItem.isRow) {
                        props.updateRow(newItem);
                    } else {
                        props.updateColumn(newItem);
                    }
                }}
            />
        )}
        {labelItem && (
            <EditLabelsDialog
                open={!!labelItem}
                tableItem={labelItem.tableItem}
                onClose={() => setLabelItem(undefined)}
                updateItem={(newItem) => {
                    if (labelItem.isRow) {
                        props.updateRow(newItem);
                    } else {
                        props.updateColumn(newItem);
                    }
                }}
            />
        )}
        <Stack direction="column" spacing={1}>
            <Typography variant="subtitle2">Rows ({props.rows.length})</Typography>
            {props.rows.map(row =>
                <ConfiguredTableItem
                    key={row.metric.name}
                    tableItem={row}
                    update={props.updateRow}
                    manageOptions={() => setManageItem({tableItem: row, isRow: true})}
                    manageLabels={() => setLabelItem({tableItem: row, isRow: true})}
                    remove={props.removeRow} />
            )}
            {props.rows.length === 0 &&
                <EmptySection type="rows" />
            }
        </Stack>

        <Stack direction="column" spacing={1}>
            <Typography variant="subtitle2">Columns ({props.columns.length})</Typography>
            {props.columns.map(col =>
                <ConfiguredTableItem
                    key={col.metric.name}
                    tableItem={col}
                    update={props.updateColumn}
                    manageOptions={() => setManageItem({tableItem: col, isRow: false})}
                    manageLabels={() => setLabelItem({tableItem: col, isRow: false})}
                    remove={props.removeColumn}
                    disableMultiEntity />
            )}
            {props.columns.length === 0 &&
                <EmptySection type="columns" />
            }
        </Stack>
        </>
    )
};

type BooleanKeys<T> = {
  [K in keyof T]: T[K] extends boolean ? K : never
}[keyof T];

const OptionsSection = (props: TableConfigurationProps) => {
    const { options, updateOptions } = props;

   const handleOptionChange = <K extends keyof TableOptions>(optionKey: K, value: TableOptions[K]) => {
        const newOptions = { ...options, [optionKey]: value };
        updateOptions(newOptions);
    };

    const getSwitchInput = (label: string, optionKey: BooleanKeys<TableOptions>) => (
        <FormControlLabel
            label={label}
            labelPlacement="start"
            sx={{ justifyContent: 'space-between', margin: 0 }}
            control={
                <Switch
                    checked={options[optionKey]}
                    onChange={e => handleOptionChange(optionKey, e.target.checked)}
                />
            }
        />
    );

    return (
        <FormGroup>
            {getSwitchInput("Show Values", "showValues")}
            {getSwitchInput("Show Counts", "showCounts")}
            {getSwitchInput("Show Index Scores", "showIndexScores")}
            {getSwitchInput("Show Total Column", "showTotalColumn")}
            {getSwitchInput("Highlight Significance", "highlightSignificance")}
            <TextField
                select
                fullWidth
                sx={{ mt: 2 }}
                label="Decimal Places"
                value={options.decimalPlaces}
                onChange={e => handleOptionChange("decimalPlaces", Number(e.target.value))}
            >
                <MenuItem value={0}>0 (whole numbers)</MenuItem>
                <MenuItem value={1}>1 decimal place</MenuItem>
                <MenuItem value={2}>2 decimal places</MenuItem>
            </TextField>
        </FormGroup>
    );
};

export default TableConfiguration;