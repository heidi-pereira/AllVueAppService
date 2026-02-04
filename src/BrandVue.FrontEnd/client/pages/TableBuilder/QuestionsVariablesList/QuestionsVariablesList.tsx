import { useState } from 'react';
import Stack from '@mui/material/Stack';
import ToggleButton from '@mui/material/ToggleButton';
import ToggleButtonGroup from '@mui/material/ToggleButtonGroup';
import { useMetricStateContext } from '../../../metrics/MetricStateContext';
import { useAppSelector } from '../../../state/store';
import { selectHydratedVariableConfiguration } from '../../../state/variableConfigurationsSlice';
import { useEntityConfigurationStateContext } from '../../../entity/EntityConfigurationStateContext';
import { CalculationType, MainQuestionType, QuestionVariableDefinition, VariableConfigurationModel } from '../../../BrandVueApi';
import { Metric } from '../../../metrics/metric';
import QuestionVariableListItem from './QuestionVariableListItem';
import { IEntityConfiguration } from '../../../entity/EntityConfiguration';
import Typography from '@mui/material/Typography';
import InputAdornment from '@mui/material/InputAdornment';
import TextField from '@mui/material/TextField';
import SearchIcon from '@mui/icons-material/Search';
import Button from '@mui/material/Button';
import AddIcon from '@mui/icons-material/Add';
import toast from 'react-hot-toast';
import { TableItem } from '../TableBuilderTypes';

enum ListDisplayMode {
    Questions,
    Variables
}

function splitMetrics(metrics: Metric[], variableByIdentifier: Map<string, VariableConfigurationModel>) {
    const questionMetrics: Metric[] = [];
    const variableMetrics: Metric[] = [];

    metrics.forEach(m => {
        const variable = variableByIdentifier.get(m.primaryVariableIdentifier);
        if (variable && variable.definition instanceof QuestionVariableDefinition) {
            questionMetrics.push(m);
        } else {
            variableMetrics.push(m);
        }
    });
    return { questionMetrics, variableMetrics };
}

interface QuestionsVariablesListProps {
    rows: TableItem[];
    columns: TableItem[];
    addToRows(metric: Metric): void;
    addToColumns(metric: Metric): void;
    selectedMetric: Metric | undefined;
    selectMetric(metric: Metric): void;
}

const QuestionsVariablesList = (props: QuestionsVariablesListProps) => {
    const [listDisplayMode, setListDisplayMode] = useState<ListDisplayMode>(ListDisplayMode.Questions);

    const { crosstabPageMetrics, questionTypeLookup } = useMetricStateContext();
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const variableByIdentifier = new Map<string, VariableConfigurationModel>(variables.map(v => [v.identifier, v]));
    const { entityConfiguration } = useEntityConfigurationStateContext();

    const availableMetrics = crosstabPageMetrics.filter(metric => metric.calcType !== CalculationType.Text);
    const { questionMetrics, variableMetrics } = splitMetrics(availableMetrics, variableByIdentifier);

    const handleListDisplayModeChange = (event: React.MouseEvent, newMode: ListDisplayMode | null) => {
        if (newMode !== null) {
            setListDisplayMode(newMode);
        }
    };

    return (
        <Stack direction="column" spacing={3} sx={{ height: '100%' }}>
            <ToggleButtonGroup
                value={listDisplayMode}
                exclusive
                onChange={handleListDisplayModeChange}
                size="small"
                sx={{ width: '100%' }}
            >
                <ToggleButton value={ListDisplayMode.Questions} fullWidth>Questions</ToggleButton>
                <ToggleButton value={ListDisplayMode.Variables} fullWidth>Variables</ToggleButton>
            </ToggleButtonGroup>
            {listDisplayMode === ListDisplayMode.Questions &&
                <ItemsList
                    metrics={questionMetrics}
                    entityConfiguration={entityConfiguration}
                    questionTypeLookup={questionTypeLookup}
                    rows={props.rows}
                    addToRows={props.addToRows}
                    columns={props.columns}
                    addToColumns={props.addToColumns}
                    selectedMetricName={props.selectedMetric?.name}
                    selectMetric={props.selectMetric}
                    nameOfListItems="questions"
                />
            }
            {listDisplayMode === ListDisplayMode.Variables &&
                <ItemsList
                    metrics={variableMetrics}
                    entityConfiguration={entityConfiguration}
                    questionTypeLookup={questionTypeLookup}
                    rows={props.rows}
                    addToRows={props.addToRows}
                    columns={props.columns}
                    addToColumns={props.addToColumns}
                    selectedMetricName={props.selectedMetric?.name}
                    selectMetric={props.selectMetric}
                    nameOfListItems="variables"
                    createVariable={() => toast.error("Not implemented")}
                />
            }
        </Stack>
    );
};

