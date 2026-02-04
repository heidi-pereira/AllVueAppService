import React, { useState, useMemo, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
    Alert,
    Box,
    Select,
    MenuItem,
    Paper,
    CircularProgress,
    Checkbox,
    FormControlLabel,
    FormControl,
    Button,
} from '@mui/material';
import PageTitle from '../shared/PageTitle';
import InputContainer from '@shared/Inputs/InputContainer';
import {
    useGetApiRolesByCompanyIdQuery,
    useGetApiUsercontextQuery,
    useGetApiCompaniesCompanyAndChildrenByCompanyIdQuery,
    useGetApiProductsGetproductsQuery,
    usePostApiUserAddUserMutation,
} from '../../rtk/apiSlice';
import { UserToAdd } from "../../orval/api/models/userToAdd";
import { CompanyWithProducts } from "../../orval/api/models/companyWithProducts";

import { toast } from "mui-sonner";
import { removeRolesNotAvailable } from './RoleHelper';
import { isRoleReadOnly } from '../RolesTable/HardCodedRoles';
import './_userDialog.scss';
import validator from 'validator';

import {
    StyledRoleContainer,
    StyledRoleLabel,
    StyledNameContainer,
    StyledEmailRoleContainer,
    StyledAlert,
} from './sharedStyles';



