import React from 'react';
import { TextField } from "@mui/material";
import SearchIcon from '@mui/icons-material/Search';

interface SearchInputProps {
  value: string;
  onChange: (value: string) => void;
}

const SearchInput: React.FC<SearchInputProps> = ({ value, onChange }) => {
    return (
        <TextField
            fullWidth
            value={value}
            placeholder="Search"
            variant="standard"
            onChange={(e) => onChange(e.target.value.toLowerCase())}
            slotProps={{
                input: { 
                sx: { fontSize: 12 }, 
                disableUnderline: true,
                endAdornment: (
                    <SearchIcon sx={{ fontSize: 18 }} />
                )
                }
            }}
        />
    );
};

export default SearchInput;