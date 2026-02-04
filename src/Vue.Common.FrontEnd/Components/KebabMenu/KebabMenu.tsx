import React, { useState } from 'react';
import { IconButton, Menu, MenuItem } from '@mui/material';
import MoreVertIcon from '@mui/icons-material/MoreVert';

export interface MenuOption<T = unknown> {
    label: string;
    onClick: (rowData: T) => void;
}

interface KebabMenuProps<T> {
    options: MenuOption<T>[] | ((row: any) => MenuOption<T>[]);
    rowData: T;
}

export function KebabMenu<T>({ options, rowData }: KebabMenuProps<T>) {
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

    const handleOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleClose = () => {
        setAnchorEl(null);
    };

    const handleItemClick = (option: MenuOption<T>) => {
        option.onClick(rowData);
        handleClose();
    };
    const resolvedOptions = typeof options === 'function' ? options(rowData) : options;
    
    // Safety check to ensure we have a valid array
    if (!Array.isArray(resolvedOptions)) {
        return null;
    }

    return (
    <>
        <IconButton size="small" onClick={handleOpen}>
            <MoreVertIcon fontSize="small" />
        </IconButton>
        <Menu anchorEl={anchorEl} open={!!anchorEl} onClose={handleClose}>
                {resolvedOptions.map((option, index) => (
                <MenuItem key={index} onClick={() => handleItemClick(option)}>
                {option.label}
                </MenuItem>
            ))}
        </Menu>
    </>
    );
}