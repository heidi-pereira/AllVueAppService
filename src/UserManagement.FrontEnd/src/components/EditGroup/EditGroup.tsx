import React from 'react';
import {
  Box,
  Grid,
  Typography,
  TextField,
  Button,
  Checkbox,
  FormControlLabel,
  Tooltip,
  Alert,  
} from '@mui/material';
import HeaderBar from '../shared/HeaderBar';
import PageTitle from '../shared/PageTitle';
import { useNavigate, useParams } from 'react-router-dom';
import { userManagementApi as api } from "@/rtk/api/enhancedApi";
import { skipToken } from '@reduxjs/toolkit/query/react';
import { getAllCompanyNames } from '../shared/helpers';
import GroupQuestionsDialog, { Question } from './GroupQuestionsDialog';
import GroupUsersDialog from './GroupUsersDialog';
import CustomDropdown from '../shared/CustomDropdown';
import ResponsesPercentageBar from '../shared/ResponsesPercentageBar';
import { User } from './GroupUsersDialog';
import GroupFiltersDialog, { Filter } from './GroupFiltersDialog';
import { toast } from "mui-sonner";
import { DataGroup } from '@/rtk/apiSlice';
import CustomDialog from '../shared/CustomDialog';
import InfoIcon from '@mui/icons-material/Info';
import { buildSelectedVariablesWithOptions, useUpdateResponseFilterCount } from './filterUtils';

