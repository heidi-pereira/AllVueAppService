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
import { GridColDef } from "@mui/x-data-grid";
import { User } from "../EditGroup/GroupUsersDialog";

interface DataGroupUsersDialogProps {
    open: boolean;
    dataGroupName: string;
    users: Array<User>;
    onClose: () => void;
}

const DataGroupUsersDialog: React.FC<DataGroupUsersDialogProps> = ({
    open,
    users,
    dataGroupName,
    onClose
}) => {

    const columns: GridColDef[] = [
        {
            field: 'name',
            headerName: 'Name',
            flex: 1,
            renderCell: (params) => {
                return (<>{params.row.firstName} {params.row.lastName}</>);
            }
        },
        { field: 'email', headerName: 'Email', flex: 1 },
    ];

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle align="center">
                Users for {dataGroupName}
                <IconButton
                    aria-label="close"
                    onClick={onClose}>
                    <CloseIcon/>
                </IconButton>
            </DialogTitle>
            <DialogContent>
                <Typography >
                   {users.length} Users
                </Typography>
                <DataGridView
                    id="user-projects-dialog-table"
                    rows={users}
                    columns={columns}
                    maxPageHeight="350px"
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

export default DataGroupUsersDialog;