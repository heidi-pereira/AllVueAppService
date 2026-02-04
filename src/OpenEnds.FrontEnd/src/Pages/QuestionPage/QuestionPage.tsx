import { useEffect, useState, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Typography, Box, Button, Link, Grid, Menu, MenuItem, TextField, InputAdornment } from '@mui/material';
import QuestionSelector from './QuestionSelector';
import { OpenEndQuestion, OpenEndQuestionsResponse, StatusEvent } from '@model/Model';
import * as OpenEndApi from '@model/OpenEndApi';
import useTimeout from '../../hooks/useTimeout'; // Import the useTimeout hook
import TileComponent from './QuestionTile';
import { useCustomConfirm } from '@/hooks/useCustomConfirm';
import { isNonEmptyString } from '../../utils';
import ailaLogo from "../../assets/aila-logo.svg";

const OpenEnds = () => {
    const { surveyId } = useParams();
    const confirm = useCustomConfirm();
    const navigate = useNavigate();

    const [selectedQuestionId, setSelectedQuestionId] = useState<undefined | number>();
    const [questionsResponse, setQuestionsResponse] = useState<OpenEndQuestionsResponse>();
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const [menuQuestionId, setMenuQuestionId] = useState<number | null>(null);
    const [additionalInstructions, setAdditionalInstructions] = useState<string>("");

    const [reload, setReload] = useState(true);
    const [hasUnfinishedQuestions, setHasUnfinishedQuestions] = useState(false); // New state

    const questions = questionsResponse ? questionsResponse.openTextQuestions : [];

    useTimeout(() => {
        if (questions.some(q =>
            q.status.statusEvent !== StatusEvent.newProject
            && q.status.statusEvent !== StatusEvent.finished)) {
            setReload(true);
        }
    }, hasUnfinishedQuestions ? 5000 : null); // Call useTimeout with a 3-second timer if there are unfinished questions

    useEffect(() => {
        if (reload && isNonEmptyString(surveyId)) {
            const fetchQuestions = async () => {
                try {
                    const response = await OpenEndApi.getQuestions(surveyId);
                    setQuestionsResponse(response);
                    setHasUnfinishedQuestions(response.openTextQuestions.some(q =>
                        q.status.statusEvent !== StatusEvent.newProject
                        && q.status.statusEvent !== StatusEvent.finished)); // Update state
                } catch (error) {
                    console.error('Error fetching questions:', error);
                }
            };

            fetchQuestions();
            setReload(false);
        }
    }, [surveyId, reload]);

    const handleAnalyse = async (questionId?: number) => {
        if (isNonEmptyString(surveyId)) {
            setSelectedQuestionId(undefined);
            await OpenEndApi.initialiseProject(surveyId, questionId!, additionalInstructions);
            setReload(true);
        }
    };

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, question: OpenEndQuestion) => {
        if (question.status.statusEvent === StatusEvent.finished) {
            event.stopPropagation();
            setAnchorEl(event.currentTarget);
            setMenuQuestionId(question?.question.id);
        }
    };

    const handleMenuClose = () => {
        setAnchorEl(null);
        setMenuQuestionId(null);
    };

    const handleRecalculate = async () => {
        confirm({
            description: <>
                Recalculating this analysis will erase your changes and analyse your data from the beginning. Please note, since Aila uses AI, the results may vary slightly each time.
                <br /><br />
                <b>Are you sure you want to recalculate?</b>
            </>,
            title: 'Recalculate all responses',
            confirmationText: 'Recalculate'
        })
            .then(async () => {
                if (isNonEmptyString(surveyId)) {
                    try {
                        await OpenEndApi.recalculateQuestion(surveyId, menuQuestionId!);
                        setReload(true);
                    } catch (error) {
                        console.error('Error deleting theme:', error);
                    }
                }
            })
            .catch(() => {
                // User cancelled the confirmation
            });
        handleMenuClose();
    };

    const handleRemove = async () => {
        confirm({
            description: <><b>Are you sure you want to delete this analysis?</b></>,
            title: 'Delete analysis',
            confirmationText: 'Delete'
        })
            .then(async () => {
                if (isNonEmptyString(surveyId)) {
                    try {
                        await OpenEndApi.deleteQuestion(surveyId, menuQuestionId!);
                        setReload(true);
                    } catch (error) {
                        console.error('Error deleting theme:', error);
                    }
                }
            })
            .catch(() => {
                // User cancelled the confirmation
            });
        handleMenuClose();
    };

    const eligibleQuestions = useMemo(() => {
        return questions.filter(q => q.status.statusEvent === StatusEvent.newProject);
    }, [questions]);

    const analysedQuestions = useMemo(() => {
        return questions.filter(q => q.status.statusEvent !== StatusEvent.newProject);
    }, [questions]);

    const handleViewThemes = (question: OpenEndQuestion) => {
        if (question.status.statusEvent === StatusEvent.finished) {
            // Navigate to the results route with the selected questionId
            navigate(`question/${question!.question.id}/themes`);
        }
    }

    return (
        <Box>
            <Box display="flex" flexDirection="row" gap={2}>
                <Box>
                    <Box>
                        <Typography variant="h6" gutterBottom sx={{ fontSize: '1.123rem' }}>
                            Open ends
                        </Typography>
                        <Typography variant="body2" gutterBottom>
                            Choose from a list of open-ended survey questions to begin generating insightful themes. Need help? See <Link href="https://docs.savanta.com/internal/Content/AllVue/The_Analysis_page.html" target='_blank' underline='hover' sx={{ color: 'primary.main', '& :hover': { fontWeight: '500' } }}>
                                <Typography variant="body2" component="span">
                                    more information here
                                </Typography>
                            </Link>
                        </Typography>
                    </Box>
                    {eligibleQuestions.length === 0 &&
                        <Box sx={{ mt: 2 }}>
                            <Typography variant="h6" gutterBottom sx={{ fontSize: '1.123rem' }}>
                                Question
                            </Typography>
                            <Typography gutterBottom>
                                {analysedQuestions.length === 0
                                    ? 'There are no open-ended questions available for analysis.'
                                    : 'All open-ended questions have been analysed.'}
                            </Typography>
                        </Box>
                    }
                    {eligibleQuestions.length > 0 &&
                        <>
                            <QuestionSelector
                                questions={eligibleQuestions}
                                respondentCount={questionsResponse?.respondentCount}
                                selectedQuestionId={selectedQuestionId}
                                setSelectedQuestionId={setSelectedQuestionId} />

                        </>
                    }
                </Box>
                <Box>
                    <Typography variant="h6" gutterBottom sx={{ fontSize: '1.123rem' }}>
                        Additional instructions <Typography component="span" sx={{ color: '#999797'}}>(optional)</Typography>
                    </Typography>
                    <Typography variant="body2" gutterBottom>
                        You can give Aila more context to help with theming your data. For example, giving more context for tags or instructions on how to split your data into themes
                    </Typography>
                    {eligibleQuestions.length > 0 &&
                            <TextField
                        id="additionalInstructions"
                        multiline
                        rows={4}
                        maxRows={8}
                        placeholder="Instructions here"
                        value={additionalInstructions}
                        onChange={(e) => setAdditionalInstructions(e.target.value)}
                        fullWidth
                        sx={{ backgroundColor: '#fff' }}
                        slotProps={{
                            input: {
                                inputProps: { maxLength: 200 },
                                startAdornment: (
                                    <InputAdornment position="start" sx={{ alignSelf: "flex-start"}}>
                                        <img src={ailaLogo} alt="aila logo" />
                                    </InputAdornment>
                                ),
                            },
                            formHelperText: {
                                sx: { backgroundColor: 'background.default', fontWeight: 500 }
                            }
                        }}
                        helperText={`${additionalInstructions.length}/200`}
                        />

                    }
                </Box>
            </Box>
            {eligibleQuestions.length > 0 &&
                <Button
                    variant="contained"
                    color="primary"
                    onClick={() => handleAnalyse(selectedQuestionId)}
                    disabled={!selectedQuestionId}
                    sx={{
                        mt: 3,
                        textTransform: 'none',
                        '&.Mui-disabled': {
                            backgroundColor: 'primary.main',
                            opacity: '32%',
                            color: 'white',
                        },
                    }}
                >
                    Run analysis
                </Button>}
            {analysedQuestions.length > 0 && isNonEmptyString(surveyId) &&
                <>
                    <Typography mt={3} variant="h6" gutterBottom sx={{ fontSize: '1.123rem' }}>
                        Analysed questions
                    </Typography>
                    <Box mt={1}>
                        <Grid container spacing={2}>
                            {analysedQuestions.map(q => (
                                <TileComponent
                                    key={q.question.id}
                                    surveyId={surveyId}
                                    question={q}
                                    handleViewThemes={handleViewThemes}
                                    handleMenuOpen={handleMenuOpen}
                                />
                            ))}
                        </Grid>
                        <Menu
                            anchorEl={anchorEl}
                            open={Boolean(anchorEl)}
                            onClose={handleMenuClose}
                        >
                            <MenuItem onClick={handleRecalculate}>Recalculate analysis</MenuItem>
                            <MenuItem onClick={handleRemove}>Delete</MenuItem>
                        </Menu>
                    </Box>
                </>
            }
        </Box>
    );
};

export default OpenEnds;
