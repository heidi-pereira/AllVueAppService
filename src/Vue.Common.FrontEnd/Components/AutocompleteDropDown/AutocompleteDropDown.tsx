import React from "react";
import TextField from "@mui/material/TextField";
import Autocomplete from "@mui/material/Autocomplete";
import CircularProgress from "@mui/material/CircularProgress";
import { SxProps, Theme } from "@mui/material/styles";
import Box from "@mui/material/Box";

export interface IAutocompleteItem {
  value: string;
  label: string;
}

export interface IAutocompleteDropDownProps {
  id: string;
  label?: string;
  placeholder?: string;
  value: string | null;
  onChange: (value: string | null) => void;
  items: IAutocompleteItem[];
  loading?: boolean;
  className?: string;
  sx?: SxProps<Theme>;
  freeSolo?: boolean;
  disabled?: boolean;
  size?: 'small' | 'medium';
}

const AutocompleteDropDown = (props: IAutocompleteDropDownProps) => {
  const {
    id,
    label,
    placeholder,
    value,
    onChange,
    items,
    loading = false,
    className,
    sx,
    freeSolo = false,
    disabled = false,
    size = 'small',
  } = props;

  // Find the selected item based on the value
  const selectedItem = items.find((item) => item.value === value) || null;

  const handleChange = (_: React.SyntheticEvent, newValue: IAutocompleteItem | string | null) => {
    if (!newValue) {
      onChange(null);
      return;
    }
    
    if (typeof newValue === 'string') {
      // If freeSolo is true and the user entered a custom value
      onChange(newValue);
    } else {
      // If the user selected an option from the dropdown
      onChange((newValue as IAutocompleteItem).value);
    }
  };

  return (
    <Autocomplete
      id={id}
      options={items}
      getOptionLabel={(option) => {
        if (typeof option === 'string') {
          return option;
        }
        return option.label || '';
      }}
      value={selectedItem}
      onChange={handleChange}
      loading={loading}
      className={className}
      sx={sx}
      disablePortal={false} 
      disabled={disabled}
      freeSolo={freeSolo}
      size="small"
      popupIcon={null} // Remove dropdown arrow to save space
      openOnFocus={true} // Open dropdown on input focus
      autoHighlight={true} // Better keyboard navigation
      blurOnSelect={true} // Close dropdown on selection
      renderInput={(params) => (
        <TextField
          {...params}
          label={label}
          placeholder={placeholder}
          variant="outlined"
          size="small"
        />
      )}
    />
  );
};

export default AutocompleteDropDown;