interface ItemsListProps {
    metrics: Metric[];
    entityConfiguration: IEntityConfiguration;
    questionTypeLookup: { [key: string]: MainQuestionType; };
    rows: TableItem[];
    addToRows(metric: Metric): void;
    columns: TableItem[];
    addToColumns(metric: Metric): void;
    selectedMetricName: string | undefined;
    selectMetric(metric: Metric): void;
    nameOfListItems: string;
    createVariable?(): void;
}

const ItemsList = (props: ItemsListProps) => {
    const [searchText, setSearchText] = useState<string>('');
    const loweredSearchText = searchText.trim().toLowerCase();
    const filteredMetrics = props.metrics.filter(metric =>
        metric.displayName?.toLowerCase().includes(loweredSearchText) ||
        metric.helpText?.toLowerCase().includes(loweredSearchText)
    );
    const capitalizedItemName = props.nameOfListItems.charAt(0).toUpperCase() + props.nameOfListItems.slice(1);

    return (
        <Stack direction="column" spacing={2}>
            <Stack direction="column" spacing={0.5}>
                <Typography variant="subtitle1">
                    Available {capitalizedItemName}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    Add {props.nameOfListItems} to rows and columns to build your table
                </Typography>
            </Stack>
            {props.createVariable &&
                <Button variant="contained" onClick={props.createVariable} startIcon={<AddIcon />}>
                    Create Variable
                </Button>
            }
            <TextField
                placeholder="Search questions..."
                size="small"
                value={searchText}
                onChange={e => setSearchText(e.target.value)}
                slotProps={{
                    input: {
                        startAdornment: (
                            <InputAdornment position="start">
                                <SearchIcon fontSize="small" />
                            </InputAdornment>
                        ),
                    }
                }}
                sx={{ maxWidth: 350 }}
            />
            {loweredSearchText != '' &&
                <Typography variant="body2" color="text.secondary">
                    {filteredMetrics.length} of {props.metrics.length} {props.nameOfListItems}
                </Typography>
            }
            {filteredMetrics.map(metric => (
                <QuestionVariableListItem
                    key={metric.name}
                    metric={metric}
                    entityConfiguration={props.entityConfiguration}
                    questionTypeLookup={props.questionTypeLookup}
                    isSelected={metric.name === props.selectedMetricName}
                    onSelect={() => props.selectMetric(metric)}
                    isRow={props.rows.some(r => r.metric.name === metric.name)}
                    addToRows={() => props.addToRows(metric)}
                    isColumn={props.columns.some(c => c.metric.name === metric.name)}
                    addToColumns={() => props.addToColumns(metric)}
                />
            ))}
            {filteredMetrics.length === 0 &&
                <Typography variant="subtitle1" color="text.secondary" sx={{ textAlign: 'center' }}>
                    No {props.nameOfListItems} found {loweredSearchText != '' && ("matching \"" + searchText + "\"")}
                </Typography>
            }
        </Stack>
    );
};

export default QuestionsVariablesList;