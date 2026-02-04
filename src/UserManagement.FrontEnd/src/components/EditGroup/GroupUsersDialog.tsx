import React from "react";
import {
  Box,
  Typography,
  Checkbox,
  FormControlLabel,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Button,
  Paper,
  IconButton,
  Skeleton
} from "@mui/material";
import CloseIcon from '@mui/icons-material/Close';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import {CustomDropdownChildProps} from "../shared/CustomDropdown";
import { DataGroup } from '@/rtk/apiSlice';
import SearchInput from "../shared/SearchInput";

export interface User {
  id: string;
  name: string;
  email: string;
  isSelected: boolean;
}

interface ExtendedUser extends User {
  index: number;
  searchText: string;
}

interface GroupUsersDialogProps extends CustomDropdownChildProps {
  currentDataGroup: number;
  users: Array<User>;
  lookupUserToDataGroup: Record<string, DataGroup>;
  onCancel?: () => void;
  onChange?: (selectedUsers: Array<{ index: number; isSelected: boolean }>) => void;
}

const addIndexAndSearchTextToUsers = (Users: Array<User>): Array<ExtendedUser> => {
  return Users.map((q, idx) => ({ ...q, index: idx, searchText: `${q.name.toLowerCase()} ${q.email.toLowerCase()}` }));
};

