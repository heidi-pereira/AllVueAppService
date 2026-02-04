import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { userManagementApi as api} from '@/rtk/api/enhancedApi';
import { DataGridView } from '@shared/DataGridView/DataGridView';
import {
    Alert,
    Box,
    Button,
    Link,
} from '@mui/material';

import { GridColDef, GridRowsProp } from '@mui/x-data-grid';
import styles from './ProjectsTable.module.scss';
import CompanyDropDown from '../shared/CompanyDropDown';
import PageTitle from '../shared/PageTitle';
import ProjectAccessStatus from '../shared/ProjectAccessStatus';

const ProjectsTable: React.FC = () => {
    const navigate = useNavigate();
    const [selectedCompany, setSelectedCompany] = useState<string>('');
    const { data: projects, isLoading: projectsLoading, error: projectsError } = api.useGetApiProjectsQuery(
        selectedCompany && selectedCompany !== ""
        ? { companyId: selectedCompany }
            : {});

    const { data: companies, isLoading: companiesLoading } = api.useGetApiUsersGetcompaniesQuery();

    const handleCompanyChange = (value: string) => {
        setSelectedCompany(value);
    };

    const columns: GridColDef[] = [
        {
            field: 'name', headerName: 'Project', flex: 2,
            renderCell: (params) => {
                return (
                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Link href={params.row.url}>
                            {params.row.name}
                        </Link>
                    </Box>
                );
            }
        },
        { 
            field: 'userAccess',
            headerName: 'User access',
            flex: 1,
            renderCell: (params) => (
                <Box sx={{ display: 'flex', alignItems: 'center', height: 'var(--height)' }}>
                    <ProjectAccessStatus accessLevel={params.row.userAccess} includeDescription={false} />
                </Box>
            ),
        },
        { field: 'companyName',
            headerName: 'Company',
            flex: 2,
            renderCell: (params) => {
                if (companiesLoading) {
                    return (<>{params.row.companyName}</>);
                }
                const ownerCompany = companies?.find(x => x.id === params.row.companyId);
                if (ownerCompany) {
                    return (<a href={ownerCompany.url + '/usermanagement'} target="_blank" rel="noopener noreferrer">{ownerCompany.displayName}</a>);
                }
                return (<>{params.row.companyName}</>);
            }
        },
        { field: 'dataGroupCount', headerName: 'Data groups', flex: 2 },
        {
            field: 'actions',
            headerName: '',
            flex: 1,
            renderCell: (row) => (
                <Button onClick={() => { 
                    navigate(`/projects/${row.row.companyId}/${row.row.projectId.type.toLowerCase()}/${row.row.projectId.id}`)}}>Configure</Button>
            )
        },
    ];

    if (projectsError) return <Alert severity="error">Loading projects Error: {`${projectsError?.data?.error}`}</Alert>;
    const allSameCompany = projects && projects.length > 0
        ? projects.every(p => p.companyId === projects[0].companyId)
        : true;
    const filteredColumns = allSameCompany ? columns.filter(col => col.field !== 'companyName') : columns;
    return (
        <div className={styles.usersTableContainer}>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                <PageTitle href="/" title="Manage Projects" />
            </Box>
            <DataGridView
                id="project-table"
                loading={projectsLoading}
                rows={projects as GridRowsProp}
                columns={filteredColumns}
                globalSearch={true}
                getRowId={(row) => `${row.projectId.type}${row.projectId.id}`}
                minPageHeight="480px"
                maxPageHeight='calc(100vh - 230px)'
                toolbar_lhs={<CompanyDropDown selectedCompany={selectedCompany} onChange={handleCompanyChange}/>}
            />
        </div>
    );
};

export default ProjectsTable;