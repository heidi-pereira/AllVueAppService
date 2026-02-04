import React, { useState } from 'react';
import { useSelector } from 'react-redux';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import { Button } from '@mui/material';
import {
    PermissionFeatureDto,
    CreateRoleCommand,
    UpdateRoleCommand,
    PostApiRolesApiArg,
    PutApiRolesByIdApiArg,
    RoleDto
} from '../../rtk/apiSlice';
import { userManagementApi as api } from '@/rtk/api/enhancedApi';
import { BlueCheckbox } from '@shared/Inputs/BlueCheckbox';
import InputContainer from '@shared/Inputs/InputContainer';
import Modal from '@shared/Modals/Modal';
import styled from 'styled-components';
import { FetchBaseQueryError, RootState } from '@reduxjs/toolkit/query';
import { SerializedError } from '@reduxjs/toolkit';

interface CreateRoleModalProps {
    open: boolean;
    onClose: () => void;
    permissionGroups: PermissionFeatureDto[];
    allRoles: RoleDto[];
    role?: RoleDto;
    isReadOnly?: boolean;
}

const MaxLength = styled.span`
  position: absolute;
  right: 0;
  top: 0;
  font-size: 0.95rem;
  color: #888;
  line-height: 1.5;
`;

const ErrorMessage = styled.div`
  background: #fff3cd;
  color: #856404;
  border: 1px solid #ffeeba;
  border-radius: 4px;
  padding: 8px 12px;
  margin-bottom: 8px;
`;

const StyledTableContainer = styled(TableContainer)`
  box-shadow: none !important;
  background: transparent !important;
  border-radius: 0 !important;
  border: none !important;
  max-height: 45vh;
  overflow-y: auto;

  @media (max-height: 700px) {
    max-height: 30vh;
  }
  
  @media (max-height: 600px) {
    max-height: 20vh;
  }
 
  @media (max-height: 400px) {
    max-height: 12vh;
  }
`;

const StyledTable = styled(Table)`
    table-layout: fixed !important;
    width: 100% !important;
`;

const StyledTableHead = styled(TableHead)`
    background-color: #f8f9fa !important;
`;

const StyledTableBody = styled(TableBody)`
    border: 1px solid #e0e0e0;
`;

const MatrixTableCell = styled(TableCell)`
  text-align: center !important;
  vertical-align: middle !important;
  padding: 6px 4px !important;
  border: 1px solid #e0e0e0 !important;
  width: 100px !important;
  max-width: 100px !important;
  min-width: 100px !important;
  
  /* Ensure checkboxes are centered */
  & > * {
    display: flex;
    align-items: center;
    justify-content: center;
    margin: 0 auto;
  }
`;

const FeatureNameCell = styled(TableCell)`
  padding: 6px 12px !important;
  border: 1px solid #e0e0e0 !important;
  font-weight: 500 !important;
  width: 200px !important;
  max-width: 200px !important;
  min-width: 200px !important;
`;

