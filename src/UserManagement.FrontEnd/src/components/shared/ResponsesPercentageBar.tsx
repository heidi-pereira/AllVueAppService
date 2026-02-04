import React from 'react';
import { Box, Typography, LinearProgress } from '@mui/material';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';

interface ResponsesPercentageBarProps {
    responsesCount: number;
    totalCount: number;
    isLoading?: boolean;
    errorMessage?: string;
}

const ResponsesPercentageBar: React.FC<ResponsesPercentageBarProps> = ({
    responsesCount,
    totalCount,
    isLoading = false,
    errorMessage = "",
}) => {
    const percentage = totalCount > 0 ? (responsesCount / totalCount) * 100 : 0;
    const hasError = Boolean(errorMessage);

    const renderLabel = () => {
        if (isLoading) return 'Loading data...';
        if (hasError) return errorMessage;
        return `${responsesCount.toLocaleString()} / ${totalCount.toLocaleString()}`;
    };

    return (
        <>
            <Box display="flex" justifyContent="space-between" alignItems="center">
                <Typography variant="responsesPercentageBar">
                    {!hasError && <>Responses </>}
                    <strong>{renderLabel()}</strong>
                </Typography>
                {!isLoading && !hasError && (
                    <Typography variant="responsesPercentageBar">
                        <VisibilityOutlinedIcon fontSize="inherit" /> Visible: {percentage.toFixed(0)}%
                    </Typography>
                )}
            </Box>
            <LinearProgress variant="determinate" value={isLoading ? 0 : percentage} />
        </>
    );
};

export default ResponsesPercentageBar;