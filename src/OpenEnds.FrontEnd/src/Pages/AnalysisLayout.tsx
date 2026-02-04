import React, { useCallback, useEffect, useState } from 'react';
import { Tabs, Tab, Typography, Box, Button, IconButton } from '@mui/material';
import { Outlet, useLocation, useNavigate, useParams } from 'react-router-dom';
import * as OpenEndApi from '@model/OpenEndApi';
import { Question } from '@model/Model';
import { useThemeSummaryStore } from '@model/themeSummaryStore';
import ProgressBarLoader from './ProgressBarLoader';
import HelpIcon from './HelpIcon';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { isNonEmptyString } from '../utils';
import CopyToClipboardTooltip from '../Template/CopyToClipboardTooltip';
import ailaLogo from "../assets/aila-logo.svg";

const AnalysisLayout = () => {
    const location = useLocation();
    const navigate = useNavigate();
    const { questionId, surveyId } = useParams();
    const [selectedTab, setSelectedTab] = useState(0);
    const [question, setQuestion] = useState<Question | undefined>(undefined);
    const { themeSummary, reloadThemeSummary } = useThemeSummaryStore();

    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (!loading && isNonEmptyString(surveyId)) {
            const fetchData = async () => {
                try {
                    reloadThemeSummary(surveyId, Number(questionId));
                } catch (error) {
                    console.error('Error fetching data:', error);
                }
            };

            fetchData();
        }
    }, [surveyId, questionId, loading, reloadThemeSummary]);

    useEffect(() => {
        // Fetch question from the API when the component loads
        if (isNonEmptyString(surveyId)) {
            const fetchQuestion = async () => {
                try {
                    const response = await OpenEndApi.getQuestion(surveyId, Number(questionId));
                    setQuestion(response);
                } catch (error) {
                    console.error('Error fetching question:', error);
                }
            };

            fetchQuestion();
        }
    }, [surveyId, questionId, reloadThemeSummary]);

    useEffect(() => {
        if (location.pathname.includes('themes')) {
            setSelectedTab(0);
        } else if (location.pathname.includes('configuration')) {
            setSelectedTab(1);
        }
    }, [location.pathname]);

    const handleTabChange = (_event: React.ChangeEvent, newValue: number) => {
        setSelectedTab(newValue);
        if (newValue === 0) {
            navigate('themes');
        } else if (newValue === 1) {
            navigate('configuration');
        }
    };

    const handleDoubleClick = () => {
        const textToCopy = `${surveyId}_${questionId}`;
        navigator.clipboard.writeText(textToCopy).then(() => {
            console.log('Text copied to clipboard');
        }).catch(err => {
            console.error('Failed to copy text: ', err);
        });
    };

    const handleBackClick = () => {
        const questionPage = `/survey/${surveyId}`;
        navigate(questionPage);
    }

    const handleProgressComplete = useCallback(() => setLoading(false), []);

    const getAddedInstructions = (instructions: string) => {
        const toolTipContentPretext = <Typography component="span" sx={{ fontWeight: 500 }}>Added Instructions: </Typography>
        const toolTipContent = <Typography>{toolTipContentPretext} {instructions}</Typography>

        return (
            <CopyToClipboardTooltip toolTipContent={toolTipContent} toolTipTextToCopy={instructions}>
                <Typography variant="body2" component="span" sx={{ fontWeight: 500, textWrap: 'nowrap', cursor: 'pointer' }}>
                    <IconButton disableRipple sx={{ pt: 0.5 }}>
                        <img src={ailaLogo} alt="aila logo" />
                    </IconButton>
                    Added instructions
                </Typography>
            </CopyToClipboardTooltip>
        )
    }

    return (
        <div>
            <Box display='flex' justifyContent="space-between">
                <Box display='flex' alignItems='center' gap={1}>
                    <Button variant="text" color="primary" sx={{ alignItems: 'center' }} onClick={handleBackClick} startIcon={<ArrowBackIcon style={{ fontSize: "medium" }} />}>
                        <Typography variant="h6" sx={{ fontWeight: '400' }}>Back</Typography>
                    </Button>
                    <Typography variant="h6" fontWeight="500" onDoubleClick={handleDoubleClick}>
                        Question: {question?.varCode}: {question?.text}
                        {themeSummary?.additionalInstructions && getAddedInstructions(themeSummary?.additionalInstructions)}
                    </Typography>
                </Box>
                <Box>
                    <HelpIcon helpUrl='https://docs.savanta.com/internal/Content/AllVue/The_Analysis_page.html' iconText='Analysis help' />
                </Box>
            </Box>
            {loading && isNonEmptyString(surveyId) ? (
                <ProgressBarLoader
                    surveyId={surveyId}
                    questionId={Number(questionId)}
                    onComplete={handleProgressComplete}
                />
            ) : (
                <>
                    <Box display="flex" justifyContent="space-between" alignItems="center">
                        <Tabs value={selectedTab} onChange={handleTabChange}>
                            <Tab label="Themes" />
                            <Tab label="Configuration" />
                        </Tabs>
                    </Box>
                    <Outlet />
                </>
            )}
        </div>
    );
};

export default AnalysisLayout;
