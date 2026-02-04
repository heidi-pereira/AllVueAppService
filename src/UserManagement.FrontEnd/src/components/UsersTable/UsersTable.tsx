import React, { useState } from 'react';
import { useSelector } from 'react-redux';
import { GridColDef, GridRowsProp } from '@mui/x-data-grid';
import { toast } from 'mui-sonner'
import type { RootState } from '../../store';
import {
    useGetApiUsersGetusersQuery,
    useGetApiUsersGetcompaniesQuery,
    useGetApiUsersGetprojectsQuery,
    UserProject,
    ProjectIdentifier,
} from '../../rtk/apiSlice';
import UserTableHeader from '../UserTableHeader';
import { DataGridView } from '@shared/DataGridView/DataGridView';
import { DataGridViewRenderDate, DataGridViewRenderYesNo } from '@shared/DataGridView/DataGridViewRenders';
import { MenuOption } from '@shared/KebabMenu/KebabMenu';
import EmptyUsers from './EmptyUsers';
import { User,ProjectType } from '../../orval/api/models';
import './_usersTable.scss';
import UserTableProjectDialog, { ProjectInfo } from './UserTableProjectDialog';
import  { getBasePathFromCurrentPage } from '../../urlHelper';
import { useNavigate } from 'react-router-dom';
import { Box, Button } from '@mui/material';
import Add from '@mui/icons-material/Add';
import CompanyDropDown from '../shared/CompanyDropDown';
import AutocompleteDropDown from '@shared/AutocompleteDropDown/AutocompleteDropDown';
import { isRoleReadOnly, displayRoleName } from '../RolesTable/HardCodedRoles';
import DeleteUserDialog from './DeleteUserDialog';
import ResendEmailDialog from './ResendEmailDialog';

const ALL_ITEMS = "*";

