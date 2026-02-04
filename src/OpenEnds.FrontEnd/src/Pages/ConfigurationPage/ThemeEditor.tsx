import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { Box, Button, CircularProgress, Divider, Skeleton, Typography } from '@mui/material';
import { PreviewStatsResponse, ThemeConfiguration, ThemeSensitivityConfigurationResponse } from '@model/Model';
import * as OpenEndApi from '@model/OpenEndApi';
import { useParams } from 'react-router-dom';
import { useThemeSummaryStore } from '@model/themeSummaryStore';
import KeywordsEditor from './KeywordEditor';
import mixpanel, { Dict } from 'mixpanel-browser';
import NameEditor from './NameEditor';
import PrecisionEditor from './PrecisionEditor';
import HelpIcon from '../HelpIcon';
import { calculatePercentage, displayPercentage } from '@/utils';
import useDebouncedCallback from '@/hooks/useDebouncedCallback';
import { isNonEmptyString } from '../../utils';

interface SensitivityTunerProps {
    themeConfiguration: ThemeConfiguration 
}

const ThemeEditor: React.FC<SensitivityTunerProps> = ({ themeConfiguration }) => {

    const { questionId, surveyId } = useParams();
    const { themeSummary, reloadThemeSummary } = useThemeSummaryStore();
    const [sensitivity, setSensitivity] = useState<number>(0);
    const [displayName, setDisplayName] = useState<string>('');

    const [keywords, setKeywords] = useState<string[]>([])
    const [previewStats, setPreviewStats] = useState<PreviewStatsResponse>();
    const [loadingPreviewStats, setLoadingPreviewStats] = useState<boolean>(false);
    const [themeSensitivity, setThemeSensitivity] = useState<ThemeSensitivityConfigurationResponse>()

    useEffect(() => {
        if (isNonEmptyString(surveyId)) {
            const fetchThemeSensitivity = async () => {
                try {
                    const response = await OpenEndApi.getThemeSensitivity(surveyId, Number(questionId), themeConfiguration.id);
                    setThemeSensitivity(response);
                } catch (error) {
                    console.error('Error fetching theme sensitivity:', error);
                }
            };

            fetchThemeSensitivity();
            setDisplayName(themeConfiguration.name);
            setSensitivity(themeConfiguration.matchingBehaviour.matchingSensitivity);
            setKeywords(themeConfiguration.matchingBehaviour.keywords);
            setPreviewStats(undefined);
        }
    }, [themeConfiguration.id, surveyId, questionId, themeConfiguration.name, themeConfiguration.matchingBehaviour.matchingSensitivity, themeConfiguration.matchingBehaviour.keywords]);

    const { themes, maxValue, minValue } = useMemo(() => {
        const themes = themeSensitivity?.themes ?? [];

        const minValue = themes.length
            ? parseFloat((Math.min(...themes.map(t => t.distanceScore)) - 0.0005).toFixed(3))
            : 0;
        const maxValue = themes.length
            ? parseFloat(((Math.ceil(Math.max(...themes.map(t => t.distanceScore)) * 100) / 100) + 0.0005).toFixed(3))
            : 0;
        return { themes, maxValue, minValue };
    }, [themeSensitivity]);

    const debouncedTrack = useDebouncedCallback((event: string, properties?: Dict) => {
        mixpanel.track(event, properties);
    }, 1000);

    const handleSensitivityChange = useCallback((_event: Event, newValue: number | number[]) => {
        debouncedTrack('Text Analysis Theme Sensitivity Change', { "Survey": surveyId, "Question": questionId, "Theme": themeConfiguration!.id, "ThemeText": themeConfiguration!.name });
        setSensitivity(newValue as number);
    }, [sensitivity, debouncedTrack, questionId, surveyId, themeConfiguration]);

    const changesToSave = (sensitivity != themeConfiguration?.matchingBehaviour.matchingSensitivity)
        || (keywords != themeConfiguration.matchingBehaviour.keywords)
        || (displayName != themeConfiguration.name && displayName.trim() != '');

    const handleSave = async () => {
        if (isNonEmptyString(surveyId)) {
            try {
                if (sensitivity != themeConfiguration.matchingBehaviour.matchingSensitivity) {
                    mixpanel.track('Text Analysis Theme Sensitivity Change Saved', { "Survey": surveyId, "Question": questionId, "Theme": themeConfiguration!.id, "ThemeText": themeConfiguration!.name });
                }

                if (keywords != themeConfiguration.matchingBehaviour.keywords) {
                    mixpanel.track('Text Analysis Theme Match Patterns Change Saved', { "Survey": surveyId, "Question": questionId, "Theme": themeConfiguration!.id, "ThemeText": themeConfiguration!.name });
                }

                if (displayName != themeConfiguration.name) {
                    mixpanel.track('Text Analysis Theme Display Name Change Saved', { "Survey": surveyId, "Question": questionId, "Theme": themeConfiguration!.id, "ThemeText": themeConfiguration!.name });
                }

                await OpenEndApi.updateThemeConfiguration(
                    surveyId,
                    Number(questionId),
                    themeConfiguration.id,
                    sensitivity,
                    themeConfiguration.matchingBehaviour.keywords,
                    keywords,
                    displayName.trim());
                reloadThemeSummary(surveyId, Number(questionId));
            } catch (error) {
                console.error('Error saving configuration:', error);
            }
        }
    };

    const handleReset = () => {
        setDisplayName(themeConfiguration.name)
        setSensitivity(themeConfiguration.matchingBehaviour.matchingSensitivity)
        setKeywords(themeConfiguration.matchingBehaviour.keywords)
    }

    const debouncedFetchStats = useDebouncedCallback(async () => {
        if (isNonEmptyString(surveyId) && !themeConfiguration.matchingBehaviour.delegatedMatching) {
            setLoadingPreviewStats(true);
            const response = await OpenEndApi.previewStats(surveyId, Number(questionId), themeConfiguration.id, sensitivity, keywords, themeConfiguration.matchingBehaviour.matchingExamples);
            setPreviewStats(response);
            setLoadingPreviewStats(false);
        }
    }, 1000);

    useEffect(debouncedFetchStats, [sensitivity, keywords, surveyId, questionId, themeConfiguration.id, themeConfiguration.matchingBehaviour.delegatedMatching]);

    const handleMatchPatternChanged = (matches: string[]) => {
        mixpanel.track('Text Analysis Theme Match Patterns Change', { "Survey": surveyId, "Question": questionId, "Theme": themeConfiguration.id, "ThemeText": themeConfiguration.name });
        setKeywords(matches);
    }

    const themePercentage = () => {
        if (themeConfiguration.matchingBehaviour.delegatedMatching) {
            const theme = themeSummary?.themes.find(t => t.themeId === themeConfiguration.id);

            return (
                <>
                    <Typography component="span" sx={{ fontWeight: 600 }}>{theme && displayPercentage(theme.percentage)}</Typography> {theme?.count}/{themeSummary?.openTextAnswerCount}
                </>
            );
        }

        if (loadingPreviewStats) {
            return <Box component="span" ml={1}><CircularProgress sx={{ verticalAlign: 'sub' }} size={20} /> Calculating</Box>;
        }

        if (previewStats) {
            return (
                <>
                    <Typography component="span" sx={{ fontWeight: 600 }}>{calculatePercentage(previewStats.combinedMatches, previewStats.total)}</Typography> {previewStats.combinedMatches}/{previewStats.total}
                </>
            );
        }

        return (
            <>
                <Typography component="span" sx={{ fontWeight: 600 }}>-</Typography>
            </>
        );
    }

    return (<>
        {!previewStats && !themeConfiguration.matchingBehaviour.delegatedMatching && <Skeleton variant="rectangular" width="100%" height="100%" />}
        <Box>
            <NameEditor displayName={displayName} setDisplayName={setDisplayName} />
            <Divider sx={{ mt: 3, mb: 3 }} />
            {themeConfiguration.matchingBehaviour.delegatedMatching && <Typography>Click on a subtheme on the left to configure it.</Typography>}
            {previewStats && !themeConfiguration.matchingBehaviour.delegatedMatching &&
                <>
                <Typography fontWeight="500">Theme tuning
                    <HelpIcon helpUrl='https://docs.savanta.com/internal/Content/AllVue/The_Settings_page.html' />
                </Typography>
                <Typography display='block' mb={1} variant='info'>
                    You can make adjustments to the theme, start by using the Theme precision tool then add any keywords. Adjustments work independently, giving you full control. Once finished tuning you must apply your changes.
                </Typography>
                <Typography display='block' mb={1}>
                    Theme precision<Typography variant='info'> - responses are sorted by AI from most precise to most general, adjust the slider to include more or less responses</Typography>
                </Typography>
                <Typography>
                    Keywords<Typography variant='info'> - add or remove specific words to include those responses</Typography>
                </Typography>

                    <PrecisionEditor
                        total={themeSensitivity?.totalTexts ?? 0}
                        sensitivity={sensitivity}
                        setSensitivity={setSensitivity}
                        themes={themes}
                        minValue={minValue}
                        maxValue={maxValue}
                        handleSensitivityChange={handleSensitivityChange}
                    />


                    <Divider sx={{ mt: 3, mb: 3 }} />

                    <KeywordsEditor
                        total={previewStats.total}
                        patternMatches={previewStats.keywordMatches}
                        items={keywords}
                        onItemsChange={(p) => handleMatchPatternChanged(p)}
                    />
                </>
            }
            <Box boxShadow='0 4px 8px 1px rgba(0,0,0,0.40)' borderRadius={2} p='0.5rem 1rem' mt={3} sx={{ position: 'sticky', bottom: 16, width: '100%', backgroundColor: '#f8f9f9', display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 2, zIndex: 1 }}>
                <Typography flexGrow={1}>
                    Theme percentage {themePercentage()}
                </Typography>
                <Button disabled={!changesToSave} variant="text" onClick={handleReset}>
                    Cancel
                </Button>
                <Button disabled={!changesToSave} variant="contained" color="primary" onClick={handleSave}>
                    Apply
                </Button>
            </Box>

        </Box>
    </>);
};

export default ThemeEditor;
