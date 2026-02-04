import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import { Box, Button, Chip, CircularProgress, Typography } from '@mui/material';
import HeaderBox from '../../Template/HeaderBox';
import MergeIcon from '@mui/icons-material/Merge';
import * as OpenEndApi from '@model/OpenEndApi';
import { OpenEndTheme } from '../../Model/Model';
import { useThemeSummaryStore } from '../../Model/themeSummaryStore';
import mixpanel from 'mixpanel-browser';
import * as Utils from '@/utils';

interface MergeThemesProps {
    themes: OpenEndTheme[];
}

const MergeThemes: React.FC<MergeThemesProps> = ({ themes }) => {
    const { questionId, surveyId } = useParams();

    const [isCreatingMerge, setIsCreatingMerge] = useState<boolean>(false);
    const reloadThemeSummary = useThemeSummaryStore((state) => state.reloadThemeSummary);

    const questionIdNumber = questionId ? Number.parseInt(questionId) : undefined;

    const mergeIcon = () => {
        const sx = { fontSize: 'large', color: 'primary.main' };
        return <MergeIcon sx={sx} />;
    }

    const themeChip = (theme: OpenEndTheme, id: number) => {
        const percentage = <Typography component="span" variant="inherit" fontWeight="600">{Utils.displayPercentage(theme.percentage)}</Typography>;
        const label = <Box sx={{ display: 'flex', gap: 0.5, alignItems: 'center' }}>{percentage}{theme.themeText}</Box>;

        return (
            <Chip key={id} label={label} variant="outlined" size="small" className="small" color="primary" />
        )
    }

    const createMerge = async () => {
        if (Utils.isNonEmptyString(surveyId) && questionIdNumber) {
            try {
                if (themes.length !== 2) {
                    throw new Error("Please ensure there are two themes selected before attempting to merge");
                }

                const theme1 = themes[0];
                const theme2 = themes[1];

                setIsCreatingMerge(true);

                mixpanel.track('Text Analysis Theme Merge - Themes tab', { "Survey": surveyId, "Question": questionId });

                await OpenEndApi.mergeThemes(surveyId, questionIdNumber, theme1.themeId, theme2.themeId);
                await reloadThemeSummary(surveyId, questionIdNumber);
            } catch (error) {
                console.error('Error merging themes:', error);
            } finally {
                setIsCreatingMerge(false);
            }
        }
    }

    const getMergeControls = () => {
        return (
            <>
                <Button
                    variant="text"
                    onClick={createMerge}
                    sx={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "stretch",
                        cursor: "pointer",
                        m: "0 1rem",
                        p: 0,
                        gap: 1
                    }}
                    disabled={isCreatingMerge}
                >
                    {isCreatingMerge ? <CircularProgress size={14} sx={{ marginLeft: 1 }} /> : mergeIcon()}
                    <Typography variant="body2" className="actionText" noWrap>
                        Merge themes
                    </Typography>
                </Button>
                {themes.map(themeChip)}
            </>
        )
    }

    return (
        <HeaderBox sx={{ mt: 2, mb: 1, gap: 1 }}>
            <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                {getMergeControls()}
            </Box>
        </HeaderBox>
    );
};

export default MergeThemes;