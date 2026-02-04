import { OpenEndQuestion } from '@model/Model';
import { FormControl, InputLabel, MenuItem, Select, Typography, Box, SelectChangeEvent, Tooltip } from '@mui/material';
import useGlobalDetailsStore from '@model/globalDetailsStore';
import HelpIcon from '../HelpIcon';

interface IQuestionSelectorProps {
    questions: OpenEndQuestion[];
    respondentCount: number | undefined;
    selectedQuestionId?: number;
    setSelectedQuestionId: (selectedQuestionId: number) => void;
}

const QuestionSelector = ({ questions, respondentCount, selectedQuestionId, setSelectedQuestionId }: IQuestionSelectorProps) => {

    const { details } = useGlobalDetailsStore();

    const handleSelectQuestion = (event: SelectChangeEvent) => {
        const questionId = event.target.value;
        setSelectedQuestionId(Number(questionId));
    };

    const getHelpTooltip = () => {
        const tooltipText = "Response counts may differ by question (e.g. if some are optional)."
        return (
            <Tooltip title={tooltipText} placement={'bottom'}>
                <Typography sx={{ display: 'inline-flex' }}>
                    <HelpIcon />
                </Typography>
            </Tooltip>
        )
    }

    return (
        <Box sx={{ mt: 2 }}>
            <Typography variant="h6" gutterBottom sx={{ fontSize: '1.123rem' }}>
                Question{respondentCount && <Typography component="span" sx={{ fontWeight:600 }}>: {respondentCount} respondents</Typography>}
            </Typography>
            <Typography variant="body2" gutterBottom>
                Select a question to begin
            </Typography>
            <FormControl sx={{ width: "500px", backgroundColor: '#fff' }} size="small">
                {!selectedQuestionId && <InputLabel id="question-select-label" shrink={false} sx={{ fontSize: '0.75rem', lineHeight: '1.75' }} >Select question</InputLabel>}
                <Select
                    labelId="question-select-label"
                    value={selectedQuestionId?.toString() ?? ''}
                    onChange={handleSelectQuestion}
                    sx={{ fontWeight: 400 }}
                    MenuProps={{
                        PaperProps: {
                            style: {
                                maxHeight: 350,
                                width: 500,
                            },
                        },
                    }}
                >
                    {questions.map((question) => (
                            <MenuItem disabled={question.questionCount > details.maxTexts} key={question.question.id}
                            value={question.question.id.toString()}
                                sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', p: '10px 8px' }}>
                            {question.question.id !== selectedQuestionId &&
                                    <Typography variant="caption" sx={{ letterSpacing: '0.0375rem', color: '#6E7881', fontWeight: 500, textWrap: 'wrap' }}>
                                    {question.question.varCode}
                                        <Typography component="span" variant="inherit" sx={{ ml: 1 }}>{question.questionCount.toLocaleString()} responses{getHelpTooltip()}</Typography>
                                    </Typography>}
                            <Tooltip title={question.question.text} placement={'right'}>
                                <Typography variant="body2" sx={{ textWrap: 'wrap', WebkitLineClamp: question.question.id === selectedQuestionId ? 1 : 3, overflow: 'hidden', textOverflow: 'ellipsis', display: '-webkit-box', WebkitBoxOrient: 'vertical' }}>
                                    {question.question.text}
                                    </Typography>
                                </Tooltip>
                            </MenuItem>
                        )
                    )}
                </Select>
            </FormControl>
        </Box>
    );
};

export default QuestionSelector;
