import { memo, useEffect, useRef, useState } from 'react';
import { Box, Button, Slider, Typography } from '@mui/material';
import HelpIcon from '../HelpIcon';
import * as Utils from '@/utils';
import AddIcon from '@mui/icons-material/Add';
import RemoveIcon from '@mui/icons-material/Remove';
import { ThemeSensitivityConfigurationItem } from '../../Model/Model';

interface PrecisionEditorProps {
    total: number;
    sensitivity: number;
    setSensitivity: (value: number) => void;
    themes: ThemeSensitivityConfigurationItem[];
    minValue: number;
    maxValue: number;
    handleSensitivityChange: (_event: Event, newValue: number | number[]) => void;
}

const PrecisionEditor = ({
    total,
    sensitivity,
    setSensitivity,
    themes,
    minValue,
    maxValue,
    handleSensitivityChange
}: PrecisionEditorProps) => {

    const scrollContainer = useRef<HTMLDivElement>(null);
    const [includedCount, setIncludedCount] = useState<number>(0);
    const isFirstRender = useRef(true);
    
    useEffect(() => {
        if (scrollContainer.current && themes.length) {
            const nearestIndex = themes.findIndex(entry => entry.distanceScore > sensitivity);

            if (nearestIndex !== -1) {
                const listItem = scrollContainer.current.children[nearestIndex] as HTMLElement;
                const itemTop = listItem.offsetTop - (scrollContainer.current!.clientHeight / 2);
                const scrollBehaviour = isFirstRender.current ? 'instant' : 'smooth';
                scrollContainer!.current?.scrollTo({ behavior: scrollBehaviour, top: itemTop })
                setIncludedCount(nearestIndex)

            }
            else {
                setIncludedCount(themes.length)
            }
        }
        isFirstRender.current = false;
    }, [sensitivity, themes]);

    return (
        <>
            <Box display='flex' justifyContent='space-between' mb={2} mt={3}>
                <Typography fontWeight="500">
                    Theme precision<HelpIcon helpText='Each response is given a score by an AI model based on how well it fits the theme.<br/>You can adjust the theme precision to decide which responses to include.<br/>The higher the score, the better the match. Keep in mind that the AI model isn’t flawless, and some mismatches might slip through.<br/>To catch these, you can lower the threshold and use keyword matching for the rest. Responses are sorted from best to least fitting.'/>
                </Typography>
                <Typography fontWeight="500">
                    Matched {Utils.calculatePercentage(includedCount, total)}
                    <Typography variant="info"> {includedCount}/{total}</Typography>
                </Typography>
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 3, mt: 2, mb: 2, mr: 1, ml: 1 }}>
                <Box>
                    <Button color="primary" sx={{ alignItems: 'center' }} onClick={() => {
                        const currentIndex = themes.findLastIndex(entry => entry.distanceScore <= sensitivity);
                        const newSensitivity = (currentIndex > 0) ? themes[currentIndex - 1].distanceScore : minValue;
                        setSensitivity(newSensitivity)
                    }} startIcon={<RemoveIcon style={{ fontSize: "medium" }} />}>
                        <Typography variant="h6" sx={{ fontWeight: '400' }}>Precise</Typography>
                    </Button>
                </Box>
                <Slider
                    value={sensitivity}
                    min={minValue}
                    max={maxValue}
                    step={0.001}
                    onChange={handleSensitivityChange}
                    aria-labelledby="sensitivity-slider"
                />
                <Box>
                    <Button color="primary" sx={{ alignItems: 'center' }} onClick={() => {
                        const currentIndex = themes.findIndex(entry => entry.distanceScore > sensitivity);
                        const newSensitivity = (currentIndex >= 0) ? themes[currentIndex].distanceScore : maxValue;
                        setSensitivity(newSensitivity);
                    }} startIcon={<AddIcon style={{ fontSize: "medium" }} />}>
                        <Typography variant="h6" sx={{ fontWeight: '400' }}>Broad</Typography>
                    </Button>
                </Box>
            </Box>

            <Box sx={{ position: 'relative' }}>
                <Box ref={scrollContainer} sx={{ position: 'relative', maxHeight: 400, overflow: 'hidden', mt: 2, '&:hover': { overflowY: 'scroll' } }}>
                    {themes.map((entry, index) => {
                        const included = entry.distanceScore <= sensitivity
                        const sxProps = included
                            ? { backgroundColor: 'rgba(25, 118, 210, 0.25)', cursor: 'pointer' }
                            : { backgroundColor: '#fff', cursor: 'pointer' };

                        return (
                            <Box key={index} sx={sxProps} onClick={() => setSensitivity(entry.distanceScore)}>
                                <Box p={1} sx={{ display: 'flex', alignItems: 'center' }}>
                                    <Typography paddingLeft={1} sx={{ fontWeight: included ? '600' : undefined }}>{entry.text}</Typography>
                                </Box>
                            </Box>
                        )
                    })
                    }
                </Box>
                <Typography sx={{ position: 'absolute', top: 5, right: 16, fontSize: '0.8rem', backgroundColor: '#C5DDF4' }}>Included</Typography>
                <Typography sx={{ position: 'absolute', bottom: 5, right: 16, fontSize: '0.8rem' }}>Not included</Typography>
            </Box>
        </>
    );
};

// Memoize the component to prevent unnecessary re-renders as the box of texts can be quite large
export default memo(PrecisionEditor);
