import { Box, LinearProgress, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import * as OpenEndApi from '@model/OpenEndApi';
import { OpenEndQuestionStatusResponse, StatusEvent } from '@model/Model';
import mixpanel from 'mixpanel-browser';

interface ProgressBarLoaderProps {
    surveyId: string;
    questionId: number;
    onComplete: () => void;
}

const ProgressBarLoader = ({ surveyId, questionId, onComplete }: ProgressBarLoaderProps) => {
    const [progress, setProgress] = useState<OpenEndQuestionStatusResponse>({progress: 0, message: 'Initialising analysis...', statusEvent: undefined });

    useEffect(() => {
        let isMounted = true;

        const checkStatus = async () => {
            try {
                const statusResponse = await OpenEndApi.getQuestionStatus(surveyId, questionId);
                if (isMounted && statusResponse) {


                    if (statusResponse.statusEvent == StatusEvent.newProject) {
                        mixpanel.track('Text Analysis New Analysis', { "Survey": surveyId, "Question": questionId });
                    }

                    setProgress(statusResponse);
                    if (statusResponse.progress >= 100) {
                        onComplete();
                    } else {
                        setTimeout(checkStatus, 3000);
                    }
                }
            } catch (error) {
                console.error('Error fetching status:', error);
            }
        };

        checkStatus();

        return () => {
            isMounted = false;
        };
    }, [surveyId, questionId, onComplete]);

    return (
        <Box>
            {progress && (
                <Box m={7} p={3} boxShadow={3} borderRadius={2} flexGrow={1}>
                    <Typography variant="h6" mb={1}>
                        {progress.message}
                    </Typography>
                    <LinearProgress variant="determinate" value={progress.progress} />
                    <Typography mt={1} variant="body2" color="textSecondary">{`${progress.progress}%`}</Typography>
                </Box>
            )}
        </Box>
    );
};

export default ProgressBarLoader;
