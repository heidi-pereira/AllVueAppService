import React from 'react';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import IconButton from '@mui/material/IconButton';
import CloseIcon from '@mui/icons-material/Close';
import Button from '@mui/material/Button';
import { Link } from "react-router-dom"
import Typography from '@mui/material/Typography';
import { DataGridView } from '@shared/DataGridView/DataGridView';
import './_userTableProjectDialog.scss';
import Tooltip from '@mui/material/Tooltip';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import { ProjectIdentifier } from '@/rtk/apiSlice';

export interface ProjectInfo {
    projectId: ProjectIdentifier;
    name: string;
    companyId: string;
    companyName: string;
    dataGroupId: number;
    dataGroupName: string;
    message: string;
}

interface UserTableProjectDialogProps {
    open: boolean;
    onClose: () => void;
    user: UserInfo;
    projects: ProjectInfo[];
}


const UserTableProjectDialog: React.FC<UserTableProjectDialogProps> = ({
    open,
    onClose,
    user,
    projects,
}) => {
    const projectsMerged = projects.filter(project => project !== undefined);
    const allProjectsOwnedBySameCompany = projectsMerged && projectsMerged.length > 0 ? projectsMerged.every(project => project.companyName === projectsMerged[0].companyName):false;
    const columns = [
        {
            field: 'name',
            headerName: 'Project',
            flex: 2,
            renderCell: (params) => {
                if (params.row.message && params.row.message.length > 0) {
                    return (<div className="warning">
                                <Tooltip title={
                            <>
                                There is a problem with this project.<br /> Probable cause:<br />
                                The project is not related to the company of the user.<br />
                                <br />
                                {params.row.message}<br />
                            </>
                        }>
                                    <WarningAmberIcon fontSize="small" />
                                </Tooltip>
                                {params.row.name}
                            </div>);
                }
                return (
                    <Link
                        to={`/projects/${params.row.companyId}/${params.row.projectId.type.toLowerCase()}/${params.row.projectId.id}`}
                        style={{ cursor: 'pointer'}}
                    >
                        {params.row.name}
                    </Link>
                );
            },
        },
        {
            field: 'companyName',
            headerName: 'Company',
            flex: 2,
            renderCell: (params) => {
                if (!params.row.companyName) {
                    return "N/A";
                }
                return params.row.companyName;
            }
        },
        {
            field: 'permissionGroup',
            headerName: 'Data group',
            flex: 2,
            renderCell: (params) => (
                <Link
                        to={`/projects/${params.row.companyId}/${params.row.projectId.type.toLowerCase()}/${params.row.projectId.id}/group/${params.row.dataGroupId}`}
                        style={{ cursor: 'pointer'}}
                    >
                    {params.row.dataGroupName}
                </Link>
            ),
        }
    ];

    const filteredColumns = allProjectsOwnedBySameCompany
        ? columns.filter(col => col.field !== 'companyName')
        : columns;

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle>
                Project access
                <IconButton
                    aria-label="close"
                    onClick={onClose}>
                    <CloseIcon/>
                </IconButton>
            </DialogTitle>
            <DialogContent>
                <Typography >
                    {(user.firstName || user.lastName) &&
                        <span className="userName">{user.firstName} {user.lastName}</span>
                    }
                    <span className="email">{user.email}</span> {user.role}
                </Typography>
                <Typography variant="subtitle2" sx={{ minHeight: 40, lineHeight: 1.5, py: 1 }}>
                    {projects.length} projects
                    <span className="ownedby">{allProjectsOwnedBySameCompany ? `(owned by ${projectsMerged[0].companyName})` : ''}</span>
                </Typography>
                <DataGridView
                    id="user-projects-dialog-table"
                    rows={projectsMerged}
                    columns={filteredColumns}
                    getRowId={(row) => `${row.projectId.type}-${row.projectId.id}`}/>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="cancel" variant="contained">
                    Cancel
                </Button>
            </DialogActions>
        </Dialog>
    );
}
export default UserTableProjectDialog;