const UsersTable: React.FC = () => {
    const themeDetails = useSelector((state: RootState) => state.userDetailsReducer.user?.themeDetails);
    const { data: companies, isLoading: companiesLoading, error: companiesError } = useGetApiUsersGetcompaniesQuery();
    const { data: projects, isLoading: isProjectsLoading, error: projectsError } = useGetApiUsersGetprojectsQuery();
    const [selectedCompany, setSelectedCompany] = useState('');
    const [selectedProject, setSelectedProject] = useState<ProjectInfo | undefined>(undefined);
    const [includeSavantaUsers, setIncludeSavantaUsers] = useState<boolean>(false);
    const { data: users, error, isLoading, refetch: refreshUsers, isFetching } = useGetApiUsersGetusersQuery(selectedCompany && selectedCompany !== "" && selectedCompany !== ALL_ITEMS
        ? { companyId: selectedCompany, includeSavantaUsers: includeSavantaUsers }
        : { includeSavantaUsers: includeSavantaUsers });

    const formatProjectType = (identifier: ProjectIdentifier): string => {
        return identifier.type + "*" + identifier.id;
    }
    const handleProjectChange = (value: string) => {
        const project = projects?.find(p => formatProjectType(p.projectId) === value);
        setSelectedProject(project?.projectId);
    };

    const filteredRowsOfUsers = (isLoading || users==undefined) ? [] : users.filter(row => {
        if (selectedProject === undefined) return true;
        const isAvailableByUser = row.projects.find(x => x.id === selectedProject.id && x.type === selectedProject.type) != undefined;
        if (isAvailableByUser) {
            return true;
        }
        if (companies == undefined || companiesLoading) {
            return false;
        }
        const ownerCompany = companies.find(c => c.id === row.ownerCompanyId);
        const isAvailableByCompany = ownerCompany.projects.find(x => x.id === selectedProject.id && x.type === selectedProject.type) != undefined;
        return isAvailableByCompany;
        }
    );

    const handleCompanyChange = (value: string) => {
        setSelectedCompany(value);
        setSelectedProject(undefined); 
        const includeSavantaUsers = companies.find(x=> x.id === value)?.displayName === "Savanta";
        setIncludeSavantaUsers(includeSavantaUsers);
    };

    const [resendEmailDialogOpen, setResendEmailDialogOpen] = useState(false);
    const [userToResendEmail, setUserToResendEmail] = useState<User | null>(null);

    const [removeDialogOpen, setRemoveDialogOpen] = useState(false);
    const [userToRemove, setUserToRemove] = useState<User | null>(null);

    const [openDialog, setOpenDialog] = useState(false);
    const [dialogUser, setDialogUser] = useState<UserInfo | null>(null);
    const [dialogProjects, setDialogProjects] = useState<ProjectInfo[]>([]);
    const navigate = useNavigate();


    const handleResendEmailClick = (user: User) => {
        setUserToResendEmail(user);
        setResendEmailDialogOpen(true);
    };

    const handleResendEmailCancel = () => {
        setResendEmailDialogOpen(false);
        setUserToResendEmail(null);
    };


    const handleOpenProjectsDialog = (user: User, projects: ProjectInfo[]) => {
        if (projects.length === 1) {
            navigate(`/projects/${projects[0].companyId}/${projects[0].projectId.type.toLowerCase()}/${projects[0].projectId.id}`);
            return;
        }
        setDialogUser(user);
        setDialogProjects(projects);
        setOpenDialog(true);
    };

    const getUnknownProjectName = (projectId: ProjectIdentifier): string => {
        switch (projectId.type) {
            case ProjectType.AllVueSurvey:
                return `AllVue Survey Project (${projectId.id})`;
            case ProjectType.AllVueSurveyGroup:
                return `AllVue Survey Group (${projectId.id})`;
            case ProjectType.BrandVue:
            default:
        }
        return `Unknown Project (${projectId.id})`;
    };

    const projectAndUserProjectToProjectInfo = (project: Project, userProject: UserProject): ProjectInfo => {
        return {
            projectId: project.projectId,
            name: project.name,
            companyId: project.companyId,
            companyName: project.companyName,
            dataGroupId: userProject?.dataGroupId || 0,
            dataGroupName: userProject?.dataGroupName || "",
            message: ""
        };
    };

    const projectInfoWithErrorMessage = (projectIdentifier: ProjectIdentifier, message: string): ProjectInfo => {
        return {
            projectId: projectIdentifier,
            name: getUnknownProjectName(projectIdentifier),
            companyId: "",
            companyName: "",
            dataGroupId: 0,
            dataGroupName: "",
            message: message
        }
    };

    const getProject = (userProject: UserProject, ownerCompany: ICompanyWithProjects |undefined, email: string): ProjectInfo => {
        if (projectsError || isProjectsLoading) {
            return projectInfoWithErrorMessage(userProject.projectIdentifier, projectsError ? projectsError.data?.error || projectsError.status : "Loading projects...");
        }
        const project = projects?.find(x => (x.projectId.id === userProject.projectIdentifier.id) && (x.projectId.type === userProject.projectIdentifier.type));
        if (project === undefined) {
            return projectInfoWithErrorMessage(userProject.projectIdentifier, `User ${email} does not have access to ${getUnknownProjectName(userProject.projectIdentifier)}`);
        }
        const projectInfo = projectAndUserProjectToProjectInfo(project, userProject);
        if (ownerCompany) {
            const hasAccess = project.companyId === ownerCompany.id || ownerCompany.childCompaniesId.find(x => x === project.companyId);
            if (!hasAccess) {
                return {
                    ...projectInfo,
                    message: `User ${email} does not have access to ${projectInfo.name} - ${getUnknownProjectName(projectInfo.projectId)}`
                };
            };
        }
        return projectInfo;
    }

    const isUserProjectList = (list: UserProject[] | ProjectIdentifier[] | undefined): list is UserProject[] => {
        return Array.isArray(list) && (list.length === 0 || 'projectIdentifier' in list[0]);
    }

    const combineProjectInfo = (projectList1: UserProject[] | ProjectIdentifier[] | undefined, projectList2: UserProject[] | ProjectIdentifier[] | undefined): UserProject[] => {
        const userProjectList1 = isUserProjectList(projectList1) ? projectList1 : projectList1?.map(p => ({ projectIdentifier: p, companyId: '', dataGroupId: 0, dataGroupName: '' })) || [];
        const userProjectList2 = isUserProjectList(projectList2) ? projectList2 : projectList2?.map(p => ({ projectIdentifier: p, companyId: '', dataGroupId: 0, dataGroupName: '' })) || [];
        return combineUserProjectLists(userProjectList1, userProjectList2);
    }

    const combineUserProjectLists = (projectList1: UserProject[] | undefined, projectList2: UserProject[] | undefined): UserProject[] => {
        const projects = [...(projectList1 || [])];
        if (projectList2 && projectList2.length > 0) {
            projectList2.forEach((p: UserProject) => {
                if (projects.find(x => x.projectIdentifier.id === p.projectIdentifier.id)) return; // already in the list
                projects.push(p);
            });
        }
        return  projects;
    }

    const columns: GridColDef[] = [
        { field: 'fullName', 
            headerName: 'Name',
            flex: 2,
            valueGetter: (value, user: User) => `${user.firstName || ''} ${user.lastName || ''}`.trim()
        },
        { field: 'email', headerName: 'Email', flex: 2 },
        { field: 'ownerCompanyId', headerName: 'Company', flex: 2, 
            renderCell: (params) => {
                if (companiesLoading) {
                    return (<>{params.row.ownerCompanyDisplayName}</>);
                }
                const ownerCompany = companies?.find(x => x.id === params.row.ownerCompanyId);
                if (ownerCompany) {

                    return (<a href={ownerCompany.url + '/usermanagement'} target="_blank" rel="noopener noreferrer"> {ownerCompany.displayName}</a>);
                }
                return (<>{params.row.ownerCompanyDisplayName}</>);
            }

        },
        { field: 'projects', headerName: 'Project Access', flex: 2,
            renderCell: (params) => {
                if (companiesLoading) {
                    return <span>Loading...</span>;
                }
                const ownerCompany = companies?.find(x => x.id === params.row.ownerCompanyId);
                if (!ownerCompany) return <span>N/A</span>;
                const projects = combineProjectInfo(ownerCompany.projects, params.row.projects);
                if (projects.length === 0) return <span>None</span>;
                return (<a
                            style={{ cursor: 'pointer'}}
                            onClick={() => {
                                if (projectsError) {
                                    toast.error(`projects failed to load ${projectsError.data?.error || projectsError.status}`);
                                } else if (isProjectsLoading) {
                                    toast.warning(`Please wait for projects to complete loading`);
                                } else {
                                    handleOpenProjectsDialog(params.row, projects.map(x => getProject(x, ownerCompany, params.row.email)));
                                }
                            }}>
                            {(projects.length === 1) ?
                                <>{getProject(projects[0], undefined, "")?.name || "1 Project"}</> :
                                <>{projects.length} projects</>
                            }
                        </a>);
            },
            sortComparator: (v1, v2, param1, param2) => {
                //
                //Horrible hack because we are using the Community DataGrid and it does not support custom sort comparators
                //
                const row1 = users.find(u => u.id === param1.id);
                const row2 = users.find(u => u.id === param2.id);

                const ownerCompany1 = companies?.find(x => x.id === row1.ownerCompanyId);
                const ownerCompany2 = companies?.find(x => x.id === row2.ownerCompanyId);
                const projects1 = combineProjectInfo(ownerCompany1?.projects, row1.projects);
                const projects2 = combineProjectInfo(ownerCompany2?.projects, row2.projects);
                const count1 = projects1.length;
                const count2 = projects2.length;
                return count1 - count2;
            }
        },
        {
            field: 'role', headerName: 'Role', flex: 2,
            renderCell: (params) => {
                if (isRoleReadOnly(params.row.role))
                    return (<span className="disabled">{displayRoleName(params.row.role)}</span>);
                else
                    return (<>{params.row.role}</>);
            }
        },
        { field: 'verified',
            headerName: 'Verified',
            flex: 1,
            renderCell: (params) => DataGridViewRenderYesNo(params.row.verified)
        },
        { field: 'lastLogin', 
            headerName: 'Last Login', 
            flex: 2,
            renderCell: (params) => DataGridViewRenderDate(params.row.lastLogin),
        }
    ];

    if (error) return <div>Loading user Error: {`${error}`}</div>;

    const handleRemoveClick = (user: User) => {
        setUserToRemove(user);
        setRemoveDialogOpen(true);
    };

    const handleRemoveCancel = () => {
        setRemoveDialogOpen(false);
        setUserToRemove(null);
    };

    const handleUserDeleted = () => {
        refreshUsers();
    };

    const emptyUsers = (!isLoading && !companiesLoading && !isProjectsLoading) && (!users || users.length === 0);

    const getCompanyByName = (company: string): CompanyWithProductsAndProjects |undefined => {
        if (companiesError) {
            return undefined;
        }
        return companies.find(x => x.displayName === company);
    } 
    const renderAddUserButton = () => {
        return (

            <Button
                    startIcon={<Add />}
                    sx={{ textTransform: 'none' }}
                    variant="outlined"
                    color="primary"
                    onClick={() => navigate(`/users/add/${selectedCompany === ALL_ITEMS ? '':selectedCompany}`)}>
                    Add user
                </Button>
        );
    }
    const renderProjectSelection = () => {
        if (!projects || projects.length <= 1) return null;
        if (!users || users.length == 0) return null;
        const selectedProjectAsText = selectedProject ? formatProjectType(selectedProject) : ALL_ITEMS;
        const filteredProjects = selectedCompany && selectedCompany !== "" && selectedCompany !== ALL_ITEMS ? 
            projects.filter(p => {
                const ownerCompany = companies?.find(x => x.id === p.companyId);
                if (!ownerCompany) return false;
                const isAvailableByCompany = ownerCompany.id === selectedCompany;
                if (isAvailableByCompany)
                    return true;
                return false;
            })
            : [...projects];
        const sortedProjects = [...filteredProjects].sort((a, b) =>
            a.name.localeCompare(b.name, undefined, { sensitivity: 'base' })
        );
        const projectItems = [
            { value: ALL_ITEMS, label: 'Show all projects' }, // Your custom element
            ...sortedProjects.map(project => ({
                value: formatProjectType(project.projectId),
                label: project.name
            }))
        ];
        if (sortedProjects.length <= 1) {
            return null;
        }
        return (
                <AutocompleteDropDown
                    id="project-select-label"
                    label="Project"
                    value={selectedProjectAsText && selectedProjectAsText.length > 1 ? selectedProjectAsText : ALL_ITEMS}
                    onChange={handleProjectChange}
                    items={projectItems}
                    loading={isProjectsLoading}
                    sx={{
                        display: 'flex',
                        flex: 1,
                        mr: 1,
                        textTransform: 'none',
                        width: '100%',
                        minWidth: {
                            xs: '100px',
                            md: '300px',
                            lg: '400px'
                        },
                    }}
                    size="small"
                />
        );
    };

    const getActionOptions = (row: User): MenuOption[] => {
        const menuItems = [] as MenuOption[];

        if (!row.isExternalLogin) {
            menuItems.push({
                label: row.verified ? 'Reset Password' : 'Resend invite',
                onClick: () => handleResendEmailClick(row),
            });
        }
        menuItems.push({
            label: 'Edit',
            onClick: () => {
                window.location.href = `${getBasePathFromCurrentPage()}/users/${row.id}/edit`;
            },
        });

        menuItems.push(
            {
                label: 'Remove',
                onClick: () => handleRemoveClick(row)
            }
        );

        if (!companiesLoading && !companiesError) {
            const company = getCompanyByName(row.ownerCompanyDisplayName);
            const encodedEmail = encodeURIComponent(row.email);
            menuItems.push({
                label: 'Legacy Auth',
                onClick: () => window.location.href = `${company.url}/auth/UsersPage?SearchString=${encodedEmail}`,
            });
        }
        return menuItems;
    };

    const allUsersOwnedBySameCompany = users?.every(user => user.ownerCompanyId === users[0].ownerCompanyId);
    const filteredColumns = allUsersOwnedBySameCompany
        ? columns.filter(col => col.field !== 'ownerCompanyId')
        : columns;

    return (
        <div className='usersTableContainer'>
             <UserTableHeader 
                selectedCompany={companies?.find(x => x.id === selectedCompany)?.displayName ?? themeDetails?.companyDisplayName }
                disabledMangeData={emptyUsers} />
            {(emptyUsers) ?
                <>
                    <Box display="flex" justifyContent="space-between" alignItems="center" >
                        <Box>
                            <CompanyDropDown selectedCompany={selectedCompany} onChange={handleCompanyChange} />
                            {renderProjectSelection()}
                        </Box>
                    </Box>
                    <EmptyUsers companyId={selectedCompany === ALL_ITEMS ? '' : selectedCompany} />
                </>
                :
                <DataGridView 
                    id="user-table"
                    loading={isLoading || isFetching }
                    rows={filteredRowsOfUsers as GridRowsProp}
                    columns={filteredColumns}
                    perRowOptions={getActionOptions}
                    globalSearch={true}
                    minPageHeight="480px"
                    maxPageHeight='calc(100vh - 230px)'
                    toolbar_lhs={<>
                                        <CompanyDropDown 
                                            selectedCompany={selectedCompany} 
                                            onChange={handleCompanyChange} 
                                        />
                                    {renderProjectSelection()}
                                    </>
                                }
                    toolbar_rhs={renderAddUserButton()}
                />
            }
            {userToRemove && (
                <DeleteUserDialog
                    open={removeDialogOpen}
                    user={userToRemove}
                    onClose={handleRemoveCancel}
                    onDeleted={handleUserDeleted}
                />
            )}
            {userToResendEmail && (
                <ResendEmailDialog
                    verified={userToResendEmail?.verified || false}
                    open={resendEmailDialogOpen}
                    user={userToResendEmail}
                    onClose={handleResendEmailCancel}
                />
            )}
             {dialogUser && (
                 <UserTableProjectDialog
                     open={openDialog}
                     onClose={() => setOpenDialog(false)}
                     user={dialogUser}
                     projects={dialogProjects}
                 />
             )}
        </div>
    );
};

export default UsersTable;