const GroupUsersDialog: React.FC<GroupUsersDialogProps> = ({ currentDataGroup, users, lookupUserToDataGroup, onCancel, onChange, onClose, isLoading }) => {
  const SKELETON_ITEM_INDEX = -1;
  const [userStates, setUserStates] = React.useState([] as Array<ExtendedUser>);
  const [originalSelection, setOriginalSelection] = React.useState([] as Array<ExtendedUser>);
  const [showSelection, setShowSelection] = React.useState(false);
  const [filterText, setFilterText] = React.useState("");

  React.useEffect(() => {
    const amendedUsers = addIndexAndSearchTextToUsers(users);
    setOriginalSelection(getSelectedUsersForCompare(amendedUsers));
    setUserStates(amendedUsers);
  }, [users]);

  const allUsersSelected = () => {
    return userStates.length > 0 && userStates.every((q) => q.isSelected);
  };

  const someUsersSelected = () => {
    return !allUsersSelected() && userStates.some((q) => q.isSelected);
  };

  const usersCheckboxClicked = () => {
    const updated = userStates.map((q) => ({ ...q, isSelected: !allUsersSelected() }));
    setUserStates(updated);
  };

  function getSelectedUsersForCompare(users: Array<ExtendedUser>): Array<ExtendedUser> {
    return users.filter(q => q.isSelected);
  };

  const selectionHasChanged = () => {
    const selectedUsers = getSelectedUsersForCompare(userStates);
    if (selectedUsers.length !== originalSelection.length) return true;
    if (!selectedUsers.every((q, idx) => q.index === originalSelection[idx].index)) return true;
    return false;
  };

  const handleUserCheckboxClick = (idx: number) => {
    const updated = userStates.map((q, i) =>
      i === idx ? { ...q, isSelected: !q.isSelected } : { ...q }
    );
    setUserStates(updated);
  };

  const handleAddClick = () => {
    if (onChange) {
      onChange(userStates);
    }
    onClose();
  };

  const handleCancelClick = () => {
    if (onCancel) {
      onCancel();
    }
    onClose();
  };

  const getListItem = (q: ExtendedUser) => {
    if (showSelection && !q.isSelected) return null;
    const dataGroup = lookupUserToDataGroup ? lookupUserToDataGroup[q.id] : undefined;
    const nameOfOtherDataGroupUserIsAssignedTo = dataGroup && dataGroup.id !== currentDataGroup ? dataGroup.ruleName : undefined;
    return getFullListItem(q.index, q.isSelected, q.name, q.email, nameOfOtherDataGroupUserIsAssignedTo, false);
  };

  const getFullListItem = (index: number, isSelected: boolean, name: string | React.ReactNode, email: string | React.ReactNode, currentDataGroupName: string | undefined, isDisabled: boolean) => {
    return (
    <ListItem key={index} disableRipple disablePadding sx={{ p: showSelection ? 0 : 2, m: 0, pt: 0, pb: 0, mb: 0,
          '&:hover': { backgroundColor: 'transparent' } }}>
      {!showSelection && <ListItemIcon
          sx={{ minWidth: '0' }}>
        <Checkbox
          edge="start"
          tabIndex={-1}
          disableRipple
          disabled={isDisabled}
          checked={isSelected}
          aria-label={typeof name === "string" ? name : undefined}
          onClick={() => handleUserCheckboxClick(index)}
          sx={{p: 0, pt: 0.2, width: 12, height: 12, 'svg': { width: '0.7em', height: '0.7em' }}}
        />
      </ListItemIcon>
      }
      <ListItemText
        primary={
          <Box display="flex" alignItems="baseline" gap={2}>
            <Typography variant="userListName">
              {name}
            </Typography>
            <Typography variant="userListEmail">
              {email}
            </Typography>
            {currentDataGroupName &&
              <Typography variant="dataGroupCurrentlyIn" sx={{ ml: "auto" }}>
                currently in <b>{currentDataGroupName}</b>
              </Typography>
            }
          </Box>
        }
        secondary={null}
        sx={{ pr: 1, pl: 1 }}
        onClick={() => { if (!showSelection && !isDisabled) handleUserCheckboxClick(index) }}
      />
      {showSelection && <ListItemIcon sx={{ minWidth: 0}}>
        <IconButton onClick={() => { if (!isDisabled) handleUserCheckboxClick(index) }}>
          <CloseIcon sx={{ fontSize: 12}} />
        </IconButton>
      </ListItemIcon>
      }
    </ListItem>
    );
  };

  const skeletonListItem = (nameWidth: number, emailWidth: number) => {
    const name = <Skeleton variant="text" width={nameWidth} />;
    const email = <Skeleton variant="text" width={emailWidth} />;
    return getFullListItem(SKELETON_ITEM_INDEX, false, name, email, undefined, true);
  };

  const skeletonListItems = () => {
    if (!isLoading) return null;
    const items = [[150, 200], [120, 250], [180, 220], [130, 230], [160, 210]];
    return items.map(item => skeletonListItem(item[0], item[1]));
  }

  return (
    <>
    <Paper sx={{ p: 2}}>
      { showSelection ?
        <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 2 }} onClick={() => setShowSelection(false)}>
          <Box sx={{ display: "flex", justifyContent:"flex-start", alignItems: "center" }}><ChevronLeftIcon sx={{fontSize: 16}} /><Typography variant="backButton"> Back</Typography></Box>
            <Typography variant="body2" color="textSecondary" sx={{ fontSize: 12 }}>
              {userStates.filter(q => q.isSelected).length} User{userStates.filter(q => q.isSelected).length == 1 ? "" : "s"}
            </Typography>
        </Box>
        :
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          borderBottom: 1,
          borderColor: "divider",
          pb: 1,
          pl: 2,
          pr: 2,
          ml: -2,
          mr: -2
        }}
      >
        <SearchInput onChange={setFilterText} value={filterText}/>
      </Box>
}
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center"
        }}
      >

      { !showSelection &&
            <>
      <FormControlLabel control={<Checkbox checked={allUsersSelected()} indeterminate={someUsersSelected()} 
        onClick={usersCheckboxClicked} />} disabled={isLoading} label="Users" 
        sx={{ p: 0, pl: 0.5, pt:0.2, width: 12, height: 12, fontSize: 14, 'svg': { width: '0.7em', height: '0.7em' }}} />
        <Button sx={{ textTransform: "none", fontSize: 12 }} onClick={() => setShowSelection(true)}>
          view {userStates.filter(q => q.isSelected).length} / {userStates.length} 
        </Button>
        </>
      }
      </Box>
      <List dense sx={{ maxHeight: 200, overflowY: "auto" }}>
      {skeletonListItems()}
      {userStates.filter(q => q.searchText.includes(filterText)).map(getListItem)}
      </List>
      {!showSelection &&
      <Box sx={{ display: "flex", gap: 1, mt: 3 }}>
        <Button
          variant="contained"
          color="cancel"
          onClick={handleCancelClick}
        >
          Cancel
        </Button>
        <Button
          variant="contained"
          color="primary"
          disabled={!selectionHasChanged()}
          onClick={handleAddClick}
        >
          Update
        </Button>
      </Box>
    }
    </Paper>
    </>
  );
};

export default GroupUsersDialog;