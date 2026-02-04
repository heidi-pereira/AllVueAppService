import { Box, Typography, Chip, Grid, TextField, IconButton, InputAdornment } from '@mui/material';
import { useState, useMemo, useEffect, useCallback } from 'react';
import SearchIcon from '@mui/icons-material/Search';
import ClearIcon from '@mui/icons-material/Clear';
import RecordVoiceOverOutlinedIcon from '@mui/icons-material/RecordVoiceOverOutlined';
import GridViewIcon from '@mui/icons-material/GridView';
import CodeOffIcon from '@mui/icons-material/CodeOff';
import { OpenEndQuestionSummaryResponse } from '@model/Model';
import HeaderBox from '../../Template/HeaderBox';
import ThemeIconNameBox from '../../Template/ThemeIconNameBox';
import * as Utils from '@/utils';
import useDebouncedCallback from '@/hooks/useDebouncedCallback';
import Subthemes from './Subthemes';
import MergeThemes from './MergeThemes';
import { themeAsRoot } from '@/utils';

interface ResponseDetailsProps {
    themeSummary: OpenEndQuestionSummaryResponse;
    selectedThemes: string[];
    showUncoded: boolean;
    itemsToShow: number;
    showEndMessage: boolean;
    resetItemsToShow: () => void;
    setFilteredCount: (filteredCount: number) => void;
}

