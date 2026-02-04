import React, { useState } from 'react';
import { Box, Chip, TextField, IconButton, InputAdornment, Typography } from '@mui/material';
import { East, Close } from '@mui/icons-material';
import HelpIcon from '../HelpIcon';
import * as Utils from '@/utils';

interface StringArrayPillsProps {
    patternMatches: number;
    total: number;
    items: string[];
    onItemsChange: (items: string[]) => void;
}

const KeywordsEditor: React.FC<StringArrayPillsProps> = ({ items, onItemsChange, patternMatches, total }) => {
    const [newItem, setNewItem] = useState<string>('');

    const handleAddItem = () => {
        if (newItem.trim() !== '') {
            const updatedItems = [...items, newItem.trim()];
            onItemsChange(updatedItems);
            setNewItem('');
        }
    };

    const handleRemoveItem = (itemToRemove: string) => {
        const updatedItems = items.filter(item => item !== itemToRemove);
        onItemsChange(updatedItems);
    };

    return (
        <><Box display='flex' justifyContent='space-between' mb={2} mt={3}>
            <Typography fontWeight="500">
                Keywords<HelpIcon helpText='Keywords will help better identify and group relevant responses, making your themes more accurate and meaningful.' />
            </Typography>
            <Typography variant="h5" fontWeight="500">
                Matched {Utils.calculatePercentage(patternMatches, total)}
                <Typography variant="info"> {patternMatches}/{total}</Typography>
            </Typography>
        </Box><Box>
                <Box mb={1} sx={{ display: 'flex', alignItems: 'center' }}>
                    <TextField
                        value={newItem}
                        onChange={(e) => setNewItem(e.target.value)}
                        label="E.g. information"
                        variant="outlined"
                        size='small'
                        sx={{ width: '500px', mr: 2 }}
                        InputProps={{
                            endAdornment: (
                                <InputAdornment position="end">
                                    <IconButton color="primary" onClick={handleAddItem}>
                                        <East />
                                    </IconButton>
                                </InputAdornment>
                            ),
                        }} />
                    <Typography variant='info'>
                        (* = any characters following)
                    </Typography>
                </Box>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
                    {items.map((item, index) => (
                        <Chip
                            sx={{ backgroundColor: '#fff', color: '#1376cd', border: '1px solid #1376cd' }}
                            key={index}
                            label={item}
                            onDelete={() => handleRemoveItem(item)}
                            deleteIcon={<Close sx={{ width: '12px', color: 'unset' }} />} />
                    ))}
                </Box>
            </Box>
            <Typography variant="info">
                These keywords will help better identify and group relevant responses, making your themes more accurate and meaningful.
            </Typography>
        </>
    );
};

export default KeywordsEditor;
