import React from "react";
import {
  Popper,
  ClickAwayListener,
  Box,
  Paper,
  Typography
} from "@mui/material";

interface CustomDropdownProps {
    name?: string;
    text?: string;
    isDisabled?: boolean;
    isLoading?: boolean;
    hasError?: boolean;
    errorMessage?: string;
    children: React.ReactNode;
}

export interface CustomDropdownChildProps {
    onClose: () => void;
    isLoading?: boolean;
}

const CustomDropdown: React.FC<CustomDropdownProps> = ({name, text, children, isDisabled, isLoading, hasError, errorMessage}) => {
  const [open, setOpen] = React.useState(false);
  const [anchorWidth, setAnchorWidth] = React.useState(0);
  const anchorRef = React.useRef(null);

  const handleToggle = () => {
    if (!isDisabled) {
      setOpen((prev) => !prev);
      if (anchorRef.current) {
          setAnchorWidth((anchorRef.current as HTMLDivElement).clientWidth);
      }
    }
  };
  const handleClose = () => {
    setOpen(false);
  };

  const renderChildren = () => {
    const props: CustomDropdownChildProps = {
      onClose: handleClose,
      isLoading: isLoading || false,
    };
    return React.Children.map(children, child => {
      if (React.isValidElement(child)) {
        return React.cloneElement(child, props);
      }
      return child;
    });
  };

  return (
    <Box>
      <Box
        ref={anchorRef}
        role="combobox"
        onClick={handleToggle}
        sx={{
          display: "flex",
          alignItems: "center",
          cursor: "pointer",
          border: "1px solid",
          borderColor: open ? "primary.main" : "grey.400",
          borderRadius: 1,
          color: "grey.900",
          padding: "7px",
          boxShadow: open ? 2 : 0,
          transition: "border-color 0.2s, box-shadow 0.2s",
          justifyContent: "space-between",
          textTransform: "none",
          pointerEvents: isDisabled ? "none" : "auto",
          opacity: isDisabled ? 0.5 : 1,
          backgroundColor: isDisabled ? "#f5f5f5" : "transparent",
        }}
        aria-disabled={isDisabled}
        aria-label={name}
      >
        <span>{text || "Select"}</span>
        <Box
          component="span"
          sx={{
            ml: 1,
            display: "flex",
            alignItems: "center",
            color: "grey.600",
            transform: open ? "rotate(180deg)" : "rotate(0deg)"
          }}
        >
          <svg width="24" height="24" viewBox="0 0 24 24" focusable="false" fill="currentColor">
        <path d="M7 10l5 5 5-5z"/>
          </svg>
        </Box>
      </Box>
      {hasError && errorMessage &&
      <Typography variant="caption" color="error" sx={{ mt: 0.5, minHeight: '1.25em' }}>
        {errorMessage}
      </Typography>}
      <Popper
        open={open}
        anchorEl={anchorRef.current}
        placement="bottom-start"
      >
        <ClickAwayListener onClickAway={handleClose}>
          <Paper
            elevation={5}
            sx={{
              p: 0,
              borderRadius: 2,
              minWidth: 400,
              width: anchorWidth
            }}
          >
            {renderChildren()}
          </Paper>
        </ClickAwayListener>
      </Popper>
    </Box>
  );
};

export default CustomDropdown;