const EditGroup: React.FC = () => {
  const navigate = useNavigate();
  const params = useParams();
  const { data: originalDataGroup, isLoading: isLoading, error: dataGroupError } = api.useGetApiUsersdatapermissionsGetdatagroupByIdQuery(params.groupId ? { id: Number(params.groupId) } : skipToken);
  const { data: project, isLoading: isLoadingProject, error: projectError } = api.useGetApiProjectsByCompanyAndProjectTypeProjectIdQuery({ company: params.company, projectId: params.projectId, projectType: params.projectType });
  const { data: companyAncestorNames } = api.useGetApiCompaniesByCompanyIdAncestornamesQuery(project?.companyId ? { companyId: project?.companyId } : skipToken);
  const { data: userData, error: userError, isLoading: isLoadingUsers, isFetching: isFetchingUsers } = api.useGetApiUsersGetusersforprojectbycompanyByCompanyIdQuery(project?.companyId ? { companyId: project?.companyId } : skipToken);
  const { data: variableData, error: variableError, isLoading: isLoadingVariables, isFetching: isFetchingVariables } = api.useGetApiProjectsByCompanyIdAndProjectTypeProjectIdVariablesAvailableQuery(project?.companyId ? { companyId: params.company, projectId: params.projectId, projectType: params.projectType } : skipToken);

  const [ createDataGroup, { isLoading: isCreatingDataGroup }] = api.usePostApiUsersdatapermissionsAdddatagroupMutation();
  const [ updateDataGroup, { isLoading: isUpdatingDataGroup }] = api.usePostApiUsersdatapermissionsUpdatedatagroupMutation();
  const [groupName, setGroupName] = React.useState('');
  const { data: dataGroups, isLoading: dataGroupsAreLoading, error: dataGroupsError } = api.useGetApiUsersdatapermissionsGetdatagroupsByCompanyAndProjectTypeProjectIdQuery(
    project?.companyId ? { company: project?.companyId, projectId: Number(params.projectId), projectType: params.projectType as ProjectType } : skipToken
  );
  const [shareAll, setShareAll] = React.useState(false);
  const [hasMissingFilters, setHasMissingFilters] = React.useState(false);
  const [hasMissingQuestions, setHasMissingQuestions] = React.useState(false);
  const [questions, setQuestions] = React.useState([] as Array<Question>);
  const [users, setUsers] = React.useState([] as Array<User>);
  const [filters, setFilters] = React.useState([] as Array<Filter>);
  const [userToDataGroup, setUserToDataGroup] = React.useState<Record<string, DataGroup>>({});
  const [shareWithAllIsAvailable, setShareWithAllIsAvailable] = React.useState<boolean>(false);
  const [reasonWhyCantShareProjectWithAllUsers, setReasonWhyCantShareProjectWithAllUsers] = React.useState<string>("");
  const isEditMode = Boolean(params.groupId);
  const [maximumNumberOfResponse, setMaximumNumberOfResponse] = React.useState(0);
  const [filterCountOfRespondents, setFilterCountOfRespondents] = React.useState(0);
  const [filterCountError, setFilterCountError] = React.useState(""); 
  const [userLinkDialogOpen, setUserLinkDialogOpen] = React.useState(false);
  const [isLoadingFilteredCount, setIsLoadingFilteredCount] = React.useState(false);
  const [formIsDirty, setFormIsDirty] = React.useState(false);
  const updateResponseFilterCount = useUpdateResponseFilterCount();

  React.useEffect(() => {
    const selectedUserIds = isEditMode && originalDataGroup ? originalDataGroup.userIds : [];
    const listOfUsers = userData?.map((user) => ({
      id: user.id,
      name: `${user.firstName} ${user.lastName}`,
      email: user.email,
      isSelected: selectedUserIds.includes(user.id || ""),
    } as User)) || [];
    setUsers(listOfUsers);
  }, [userData, originalDataGroup]);

  React.useEffect(() => {
    if (isEditMode && !isLoading && originalDataGroup) {
      setGroupName(originalDataGroup.ruleName);
      setShareAll(originalDataGroup.allCompanyUsersCanAccessProject);
    }
  }, [originalDataGroup]);

  React.useEffect(() => {
      if (!dataGroupsAreLoading && dataGroups) {
          const userIdToDataGroup: Record<string, DataGroup> = {};
          dataGroups.forEach((group: DataGroup) => {
              group.userIds.forEach((userId) => {
                  userIdToDataGroup[userId] = group;
              });
          });
          const existingGroupWithShareAll = dataGroups.find(group => group.allCompanyUsersCanAccessProject);
          if (existingGroupWithShareAll && existingGroupWithShareAll.id != params.groupId) {
              const message = `This option is not available. As you are currently sharing this project with all ${getAllCompanyNames(project?.companyName, companyAncestorNames)} users, through the data group '${existingGroupWithShareAll.ruleName}'`;
              setShareWithAllIsAvailable(false);
              setReasonWhyCantShareProjectWithAllUsers(message);
          } else {
              setShareWithAllIsAvailable(true);
              setReasonWhyCantShareProjectWithAllUsers("");
          }
        setUserToDataGroup(userIdToDataGroup);
      }
  }, [dataGroupsAreLoading, dataGroups]);

  React.useEffect(() => {
    if (dataGroupError && dataGroupError.status !== 200) {
      toast.error(dataGroupError.data?.error || 'Failed to load data group');
    }
  }, [dataGroupError]);

  React.useEffect(() => {
      if (dataGroupsError && dataGroupsError.status !== 200) {
          toast.error(dataGroupsError.data?.error || 'Failed to load data groups');
      }
  }, [dataGroupsError]);

  React.useEffect(() => {
    const selectedQuestionIds = isEditMode && originalDataGroup ? originalDataGroup.availableVariableIds : [];
    const counts = variableData?.unionOfQuestions
        ? variableData.unionOfQuestions
            .filter(q => typeof q.count === 'number')
            .map(q => q.count)
        : [];
    const maxCount = counts.length > 0 ? Math.max(...counts) : 0;
    setMaximumNumberOfResponse(maxCount);
    setFilterCountOfRespondents(maxCount);
    const questions = variableData?.unionOfQuestions?.map(variable => ({
        id: variable.id,
        title: variable.name,
        description: variable.description,
        percent: maxCount === 0 ? 0 : variable.count * 100 / maxCount,
        isSelected: selectedQuestionIds.includes(variable.id),
        numberOfResponses: variable.count,
        isHiddenInAllVue: variable.isHiddenInAllVue,
    })) || [];
    setQuestions(questions);
    const questionIdSet = new Set(questions.map(q => q.id));
    if (!isLoadingVariables && !isFetchingVariables && project?.companyId) {
      setHasMissingQuestions(selectedQuestionIds.some(id => !questionIdSet.has(id)));
    }

    const selectedFilters = isEditMode && originalDataGroup ? originalDataGroup.filters : [];
    const filterData = variableData?.unionOfQuestions
          .filter(variable => variable.options?.length > 1 &&
              variable?.surveySegments.length === variableData.surveySegments.length &&
              variable?.answerType === "Category" &&
              variable?.calculationType !== "text")
          .map(variable => ({
        id: variable.id,
        name: variable.name,
        description: variable.description,
        options: variable.options.map(opt => ({
            id: opt.id,
            name: opt.name,
            isSelected: selectedFilters.some(selected => selected.variableConfigurationId == variable.id && selected.entityIds.includes(opt.id)),
            percent: maxCount === 0 ? 0 : opt.count * 100 / maxCount
        }))
    })) || [];
    setFilters(filterData);
    updateResponseFilterCount(params,
        selectedFilters,
        setFilterCountOfRespondents,
        setIsLoadingFilteredCount,
        setFilterCountError,
        maxCount);

    if (!isLoadingVariables && !isFetchingVariables && project?.companyId) {
      setHasMissingFilters(selectedFilters.some(id => !questionIdSet.has(id.variableConfigurationId)));
    }

  }, [variableData, originalDataGroup]);

  React.useEffect(() => {
    if (variableError) {
      toast.error(variableError.data?.error || 'Failed to load variables');
    }
  }, [variableError]);

  React.useEffect(() => {
    if (userError) {
      toast.error(userError.data?.error || 'Failed to load users');
    }
  }, [userError]);

  const allCompanyNames = getAllCompanyNames(project?.companyName, companyAncestorNames);
  const title = isEditMode ? `Edit data group` : `Create data group`;
  const saveButtonText = isEditMode ? `Save changes` : `Create data group`;

  const getBackUrl = () => {
      return `/projects/${params.company}/${params.projectType}/${params.projectId}`;
  };

  const handleQuestionsUpdate = (updatedQuestions: Array<Question>) => {
    setQuestions(updatedQuestions);
    setFormIsDirty(true);
  };

  const currentNumberOfResponses = () => {
      const selectedQuestions = questions.filter(q => q.isSelected);
      if (selectedQuestions.length === 0) {
          return maximumNumberOfResponse;
      }
      const responseCounts = selectedQuestions.map(q => q.numberOfResponses).filter(n => typeof n === 'number');
      const currentCount = responseCounts.length > 0 ? Math.max(...responseCounts) : maximumNumberOfResponse;
      return currentCount;
  }

  const getQuestionText = () => {
    const selectedQuestions = questions.filter(q => q.isSelected);
    if (selectedQuestions.length === 0 && !hasMissingQuestions) return "All";
      return `${selectedQuestions.length} of ${questions.length} Questions`;
  };

  const handleUsersUpdate = (updatedUsers: Array<User>) => {
    setUsers(updatedUsers);
    setFormIsDirty(true);
  };

  const getUserText = () => {
    if (shareAll) return `All ${allCompanyNames} users`;
    const selectedUsersCount = users ? users.filter(q => q.isSelected).length : 0;
    if (selectedUsersCount === 0) return "Select";
    return `${selectedUsersCount} of ${users.length} Users`;
  };


  const handleFiltersUpdate = async(updatedFilters: Array<Filter>) => {
    setFilters(updatedFilters);
    await updateResponseFilterCount(params,
        buildSelectedVariablesWithOptions(updatedFilters),
        setFilterCountOfRespondents,
        setIsLoadingFilteredCount,
        setFilterCountError,
        maximumNumberOfResponse);
  };

  const getFilterText = () => {
    const allOptions = filters.flatMap(filter => filter.options);
    const selectedOptions = allOptions.filter(option => option.isSelected);
    if (selectedOptions.length === 0) return "None";
    if (selectedOptions.length === allOptions.length) return "All";
    const questionsSelected = filters.filter(filter => filter.options.some(x => x.isSelected));

    if (questionsSelected.length === 1) {
        return `${questionsSelected[0].name} (${selectedOptions.map(x => x.name).join(", ")})`;
    }
    return `${questionsSelected.length} questions with ${selectedOptions.length} filters`;
  };

  const isFormValid = () => {
    if (groupName.trim() === '') return false;
    return true;
  };

  const doSaveDataGroup = async () => {
    const originalAvailableVariableIds = originalDataGroup?.availableVariableIds??[];
    const originalFilters = originalDataGroup?.filters??[];
    const dataGroup: DataGroup = {
      id: isEditMode ? Number(params.groupId) || 0 : 0,
      projectId: project?.projectId.id || Number(params.projectId),
      projectType: project?.projectId.type || params.projectType,
      ruleName: groupName,
      allCompanyUsersCanAccessProject: shareAll,
      company: project?.companyId || '',
      availableVariableIds: hasMissingQuestions ? originalAvailableVariableIds: questions.filter(q => q.isSelected).map(q => q.id),
      userIds: users.filter(u => u.isSelected).map(u => u.id),
        filters: hasMissingFilters ? originalFilters : buildSelectedVariablesWithOptions(filters)
    };

    if (isEditMode) {
        const { error } = await updateDataGroup({ dataGroup: dataGroup });
      return error;
    } else {
        const { error } = await createDataGroup({ dataGroup: dataGroup });
      return error;
    }
  };

  const saveDataGroup = async () => {
    if (!isFormValid()) return;
    const response = await doSaveDataGroup();
    if (response && response.status !== 200) {
      toast.error(`Failed to ${isEditMode ? 'update' : 'create'} data group ${groupName} ${response.data?.error || response.status}`);
    } else {
      navigate(getBackUrl());
    }
  };

  const renderShareWithAllUsers = () => {
      if (shareWithAllIsAvailable) {
          return (
              <Checkbox
                  checked={shareAll}
                  onChange={e => setShareAll(e.target.checked)}
              />
              );
      }
    return (
        <Tooltip title={reasonWhyCantShareProjectWithAllUsers}>
            <span>
                <Checkbox
                    disabled
                />
            </span>
        </Tooltip>
    );

  };

  const handleUsersLink = () => {
    if (formIsDirty) {
      setUserLinkDialogOpen(true);
    } else {
      navigate("/");
    }
  };
  if (isLoadingProject) {
    return <></>;
  }
  if (projectError && projectError.status !== 200) {
    return (
        <Alert severity="error">
           {projectError?.data?.error}
        </Alert>
    );
  }
  return (
    <>
      <HeaderBar>
        <PageTitle href={getBackUrl()} title={title} />
      </HeaderBar>
      <Grid container spacing={2} marginBottom={4}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Typography variant="formlabel">Project</Typography>
          <TextField
            value={project?.name || ''}
            variant="outlined"
            fullWidth
            inputProps={{ "aria-label": "Project name" }}
            disabled
          />
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <Box display="flex" alignItems="baseline" justifyContent="space-between">
            <Typography variant="formlabel">Data group name</Typography>
            <Typography sx={{ color: 'grey.600', fontSize: 14 }}>(max 35 characters)</Typography>
          </Box>
          <TextField
            value={groupName}
            onChange={e => { setGroupName(e.target.value); setFormIsDirty(true); }}
            variant="outlined"
            placeholder="E.g. East Midlands"
            fullWidth
            inputProps={{ maxLength: 35, "aria-label": "Data group name" }}
          />
        </Grid>

      <Grid size={{ xs: 12 }}>
      <Typography sx={{ mb: 1 }}>
        Limit what users in this data group can access by choosing specific questions, filters, or both.
      </Typography>
      <Typography>
        Users will only see the questions and filtered data you select. If you leave everything unselected, all current and future data will be visible.
      </Typography>
      </Grid>
      
        <Grid size={{ xs: 12, md: 6 }}>
          <Typography variant="formlabel">Questions</Typography>
          {hasMissingQuestions &&
              <Alert severity="info" icon={<InfoIcon />} aria-label="The question list is not available" >
                  <Typography variant='alertHeading'>The question list is not available&nbsp;</Typography>
                  <Typography variant='alertBody'>because you do not have access to all of the questions selected for the data group.</Typography>
              </Alert>
          }
          {!hasMissingQuestions &&
            <CustomDropdown text={getQuestionText()} name="questions" isLoading={isLoadingVariables || isFetchingVariables}
             hasError={variableError !== undefined} errorMessage="Failed to load questions. Please try again.">
              <GroupQuestionsDialog questions={questions} onChange={handleQuestionsUpdate} />
            </CustomDropdown>
          }
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <Typography variant="formlabel">Filters</Typography>
          {hasMissingFilters &&
              <Alert severity="info" icon={<InfoIcon />} aria-label="Filtering is not available" >
                  <Typography variant='alertHeading'>Filtering is not available&nbsp;</Typography>
                  <Typography variant='alertBody'>because you do not have access to all of the questions in the filtered list.</Typography>

              </Alert>
          }
          {!hasMissingFilters &&
            <CustomDropdown text={getFilterText()} name="filters" isLoading={isLoadingVariables || isFetchingVariables}
             hasError={variableError !== undefined} errorMessage="Failed to load filters. Please try again.">
               <GroupFiltersDialog filters={filters} totalCountOfRespondents={maximumNumberOfResponse} filterCountOfRespondents={filterCountOfRespondents} onChange={handleFiltersUpdate} isLoadingFilteredCount={isLoadingFilteredCount} filterCountError={filterCountError}/>
            </CustomDropdown>
          }
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <Box sx={{ mt: 1, mb: 2 }}>
            <ResponsesPercentageBar responsesCount={currentNumberOfResponses()} totalCount={maximumNumberOfResponse}/>
          </Box>
        </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
              <Box sx={{ mt: 1, mb: 2 }}>
                  <ResponsesPercentageBar responsesCount={filterCountOfRespondents} totalCount={maximumNumberOfResponse} />
              </Box>
          </Grid>
        </Grid>
      <Grid container columnSpacing={4}>
        <Grid size={{ xs: 12}}>
          <Typography variant="formlabel">Users</Typography>
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <CustomDropdown text={getUserText()} name="users" isDisabled={shareAll || (!isLoadingUsers && !isFetchingUsers && users.length === 0)} isLoading={isLoadingUsers || isFetchingUsers}
          hasError={userError !== undefined} errorMessage="Failed to load users. Please try again.">
            <GroupUsersDialog currentDataGroup={originalDataGroup?.id} users={users} lookupUserToDataGroup={userToDataGroup} onChange={handleUsersUpdate} />
          </CustomDropdown>
          {(!isLoadingUsers && !isFetchingUsers && users.length === 0) &&
            <Alert severity="info" icon={<InfoIcon />} aria-label="No users added yet" >
              <Typography variant='alertHeading'>No users added yet</Typography>
              <Typography variant='alertBody'>There are currently no users for this company. Add users anytime, or create your group and assign users later. <a onClick={() => handleUsersLink()}>Manage users</a></Typography>
            </Alert>
          }
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <FormControlLabel
            control={renderShareWithAllUsers()}
            label={`Share with all ${allCompanyNames} users`}
          />
        </Grid>
</Grid>
      <Grid container spacing={4} marginBottom={4}>
        <Grid size={{ xs: 12, md: 6 }}>
      {/* Action buttons */}
      <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
        <Button
          color="cancel" 
          variant="contained"
          disabled={isCreatingDataGroup || isUpdatingDataGroup}
          onClick={() => { 
                    navigate(getBackUrl())}}>
          Cancel
        </Button>
        <Button
          variant="contained"
          color="primary"
          disabled={ isLoading || isCreatingDataGroup || isUpdatingDataGroup || !isFormValid() || (dataGroupError !== null && dataGroupError !== undefined)}
          onClick={saveDataGroup}
        >
          {saveButtonText}
        </Button>
      </Box>
        </Grid>
      </Grid>
      <CustomDialog
          open={userLinkDialogOpen}
          title={`Are you sure you want to continue?`}
          question={`You have unsaved changes which will be lost if you continue`}
          confirmButtonText="Continue"
          confirmButtonColour="error"
          onCancel={() => setUserLinkDialogOpen(false)}
          onConfirm={() => {
              setUserLinkDialogOpen(false);
              navigate("/");
          }}
      />
    </>
  );
};

export default EditGroup;