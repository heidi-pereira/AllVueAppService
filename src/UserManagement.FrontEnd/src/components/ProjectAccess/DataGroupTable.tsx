import React from "react";
import { userManagementApi as api } from "@/rtk/api/enhancedApi";
import { DataGridView } from "@shared/DataGridView/DataGridView";
import { MenuOption } from '@shared/KebabMenu/KebabMenu';
import { GridColDef, GridRowsProp } from "@mui/x-data-grid";
import { DataGroup, ProjectType, User, Variable } from "@/rtk/apiSlice";
import { useNavigate } from "react-router-dom";
import { skipToken } from "@reduxjs/toolkit/query/react";
import DataGroupQuestionsDialog from "./DataGroupQuestionsDialog";
import DataGroupUsersDialog from "./DataGroupUsersDialog";
import CustomDialog from "../shared/CustomDialog";
import { toast } from "mui-sonner";

export interface DataGroupTableProps {
    dataGroups: DataGroup[];
    companyId?: string;
    projectId?: number;
    projectType?: ProjectType;
    editUrl: string;
    isLoading: boolean;
}

const DataGroupTable: React.FC<DataGroupTableProps> = ({ dataGroups, isLoading, editUrl, companyId, projectId, projectType }) => {
    const navigate = useNavigate();
    const { data: userData, isLoading: userDataIsLoading, error: userError } = api.useGetApiUsersGetusersforprojectbycompanyByCompanyIdQuery(companyId ? { companyId: companyId } : skipToken);
    const { data: variableData, isLoading: variableDataIsLoading, error: variableError } = api.useGetApiProjectsByCompanyIdAndProjectTypeProjectIdVariablesAvailableQuery(projectId && projectType && companyId ? { companyId: companyId, projectId: projectId, projectType: projectType } : skipToken);
    const [ deleteDataGroup ] = api.useDeleteApiUsersdatapermissionsDeleteallvueruleByIdMutation();
    const [ questionCount, setQuestionCount ] = React.useState(0);
    const [ filterCount, setFilterCount ] = React.useState(0);
    const [ userCount, setUserCount ] = React.useState(0);
    const [ dataGroupName, setDataGroupName ] = React.useState("");
    const [ dialogQuestions, setDialogQuestions ] = React.useState<Variable[]>([]);
    const [ dialogUsers, setDialogUsers ] = React.useState<User[]>([]);
    const [ questionsDialogOpen, setQuestionsDialogOpen ] = React.useState(false);
    const [ usersDialogOpen, setUsersDialogOpen ] = React.useState(false);
    const [ deleteDialogOpen, setDeleteDialogOpen ] = React.useState(false);
    const [ dataGroupToDelete, setDataGroupToDelete ] = React.useState<DataGroup | null>(null);
    
    React.useEffect(() => {
        if (variableData && !variableError) {
            setQuestionCount(variableData.unionOfQuestions.length);
            const filterableQuestionsWithOptions = variableData.unionOfQuestions.
                filter(variable => variable.options?.length > 1 &&
                    variable?.surveySegments.length === variableData.surveySegments.length);
            setFilterCount(filterableQuestionsWithOptions.length);
        }
    }, [variableData]);

    React.useEffect(() => {
        if (userData && !userError) {
            setUserCount(userData.length);
        }   
    }, [userData]);

    const areQuestionsNotAvailable = (row: DataGroup) => {
        if (!variableData)
            return false;
        if (!variableData.unionOfQuestions)
            return true;
        if (!row?.availableVariableIds || row.availableVariableIds.length === 0) return false;

        const availableIds = new Set(variableData.unionOfQuestions.map(q => q.id));
        return row.availableVariableIds.some(id => !availableIds.has(id));
    };

    const areFiltersNotAvailable = (row: DataGroup) => {
        if (!variableData)
            return false;
        if (!variableData.unionOfQuestions) return true;
        if (!row?.filters || row.filters.length === 0) return false;

        const availableIds = new Set(variableData.unionOfQuestions.map(q => q.id));
        return row.filters.some(id => !availableIds.has(id.variableConfigurationId));
    };

    const columns: GridColDef[] = [
        { field: 'ruleName', headerName: 'Group name', flex: 2 },
        { field: 'questions', headerName: 'Questions', flex: 1,
            renderCell: (params) => {
                if (!params.row.availableVariableIds || params.row.availableVariableIds.length === 0) {
                    return <>All</>;
                }
                if (areQuestionsNotAvailable(params.row)) {
                    return <>N/A</>;
                }
                return <a
                    style={{ cursor: 'pointer'}}
                    onClick={() => {
                            handleOpenQuestionsDialog(params.row.ruleName, params.row.availableVariableIds);
                        }
                    }>{params.row.availableVariableIds.length} of {questionCount}</a>;
            },
        },
        { field: 'filters', headerName: 'Filters', flex: 1,
            renderCell: (params) => {
                if (!params.row.filters || params.row.filters.length === 0) {
                    return <>None</>;
                }
                if (areFiltersNotAvailable(params.row)) {
                    return <>N/A</>;
                }
                return <>{params.row.filters.length} of {filterCount}</>;
            },
        },
        { field: 'users', headerName: 'Users', flex: 1,
            renderCell: (params) => {
                if (params.row.allCompanyUsersCanAccessProject) return <>All</>;
                if (params.row.userIds?.length === 0) {
                    return <>{params.row.userIds?.length} of {userCount}</>;
                }
                return <a
                    style={{ cursor: 'pointer'}}
                    onClick={() => {
                            handleOpenUsersDialog(params.row.ruleName, params.row.userIds);
                        }
                    }>{params.row.userIds?.length} of {userCount}</a>;
            },
        }
    ];

    const getActionOptions = (row: DataGroup): MenuOption[] => {
        const menuItems = [] as MenuOption[];

        menuItems.push(
            {
                label: 'Edit',
                onClick: () => handleEditClick(row)
            }
        );

        menuItems.push(
            {
                label: 'Delete',
                onClick: () => handleRemoveClick(row)
            }
        );
        return menuItems;
    };

    const handleEditClick = (row: DataGroup) => {
        navigate(`${editUrl}${row.id}`);
    };

    const handleRemoveClick = (row: DataGroup) => {
        setDataGroupToDelete(row);
        setDeleteDialogOpen(true);
    };

    const handleDeleteConfirm = async () => {
        if (!dataGroupToDelete) return;
        const { error } = await deleteDataGroup({ id: dataGroupToDelete.id, company: dataGroupToDelete.company, projectType: dataGroupToDelete.projectType, projectId: dataGroupToDelete.projectId});
        if (error && error.status !== 200) {
            toast.error(`${error.data.error}`);
        } else {
            setDeleteDialogOpen(false);
            setDataGroupToDelete(null);
        }
    };

    const handleOpenQuestionsDialog = (ruleName: string, variableIds: number[]) => {
        if (!variableIds || variableIds.length === 0) return;
        if (!variableData) return;
        const questions = variableData.unionOfQuestions.filter(v => variableIds.includes(v.id));
        setDataGroupName(ruleName);
        setDialogQuestions(questions);
        setQuestionsDialogOpen(true);
    };

    const handleOpenUsersDialog = (ruleName: string, userIds: string[]) => {
        if (!userIds || userIds.length === 0) return;
        if (!userData) return;
        const users = userData.filter(u => userIds.includes(u.id!));
        setDataGroupName(ruleName);
        setDialogUsers(users);
        setUsersDialogOpen(true);
    };

    if (!dataGroups || dataGroups.length === 0) {
        return <></>;
    }
    
    return <><DataGridView
            id="datagroup-table"
            loading={isLoading || userDataIsLoading || variableDataIsLoading}
            rows={dataGroups as GridRowsProp}
            columns={columns}
            perRowOptions={getActionOptions}
            globalSearch={false} 
        />
        <DataGroupQuestionsDialog
            open={questionsDialogOpen}
            questions={dialogQuestions}
            dataGroupName={ dataGroupName }
            onClose={() => setQuestionsDialogOpen(false)}
        />
        <DataGroupUsersDialog
            open={usersDialogOpen}
            users={dialogUsers}
            dataGroupName={ dataGroupName }
            onClose={() => setUsersDialogOpen(false)}
        />
        <CustomDialog
            open={deleteDialogOpen}
            title={`Delete data group: ${dataGroupToDelete?.ruleName}`}
            question={`The data associated with this group will be deleted.`}
            confirmButtonText="Delete"
            confirmButtonColour="error"
            onCancel={() => setDeleteDialogOpen(false)}
            onConfirm={handleDeleteConfirm}
        />
        </>
};  

export default DataGroupTable;