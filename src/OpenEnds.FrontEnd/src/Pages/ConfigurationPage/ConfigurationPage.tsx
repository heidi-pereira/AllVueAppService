import { useState, useEffect } from 'react';
import { Box, Skeleton } from '@mui/material';
import * as OpenEndApi from '@model/OpenEndApi';
import { OpenEndTheme, ThemeConfigurationResponse } from '@model/Model';
import { useParams } from 'react-router-dom';
import { useThemeSummaryStore } from '@model/themeSummaryStore';
import RootBox from '../../Template/RootBox';
import CreateThemeDialog from './CreateThemeDialog';
import ThemeEditor from './ThemeEditor';
import mixpanel from 'mixpanel-browser';
import ThemeList from './ThemeList';
import { useCustomConfirm } from '../../hooks/useCustomConfirm';
import { isNonEmptyString, themesAsHierarchy } from '../../utils';

export default function ThemeSensitivity() {
    const { questionId, surveyId } = useParams();
    const themeSummary = useThemeSummaryStore((state) => state.themeSummary);
    const reloadThemeSummary = useThemeSummaryStore((state) => state.reloadThemeSummary);
    const confirm = useCustomConfirm();

    const [selectedTheme, setSelectedTheme] = useState<OpenEndTheme>();
    const [createDialogOpen, setCreateDialogOpen] = useState(false);
    const [themeConfiguration, setThemeConfiguration] = useState<ThemeConfigurationResponse>();

    useEffect(() => {
        mixpanel.track('Text Analysis Configuration Page Loaded', { "Survey": surveyId, "Question": questionId });
    }, [surveyId, questionId]);

    useEffect(() => {
        if (themeSummary && isNonEmptyString(surveyId)) {
            const fetchThemeConfiguration = async () => {
                try {
                    const response = await OpenEndApi.getThemeConfiguration(surveyId, Number(questionId));
                    setThemeConfiguration(response);
                } catch (error) {
                    console.error('Error fetching theme configuration:', error);
                }
            };

            fetchThemeConfiguration();

            if (!themeSummary.themes.find(t => t.themeId === selectedTheme?.themeId)) { 
                handleSelectedThemeChange(themeSummary.themes[0]);
            }
        }
    }, [themeSummary, surveyId, questionId]);

    const handleSelectedThemeChange = (theme: OpenEndTheme) => {
        setSelectedTheme(theme);
    };

    const handleDeleteTheme = async () => {
        if (selectedTheme && isNonEmptyString(surveyId)) {
            confirm({ 
                title:'Delete theme', 
                description: <b>{`Are you sure you want to delete the theme "${selectedTheme.themeText}"?`}</b>, confirmationText: 'Delete' })
                .then(async () => {
                    try {
                        mixpanel.track('Text Analysis Theme Delete', {
                            "Survey": surveyId,
                            "Question": questionId,
                            "Theme": selectedTheme.themeId,
                            "ThemeText": selectedTheme.themeText
                        });

                        await OpenEndApi.deleteThemeConfiguration(surveyId, Number(questionId), selectedTheme.themeId);
                        await reloadThemeSummary(surveyId, Number(questionId));
                    } catch (error) {
                        console.error('Error deleting theme:', error);
                    }
                })
                .catch(() => {
                    // User cancelled the confirmation
                });
        }
    };

    const handleCreateDialogOpen = () => {
        setCreateDialogOpen(true);
    };

    const handleCreateDialogClose = () => {
        setCreateDialogOpen(false);
    };

    const selectedThemeConfiguration = themeConfiguration?.themes.find(t => t.id === selectedTheme?.themeId);
    const rootThemes = themeSummary ? themesAsHierarchy(themeSummary.themes) : [];

    return (
        <RootBox borderRadius={2} m={2} sx={{ flexGrow: 1, display: 'flex' }}>
            <Box mr={1} sx={{ minWidth: '350px' }}>
                {themeSummary ? (
                    <ThemeList
                        themes={rootThemes}
                        selectedTheme={selectedTheme}
                        onThemeSelect={handleSelectedThemeChange}
                        onCreateDialogOpen={handleCreateDialogOpen}
                        onDeleteTheme={handleDeleteTheme}
                    />
                ) : (
                    <Skeleton variant="rectangular" sx={{m:3}} width={350} height={600} />
                )}
            </Box>
            <Box sx={{ flexGrow: 1, p: 3 }}>
                {selectedThemeConfiguration ? (
                    <ThemeEditor themeConfiguration={selectedThemeConfiguration} />
                ) : (
                    <Skeleton variant="rectangular" width="100%" height={600} />
                )}
            </Box>
            {isNonEmptyString(surveyId) && <CreateThemeDialog open={createDialogOpen} onClose={handleCreateDialogClose} surveyId={surveyId} questionId={Number(questionId)} />}
        </RootBox>
    );
}