const ResponseDetails: React.FC<ResponseDetailsProps> = ({ themeSummary, selectedThemes, showUncoded, itemsToShow, showEndMessage, resetItemsToShow, setFilteredCount }) => {
    const [searchQuery, setSearchQuery] = useState<string>('');
    const [debouncedSearchQuery, setDebouncedSearchQuery] = useState<string>('');

    const debouncedSetSearchQuery = useDebouncedCallback((query: string) => setDebouncedSearchQuery(query), 300)

    const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const query = event.target.value;
        setSearchQuery(query);
        debouncedSetSearchQuery(query);
    };

    const handleClearSearch = () => {
        setSearchQuery('');
        setDebouncedSearchQuery('');
    };

    const getThemeText = useCallback((themeIndex: number): string => {
        if (themeIndex in themeSummary.themes) {
            return themeSummary.themes.find(t => t.themeIndex === themeIndex)!.themeText;
        } else {
            console.warn(`Theme index ${themeIndex} does not exist in themeSummary.themes`);
            return 'Unknown Theme';
        }
    }, [themeSummary.themes]);

    const highlightText = (text: string, highlight: string) => {
        if (!highlight.trim()) {
            return text;
        }
        const regex = new RegExp(`(${highlight})`, 'gi');
        return text.split(regex).map((part, index) =>
            regex.test(part) ? <span key={index} style={{ backgroundColor: 'yellow' }}>{part}</span> : part
        );
    };

    const filteredResponses = useMemo(() => {
        return themeSummary.textThemes.filter(response =>
            response.text.toLowerCase().includes(debouncedSearchQuery.toLowerCase()) &&
            ((selectedThemes.length === 0 && response.themes.length > 0 && !showUncoded) || response.themes.some(themeIndex => selectedThemes.includes(getThemeText(themeIndex))) || (showUncoded && response.themes.length === 0))
        );
    }, [debouncedSearchQuery, themeSummary.textThemes, selectedThemes, getThemeText, showUncoded]);

    const findThemeByText = (themeText: string) => {
        return themeSummary.themes.find(theme => theme.themeText === themeText);
    }

    const filteredCount = filteredResponses.length;
    const totalCount = themeSummary.totalCount;
    const singleSelectedTheme = selectedThemes?.length === 1 ? findThemeByText(selectedThemes[0]) : undefined;
    const pairSelectedThemes = selectedThemes?.length === 2 ? selectedThemes.map(findThemeByText) : undefined;
    const themesToDisplay = singleSelectedTheme
            ? themeSummary.themes.filter(t => t.parentId === singleSelectedTheme.themeId || t.themeId === singleSelectedTheme.themeId)
        : themeSummary.themes.filter(t => t.parentId === null);
    const themesToDisplayLookup = useMemo(
        () => themesToDisplay.map(t => ({ themeIndex: t.themeIndex, themeText: t.themeText, parentId: t.parentId })),
        [themesToDisplay]
    );

    setFilteredCount(filteredCount);

    useEffect(() => {
        const scrollableGrid = document.getElementById('scrollableGrid');
        if (scrollableGrid) {
            scrollableGrid.scrollTop = 0;
        }
        resetItemsToShow(); // Reset items to show when filters change
    }, [selectedThemes, debouncedSearchQuery, showUncoded]);


    const titleWithIcon = (title: string, key: React.Key, icon: JSX.Element) => {
        return (
            <ThemeIconNameBox key={key}>
                {icon}
                <Typography variant="h5" fontWeight="600" noWrap sx={{ mr: 1 }}>
                    {title}
                </Typography>
            </ThemeIconNameBox>
        )
    }

    const responseTitle = () => {
        if (selectedThemes.length > 0) {
            return selectedThemes.map((theme, index) => {
                return titleWithIcon(theme, index, <GridViewIcon sx={{ color: '#31B10A' }} />);
            });
        }

        if (showUncoded) {
            return titleWithIcon("Uncoded", "uncoded", <CodeOffIcon sx={{ color: '#999797' }} />);
        }
        
        return titleWithIcon("All coded responses", "all-coded-responses", <RecordVoiceOverOutlinedIcon sx={{ color: '#ED2283' }} />);
    }

    const getThemeChip = (label: string, isSubtheme: boolean) => {
        return isSubtheme
        ? <Chip label={label} variant='outlined' size="small" className="small" color='primary' />
        : <Chip label={label} size="small" className="small" color='secondary' />
    }

    return (
        <>
            <Box>
                <HeaderBox>
                    {responseTitle()}
                    <Typography variant="h5" component="span" fontWeight="500">
                        {filteredCount}/
                    </Typography>
                    <Typography variant="h5" component="span" color="#686C6F" fontWeight="500">
                        {totalCount} ({Utils.calculatePercentage(filteredCount, totalCount)})
                    </Typography>
                </HeaderBox>
            </Box>
            {singleSelectedTheme && <Subthemes theme={themeAsRoot(themeSummary.themes, singleSelectedTheme)} />}
            {pairSelectedThemes && pairSelectedThemes.every(theme => theme !== undefined) && <MergeThemes themes={pairSelectedThemes} />}
            <Box sx={{ pl: 3, pr: 3, minHeight: 0, flex: 1, display: 'flex', flexDirection: 'column' }}>
                <TextField
                    label={!searchQuery && "Search"}
                    variant="standard"
                    fullWidth
                    margin="normal"
                    value={searchQuery}
                    onChange={handleSearchChange}
                    InputProps={{
                        endAdornment: (
                            <InputAdornment position="end">
                                {searchQuery && (
                                    <IconButton onClick={handleClearSearch}>
                                        <ClearIcon />
                                    </IconButton>
                                )}
                                <IconButton>
                                    <SearchIcon />
                                </IconButton>
                            </InputAdornment>
                        ),
                    }}
                    slotProps={{ inputLabel: { shrink: false, style: { color: searchQuery ? '' : '#6E7881' } } } }
                    sx={{ mt: 0,
                        '& .MuiInput-underline:before, & .MuiInput-underline:hover:not(.Mui-disabled):before, & .MuiInput-underline:after': {
                            borderBottom: 'none',
                        },
                    }} />
                <Box sx={{ flex: 1, overflow: 'auto', mt: 1 }}>
                    {filteredResponses.slice(0, itemsToShow).map((response, index) => {
                        const themes = themesToDisplayLookup.filter(t => response.themes.includes(t.themeIndex));

                        const sortedThemes = themes.sort((a, b) => {
                            return Number(selectedThemes.includes(b.themeText)) - Number(selectedThemes.includes(a.themeText));
                        });

                        return (
                            <Box key={index} mt={1} mb={3}>
                                <Typography variant="body2" fontWeight={showUncoded ? "400" : "500"} mb={0.5} className="quote">
                                    {highlightText(response.text, debouncedSearchQuery)}
                                </Typography>
                                <Grid container spacing={1}>
                                    {sortedThemes.map((theme, themeIndex) => (
                                        <Grid item key={themeIndex}>
                                        {getThemeChip(theme.themeText, theme.parentId !== null)}
                                        </Grid>
                                    ))}
                                </Grid>
                            </Box>
                        )
                    })}
                    {showEndMessage && <p style={{ textAlign: 'center' }}><b>End of responses</b></p>}
                </Box>
            </Box>
        </>
    );
};

export default ResponseDetails;
