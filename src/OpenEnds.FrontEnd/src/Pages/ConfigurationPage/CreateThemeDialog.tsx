import React, { useState, useEffect } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, TextField, Button, Typography, Box, CircularProgress } from '@mui/material';
import * as OpenEndApi from '@model/OpenEndApi';
import { useThemeSummaryStore } from '@model/themeSummaryStore';
import mixpanel from 'mixpanel-browser';
import { GridView } from '@mui/icons-material';
import { PreviewMatchResponse } from '@/Model/Model';
import * as Utils from '@/utils';

interface CreateThemeDialogProps {
    open: boolean;
    onClose: () => void;
    surveyId: string;
    questionId: number;
}

const CreateThemeDialog: React.FC<CreateThemeDialogProps> = ({ open, onClose, surveyId, questionId }) => {
    const [themeName, setThemeNames] = useState<string>('');
    const [isCreating, setIsCreating] = useState<boolean>(false);
    const reloadThemeSummary = useThemeSummaryStore((state) => state.reloadThemeSummary);
    const [preview, setPreview] = useState<PreviewMatchResponse>();
    const [loadingPreview, setLoadingPreview] = useState<boolean>(false);

    useEffect(() => {
        if (open) {
            mixpanel.track('Text Analysis Theme New Open', { "Survey": surveyId, "Question": questionId });

            setThemeNames('');
        }
    }, [open, surveyId, questionId]);

    const handleCreateThemes = async () => {
        setIsCreating(true);
        try {
            mixpanel.track('Text Analysis Theme New Create', { "Survey": surveyId, "Question": questionId });

            await OpenEndApi.createThemeConfiguration(surveyId, questionId, themeName, null);
            await reloadThemeSummary(surveyId, questionId);
            onClose();
        } catch (error) {
            console.error('Error creating themes:', error);
        } finally {
            setIsCreating(false);
        }
    };

    useEffect(() => {
        setPreview(undefined);
        if (themeName) {
            const timer = setTimeout(async () => {
                setLoadingPreview(true);
                const response = await OpenEndApi.getNamePreview(surveyId, questionId, themeName);
                setPreview(response);
                setLoadingPreview(false);
            }, 1000);
            return () => clearTimeout(timer);
        }
    }, [themeName, surveyId, questionId]);

    return (
        <Dialog open={open} onClose={onClose}>
            <DialogTitle>Create Theme</DialogTitle>
            <DialogContent>
                <Typography mb={3}>
                    To create a new theme, simply name it, and Aila will analyse the survey responses to generate a theme for you.
                </Typography>
                <Typography fontSize='medium' gutterBottom><b>New theme</b></Typography>
                <Box display={'flex'} gap={2} alignItems={'center'}>
                    <GridView sx={{ color: '#31B10A' }} />
                    <TextField
                        autoFocus
                        type="text"
                        fullWidth
                        variant="outlined"
                        placeholder="Enter theme name"
                        value={themeName}
                        onChange={(e) => setThemeNames(e.target.value)}
                    />
                    <Box width={400}>
                        { loadingPreview && <Box sx={{display: 'flex', gap: 1, alignContent: 'center'}}><CircularProgress size={20} /> Calculating</Box> }
                        { preview && <Typography color="textSecondary"><b>Matched {Utils.calculatePercentage(preview.matches, preview.total)}</b> {preview.matches} / {preview.total}</Typography> }
                    </Box>
                </Box>
            </DialogContent>
            <DialogActions>
                <Button color='secondary' variant='contained' onClick={onClose} disabled={isCreating}>Cancel</Button>
                <Button variant='contained' onClick={handleCreateThemes} color="primary" disabled={isCreating || themeName.length === 0}>Create</Button>
            </DialogActions>
        </Dialog>
    );
};

export default CreateThemeDialog;