const SelectAllContainer = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
`;

export const CreateRoleModal: React.FC<CreateRoleModalProps> = ({
    open,
    onClose,
    permissionGroups: permissionFeatures,
    allRoles,
    role,
    isReadOnly
}) => {
    const userContext = useSelector((state: RootState) => state.userDetailsReducer.user);
    const [roleName, setRoleName] = useState('');
    const [selected, setSelected] = useState<Set<string>>(new Set());
    const [error, setError] = useState<string | null>(null);
    const [postRole, { isLoading: isLoadingPost }] = api.usePostApiRolesMutation();
    const [putRole, { isLoading: isLoadingPut }] = api.usePutApiRolesByIdMutation();

    const requiredRoleNameError = 'Role name is required.';
    const roleNameLengthError = 'Role name must be 35 characters or less.';
    const roleNameInUseError = 'Role name already in use. Please use a different name';
    const isRoleNameError = error === requiredRoleNameError || error === roleNameLengthError || error === roleNameInUseError;
    const isValidationError = isRoleNameError;

    const permissionTypes = [
        { key: 'create', label: 'Add' },
        { key: 'edit', label: 'Edit' },
        { key: 'delete', label: 'Delete' },
        { key: 'access', label: 'Access' }
    ];

    const categorizePermission = (permissionName: string): string => {
        const lowerName = permissionName.toLowerCase();
        if (lowerName.includes('create') || lowerName.includes('add')) return 'create';
        if (lowerName.includes('edit') || lowerName.includes('update') || lowerName.includes('modify')) return 'edit';
        if (lowerName.includes('delete') || lowerName.includes('remove')) return 'delete';
        return 'access'; // Default for view, read, access permissions
    };

    // Transform permission groups into matrix structure
    const permissionMatrix = permissionFeatures.map(group => ({
        featureName: group.name,
        permissions: permissionTypes.map(type => {
            const matchingOptions = group.options.filter(opt => 
                opt.name && categorizePermission(opt.name) === type.key
            );
            return {
                type: type.key,
                typeLabel: type.label,
                options: matchingOptions,
                hasPermissions: matchingOptions.length > 0
            };
        })
    }));

    const getAllPermissionIds = (): string[] => {
        const allIds: string[] = [];
        permissionFeatures.forEach(group => {
            group.options.forEach(opt => {
                if (opt.id) allIds.push(opt.id.toString());
            });
        });
        return allIds;
    };

    const handleSelectAll = () => {
        const allIds = getAllPermissionIds();
        setSelected(new Set(allIds));
    };

    const handleDeselectAll = () => {
        setSelected(new Set());
    };

    const allIds = getAllPermissionIds();
    const isAllSelected = allIds.every(id => selected.has(id)) && allIds.length > 0;
    const isSomeSelected = allIds.some(id => selected.has(id));

    React.useEffect(() => {
        setError(null);

        if (role) {
            setRoleName(role.roleName);
            setSelected(new Set(role.permissions?.map(p => p.id?.toString()).filter((id): id is string => Boolean(id)) ?? []));
        } else {
            setRoleName('');
            setSelected(new Set());
        }
    }, [role, permissionFeatures, open]);

    if (!open) return null;

    const handleCheck = (id: string) => {
        setSelected(prev => {
            const next = new Set(prev);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });
    };

    const handleSubmit = async () => {
        const error = validate();
        if (error !== null) {
            return;
        }

        let result:
            | { data: RoleDto }
            | { error: FetchBaseQueryError | SerializedError }
            | undefined;

        if (role) {
            const command: UpdateRoleCommand = {
                roleId: role.id,
                roleName: roleName.trim(),
                permissionOptionIds: Array.from(selected).map(id => parseInt(id)),
                updatedByUserId: userContext?.userId ?? ''
            };
            const putRolesApiArg: PutApiRolesByIdApiArg = {
                id: role.id,
                updateRoleCommand: command,
            };
            result = await putRole(putRolesApiArg);

        } else {
            const command: CreateRoleCommand = {
                roleName: roleName.trim(),
                permissionOptionIds: Array.from(selected).map(id => parseInt(id)),
            };
            const postRolesApiArg: PostApiRolesApiArg = {
                createRoleCommand: command,
            };
            result = await postRole(postRolesApiArg);
        }

        if (result && 'error' in result && result.error) {
            const message = getErrorMessage(result.error);
            setError(message);
        } else {
            onClose();
        }
    };

    const validate = (): string | null => {
        const roleNameError = validateRoleName();
        return roleNameError;
    };

    function validateRoleName(value: string = roleName): string | null {
        setError(null);
        if (!value.trim()) {
            setError(requiredRoleNameError);
            return requiredRoleNameError;
        }
        if (value.length > 35) {
            setError(roleNameLengthError);
            return roleNameLengthError;
        }

        const existingCustomRoleNames = allRoles.filter(r => r.id !== role?.id).map(r => r.roleName);
        if (existingCustomRoleNames.some(roleName => roleName.toLowerCase() === value.trim().toLowerCase())) {
            setError(roleNameInUseError);
            return roleNameInUseError;
        }
        return null;
    }

    function getErrorMessage(error: unknown): string {
        if (
            typeof error === 'object' &&
            error !== null &&
            'data' in error &&
            typeof (error as FetchBaseQueryError).data === 'object' &&
            (error as FetchBaseQueryError).data !== null &&
            'error' in ((error as FetchBaseQueryError).data as object) &&
            typeof ((error as FetchBaseQueryError).data as { error?: unknown }).error === 'string'
        ) {
            return ((error as FetchBaseQueryError).data as { error: string }).error;
        }

        return `Failed to ${role ? 'update' : 'create'} role.`;
    }

    return (
        <Modal
            open={open}
            onClose={onClose}
            header={`${isReadOnly ? "View Role" : role ? 'Edit Role' : 'Create Role'} - ${userContext?.companyDisplayName || 'Company'}`}
            footer={
                <>
                    <Button color="cancel" variant="contained" onClick={onClose}>{isReadOnly? "OK":"Cancel"}</Button>
                    {!isReadOnly &&
                    <Button
                        color="primary"
                        variant="contained"
                        onClick={handleSubmit}
                        disabled={isValidationError || !roleName.trim() || roleName.length > 35 || allRoles.some(r => r.roleName === roleName && r.id !== role?.id)
                            || isLoadingPost || isLoadingPut}
                    >
                        {role ? 'Update role' : 'Create role'}
                    </Button>
                    }
                </>
            }
        >
            {error && !isRoleNameError && (
                <ErrorMessage>
                    {error}
                </ErrorMessage>
            )}
            <InputContainer
                label="Role name"
                value={roleName}
                placeholder="E.g. Special user"
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                    const value = e.target.value;
                    setRoleName(value);
                    validateRoleName(value);
                }}
                disabled={isReadOnly}
                errorMessage={error && (isRoleNameError) ? error : undefined}
            >
                <MaxLength>(max 35 characters)</MaxLength>
            </InputContainer>
            {!isReadOnly &&
            <SelectAllContainer>
                <Button
                    color="primary"
                    variant="contained"
                    onClick={handleSelectAll}
                    disabled={isAllSelected}
                >{isAllSelected ? "All Selected" : "Select All"}</Button>
                <Button
                    color="cancel"
                    variant="contained"
                    onClick={handleDeselectAll}
                    disabled={!isSomeSelected}
                >Deselect All</Button>
            </SelectAllContainer>
            }
            <StyledTableContainer>
                <StyledTable size="small" aria-label="permissions matrix table">
                    <StyledTableHead>
                        <TableRow>
                            <FeatureNameCell>
                                Features
                            </FeatureNameCell>
                            {permissionTypes.map(type => (
                                <MatrixTableCell key={type.key}>
                                    {type.label}
                                </MatrixTableCell>
                            ))}
                        </TableRow>
                    </StyledTableHead>
                    <StyledTableBody>
                        {permissionMatrix.map(feature => (
                            <TableRow key={feature.featureName} hover>
                                <FeatureNameCell>
                                    {feature.featureName}
                                </FeatureNameCell>
                                {feature.permissions.map(permission => (
                                    <MatrixTableCell key={permission.type}>
                                        {permission.hasPermissions ? (
                                            permission.options.length === 1 ? (
                                                <BlueCheckbox
                                                    checked={selected.has(permission.options[0].id?.toString() || '')}
                                                    onChange={() => handleCheck(permission.options[0].id?.toString() || '')}
                                                    label=""
                                                    disabled={isReadOnly}
                                                />
                                            ) : (
                                                <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                                                    {permission.options.map(opt => (
                                                        <BlueCheckbox
                                                            key={opt.id}
                                                            checked={selected.has(opt.id?.toString() || '')}
                                                            onChange={() => handleCheck(opt.id?.toString() || '')}
                                                            label={opt.name?.replace(feature.featureName, '').trim() || ''}
                                                            disabled={isReadOnly}
                                                        />
                                                    ))}
                                                </div>
                                            )
                                        ) : (
                                            <span style={{ color: '#ccc' }}>—</span>
                                        )}
                                    </MatrixTableCell>
                                ))}
                            </TableRow>
                        ))}
                    </StyledTableBody>
                </StyledTable>
            </StyledTableContainer>
        </Modal>
    );
};

export default CreateRoleModal;
