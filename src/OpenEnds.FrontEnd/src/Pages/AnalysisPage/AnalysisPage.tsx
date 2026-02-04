import { Box, Typography, Chip, Grid, Collapse, Skeleton } from '@mui/material';
import EmojiObjectsOutlinedIcon from '@mui/icons-material/EmojiObjectsOutlined';
import { useEffect, useState } from 'react';
import { useThemeSummaryStore } from '@model/themeSummaryStore';
import ResponseDetails from './ResponseDetails';
import RootBox from '../../Template/RootBox';
import HeaderBox from '../../Template/HeaderBox';
import mixpanel from 'mixpanel-browser';
import { useParams } from 'react-router-dom';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import CodeOffIcon from '@mui/icons-material/CodeOff';
import CodeIcon from '@mui/icons-material/Code';
import ThemeIconNameBox from '../../Template/ThemeIconNameBox';
import * as Utils from '@/utils';
import InfiniteScroll from 'react-infinite-scroll-component';
import * as OpenEndApi from '@model/OpenEndApi';
import { ExportFormat } from '@model/Model';
import ExportMenu from './ExportMenu';
import { largeIconSx } from '../../Theme/sxStyles';

const INITIAL_NUMBER_OF_ITEMS_TO_SHOW = 50;
const ITEMS_TO_LOAD_PER_BATCH = 10;

const SummaryComponent = () => {
    const { questionId, surveyId } = useParams();
    const themeSummary = useThemeSummaryStore((state) => state.themeSummary);

    useEffect(() => {
        mixpanel.track('Text Analysis Theme Summary Page Loaded', { "Survey": surveyId, "Question": questionId });
    }, [surveyId, questionId])

    const [selectedThemes, setSelectedThemes] = useState<string[]>([]);
    const [showSummary, setShowSummary] = useState<boolean>(true);
    const [showUncoded, setShowUncoded] = useState<boolean>(false);
    const [itemsToShow, setItemsToShow] = useState<number>(INITIAL_NUMBER_OF_ITEMS_TO_SHOW);
    const [filteredCount, setFilteredCount] = useState<number>(0);
    const [isExporting, setIsExporting] = useState<boolean>(false);

    const handleChipClick = (themeText: string) => {
        mixpanel.track('Text Analysis Theme Summary Filter By Category', { "Survey": surveyId, "Question": questionId });
        setShowUncoded(false);
        setSelectedThemes(prevSelectedThemes =>
            prevSelectedThemes.includes(themeText)
                ? prevSelectedThemes.filter(theme => theme !== themeText)
                : [...prevSelectedThemes, themeText]
        );
    };

    const resetItemsToShow = () => {
        setItemsToShow(INITIAL_NUMBER_OF_ITEMS_TO_SHOW);
    }

    const fetchMoreData = () => {
        setItemsToShow(prev => prev + ITEMS_TO_LOAD_PER_BATCH);
    };

    const handleToggleSummary = () => {
        setShowSummary((prev) => !prev);
    }

    const handleToggleUncoded = () => {
        setSelectedThemes([]);
        setShowUncoded((prev) => !prev);
    }

    useEffect(() => {
        setSelectedThemes([]);
    }, [themeSummary]);

    const uncodedIcon = () => {
        return showUncoded ? <CodeIcon sx={largeIconSx} /> : <CodeOffIcon sx={largeIconSx} />
    }

    const hasMore = itemsToShow < filteredCount;

    const exportSummary = async (format: ExportFormat) => {
        if (surveyId && questionId) {
            setIsExporting(true);

            try {
                const exportFormat = ExportFormat[format].toString();
                mixpanel.track('Text Analysis Export', { "Survey": surveyId, "Question": questionId, "Format": exportFormat });
                await OpenEndApi.getQuestionSummaryExport(surveyId, Number.parseInt(questionId), format)
            } catch (error) {
                console.error('Error exporting data:', error);
            } finally {
                setIsExporting(false);
            }
        }
    }

    const rootThemes = themeSummary ? Utils.themesAsHierarchy(themeSummary.themes) : [];

    return (
        <Box p={1} sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 260px)', overflow: 'auto' }} id="scrollableGrid">
            <InfiniteScroll
                dataLength={itemsToShow}
                next={fetchMoreData}
                hasMore={hasMore}
                loader={<h4>Loading...</h4>}
                scrollableTarget="scrollableGrid"
            >
                <RootBox p={2} borderRadius={0.5} mb={4} mt={3}>
                    {!themeSummary && <Skeleton variant="rectangular" sx={{ flex: 1, height: '200px' }} />}
                    {themeSummary &&
                        <Box display="flex" flexDirection="column" gap={2}>
                            <Box display="flex" justifyContent="space-between">
                                <HeaderBox>
                                    <ThemeIconNameBox>
                                        <EmojiObjectsOutlinedIcon sx={{ color: '#410FD8' }} />
                                        <Typography variant="h5" fontWeight="500" lineHeight="1.25rem">
                                            Summary
                                        </Typography>
                                    </ThemeIconNameBox>
                                </HeaderBox>
                                <Box display="flex" gap={5}>
                                    <Box display="flex" justifyContent="stretch" sx={{ cursor: "pointer" }} onClick={handleToggleUncoded}>
                                        {uncodedIcon()}
                                        <Typography variant="body2" className="actionText" gutterBottom width="2rem">
                                            {showUncoded ? "Coded" : "Uncoded"}
                                        </Typography>
                                    </Box>
                                    <Box display="flex" justifyContent="stretch" sx={{ cursor: "pointer" }} onClick={handleToggleSummary}>
                                        <ExpandLessIcon
                                            sx={{
                                                fontSize: 'large',
                                                color: 'primary.main',
                                                mr: 1,
                                                transition: 'transform 0.3s ease',
                                                transform: showSummary ? 'rotate(0deg)' : 'rotate(180deg)',
                                            }}
                                        />
                                        <Typography variant="body2" className="actionText" gutterBottom width="1rem">
                                            {showSummary ? "Hide" : "Show"}
                                        </Typography>
                                    </Box>
                                    <ExportMenu
                                        startExport={exportSummary}
                                        disabled={isExporting}
                                    />
                                </Box>
                            </Box>
                            <Collapse in={showSummary} timeout={300}>
                                <Typography variant="body2">
                                    {themeSummary?.summary}
                                </Typography>
                            </Collapse>
                            <Grid container spacing={1}>
                                {rootThemes?.map((theme, index) => (
                                    <Grid item key={index}>
                                        <Chip
                                            label={`${Utils.displayPercentage(theme.percentage)} ${theme.themeText}`}
                                            onClick={() => handleChipClick(theme.themeText)}
                                            color={selectedThemes.includes(theme.themeText) ? 'primary' : 'secondary'}
                                        />
                                    </Grid>
                                ))}

                            </Grid>
                        </Box>
                    }
                </RootBox>
                <RootBox p={2} borderRadius={2} sx={{ minHeight: 0, flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
                    {!themeSummary && <Skeleton variant="rectangular" sx={{ flex: 1 }} />}
                    {themeSummary && <ResponseDetails
                        themeSummary={themeSummary}
                        selectedThemes={selectedThemes}
                        showUncoded={showUncoded}
                        itemsToShow={itemsToShow}
                        showEndMessage={!hasMore}
                        resetItemsToShow={resetItemsToShow}
                        setFilteredCount={setFilteredCount}
                    />
                    }
                </RootBox>
            </InfiniteScroll>
        </Box>
    );
};

export default SummaryComponent;