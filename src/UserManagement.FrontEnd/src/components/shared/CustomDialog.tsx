import React from 'react';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import CloseIcon from '@mui/icons-material/Close';
import './CustomDialog.module.scss';

interface CustomDialogProps {
    open: boolean;
    title: string;
    question?: string;
    description?: string;
    cancelButtonText?: string;
    cancelButtonColour?: string;
    confirmButtonText?: string;
    confirmButtonColour?: string;
    onCancel: () => void;
    onConfirm: () => void;
}

const CustomDialog: React.FC<CustomDialogProps> = ({
    open,
    title,
    question,
    description,
    cancelButtonText,
    cancelButtonColour,
    confirmButtonText,
    confirmButtonColour,
    onCancel,
    onConfirm,
}) => {
    return (
        <Dialog open={open} onClose={onCancel} maxWidth="sm" fullWidth>
            <DialogTitle className="custom-dialog-title">
                {title}
                <IconButton
                    aria-label="close"
                    onClick={onCancel}
                    className="custom-dialog-close"
                    size="small"
                >
                    <CloseIcon />
                </IconButton>
            </DialogTitle>
            <DialogContent>
                {question &&<div className="custom-dialog-question">
                    {question}
                </div>}
                {description && <div>
                    {description}
                </div>}
            </DialogContent>
            <DialogActions>
                <Button
                    onClick={onCancel}
                    variant="contained"
                    color={cancelButtonColour || "cancel"}
                >
                    {cancelButtonText || 'Cancel'}
                </Button>
                <Button 
                    onClick={onConfirm}
                    variant="contained"
                    color={confirmButtonColour || "primary"}
                >
                    {confirmButtonText || 'Confirm'}
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default CustomDialog;