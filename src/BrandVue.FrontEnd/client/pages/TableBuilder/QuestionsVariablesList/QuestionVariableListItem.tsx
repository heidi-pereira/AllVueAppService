import { Box, Button, Stack, SxProps, Typography } from "@mui/material";
import { IEntityConfiguration } from "../../../entity/EntityConfiguration";
import { Metric } from "../../../metrics/metric";
import MultiLineEllipsisTypography from "../Generics/ClampedMultiLineTypography";
import AddIcon from '@mui/icons-material/Add';
import { MainQuestionType } from "../../../BrandVueApi";
import { camelCaseToWords, plural } from "../TableBuilderUtils";

interface Props {
    metric: Metric;
    entityConfiguration: IEntityConfiguration;
    questionTypeLookup: { [key: string]: MainQuestionType; }
    isSelected: boolean;
    onSelect(): void;
    isRow: boolean;
    addToRows(): void;
    isColumn: boolean;
    addToColumns(): void;
}

const QuestionVariableListItem = (props: Props) => {

    const optionCount = props.metric.entityCombination.length > 0
        ? props.entityConfiguration.getAllEnabledInstancesForTypeOrdered(props.metric.entityCombination[0]).length
        : 1;
    const questionType = props.questionTypeLookup[props.metric.name];

    const boxStyle: SxProps = { py: 0.5, px: 1, fontSize: '0.85rem' };

    return (
        <Stack direction="column"
            spacing={1}
            sx={{
                p: 1,
                border: '2px solid',
                borderColor: props.isSelected ? 'black' : 'transparent',
                boxShadow: props.isSelected ? undefined : '0 0 0 1px #ddd',
                transition: 'border-color 0.1s'
            }}
            onClick={props.onSelect}
        >
            <Typography variant="body2" color="text.secondary">
                {props.metric.displayName}
            </Typography>
            <MultiLineEllipsisTypography lineClamp={2} title={props.metric.helpText}>
                {props.metric.helpText}
            </MultiLineEllipsisTypography>
            <Stack direction="row" spacing={1}>
                <Button
                    variant="outlined"
                    startIcon={<AddIcon />}
                    disabled={props.isRow}
                    onClick={(e) => {
                        e.stopPropagation();
                        props.addToRows();
                    }}
                >
                    Rows
                </Button>
                <Button
                    variant="outlined"
                    startIcon={<AddIcon />}
                    disabled={props.isColumn}
                    onClick={(e) => {
                        e.stopPropagation();
                        props.addToColumns();
                    }}
                >
                    Columns
                </Button>
            </Stack>
            <Stack direction="row" spacing={1}>
                {questionType &&
                    <Box sx={{ ...boxStyle, background: "#ddd" }}>
                        {camelCaseToWords(questionType.toString())}
                    </Box>
                }
                <Box sx={{ ...boxStyle, border: "1px solid #ddd" }}>
                    {optionCount} {plural(optionCount, "option", "options")}
                </Box>
            </Stack>
        </Stack>
    );
};
export default QuestionVariableListItem;