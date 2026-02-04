import React, { useState } from 'react';
import { Box, ListSubheader, Menu, MenuItem, Typography } from '@mui/material';
import MaterialSymbol from '../../Template/MaterialSymbol';
import { ExportFormat } from '../../Model/Model';

interface ExportMenuButtonProps {
    startExport: (format: ExportFormat) => void;
    disabled: boolean;
}

const ExportMenuButtonComponent: React.FC<ExportMenuButtonProps> = ({ startExport, disabled }) => {
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleMenuClose = () => {
        setAnchorEl(null);
    };

    return (
        <>
            <Box
                display="flex"
                justifyContent="stretch"
                sx={{ cursor: "pointer", gap: 1 }}
                onClick={(e) => !disabled && handleMenuOpen(e)}
                aria-controls={anchorEl ? 'export-menu' : undefined}
                aria-haspopup="true"
                aria-expanded={Boolean(anchorEl)}            >
                <MaterialSymbol symbolName="file_export" size="medium" />
                <Typography variant="body2" className="actionText" gutterBottom>
                    Export
                </Typography>
            </Box>
            <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
                <ListSubheader>TABULAR</ListSubheader>
                <MenuItem onClick={() => { handleMenuClose(); startExport(ExportFormat.TabularXLSX); }}>
                    .xlsx
                </MenuItem>
                <MenuItem onClick={() => { handleMenuClose(); startExport(ExportFormat.TabularCSV); }}>
                    .csv
                </MenuItem>
                <ListSubheader>CODEBOOK</ListSubheader>
                <MenuItem onClick={() => { handleMenuClose(); startExport(ExportFormat.CodebookXLSX); }}>
                    .xlsx
                </MenuItem>
            </Menu>
        </>
    );
};

export default ExportMenuButtonComponent;