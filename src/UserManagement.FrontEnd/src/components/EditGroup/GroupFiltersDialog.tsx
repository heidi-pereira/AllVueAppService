import React from "react";
import {
  Box,
  Typography,
  List,
  ListItemText,
  IconButton,
  ListItemButton,
  Paper,
  Button,
  Skeleton
} from "@mui/material";
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import { CustomDropdownChildProps } from "../shared/CustomDropdown";
import ResponsesPercentageBar from "../shared/ResponsesPercentageBar";
import GroupFilterOptionsDialog from "./GroupFilterOptionsDialog";
import GroupFilterSelectedOptionsDialog from "./GroupFilterSelectedOptionsDialog";
import SearchInput from "../shared/SearchInput";

export interface FilterOption {
    id: number;
  name: string;
  isSelected: boolean;
  percent: number;
}

export interface Filter {
    id: number;
  name: string;
  description: string;
  options: Array<FilterOption>;
}

export interface ExtendedFilter extends Filter {
  index: number;
  searchText: string;
}

interface GroupFiltersDialogProps extends CustomDropdownChildProps {
  filters: Array<Filter>;
  totalCountOfRespondents: number;
  filterCountOfRespondents: number;
  onChange: (selectedFilters: Array<ExtendedFilter>) => void;
  isLoadingFilteredCount: boolean;
  filterCountError: string;
}

const addIndexAndSearchTextToFilters = (Filters: Array<Filter>): Array<ExtendedFilter> => {
  return Filters.map((q, idx) => ({ ...q, index: idx, searchText: `${q.name.toLowerCase()} ${q.description.toLowerCase()}` }));
};

const GroupFiltersDialog: React.FC<GroupFiltersDialogProps> = ({ filters, totalCountOfRespondents, filterCountOfRespondents, onChange, isLoadingFilteredCount, isLoading, filterCountError }) => {
  const SKELETON_ITEM_INDEX = -1;

  const amendedFilters = addIndexAndSearchTextToFilters(filters);
  const [filterText, setFilterText] = React.useState('');
  const [filterStates, setFilterStates] = React.useState(amendedFilters);
  const [showSelection, setShowSelection] = React.useState(false);
  const [selectedFilterIdx, setSelectedFilterIdx] = React.useState<number | null>(null);

  const getAllOptions = (): Array<FilterOption> => {
    return filterStates.flatMap(filter => filter.options);
  };

  const getAllSelectedOptions = (): Array<FilterOption> => {
    return getAllOptions().filter(option => option.isSelected);
  };

  const updateFilterState = (
    options: Array<FilterOption>,
  ) => {
    if (selectedFilterIdx === null) return;
    const updatedFilters = filterStates.map((filter, idx) => {
      if (idx === selectedFilterIdx) {
        return { ...filter, options: filter.options.map((option, i) => ({ ...option, isSelected: options[i].isSelected })) };
      }
      return { ...filter, options: filter.options.map(option => ({ ...option })) };
    });
    setSelectedFilterIdx(null);
    setFilterStates(updatedFilters);
    onChange(updatedFilters);
  };

  const unselectOption = (filterIndex: number, optionIndex: number) => {
    const updatedFilters = filterStates.map((filter) => ({ ...filter, options: filter.options.map((option) => ({ ...option })) }));
    updatedFilters[filterIndex].options[optionIndex].isSelected = false;
    setFilterStates(updatedFilters);
    onChange(updatedFilters);
  };

  const getListItem = (filter: ExtendedFilter) => {
    return getFullListItem(filter.index, filter.name, filter.description);
  };

  const getFullListItem = (index: number, name: string | React.ReactNode, description: string | React.ReactNode | null) => {
    return (<ListItemButton key={index} aria-label={typeof name === "string" ? name : undefined} disableRipple disabled={isLoading} onClick={() => {
        setSelectedFilterIdx(index);
      }}
        sx={{
          p: 0.5,
          '&:hover': { backgroundColor: 'transparent' }
        }}>
        <ListItemText
          primary={
            <Typography
              variant="questionListTitle"
              color="text.secondary"
              fontWeight={500}
            >
              {name}
            </Typography>
          }
          secondary={
            description && <Typography
              variant="questionListDescription"
              color="text.primary"
              fontWeight={400}
            >
              {description}
            </Typography>
          }
          sx={{ mr: 0.5 }}
        />
        <IconButton edge="end" size="small" tabIndex={-1}>
          <ChevronRightIcon fontSize="small" />
        </IconButton>
      </ListItemButton>);
  };

  const skeletonListItem = (nameWidth: number, descriptionWidth: number) => {
    const name = <Skeleton variant="text" width={nameWidth} />;
    const description = <Skeleton variant="text" width={descriptionWidth} />;
    return getFullListItem(SKELETON_ITEM_INDEX, name, description);
  };

  const skeletonListItems = () => {
    if (!isLoading) return null;
    const items = [[150, 200], [120, 250], [180, 220], [130, 230], [160, 210]];
    return items.map(item => skeletonListItem(item[0], item[1]));
  };

  return (
    <Paper sx={{ p: 2 }}>
      {selectedFilterIdx !== null ?
          <GroupFilterOptionsDialog filters={filterStates} filter={filterStates[selectedFilterIdx]} totalCountOfRespondents={totalCountOfRespondents} filterCountOfRespondents={filterCountOfRespondents} onCancel={() => setSelectedFilterIdx(null)} onChange={updateFilterState} isLoadingFilteredCount={isLoadingFilteredCount} filterCountError={filterCountError}/>
        : <>
          {showSelection ?
            <GroupFilterSelectedOptionsDialog filters={filterStates} onRemove={unselectOption} totalCountOfRespondents={totalCountOfRespondents} filterCountOfRespondents={filterCountOfRespondents} onClose={() => setShowSelection(false)} isLoadingFilteredCount={isLoadingFilteredCount} filterCountError={filterCountError} />
            :
            <>
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
              <Box
                sx={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center"
                }}
              >
                <Typography variant="dropDownTitle" color="textSecondary">
                  Filters
                </Typography>
                <Button sx={{ textTransform: "none", fontSize: 12 }} onClick={() => setShowSelection(true)} aria-label="View Selection" disabled={getAllSelectedOptions().length === 0}>
                  view {getAllSelectedOptions().length} selected
                </Button>
              </Box>

              {/* Filters List */}
              <List dense sx={{ maxHeight: 300, minHeight: 200, overflowY: "auto" }}>
                {skeletonListItems()}
                {filterStates.filter(filter => filter.searchText.includes(filterText)).map(getListItem)}
              </List>
              <Box sx={{ pt: 1 }}>
                <ResponsesPercentageBar
                  responsesCount={filterCountOfRespondents}
                  totalCount={totalCountOfRespondents}
                  isLoading={isLoadingFilteredCount||isLoading}
                  errorMessage={filterCountError}
                />
              </Box>
            </>
          } </>
      }
    </Paper>);
};

export default GroupFiltersDialog;