import React from 'react';
import Button from '@mui/material/Button';
import ButtonGroup from '@mui/material/ButtonGroup';
import Tooltip from '@mui/material/Tooltip';
import ArrowDropDownRounded from '@mui/icons-material/ArrowDropDownRounded';
import ClickAwayListener from '@mui/material/ClickAwayListener';
import Grow from '@mui/material/Grow';
import Paper from '@mui/material/Paper';
import Popper from '@mui/material/Popper';
import MenuItem from '@mui/material/MenuItem';
import MenuList from '@mui/material/MenuList';
import { CacheProvider } from '@emotion/react';
import createCache from '@emotion/cache';

import './_splitbutton.less';
export interface Option {
    label: string;
    onItemClick: () => void;
    tooltip?: string;
    icon?: React.ReactNode;
}
export interface SplitButtonProps {
    options: Option[];
    variant: 'primary' | 'hollow';
    title: string;
    disabled: boolean;
    width?: string;
    size?: 'small' | 'medium';
    id?: string;
    icon?: React.ReactNode;
}

export const SplitButton = ({
    variant = 'primary',
    size = 'medium',
    disabled = false,
    id = 'split-button',
    title= '',
    width = '',
    icon = null,
    options,
    ...props
}: SplitButtonProps) => {
    const [open, setOpen] = React.useState(false);
    const anchorRef = React.useRef<HTMLDivElement>(null);
    const [selectedIndex, setSelectedIndex] = React.useState(0);
    const mode = `button--${variant}`;
    const cache = createCache({
        key: 'css',
        prepend: true,
    });

    let style = {};
    if (width !== '') {
        style = { width: width };
    }

    const handleClick = () => {
        options[0].onItemClick();
    };

    const handleMenuItemClick = (
        event: React.MouseEvent<HTMLLIElement, MouseEvent>,
        index: number,
    ) => {
        setSelectedIndex(index);
        setOpen(false);
        options[index].onItemClick();
    };

    const handleToggle = () => {
        setOpen((prevOpen) => !prevOpen);
    };

    const handleClose = (event: Event) => {
        if (
            anchorRef.current &&
            anchorRef.current.contains(event.target as HTMLElement)
        ) {
            return;
        }

        setOpen(false);
    };

    return (
        <CacheProvider value={cache}>
            <ButtonGroup
                className="split-button-group"
                variant="contained"
                ref={anchorRef}
                disabled={disabled}
            >
                <Button
                    id={id}
                    size={size}
                    className={['split-button', `split-button--${size}`, mode].join(' ')}
                    onClick={handleClick}
                    disabled={disabled}
                    style={style}
                    startIcon={icon}
                >
                    {title}
                    
                </Button>
                {options.length > 1 &&
                    <Button
                        size={size}
                        className={[`split-button-icon`, mode].join(' ')}
                        onClick={handleToggle}
                    >
                        <ArrowDropDownRounded />
                    </Button>
                }
                
            </ButtonGroup>
            <Popper
                sx={{ zIndex: 1 }}
                open={open}
                anchorEl={anchorRef.current}
                role={undefined}
                transition
                disablePortal
            >
                {({ TransitionProps, placement }) => (
                    <Grow
                        {...TransitionProps}
                        style={{
                            transformOrigin:
                                placement === 'bottom' ? 'center top' : 'center bottom',
                        }}
                    >
                        <Paper>
                            <ClickAwayListener onClickAway={handleClose}>
                                <MenuList id="split-button-menu" autoFocusItem>
                                    {options.map((option, index) => (
                                        <Tooltip key={option.label} title={option.tooltip} arrow>
                                            <MenuItem
                                                key={option.label}
                                                selected={index === selectedIndex}
                                                onClick={(event) => handleMenuItemClick(event, index)}
                                            >
                                                {option.icon && <span style={{ marginRight: '8px' }}>{option.icon}</span>}
                                                {option.label}
                                            </MenuItem>
                                        </Tooltip>
                                    ))}
                                </MenuList>
                            </ClickAwayListener>
                        </Paper>
                    </Grow>
                )}
            </Popper>
        </CacheProvider>
    );
};