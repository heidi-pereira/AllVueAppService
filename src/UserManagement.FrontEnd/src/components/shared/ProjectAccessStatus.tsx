import { AccessStatus } from "@/orval/api/models";
import WarningOutlinedIcon from '@mui/icons-material/WarningOutlined';
import CircleIcon from '@mui/icons-material/Circle';
import LockPersonIcon from '@mui/icons-material/LockPerson';
import { Typography } from "@mui/material";

interface ProjectAccessStatusProps {
  accessLevel?: AccessStatus,
  includeDescription?: boolean;
}

const ProjectAccessStatus: React.FC<ProjectAccessStatusProps> = ({ accessLevel, includeDescription }) => {
  const getIcon = (access?: AccessStatus) => {
    switch (access) {
      case AccessStatus.AllUsers:
        return <CircleIcon fontSize="small" color="success" sx={{ mr: 1 }} />;
      case AccessStatus.Mixed:
        return <CircleIcon fontSize="small" color="warning" sx={{ mr: 1 }} />;
      case AccessStatus.Restricted:
        return <LockPersonIcon fontSize="small" color="primary" sx={{ mr: 1 }} />;
      default:
        return <WarningOutlinedIcon fontSize="small" color="warning" sx={{ mr: 1 }} />;
    }
  };

  const getTitle = (access?: AccessStatus): string => {
    switch (access) {
      case AccessStatus.AllUsers:
        return "All users";
      case AccessStatus.Mixed:
        return "Mixed";
      case AccessStatus.Restricted:
        return "Restricted";
      default:
        return "None";
    }
  };

  const getDescription = (access?: AccessStatus): string => {
    switch (access) {
      case AccessStatus.AllUsers:
        return "Everyone at your company can access this project.";
      case AccessStatus.Mixed:
        return "Project is shared with all users, but data group members are restricted to their group’s permissions.";
      case AccessStatus.Restricted:
        return "Only users in a data group can access, based on group permissions.";
      default:
        return "No access has been set up for this project yet.";
    }
  };

  const showDescription = includeDescription ?? true;

  return (
    <>
        {getIcon(accessLevel)}
        {showDescription
          ? <>
            <Typography variant="body2" sx={{ fontWeight: 500, mr: 1 }}>{getTitle(accessLevel)}</Typography>
            <Typography variant="body2" color="text.secondary" fontStyle="italic">
                {getDescription(accessLevel)}
            </Typography>
          </>
          : <Typography variant="body2" sx={{ mr: 1 }}>{getTitle(accessLevel)}</Typography>
        }
    </>
  );
};

export default ProjectAccessStatus;