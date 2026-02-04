import { useState, ReactElement } from "react";
import { ClickAwayListener, Box, IconButton, Snackbar, Tooltip } from "@mui/material";
import ContentCopyIconRounded from '@mui/icons-material/ContentCopyRounded';

interface ICopyToClipboardTooltipProps {
    children: React.ReactNode;
    toolTipContent: ReactElement;
    toolTipTextToCopy: string;
}

function CopyToClipboardTooltip(props: ICopyToClipboardTooltipProps) {
    const [tooltipOpen, setTooltipOpen] = useState(false);
    const [tooltipTextCopied, setTooltipTextCopied] = useState(false);

    const handleTooltipClose = () => {
        setTooltipOpen(false);
    };

    const toggleTooltip = (event: React.MouseEvent) => {
        event?.stopPropagation();
        setTooltipOpen(prev => !prev);
    };

    const handleTooltipTextCopy = async (event: React.MouseEvent) => {
        event.stopPropagation();
        await navigator.clipboard.writeText(props.toolTipTextToCopy);
        setTooltipTextCopied(true);
    };

    const handleSnackbarClose = () => setTooltipTextCopied(false);

    return (
        <>
            <ClickAwayListener onClickAway={handleTooltipClose}>
                <Tooltip
                    onClose={handleTooltipClose}
                    open={tooltipOpen}
                    disableFocusListener
                    disableHoverListener
                    disableTouchListener
                    title={
                        <Box sx={{ display: 'flex', alignItems: 'center' }} onClick={handleTooltipTextCopy}>
                            <span>{props.toolTipContent}</span>
                            <IconButton
                                size="small"
                                sx={{ ml: 1 }}
                                aria-label="Copy text"
                            >
                                <ContentCopyIconRounded fontSize="small" sx={{ color: "white" }} />
                            </IconButton>
                        </Box>
                    }
                    slotProps={{
                        popper: {
                            disablePortal: true,
                        },
                    }}
                >
                    <Box component='span' onClick={(event) => toggleTooltip(event)}>
                        {props.children}
                    </Box>
                </Tooltip>
            </ClickAwayListener>
            <Snackbar
                open={tooltipTextCopied}
                message="Copied to clipboard"
                autoHideDuration={1500}
                onClose={handleSnackbarClose}
            />
      </>
  );
}

export default CopyToClipboardTooltip;