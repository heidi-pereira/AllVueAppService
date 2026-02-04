import React, { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
    Box,
    MenuItem,
    Select,
    Paper,
    Alert,
    CircularProgress,
    Checkbox,
    FormControlLabel,
    Button,
} from '@mui/material';
import styled from 'styled-components';
import {
    useGetApiUserGetByUserIdQuery,
    useGetApiRolesByCompanyIdQuery,
    usePostApiUserMutation,
    useGetApiUsercontextQuery,
    useGetApiCompaniesByCompanyIdQuery,
    useGetApiProductsGetproductsQuery
} from '../../rtk/apiSlice';
import { User} from '../../orval/api/models';
import InputContainer from '@shared/Inputs/InputContainer';
import PageTitle from '../shared/PageTitle';
import './_userDialog.scss';
import { toast } from "mui-sonner";
import { removeRolesNotAvailable } from './RoleHelper';
import { isRoleReadOnly } from '../RolesTable/HardCodedRoles';

import {
    StyledRoleContainer,
    StyledRoleLabel,
    StyledNameContainer,
    StyledEmailRoleContainer,
    StyledAlert,
} from './sharedStyles';


const StyledLoadingContainer = styled(Box)`
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 400px;
`;



const EditUser: React.FC = () => {
    const { userId } = useParams<{ userId: string }>();
    const navigate = useNavigate();

    // API hooks
    const { data: currentUser, isLoading: isLoadingUser, error: userError } = useGetApiUserGetByUserIdQuery({
        userId: userId
    });
    const { data: userContext } = useGetApiUsercontextQuery();
    const { data: allTheProducts } = useGetApiProductsGetproductsQuery();
    const currentUserNotAvailable = isLoadingUser || userError || !currentUser?.ownerCompanyId;
    const { data: roles, isLoading: rolesLoading, error: rolesError } = useGetApiRolesByCompanyIdQuery(
        { companyId: currentUser?.ownerCompanyId || '' },
        { skip: currentUserNotAvailable }
    );
    const { data: company, isLoading: companyLoading, error: companyError } = useGetApiCompaniesByCompanyIdQuery(
        { companyId: currentUser?.ownerCompanyId || '' },
        { skip: currentUserNotAvailable }
    );
    const [updateUser, { isLoading: isUpdatingUser, error: updateUserError }] = usePostApiUserMutation();
    const editingMySelf = userContext?.userId === currentUser?.id;
    const editingUserAboveMe = userContext && !userContext.isSystemAdministrator && currentUser?.role === 'SystemAdministrator';
    // Form state
    const [formData, setFormData] = useState<User | null>(null);
    const canEditUser = !editingMySelf && !editingUserAboveMe;
    const canEditProducts = canEditUser && company && !company?.hasExternalSSOProvider;
    useEffect(() => {
        if (currentUser) {
            setFormData(currentUser);
        }
    }, [currentUser]);


    const augmentedRoles = useMemo(() => removeRolesNotAvailable(roles || [], currentUser, userContext), [roles, currentUser, userContext]);

    const handleInputChange = (field: keyof User, value: string) => {
        if (formData) {
            setFormData({
                ...formData,
                [field]: value
            });
        }
    };

    const handleSubmit = async () => {
        if (!formData || !userContext?.userId) return;

        // Find the role ID from the selected role name
        const selectedRole = augmentedRoles.find(role => role.roleName === formData.role);
        if (!selectedRole?.id) {
            toast.error(`Role ${formData.role} not found`);
            return;
        }

        const userToUpdate: User = {
            ...formData,
            roleId: selectedRole.id > 0 ? Number(selectedRole.id) : undefined,
        };
        const { error } = await updateUser({ user: userToUpdate });

        if (error && error.status !== 200) {
            toast.error(`Failed to update user ${formData.email} ${error.data?.error || error.status}`);
        } else {
            navigate('/');
        }
    };

    const handleCancel = () => {
        navigate('/');
    };

    if (isLoadingUser || rolesLoading || companyLoading) {
        return (
            <StyledLoadingContainer>
                <CircularProgress />
            </StyledLoadingContainer>
        );
    }
    if (userError || rolesError || companyError) {
        return (
            <Alert severity="error">
               {String(userError?.data?.error || rolesError?.data || companyError?.data?.error)}
            </Alert>
        );
    }

    if (!currentUser || !formData) {
        return (
            <Alert severity="error">
                User not found
            </Alert>
        );
    }

    if (!currentUserNotAvailable && !rolesLoading) {
        if (augmentedRoles.find(x => x.roleName === formData.role) === undefined) {
            toast.error(`The user ${currentUser.email} has role ${currentUser.role} not found in the list of company roles`);
        }
    }
    return (
        <Box className="edit-user-container">
            <Paper elevation={3} className="edit-user-paper">
                <PageTitle
                    title="Edit User"
                    href="/"
                />

                {updateUserError && (
                    <StyledAlert severity="error">
                        Failed to update user: {updateUserError.data
                            ? String(updateUserError.data.error || updateUserError.data.status)
                            : "Unknown error"}
                    </StyledAlert>
                )}

                {editingUserAboveMe && (
                    <StyledAlert severity="warning">
                        Unable to edit user above your role: {currentUser.email} is {currentUser.role}.
                    </StyledAlert>
                )}

                {editingMySelf && (
                    <StyledAlert severity="warning">
                        Unable to edit yourself.
                    </StyledAlert>
                )}

                <form noValidate autoComplete="off">
                    <InputContainer
                        label="Company"
                        value={currentUser.ownerCompanyDisplayName}
                        disabled
                    />

                    <StyledNameContainer>
                        <InputContainer
                            label="First name"
                            value={formData.firstName || ''}
                            onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('firstName', e.target.value)}
                            disabled={!canEditUser || company.hasExternalSSOProvider}
                        />
                        <InputContainer
                            label="Last name"
                            value={formData.lastName || ''}
                            onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('lastName', e.target.value)}
                            disabled={!canEditUser || company.hasExternalSSOProvider}
                        />
                    </StyledNameContainer>

                    <StyledEmailRoleContainer>
                        <InputContainer
                            label="Email address"
                            value={formData.email || ''}
                            disabled
                        />
                        <StyledRoleContainer>
                            <StyledRoleLabel>Role</StyledRoleLabel>
                            <Select
                                value={formData.role || ''}
                                onChange={(e) => handleInputChange('role', e.target.value as string)}
                                displayEmpty
                                variant="outlined"
                                disabled={!canEditUser}
                            >
                                {augmentedRoles?.map((role) => {
                                    return (
                                        <MenuItem key={role.id} value={role.roleName} sx={isRoleReadOnly(role.roleName) ? { color: 'grey' } : {}}>
                                        {role.roleDisplayName ?? role.roleName}
                                      </MenuItem>
                                )})}
                            </Select>
                        </StyledRoleContainer>
                    </StyledEmailRoleContainer>

                    {company?.products?.length > 0 && 
                    <Box marginBottom={2}>
                        <StyledRoleLabel>Products available</StyledRoleLabel>
                        {company.products.map(product => (
                            <FormControlLabel
                                key={product.id}
                                control={
                                    <Checkbox
                                        checked={formData.products?.map(x => x.id).includes(product.id)}
                                        onChange={(e) => {
                                            if (!formData) return;
                                            const checked = e.target.checked;
                                            setFormData({
                                                ...formData,
                                                products: checked
                                                    ? [...(formData.products || []), product]
                                                    : (formData.products || []).filter(currentProduct => currentProduct.id !== product.id)
                                            });
                                        }}
                                        color="primary"
                                        disabled={!canEditProducts}
                                    />
                                }
                                label={allTheProducts?.find(x => x.projectId.id === product.id)?.name || 'Unknown Product'}
                            />
                        ))}
                        </Box>
                    }
                    {(company.surveyVueEditingAvailable || company.surveyVueFeedbackAvailable) && 
                        <StyledRoleLabel>Add-ons available</StyledRoleLabel>
                    }
                    {company.surveyVueEditingAvailable && 
                        <FormControlLabel
                            control={
                                <Checkbox
                                checked={formData.surveyVueEditingAvailable}
                                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('surveyVueEditingAvailable', e.target.checked)}
                                color="primary"
                                disabled={!canEditProducts}
                                />
                            }
                        label="SurveyVue Editing"
                        />
                    }
                    {company.surveyVueFeedbackAvailable &&
                        <FormControlLabel
                            control={
                                <Checkbox
                                    checked={formData.surveyVueFeedbackAvailable}
                                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('surveyVueFeedbackAvailable', e.target.checked)}
                                color="primary"
                                disabled={!canEditProducts}
                                />
                            }
                            label="SurveyVue Feedback"
                        />
                    }

                    {/* Action Buttons */}
                    <Box sx={{ display: "flex", gap: 1, mt: 3 }}>
                        <Button
                            color="cancel" 
                            variant="contained"
                            disabled={isUpdatingUser}
                            onClick={handleCancel}>
                            Cancel
                        </Button>
                        <Button
                            variant="contained"
                            color="primary"
                            disabled={isUpdatingUser || !canEditUser}
                            onClick={(e) => {
                                e.preventDefault();
                                e.stopPropagation();
                                handleSubmit();
                            }}
                        >
                            {isUpdatingUser ? 'Updating...' : 'Update user'}
                        </Button>
                    </Box>
                </form>
            </Paper>
        </Box>
    );
};

export default EditUser;
