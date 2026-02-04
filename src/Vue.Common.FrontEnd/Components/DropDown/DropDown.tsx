import React from "react";
import InputLabel from '@mui/material/InputLabel';
import Select, { SelectChangeEvent } from '@mui/material/Select';
import MenuItem from '@mui/material/MenuItem';

export interface IDropDownProps {
    id: string;
    label?: string;
    emptyValueLabel?: string;
    selectValue: string;
    handleChange: (value: string) => void;
    items: Array<{ value: string; label: string }>;
    loading: boolean;
    className?: string;
    displayAll?: boolean;
    sx?: SxProps<Theme>;
}

const DropDown = (props: IDropDownProps) => {
    return (
        <>
            {props.label &&
                <InputLabel id={props.id} shrink={false}>{props.label}</InputLabel>
            }
            <Select
                labelId={props.id}
                value={props.selectValue}
                onChange={(event: SelectChangeEvent) => props.handleChange(event.target.value)}
                disabled={props.loading}
                className={props.className}
                sx={props.sx}
                label={props.label}
            >
                {(props.displayAll == undefined || props.displayAll) &&
                <MenuItem value="">
                    <em>{props.emptyValueLabel ??"All"}</em>
                    </MenuItem>
                }
                {props.items.map((item: { value: string; label: string }) => (
                    <MenuItem key={item.value} value={item.value}>
                        {item.label}
                    </MenuItem>
                ))}
            </Select>
        </>
    );
}

export default DropDown