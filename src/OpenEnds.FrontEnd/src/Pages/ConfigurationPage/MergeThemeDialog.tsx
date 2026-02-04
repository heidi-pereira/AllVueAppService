import React, { useEffect, useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, FormControl, InputLabel, Select, MenuItem, Typography } from '@mui/material';
import { OpenEndTheme } from '@model/Model';
import mixpanel from 'mixpanel-browser';
import { useParams } from 'react-router-dom';

interface MergeThemeDialogProps {
    open: boolean;
    onClose: () => void;
    themes: OpenEndTheme[];
    onMerge: (targetThemeId: number) => void;
    themeId?: number,
    themeName: string,
}

const MergeThemeDialog: React.FC<MergeThemeDialogProps> = ({ open, onClose, themes, onMerge, themeId, themeName }) => {
    const { questionId, surveyId } = useParams();
    const [targetThemeId, setTargetThemeId] = useState<number | ''>('');

    const handleMerge = () => {
        if (targetThemeId !== '') {
            onMerge(targetThemeId);
            onClose();
        }
    };

    useEffect(() => {
        if (open) {
            mixpanel.track('Text Analysis Theme Merge Open', { "Survey": surveyId, "Question": questionId, "Theme": themeId, "ThemeName": themeName });
        }
    }, [open, surveyId, questionId, themeId, themeName])

    return (
        <Dialog open={open} onClose={onClose}>
            <DialogTitle>Merge Theme</DialogTitle>
            <DialogContent>
                <Typography variant="body2" gutterBottom>This will add the current theme's matching into the selected theme and delete the current theme.</Typography>
                <FormControl fullWidth>
                    <InputLabel id="merge-theme-select-label">Select Theme to Merge Into</InputLabel>
                    <Select
                        labelId="merge-theme-select-label"
                        label="Select Theme to Merge Into"
                        value={targetThemeId}
                        onChange={(e) => setTargetThemeId(e.target.value as number)}
                    >
                        {themes.map((theme) => (
                            <MenuItem key={theme.themeId} value={theme.themeId}>
                                {theme.themeText}
                            </MenuItem>
                        ))}
                    </Select>
                </FormControl>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose}>Cancel</Button>
                <Button onClick={handleMerge} color="primary">Merge</Button>
            </DialogActions>
        </Dialog>
    );
};

export default MergeThemeDialog;
