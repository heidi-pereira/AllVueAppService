import React, { useState } from 'react';
import { IconButton, Menu, MenuItem } from '@mui/material';
import { MoreVert } from '@mui/icons-material';

interface ThemeMenuButtonProps {
    onDeleteTheme: () => void;
    display: boolean;
}

const ThemeMenuButtonComponent: React.FC<ThemeMenuButtonProps> = ({ onDeleteTheme, display }) => {
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleMenuClose = () => {
        setAnchorEl(null);
    };

    return (
        <>
            <IconButton sx={{visibility: display ? 'unset': 'hidden'}} onClick={handleMenuOpen}>
                <MoreVert />
            </IconButton>
            <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
                <MenuItem onClick={() => { handleMenuClose(); onDeleteTheme(); }}>
                    Delete Theme
                </MenuItem>
            </Menu>
        </>
    );
};

export default ThemeMenuButtonComponent;