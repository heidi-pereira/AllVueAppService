import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import { Box, Button, Chip, Typography } from '@mui/material';
import HeaderBox from '../../Template/HeaderBox';
import CallSplitIcon from '@mui/icons-material/CallSplit';
import * as OpenEndApi from '@model/OpenEndApi';
import { OpenEndTheme, RootTheme } from '../../Model/Model';
import * as Utils from '@/utils';
import { useThemeSummaryStore } from '../../Model/themeSummaryStore';
import mixpanel from 'mixpanel-browser';
import { ErrorOutlined } from "@mui/icons-material";
import { isNonEmptyString } from '../../utils';
import { flexCenterSx } from '../../Theme/sxStyles';

interface SubthemesProps {
    theme: RootTheme;
}

const Subthemes: React.FC<SubthemesProps> = ({ theme }) => {
    const { questionId, surveyId } = useParams();

    const subthemes = theme.subThemes;
    const [errorMessage, setErrorMessage] = useState<string>('');
    const [isCreatingSplit, setIsCreatingSplit] = useState<boolean>(false);
    const reloadThemeSummary = useThemeSummaryStore((state) => state.reloadThemeSummary);

    const questionIdNumber = questionId ? Number.parseInt(questionId) : undefined;

    const splitIcon = () => {
        const sx = { fontSize: 'large', color: 'primary.main' };
        return <CallSplitIcon sx={sx} />;
    }

    const subthemeChip = (subtheme: OpenEndTheme, index: number) => {
        const percentage = <Typography variant="caption" sx={{ fontWeight: 600, lineHeight: "unset" } }>{Utils.displayPercentage(subtheme.percentage)}</Typography>;
        const label = <Box sx={flexCenterSx}>{percentage}{subtheme.themeText}</Box>;

        return (
            <Chip key={index} label={label} variant="outlined" size="small" className="small" color="primary" />
        )
    }

    const noSubthemesAvailable = () => {
        return (
            <>
                <Typography component="span" fontSize="small" sx={{ fontWeight: "400", color: 'primary.main', pl: 1 }} >
                    No subthemes available 
                </Typography>
            </>
        );
    }

    const createSplit = async () => {
        const canDeleteCurrentTheme = isNonEmptyString(surveyId) && questionIdNumber;
        const canCreateNewThemes = subthemes.length > 1 && subthemes.every(s => s.themeText);
        if (canDeleteCurrentTheme && canCreateNewThemes) {
            try {
                setIsCreatingSplit(true);

                mixpanel.track('Text Analysis Theme Split Create - Themes tab', { "Survey": surveyId, "Question": questionId });

                await Promise.all([
                    ...subthemes.map(s => OpenEndApi.updateThemeParent(surveyId, questionIdNumber, s.themeId, null)
                    )
                ]);

                await OpenEndApi.deleteThemeConfiguration(surveyId, questionIdNumber, theme.themeId);

                await reloadThemeSummary(surveyId, questionIdNumber);
            } catch (error) {
                setErrorMessage('Error splitting theme');
                console.error('Error splitting theme:', error);
            } finally {
                setIsCreatingSplit(false);
            }
        }
    }

    const splitCheckResult = () => {
        const subthemesAvailable = subthemes?.length > 0;

        if (errorMessage && !subthemesAvailable) {
            return getErrorMessage();
        }

        if (!subthemesAvailable) {
            return noSubthemesAvailable();
        }

        return (
            <>
                <Button
                    variant="text"
                    onClick={createSplit}
                    sx={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "stretch",
                        cursor: "pointer",
                        m: "0 1rem",
                        p: 0,
                        gap: 1
                    }}
                    disabled={isCreatingSplit}
                >
                    {splitIcon()}
                    <Typography variant="body2" className="actionText" noWrap>
                        Split theme
                    </Typography>
                </Button>
                {subthemes.map(subthemeChip)}
                {errorMessage && getErrorMessage()}
            </>
        )
    }

    const getErrorMessage = () => {
        return (
            <>
                <ErrorOutlined sx={{ fontSize: 'medium', color: 'error.main', marginLeft: 1 }} />
                <Typography component="span" fontSize="small" sx={{ fontWeight: "400", color: 'error.main' }} >
                    {errorMessage}
                </Typography>
            </>
        )
    }

  return (
      <HeaderBox sx={{mt: 2, gap: 1}}>
          <Typography variant="h5" fontWeight="600" noWrap sx={{ mr: 1 }}>
              Subthemes
          </Typography>
          <Box sx={flexCenterSx}>
              {splitCheckResult()}
          </Box>
    </HeaderBox>
  );
};

export default Subthemes;