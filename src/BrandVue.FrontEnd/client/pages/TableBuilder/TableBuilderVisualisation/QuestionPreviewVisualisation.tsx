import { Button, Typography } from "@mui/material";
import Stack from "@mui/material/Stack";
import CloseIcon from "@mui/icons-material/Close";
import AddIcon from '@mui/icons-material/Add';
import { Metric } from "../../../metrics/metric";
import { useEntityConfigurationStateContext } from "../../../entity/EntityConfigurationStateContext";
import { useMetricStateContext } from "../../../metrics/MetricStateContext";
import { camelCaseToWords, getTableItemForMetric, mapTableGroupings, tableItemsToTemporaryVariableRequestModel } from "../TableBuilderUtils";
import { useEffect, useState } from "react";
import { useAppSelector } from "../../../state/store";
import { selectSubsetId } from "../../../state/subsetSlice";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import { CrosstabulatedResults, Factory } from "../../../BrandVueApi";
import {
  type MRT_ColumnDef,
} from 'material-react-table';
import { getFormattedValueText } from "../../../components/helpers/SurveyVueUtils";
import TableBuilderTip from "../TableBuilderTip";
import StyledTable from "./StyledTable";
import { selectTimeSelection } from "../../../state/timeSelectionStateSelectors";
import { TableFilterInstance } from "../TableBuilderTypes";

interface Props {
    selectedMetric: Metric;
    curatedFilters: CuratedFilters;
    addToRows: (metric: Metric) => void;
    addToColumns: (metric: Metric) => void;
    clearSelectedMetric: () => void;
}

interface QuestionPreviewRow {
    response: string;
    count: number | undefined;
    result: number;
}

function mapEntityResultsToData(results: CrosstabulatedResults | undefined): QuestionPreviewRow[] {
    if (!results) return [];
    return results.data.map(r => {
        const value = r.weightedDailyResults[0];
        return {
            response: r.entityInstance.name,
            count: value.weightedValueTotal,
            result: value.weightedResult
        };
    });
}

function hasEqualSampleSizesPerEntity(results: CrosstabulatedResults | undefined): boolean {
    if (!results) return true;
    const samples = results.data.map(r => r.weightedDailyResults[0].weightedSampleSize);
    return new Set(samples).size === 1;
}

const QuestionPreviewVisualisation = (props: Props) => {

    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [results, setResults] = useState<CrosstabulatedResults | undefined>();
    const [filterInstances, setFilterInstances] = useState<TableFilterInstance[]>([]);

    const { questionTypeLookup } = useMetricStateContext();
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const entityInstances = props.selectedMetric.entityCombination.length > 0
        ? entityConfiguration.getAllEnabledInstancesForTypeOrdered(props.selectedMetric.entityCombination[0])
        : [];
    const questionType = camelCaseToWords(questionTypeLookup[props.selectedMetric.name] ?? "");

    const rowsShareSample = hasEqualSampleSizesPerEntity(results);
    const data = mapEntityResultsToData(results);
    const sampleSize = rowsShareSample ?
        results?.data[0]?.weightedDailyResults[0]?.weightedSampleSize?.toString() ?? "-" :
        undefined;
    const columns: MRT_ColumnDef<QuestionPreviewRow>[] = [
        {
            accessorKey: 'response',
            header: 'Response',
            grow: true,
            footer: rowsShareSample ? 'Total' : undefined
        },
        {
            accessorKey: 'count',
            header: 'Count',
            size: 1,
            footer: sampleSize,
        },
        {
            accessorKey: 'result',
            header: 'Result',
            size: 1,
            Cell: ({ row }) =>
                getFormattedValueText(row.original.result, props.selectedMetric, 0)
        }
    ];

    useEffect(() => {
        let isCancelled = false;

        const tableItem = getTableItemForMetric(props.selectedMetric, entityConfiguration);
        const groupedRows = mapTableGroupings([tableItem], true).slice(0, 1);
        const requestModel = tableItemsToTemporaryVariableRequestModel(groupedRows, [], subsetId, props.curatedFilters, timeSelection);
        setIsLoading(true);

        const dataClient = Factory.DataClient(throwError => throwError());
        dataClient.crosstabResultsFromTemporaryVariables(requestModel)
            .then(entityResults => {
                if (!isCancelled) {
                    setResults(entityResults[0]);
                    setFilterInstances(groupedRows[0].filterInstances);
                }
            })
            .finally(() => {
                if (!isCancelled) {
                    setIsLoading(false);
                }
            });

        return () => { isCancelled = true; }
    }, [props.selectedMetric, subsetId, props.curatedFilters, timeSelection]);

    return (
        <Stack direction="column" spacing={1} sx={{ flex: 1, minHeight: 0, p: 2, border: '1px solid #ddd' }}>
            <Stack direction="row" spacing={1} justifyContent="space-between">
                <Typography variant="h6">Question Preview</Typography>
                <Stack direction="row" spacing={1}>
                    <Button
                        variant="outlined"
                        startIcon={<CloseIcon />}
                        onClick={() => props.clearSelectedMetric()}
                    >
                        Clear Preview
                    </Button>
                    <Button
                        variant="outlined"
                        startIcon={<AddIcon />}
                        onClick={() => props.addToRows(props.selectedMetric)}
                    >
                        Add to Rows
                    </Button>
                    <Button
                        variant="contained"
                        startIcon={<AddIcon />}
                        onClick={() => props.addToColumns(props.selectedMetric)}
                    >
                        Add to Columns
                    </Button>
                </Stack>
            </Stack>
            <Typography variant="subtitle1">{props.selectedMetric.helpText}</Typography>
            {filterInstances.length > 0 &&
                <Typography variant="body2" fontWeight='bold'>
                    {filterInstances.map(fi => fi.instance.label).join(', ')}
                </Typography>
            }
            <Typography variant="body2" color="text.secondary">{questionType} • {entityInstances.length} options</Typography>
            <StyledTable<QuestionPreviewRow>
                columns={columns}
                data={data}
                state={{ isLoading }}
            />
            {sampleSize &&
                <Typography variant="body2" color="text.secondary">Sample size: n = {sampleSize}</Typography>
            }
            <TableBuilderTip
                text="Click 'Add to Rows' or 'Add to Columns' to include this question in your cross-tabulation table, or select another question to preview it."
            />
        </Stack>
    );
};
export default QuestionPreviewVisualisation;