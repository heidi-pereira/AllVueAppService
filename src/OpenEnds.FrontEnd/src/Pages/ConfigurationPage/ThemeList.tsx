import React from 'react';
import { Box, Typography, List, ListItem, ListItemText, ListItemButton, Button } from '@mui/material';
import { OpenEndTheme, RootTheme } from '@model/Model';
import GridViewIcon from '@mui/icons-material/GridView';
import ArrowRightIcon from '@mui/icons-material/ArrowRightAltOutlined';
import AddIcon from '@mui/icons-material/Add';
import HeaderBox from '../../Template/HeaderBox';
import ThemeIconNameBox from '../../Template/ThemeIconNameBox';
import ThemeMenu from './ThemeMenu';
import * as Utils from '@/utils';

interface ThemeListProps {
    themes: RootTheme[];
    selectedTheme?: OpenEndTheme;
    onThemeSelect: (theme: OpenEndTheme) => void;
    onCreateDialogOpen: () => void;
    onDeleteTheme: () => void;
}

const ThemeList: React.FC<ThemeListProps> = ({
    themes,
    selectedTheme,
    onThemeSelect,
    onCreateDialogOpen,
    onDeleteTheme,
}) => {

    const themeListItem = (theme: OpenEndTheme, index: number) => {
        const isSubtheme = theme.parentId !== null;
        const isSelectedTheme = selectedTheme?.themeText === theme.themeText;

        const themeIcon = isSubtheme
            ? <ArrowRightIcon style={{ fontSize: "medium" }} />
            : <GridViewIcon sx={{ color: '#31B10A' }} />;
        const leftPadding = isSubtheme ? 5 : 3;

        return (
            <ListItem key={index} disablePadding disableGutters sx={{ pr: 0, pl: leftPadding }}>
                <ListItemButton
                    sx={{ pr: 0, pl: 1, borderRadius: 1 }}
                    disableGutters
                    disableRipple={isSelectedTheme}
                    onClick={() => onThemeSelect(theme)}
                    selected={isSelectedTheme}
                >
                    <ListItemText sx={{ pr: 1 }} primary={
                        <Box display='flex' justifyContent='space-between' gap={1}>
                            <ThemeIconNameBox>{themeIcon}{theme.themeText}</ThemeIconNameBox>
                            <Typography component="span">{Utils.displayPercentage(theme.percentage)}</Typography>
                        </Box>
                    } />
                    <ThemeMenu
                        display={isSelectedTheme}
                        onDeleteTheme={onDeleteTheme}
                    />
                </ListItemButton>

            </ListItem>
        )
    }

    const flattenedThemes = themes.flatMap(rootTheme => [rootTheme, ...rootTheme.subThemes])

    return (
        <>
            <Box pt={3} pr={3} pl={3} display='flex' justifyContent='space-between' alignItems="baseline">
                <HeaderBox>
                    <ThemeIconNameBox>
                        <GridViewIcon sx={{ color: '#31B10A' }} />
                        <Typography>
                            Themes
                            <Typography component="span" sx={{ fontWeight: 600, ml: 1 }}>{themes.length}</Typography>
                        </Typography>
                    </ThemeIconNameBox>
                </HeaderBox>
                <Box>
                    <Button variant="text"
                        color="primary"
                        sx={{ alignItems: 'center' }}
                        onClick={onCreateDialogOpen}
                        startIcon={<AddIcon style={{ fontSize: "medium" }} />}
                    >
                        <Typography variant="h6" sx={{ fontWeight: '400' }}>Create new</Typography>
                    </Button>
                </Box>
            </Box>
            <List component="nav">
                {flattenedThemes.map(themeListItem)}
            </List>
        </>
    );
}

export default ThemeList;