const AddUser: React.FC = () => {
    const navigate = useNavigate();
    const { companyId } = useParams<{ companyId: string }>();
    const emptyUser: UserToAdd = {
        ownerCompanyId: companyId || '',
        firstName: '',
        lastName: '',
        email: '',
        role: '',
        roleId: null,
        products: [],
        surveyVueEditingAvailable: false,
        surveyVueFeedbackAvailable: false,
    };
    const [formData, setFormData] = useState<UserToAdd>(emptyUser);
    const [formTouched, setFormTouched] = useState<Record<string, boolean>>({});
    const [formSubmitted, setFormSubmitted] = useState(false);
    const { data: userContext } = useGetApiUsercontextQuery();
    const { data: companyTree, isLoading: companyLoading, error: companyError } = useGetApiCompaniesCompanyAndChildrenByCompanyIdQuery(
        { companyId: companyId || '' }
    );
    const [company, setCompany] = useState<CompanyWithProducts |undefined>();
    const { data: allTheProducts } = useGetApiProductsGetproductsQuery();
    const { data: roles, isLoading: rolesLoading, error: rolesError } = useGetApiRolesByCompanyIdQuery(
        { companyId: formData.ownerCompanyId },
        { skip: !formData.ownerCompanyId }
    );
    const augmentedRoles = useMemo(() => removeRolesNotAvailable(roles || [], formData, userContext), [roles, formData, userContext]);
    const [disableAddAction, setDisableAddAction] = useState(true);

    const [addUser, { isLoading: isAddingUser, error: updateAddUserError }] = usePostApiUserAddUserMutation();

    useEffect(() => {
        if (companyTree) {
            setCompany(companyTree);
        }
    }, [companyTree]);

    useEffect(() => {
            setFormData(prev => ({
                ...prev,
                ownerCompanyId: company?.id ?? '',
                products: company?.products ?? [],
            }));
    }, [company]);

    useEffect(() => {
        if (augmentedRoles?.some(() => true)) {
            const hasUserDefinedRole = augmentedRoles.some(role => role.id > 0);
            const defaultRole = hasUserDefinedRole ? augmentedRoles.find(r => r.id > 0) : augmentedRoles.find(r => r.roleName === "User");
            
            // Only update if the role is empty or doesn't exist in current roles
            setFormData(prev => {
                const currentRoleExists = augmentedRoles.some(r => r.roleName === prev.role);
                if (!prev.role || !currentRoleExists) {
                    return {
                        ...prev,
                        role: defaultRole?.roleName || '',
                        roleId: defaultRole?.id || null
                    };
                }
                return prev;
            });
        }
    }, [augmentedRoles]);

    const handleCancel = () => {
        navigate('/');
    };

    function findCompanyInTree(companyTree: CompanyWithProducts | undefined, id: string): CompanyWithProducts | undefined {
        if (!companyTree) return undefined;
        if (companyTree.id === id) return companyTree;
        if (companyTree.childCompanies && companyTree.childCompanies.length > 0) {
            for (const child of companyTree.childCompanies) {
                const found = findCompanyInTree(child, id);
                if (found) return found;
            }
        }
        return undefined;
    }

    const handleInputChange = (field: keyof UserToAdd, value: string | boolean | CompanyWithProducts[]) => {
        if (formData) {
            const updatedFormData = {
                ...formData,
                [field]: value,
                // If role is being changed, also update roleId
                ...(field === 'role' && typeof value === 'string' && {
                    roleId: augmentedRoles?.find(role => role.roleName === value)?.id || null
                })
            };

            setFormData(updatedFormData);
            
            // Mark this field as touched
            setFormTouched(prev => ({
                ...prev,
                [field]: true
            }));

            setDisableAddAction(updatedFormData.email === '' 
                || updatedFormData.firstName === ''
                || updatedFormData.lastName === ''
                || !updatedFormData.roleId);
        }
    };


    const handleSubmit = async () => {
        setFormSubmitted(true);

        if (!formData.firstName || !formData.lastName || !formData.email || !formData.role) {
            toast.error('Please fill in all required fields.');
            return;
        }

        const selectedRole = augmentedRoles.find(role => role.roleName === formData.role);
        if (!selectedRole?.id) {
            toast.error(`Role ${formData.role} not found`);
            return;
        }
        const userToAdd: UserToAdd = {
            ...formData,
            roleId: selectedRole.id > 0 ? Number(selectedRole.id) : undefined,
        };
        const { error } = await addUser({ userToAdd: userToAdd });

        if (!error || error.status === 200) {
            navigate('/');
        }
    }

    function getErrorText(error) {
        let errorText = `Unknown error`;
        console.log(error);
        if (error && error.status !== "PARSING_ERROR") {
            errorText = `http status ${error.status}`;
            const errorData = error.data;
            if (errorData) {
                if (errorData.error) {
                    errorText = errorData.error;
                } else if (errorData.errors) {
                    const allMessages = Object.values(errorData.errors).flat();
                    errorText = allMessages.join(',');
                }
            }
        }
        return errorText;
    }

    function flattenCompanies(company: CompanyWithProducts): Array<{ id: string; displayName: string }> {
        const result: Array<{ id: string; displayName: string }> = [];
        function traverse(node: CompanyWithProducts) {
            result.push({ id: node.id, displayName: node.displayName });
            if (node.childCompanies && node.childCompanies.length > 0) {
                const sortedChildren = [...node.childCompanies].sort((a, b) =>
                    a.displayName.localeCompare(b.displayName)
                );
                sortedChildren.forEach(traverse);
            }
        }
        if (company) traverse(company);
        return result;
    }

    const companyOptions = useMemo(() => flattenCompanies(companyTree), [companyTree]);

    if (rolesLoading || companyLoading || (company == null && !companyError)) {
        return (
            <Box>
                <CircularProgress />
            </Box>
        );
    }
    if (rolesError || companyError) {
        return (
            <Alert severity="error">
                {String(rolesError?.data || companyError?.data?.error)}
            </Alert>
        );
    }
    const isCompanyDisabled = companyOptions.length <= 1;
    return (
        <Box className="add-user-container" component="form" noValidate>
            <Paper elevation={3} className="add-user-paper">
                <PageTitle title="Add new user" href="/" />

                    {updateAddUserError && (
                        <StyledAlert severity="error">
                            Failed to add user: {getErrorText(updateAddUserError)}
                        </StyledAlert>
                    )}
                <FormControl fullWidth margin="normal" >
                    <Select
                        id="company-select"
                        value={formData.ownerCompanyId}
                        label="Company"
                        onChange={e => {
                            setCompany(findCompanyInTree(companyTree, e.target.value));
                            setFormData(prev => ({ ...prev, ownerCompanyId: e.target.value }));
                        }}
                        sx={{ mb: 2 }}
                        disabled={isCompanyDisabled}
                        IconComponent={isCompanyDisabled ? (() => null) : undefined}
                    >
                        {companyOptions.map(c => (
                            <MenuItem key={c.id} value={c.id}>
                                {c.displayName}
                            </MenuItem>
                        ))}
                     </Select>

                    {company.hasExternalSSOProvider &&
                        <StyledAlert severity="warning">
                            This company uses an external Single Sign-On (SSO) provider. You can only use this dialog to add users to a company that does not use external SSO.
                        </StyledAlert>
                        }

                    <StyledNameContainer>
                        <InputContainer
                            label="First name *"
                            value={formData.firstName || ''}
                            required
                            onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('firstName', e.target.value)}
                            errorMessage={(formTouched.firstName || formSubmitted) && !formData.firstName ? "First name is required" : ""}
                        />
                        <InputContainer
                            label="Last name *"
                            value={formData.lastName || ''}
                            required
                            onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('lastName', e.target.value)}
                            errorMessage={(formTouched.lastName || formSubmitted) && !formData.lastName ? "Last name is required" : ""}
                        />
                    </StyledNameContainer>
                    <FormControl fullWidth margin="normal">
                    <StyledEmailRoleContainer>
                    <InputContainer
                        label="Email address *"
                        value={formData.email || ''}
                        required
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('email', e.target.value)}
                        errorMessage={(formTouched.email || formSubmitted) ? 
                            (!formData.email ? "Email is required" : 
                             !validator.isEmail(formData.email) ? "Please enter a valid email address" : "") : ""}
                        />
                    <StyledRoleContainer>
                        <StyledRoleLabel>Role</StyledRoleLabel>
                        <Select
                            value={formData.role || ''}
                            onChange={(e) => handleInputChange('role', e.target.value as string)}
                            displayEmpty
                            variant="outlined"
                        >
                            {augmentedRoles?.map((role) => {
                                return (
                                    <MenuItem key={role.id} value={role.roleName} sx={isRoleReadOnly(role.roleName) ? { color: 'grey' } : {}}>
                                        {role.roleDisplayName ?? role.roleName}
                                    </MenuItem>
                                );
                            })}
                        </Select>
                            </StyledRoleContainer>
                    
                    </StyledEmailRoleContainer>
                    </FormControl>

                    {company?.products?.length > 0 &&
                        <Box marginBottom={2}>
                            <StyledRoleLabel>Products available</StyledRoleLabel>
                            {company.products.map(product => (
                                <FormControlLabel
                                    key={product.id}
                                    control={
                                        <Checkbox
                                            checked={formData?.products?.map(x => x.id).includes(product.id)}
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
                                />
                            }
                            label="SurveyVue Feedback"
                        />
                    }
                    <Box sx={{ display: "flex", gap: 1, mt: 3 }}>
                        <Button
                            color="cancel"
                            variant="contained"
                            onClick={handleCancel}
                            disabled={isAddingUser}
                        >Cancel</Button>
                        <Button
                            color="primary"
                            variant="contained"
                            onClick={(e: React.MouseEvent<HTMLButtonElement>) => {
                                e.preventDefault();
                                e.stopPropagation();
                                handleSubmit();
                            }}
                            disabled={disableAddAction || isAddingUser || company.hasExternalSSOProvider}
                        >{isAddingUser ? 'Adding user...' : 'Add user'}</Button>
                    </Box>
                </FormControl>
            </Paper>
        </Box>
  );
};

export default AddUser;