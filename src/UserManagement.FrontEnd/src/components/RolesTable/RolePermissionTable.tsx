import React from 'react';
import styles from './_rolesTable.module.scss'
import AddActionButton from '@shared/Buttons/AddActionButton';
import { DataGridView } from '@shared/DataGridView/DataGridView';
import { GridRowsProp } from '@mui/x-data-grid';
import {
  useGetApiFeaturesQuery,
    PermissionFeatureDto,
    useDeleteApiRolesByIdMutation,
  RoleDto,
  useGetApiUsersGetcompaniesQuery,
} from '../../rtk/apiSlice';
import { userManagementApi as api } from '@/rtk/api/enhancedApi';
import CreateRoleModal from './CreateRoleModal';
import RoleTableHeader from '../RoleTableHeader';
import { Box } from '@mui/material';
import { useSelector } from 'react-redux';
import { RootState } from '@/store';
import { toast } from 'mui-sonner';
import CustomDialog from '../shared/CustomDialog';
import { joinCapitalised } from '../shared/Helpers';
import { displayRoleName } from './HardCodedRoles';
const RolePermissionTable: React.FC = () => {
  const userContext = useSelector((state: RootState) => state.userDetailsReducer.user);
  const { data: companies, isLoading: companiesLoading, error: companiesError } = useGetApiUsersGetcompaniesQuery();
  const currentCompany = companies?.find(company => company.shortCode === userContext?.userOrganisation);
  const { data: features, isLoading, error } = useGetApiFeaturesQuery();
    const { data: roles, isLoading: isRolesLoading, error: rolesError, refetch: refreshRoles } =
        api.useGetApiRolesByCompanyIdQuery(
            { companyId: currentCompany?.id || '' }
        );
    const [deleteRole] = useDeleteApiRolesByIdMutation();

    const [isModalOpen, setIsModalOpen] = React.useState(false);
    const [editingRole, setEditingRole] = React.useState<RoleDto | undefined>(undefined);
    const [roleToDelete, setRoleToDelete] = React.useState<RoleDto | null>(null);
    const [deleteDialogOpen, setDeleteDialogOpen] = React.useState(false);
    const augmentedRoles: RoleDto[] = React.useMemo(() => {
        if (!userContext || !roles) {
            return [];
        }
        const areRolesForSavanta = userContext.userOrganisation === "savanta";
        if (!areRolesForSavanta) {
            return roles?.filter(role => role.roleName !== "SystemAdministrator");
        }
        return [...roles];
    }, [roles, userContext]);

  const canUserEditRole = userContext?.isSystemAdministrator;
  const canUserDeleteRole = userContext?.isSystemAdministrator;
  const canUserCreateRole = userContext?.isSystemAdministrator;
  if (isLoading || isRolesLoading || companiesLoading) {
    return <div style={{ padding: '16px' }}>Loading features...</div>;
  }

  if (error || !features || rolesError || !roles || companiesError) {
    return (
      <div style={{ padding: '16px' }}>
        {error && <div>Error: {error.toString()}</div>}
        {rolesError && <div>Error: {rolesError.toString()}</div>}
      </div>
    );
  }

  const getPermissionsForFeature = (role: RoleDto, feature: PermissionFeatureDto): string => {
          const perms = (feature.options ?? []).filter(opt =>
              role.permissions?.some(p => p.id === opt.id)
          );
          const permissionOptionNames: string[] = perms.map(option => option.name).filter((name): name is string => typeof name === 'string' && name.length > 0);

          return permissionOptionNames.length ? joinCapitalised(permissionOptionNames) : 'No access';
  };

  const handleCreateRole = () => {
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setEditingRole(undefined);
    setIsModalOpen(false);
  }
    const isRoleReadOnly = (role?: RoleDto) => {
        return role ? (role.id < 0) : true;
    }


  const columns = [
    {
      field: 'roleName',
      headerName: 'Role',
      flex: 1,
      renderCell: (params) => (
          <span className={isRoleReadOnly(params.row) ? styles.disabledText : ''}>
              {params.value}
          </span>
      ),
    },
    ...features.map((feature) => ({
      field: feature.name,
      headerName: feature.name,
      flex: 1,
      sortable: false,
      filterable: false,
      disableColumnMenu: true
    }))
  ];

  const rows: GridRowsProp = augmentedRoles.map((role) => {
    const row: Record<string, string | number> = {
      id: role.id,
        roleName: displayRoleName(role.roleName),
    };
    features.forEach((feature) => {
      row[feature.name] = getPermissionsForFeature(role, feature);
    });
    return row;
  });

    const editClickHandler = (row: RoleDto) => {
        const role = roles.find(r => r.id === row.id);
        setEditingRole(role);
        setIsModalOpen(true);
    }

    const deleteClickHandler = (role: RoleDto) => {
        setRoleToDelete(role);
        setDeleteDialogOpen(true);
    }

    const handleDeleteCancel = () => {
        setDeleteDialogOpen(false);
        setRoleToDelete(null);
    };

    const handleDeleteConfirm = async () => {
        if (roleToDelete) {
            const { error } = await deleteRole({ id: roleToDelete.id });
            if (error && error.status !== 200) {
                toast.error(`${error.data?.error || error.status}`);
                setDeleteDialogOpen(false);
                setRoleToDelete(null);
                return;
            }
            refreshRoles();
            setDeleteDialogOpen(false);
            setRoleToDelete(null);
        }
    };

    const getPerRowOptions = (row: RoleDto) => {
        const items = [];
        if (canUserEditRole && !isRoleReadOnly(row)) {
            items.push({
                label: 'Edit',
                onClick: () => editClickHandler(row)
            });
        } else {
            items.push({
                label: 'View',
                onClick: () => editClickHandler(row)
            });

        }
        if (canUserDeleteRole && !isRoleReadOnly(row)) {
            items.push({
                label: 'Delete',
                onClick: () => deleteClickHandler(row)
            });
        };
        return items;
    };

    const getDeleteRoleDialog = (roleToDelete: RoleDto | null) => {
        const name = roleToDelete?.roleName;

        return (
            <CustomDialog
                open={deleteDialogOpen}
                title="Delete role"
                question={`Are you sure you want to delete the ${name} role?`}
                description={"This can't be undone."}
                confirmButtonText="Delete"
                confirmButtonColour="error"
                onCancel={handleDeleteCancel}
                onConfirm={handleDeleteConfirm}
            />
        );
    };
    return (
        <div className={styles.rolesTableContainer}>
            <RoleTableHeader />
            {canUserCreateRole &&
            <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                <AddActionButton
                    onClick={handleCreateRole}
                    icon="add"
                    label="Create role"
                />
            </Box>
            }
            <DataGridView
                pageHeight="600px"
                id="role-table"
                loading={isLoading}
                rows={rows}
                columns={columns}
                perRowOptions={getPerRowOptions}
                minPageHeight="350px"
                maxPageHeight='calc(100vh - 230px)'
            />
            <CreateRoleModal
                open={isModalOpen}
                onClose={handleCloseModal}
                permissionGroups={features}
                allRoles={roles}
                role={editingRole}
                isReadOnly={editingRole ? !canUserEditRole || isRoleReadOnly(editingRole) : false}
            />
            {roleToDelete && getDeleteRoleDialog(roleToDelete)}
        </div>
    );
};

export default RolePermissionTable;
