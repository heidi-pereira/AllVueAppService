import React from "react";
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import IconButton from '@mui/material/IconButton';
import CloseIcon from '@mui/icons-material/Close';
import Button from '@mui/material/Button';
import { DataGridView } from '@shared/DataGridView/DataGridView';
import Typography from '@mui/material/Typography';
import { Variable } from "@/rtk/apiSlice";
import { GridColDef } from "@mui/x-data-grid";
import { Box } from "@mui/material";

interface DataGroupQuestionsDialogProps {
    open: boolean;
    dataGroupName: string;
    questions: Array<Variable>;
    onClose: () => void;
}

const DataGroupQuestionsDialog: React.FC<DataGroupQuestionsDialogProps> = ({
    open,
    questions,
    dataGroupName,
    onClose
}) => {

    const columns: GridColDef[] = [
        {
            field: 'name',
            headerName: 'Questions',
            flex: 1,
            renderCell: (params) => {
                return (<Box sx={{ lineHeight: 2 }}>
                        <Typography variant="questionModalName">
                        {params.row.name}
                        </Typography>
                        <Typography variant="questionModalDescription">
                        {params.row.description}
                        </Typography>
                    </Box>);
            }
        }
    ];

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle align="center">
                Questions for {dataGroupName}
                <IconButton
                    aria-label="close"
                    onClick={onClose}>
                    <CloseIcon/>
                </IconButton>
            </DialogTitle>
            <DialogContent>
                <Typography >
                   {questions.length} Questions
                </Typography>
                <DataGridView
                    id="user-projects-dialog-table"
                    rows={questions}
                    columns={columns}
                    maxPageHeight="350px"
                    rowHeight={74}
                />
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="cancel" variant="contained">
                    Close
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default DataGroupQuestionsDialog;