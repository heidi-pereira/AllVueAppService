import React from "react";
import {
  Alert,
  Box,
  Typography,
  Paper,
  Grid,
  Button,
  Link,
  Tooltip,
} from "@mui/material";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import DatasetOutlinedIcon from '@mui/icons-material/DatasetOutlined';
import LockPersonOutlinedIcon from '@mui/icons-material/LockPersonOutlined';
import DomainAddOutlinedIcon from '@mui/icons-material/DomainAddOutlined';
import ShareOutlinedIcon from '@mui/icons-material/ShareOutlined';
import RemoveCircleOutlineIcon from '@mui/icons-material/RemoveCircleOutline';
import PageTitle from "../shared/PageTitle";
import HeaderBar from "../shared/HeaderBar";
import InfoTooltip from "../shared/InfoTooltip";
import { userManagementApi as api } from "@/rtk/api/enhancedApi";
import { useNavigate, useParams } from "react-router-dom";
import ProjectAccessStatus from "../shared/ProjectAccessStatus";
import CustomDialog from "../shared/CustomDialog";
import { toast } from "mui-sonner";
import { skipToken } from "@reduxjs/toolkit/query/react";
import { getAllCompanyNames } from "../shared/helpers";
import { ProjectType } from "@/orval/api/models";
import DataGroupTable from "./DataGroupTable";
const ProjectAccess: React.FC = () => {
    const navigate = useNavigate();
    const params = useParams();
    const { data: project, error: projectError, refetch: refetchProject } = api.useGetApiProjectsByCompanyAndProjectTypeProjectIdQuery({ company: params.company, projectId: Number(params.projectId), projectType: params.projectType as ProjectType });
    const { data: legacyAuthUsers, refetch: refetchLegacyAuthUsers } = api.useGetApiProjectsByProjectTypeAndProjectIdLegacysharedUserQuery({ projectId: Number(params.projectId), projectType: params.projectType as ProjectType });
    const { data: companyAncestorNames } = api.useGetApiCompaniesByCompanyIdAncestornamesQuery(project?.companyId ? { companyId: project?.companyId } : skipToken);
    const { data: dataGroups, isLoading: dataGroupsAreLoading, error: dataGroupsError, refetch: refetchDataGroups } = api.useGetApiUsersdatapermissionsGetdatagroupsByCompanyAndProjectTypeProjectIdQuery({ company: params.company, projectId: Number(params.projectId), projectType: params.projectType as ProjectType });
    const [setProjectShared] = api.usePostApiProjectsByCompanyAndProjectTypeProjectIdSetsharedMutation();
    const [migrateLegacySharedUsers] = api.useDeleteApiProjectsByCompanyAndProjectTypeProjectIdLegacysharedUserMutation();
    const [shareDialogOpen, setShareDialogOpen] = React.useState(false);
    const [legacySharedUsersDialogOpen, setLegacySharedUsersDialogOpen] = React.useState(false);
    const [ dataGroupsBoxSize, setDataGroupsBoxSize ] = React.useState<number>(6);
    const projectName = project?.name ?? "";
    const currentCompanyName = project?.companyName ?? "";
    const dataGroupEditUrl = `/projects/${project?.companyId}/${project?.projectId.type.toLowerCase()}/${project?.projectId.id}/group/`;

    React.useEffect(() => {
      if (!dataGroupsAreLoading && !dataGroupsError) {
        if (dataGroups && dataGroups.length > 0) {
          setDataGroupsBoxSize(12);
        } else {
          setDataGroupsBoxSize(6);
        }
      }
    }, [dataGroupsError, dataGroups]);

    const isProjectShared = () => {
        const hasSharedAllDataGroup = dataGroups?.some(group => group.allCompanyUsersCanAccessProject);
        return hasSharedAllDataGroup || ((project?.isShared) ?? false);
    }

    const handleShareConfirm = async () => {
      setShareDialogOpen(false);
      if (project) {
          const { error } = await setProjectShared({ company: project.companyId, projectId: project.projectId.id, projectType: project.projectId.type, isShared: !isProjectShared() });
        if (error && error.status !== 200) {
            toast.error(`${error.data.error}`);
        }
      }
    };

    const handleMigratingLegacySharedUsers = async () => {
        setLegacySharedUsersDialogOpen(false);
        if (project) {
            const { error } = await migrateLegacySharedUsers({ company: project.companyId, projectId: project.projectId.id, projectType: project.projectId.type});
            if (error && error.status !== 200) {
                toast.error(`${error.data.error}`);
            } else {
                refetchProject();
                refetchDataGroups();
                refetchLegacyAuthUsers();
            }

        }
    };
    if (projectError) {
        return <Alert severity="error">{`${projectError?.data?.error}`}</Alert>;
    }
    const allCompanyNames = getAllCompanyNames(project?.companyName, companyAncestorNames);
    const explainShareWithAllUsers = !dataGroups || dataGroups.length === 0;

    const renderConvertLegacyButton = () => {
        if (project?.isShared || legacyAuthUsers?.length > 0)
            return (
                <Button sx={{ height: 37, mb: 1, marginRight: 1.25 }} variant="contained" color="warning" startIcon={<InfoOutlinedIcon />} onClick={() => setLegacySharedUsersDialogOpen(true)}>
                    <Tooltip title="This will convert existing permissions into the new style data groups.">
                        Convert previous permissions
                    </Tooltip>
                </Button>
            );
    };

    const renderShareButtons = () => {

        if (!isProjectShared())
            return (
                    <Tooltip title={<>
This will give all current and future users at {allCompanyNames
} full access to all data in this project {projectName}
{dataGroups?.length > 0 &&
    <>
, unless they’re in a data group.
</>
}
</>}>
                        <Button sx={{ height: 37, mb: 1 }} variant="outlined" startIcon={<ShareOutlinedIcon />} onClick={() => setShareDialogOpen(true)}>
                        Share with all users
                        </Button>
                    </Tooltip>
                );
        const hasOnlyShareWithAllDataGroup = dataGroups?.filter(x => !x.allCompanyUsersCanAccessProject).length === 0;
        const sharedWithAllDataGroup = dataGroups?.find(x => x.allCompanyUsersCanAccessProject);
        return (
            <Tooltip title={<>
                This will remove access from {sharedWithAllDataGroup ? sharedWithAllDataGroup.ruleName : "All"} group.
                {hasOnlyShareWithAllDataGroup && <>&nbsp;Removing this last data group means that NO users will have access to {projectName} from {allCompanyNames}.</>}
                {!hasOnlyShareWithAllDataGroup && <>&nbsp;This means that access is still available via other data groups.</>}
                </>}>
                <Button sx={{ height: 37, mb: 1 }} variant="contained" color="error" startIcon={<RemoveCircleOutlineIcon />} onClick={() => setShareDialogOpen(true)}>
                    Remove Share with all users
                </Button>
            </Tooltip>
        );

    }
    return (
    <>
      <HeaderBar>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}>
          <PageTitle href="/projects" title={`Manage access for`}>&nbsp;
            <Link href={project?.url}>{currentCompanyName} - {projectName}</Link>
          </PageTitle>
        </Box>
    </HeaderBar>
    <Grid container spacing={4}>
      <Grid size={{ xs: 12, md: 6 }}>
        <Paper
          sx={{ backgroundColor: "#f5f6f7", p: 2, mb: 3 }}
          elevation={0}
        >
          <Typography variant="subtitle1" sx={{ display: "flex", alignItems: "center", fontWeight: 600 }}>
            Access summary
            <InfoTooltip>The access summary shows who can see this project, based on the current sharing and data group settings.</InfoTooltip>
          </Typography>
          <Box sx={{ display: "flex", alignItems: "center", mt: 1 }}>
            <ProjectAccessStatus accessLevel={project?.userAccess} />
          </Box>
        </Paper>
      </Grid>

      {/* Sharing Options Header */}
      <Grid size={{ xs: 12 }}>
        <Box sx={{ display: "flex", alignItems: "center", mb: 1 }}>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>Sharing options</Typography>
             <Link variant="body2"
                   href="https://docs.savanta.com/internal/Content/AllVue/Managing_Access_to_Projects.html"
                   underline="hover"
                   sx={{
                        display: "flex",
                        alignItems: "center"
                   }}
                   target="_blank"
                   rel="noopener">
            How do sharing and data groups work?
            <InfoOutlinedIcon fontSize="small" sx={{ ml: 0.5 }} />
          </Link>
        </Box>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          You can share this project with everyone at your company, set up data groups for specific users with custom access, or use both options together.
        </Typography>
      </Grid>

      {explainShareWithAllUsers && 
      <Grid size={{ xs: 12, md: 6 }}>
        <Box>
          <Box sx={{ display: "flex", alignItems: "center", mb: 1 }}>
            <DomainAddOutlinedIcon sx={{ mr: 1, color: "primary.main" }} />
            <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
              Share with all users
            </Typography>
            <InfoTooltip>Every company user (now and in the future) will have full access to this project. If a user is in a data group, their group’s access rules apply instead.</InfoTooltip>
          </Box>
          <Typography variant="body2" sx={{ mb: 2 }}>
            All current and future users at{" "}
            <Box component="span" sx={{ fontWeight: 700 }}>
              { allCompanyNames }
            </Box>{" "}
            will have full access to everything in this project, unless they’re in a data group.
          </Typography>
          {renderConvertLegacyButton()}
          {renderShareButtons()}
      </Box>
      </Grid>
      }
      {!explainShareWithAllUsers &&
           renderConvertLegacyButton()
      }
      <Grid size={{ xs: 12, md: dataGroupsBoxSize }}>
        <Box sx={{ display: "flex", alignItems: "flex-end", justifyContent: "space-between", mb: 1, flexWrap: 'wrap' }}>
          <Box sx={{ mb: 1 }}>
          <Box sx={{ display: "flex", alignItems: "center", mb: 1 }}>
            <LockPersonOutlinedIcon sx={{ mr: 1, color: "primary.main" }} />
            <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
              Data groups
            </Typography>
            <InfoTooltip>Set access for chosen users by using questions and filters. A user can only be in one group, which always overrides sharing with all.</InfoTooltip>
          </Box>
            <Typography variant="body2">
              Choose who can see the project and what they can access. Grant full or limited access by questions and filters.
            </Typography>
          </Box>
            {!explainShareWithAllUsers && renderShareButtons()}
            <Button variant="outlined" startIcon={<DatasetOutlinedIcon />}
            sx={{ height: 37, mb: 1 }}
            onClick={() => { 
                      navigate(`${dataGroupEditUrl}create`)}}>
              Create data group
            </Button>
        </Box>
          { dataGroups && dataGroups.length > 0 && !dataGroupsAreLoading && 
            <DataGroupTable 
              dataGroups={dataGroups ?? []} 
              isLoading={dataGroupsAreLoading} 
              editUrl={dataGroupEditUrl} 
              companyId={project?.companyId} 
              projectId={Number(params.projectId)} 
              projectType={params.projectType as ProjectType} 
            />
          }
      </Grid>
    </Grid>

    <CustomDialog
        open={shareDialogOpen}
        title={isProjectShared() ? `Remove sharing from this project` : `Share this project with all users`}
        question={isProjectShared()? `${currentCompanyName} - ${projectName} will no longer be visible to everyone at ${allCompanyNames}.` : `${currentCompanyName} - ${projectName} will be visible to everyone at ${allCompanyNames}.`}
        confirmButtonText={isProjectShared() ? "Remove sharing" : "Share project"}
        confirmButtonColour={isProjectShared() ? "error" : "primary"}
        onCancel={() => setShareDialogOpen(false)}
        onConfirm={handleShareConfirm}
            />
    <CustomDialog
        open={legacySharedUsersDialogOpen}
        title={`Migrate legacy sharing for ${currentCompanyName}`}
        question={<>
            {`Migrate sharing of ${projectName} visible to:`}
            {project?.isShared &&
                <ul>All users of {currentCompanyName}</ul>
            }
            <ul>
                {legacyAuthUsers?.map((u, idx) => (
                    <li key={idx}>{typeof u === "string" ? u : u.name}</li>
                ))}
            </ul>
        </>}
        confirmButtonText="Migrate sharing"
        confirmButtonColour={"error"}
        onCancel={() => setLegacySharedUsersDialogOpen(false)}
        onConfirm={handleMigratingLegacySharedUsers}
    />

  </>);
};

export default ProjectAccess;