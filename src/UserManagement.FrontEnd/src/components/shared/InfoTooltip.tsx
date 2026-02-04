import React from 'react';
import { IconButton, Tooltip } from "@mui/material";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";

interface InfoTooltipProps {
  children: string;
}

const InfoTooltip: React.FC<InfoTooltipProps> = ({ children }) => {
    return (
<Tooltip title={ children } arrow>
    <IconButton size="small" sx={{ ml: 1 }}>
        <InfoOutlinedIcon fontSize="small" />
    </IconButton>
</Tooltip>);
};

export default InfoTooltip;