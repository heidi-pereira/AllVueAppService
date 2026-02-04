import React from 'react';
import { Box, Button } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import DatasetRoundedIcon from '@mui/icons-material/DatasetRounded';
import PeopleOutlineRoundedIcon from '@mui/icons-material/PeopleOutlineRounded';
import PageTitle from './shared/PageTitle';

interface UserTableHeaderProps {
    selectedCompany: string;
    disabledMangeData?: boolean;
}
const UserTableHeader: React.FC<UserTableHeaderProps> = ({ selectedCompany, disabledMangeData }) => {
  const navigate = useNavigate();

  return (
    <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <PageTitle title={`User Management ${selectedCompany || ""}`} />
      <Box>
        <Button
          startIcon={<DatasetRoundedIcon />}
          variant="outlined"
          color="primary"
          sx={{ mr: 1, textTransform: 'none' }}
          onClick={() => navigate('/projects')}
          disabled={disabledMangeData}
        >
          Manage Projects
        </Button>
        <Button
          startIcon={<PeopleOutlineRoundedIcon />}
          sx={{ textTransform: 'none' }}
          variant="outlined"
          color="primary"
          onClick={() => navigate('/manageroles')}
        >
          Manage Roles
        </Button>
      </Box>
    </Box>
  );
};

export default UserTableHeader;
