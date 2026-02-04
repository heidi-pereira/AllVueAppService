import { OpenEndQuestion, StatusEvent } from "@/Model/Model";
import RootBox from "@/Template/RootBox";
import { GridView, RecordVoiceOverOutlined, MoreVert } from "@mui/icons-material";
import { Box, Grid, IconButton, LinearProgress, Typography } from "@mui/material";
import { useEffect, useState } from "react";
import * as OpenEndApi from '@model/OpenEndApi';
import { calculatePercentage } from "../../utils";
import { flexCenterSx } from "../../Theme/sxStyles";
import ailaLogo from "../../assets/aila-logo.svg";
import CopyToClipboardTooltip from "../../Template/CopyToClipboardTooltip";

interface TileComponentProps {
    question: OpenEndQuestion;
    surveyId: string;
    handleViewThemes: (question: OpenEndQuestion) => void;
    handleMenuOpen: (event: React.MouseEvent<HTMLElement>, question: OpenEndQuestion) => void;
}

const TileComponent = ({ question, surveyId, handleViewThemes, handleMenuOpen }: TileComponentProps) => {
    const [codedQuestionCount, setCodedQuestionCount] = useState<number>(0);

    useEffect(() => {
        if (question.status.statusEvent === StatusEvent.finished) {
            const fetchCodedQuestionCount = async () => {
                try {
                    const response = await OpenEndApi.getCodedTextCount(surveyId, question.question.id);
                    setCodedQuestionCount(response);
                } catch (error) {
                    console.error('Error fetching coded question count:', error);
                }
            };

            fetchCodedQuestionCount();
        }
    }, [question.status.statusEvent]);

    const analysedQuestionTile = () => {
        const percentageCoded = calculatePercentage(codedQuestionCount, question.questionCount);

        return (
            <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Box sx={flexCenterSx}>
                    <GridView sx={{ color: '#31B10A', fontSize: "16px" }} />
                    <Typography variant="body2">
                        <Typography component="span" variant="body2" sx={{ fontWeight: 600 }}>
                            {question.themeCount.toLocaleString()}
                        </Typography> Themes
                    </Typography>
                </Box>
                <Box sx={flexCenterSx}>
                    <RecordVoiceOverOutlined sx={{ color: '#ED2283', fontSize: "16px" }} />
                    <Typography variant="body2">
                        <Typography component="span" variant="body2" sx={{ fontWeight: 600 }}>
                            {percentageCoded} coded ({codedQuestionCount?.toLocaleString()}/{question.questionCount.toLocaleString()})
                        </Typography>
                    </Typography>
                </Box>
            </Box>
        );
    };

    const analysisInProgressQuestionTile = () => {
        return (
            <Box>
                <Typography variant="h6" mb={1}>
                    {question.status.message}
                </Typography>
                <LinearProgress variant="determinate" value={question.status.progress} />
                <Typography mt={1} variant="body2" color="textSecondary">{`${question.status.progress}%`}</Typography>
            </Box>
        );
    };

    const addedInstructions = (instructions: string) => {
        const toolTipContentPretext = <Typography component="span" sx={{ fontWeight: 500 }}>Added Instructions: </Typography>
        const toolTipContent = <Typography>{toolTipContentPretext} {instructions}</Typography>

        return (
            <CopyToClipboardTooltip toolTipContent={toolTipContent} toolTipTextToCopy={instructions}>
                <IconButton>
                    <img src={ailaLogo} alt="aila logo" />
                </IconButton>
            </CopyToClipboardTooltip>
        )
    }

    return (
        <Grid item xs={12} sm={6} md={4} key={question.question.id}>
            <RootBox
                mb={2}
                p={3}
                border={1}
                borderColor="grey.300"
                sx={{
                    height: '200px',
                    position: 'relative',
                    cursor: question.status.statusEvent === StatusEvent.finished ? 'pointer' : 'default',
                    boxShadow: '3px 3px 10px rgba(0, 0, 0, 0.2)', // Add drop shadow to the 
                    borderRadius: 0 // Remove rounded corners
                }}
            >
                <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }} onClick={() => handleViewThemes(question)}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems:'center'}}>
                        <Box>
                            <Typography variant="body1" sx={{ fontWeight: 600 }}>{question.question.varCode}</Typography>
                        </Box>
                        <Box>
                            {question.additionalInstructions && addedInstructions(question.additionalInstructions)}
                            {question.status.statusEvent === StatusEvent.finished &&
                                <IconButton onClick={(event) => handleMenuOpen(event, question)}>
                                    <MoreVert />
                                </IconButton>
                            }
                        </Box>
                    </Box>
                    <Box flexGrow={1} mt={1} >
                        <Typography variant="body2" sx={{
                            WebkitLineClamp: 4,
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            display: '-webkit-box',
                            WebkitBoxOrient: 'vertical'
                        }}>{question.question.text}
                        </Typography>
                    </Box>
                    {question.status.statusEvent === StatusEvent.finished && analysedQuestionTile()}
                    {question.status.statusEvent !== StatusEvent.finished && analysisInProgressQuestionTile()}
                </Box>
            </RootBox>
        </Grid>
    );
};

export default TileComponent;