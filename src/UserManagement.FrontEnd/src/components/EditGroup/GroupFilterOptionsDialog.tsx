import React from 'react';
import { useParams } from 'react-router-dom';
import { Filter, FilterOption, ExtendedFilter } from "./GroupFiltersDialog";
import { Box, Button, Checkbox, FormControlLabel, List, ListItem, ListItemIcon, ListItemText, Typography } from '@mui/material';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import ResponsesPercentageBar from '../shared/ResponsesPercentageBar';
import { buildSelectedVariablesWithOptions, useUpdateResponseFilterCount } from './filterUtils';
import NamedRowWithPercentage from './NamedRowWithPercentage';
interface GroupFilterOptionsDialogProps {
  filters: Array<ExtendedFilter>;
  filter: Filter;
  onCancel: () => void;
  onChange: (options: Array<FilterOption>) => void;
  totalCountOfRespondents: number;
  filterCountOfRespondents: number;
  isLoadingFilteredCount: boolean;
  filterCountError: string;
};

const GroupFilterOptionsDialog: React.FC<GroupFilterOptionsDialogProps> = ({ filters, filter, onCancel, onChange, totalCountOfRespondents, filterCountOfRespondents, isLoadingFilteredCount, filterCountError }) => {
  const params = useParams();
  const [firstTime, setFirstTime] = React.useState(true);
  const [options, setOptions] = React.useState(filter.options);
  const [count, setCount] = React.useState(filterCountOfRespondents);
  const [loading, setLoading] = React.useState(isLoadingFilteredCount);
  const [countError, setCountError] = React.useState(filterCountError);
  const updateResponseFilterCount = useUpdateResponseFilterCount();


  React.useEffect(() => {
      if (!firstTime) {
          updateResponseFilterCount(
              params,
              buildSelectedVariablesWithOptions(filters, filter, options),
              setCount,
              setLoading,
              setCountError,
              totalCountOfRespondents
          );
      }
      setFirstTime(false);
  }, [options, filters, filter]);

  const allOptionsSelected = () => {
    return options.every(option => option.isSelected);
  };

  const someOptionsSelected = () => {
    return !allOptionsSelected() && options.some(option => option.isSelected);
  };

  const optionsCheckboxClicked = () => {
    const updated = options.map((q) => ({ ...q, isSelected: !allOptionsSelected() }));
    setOptions(updated);
  };

  const handleOptionCheckboxClick = (index: number) => {
    const updated = options.map((q, i) =>
      i === index ? { ...q, isSelected: !q.isSelected } : { ...q }
    );
    setOptions(updated);
  };

  const selectionHasChanged = () => {
    return options.some((option, idx) => option.isSelected !== filter.options[idx].isSelected);
  };

  const handleAddClick = () => {
    if (selectionHasChanged()) {
      onChange(options);
    }
  };

  return (
    <>
      <Box sx={{ display: "flex", justifyContent: "flex-start", alignItems: "center", mb: 2 }} onClick={onCancel} role="button" aria-label="Back">
        <Box sx={{ mr: 1 }}><ChevronLeftIcon sx={{ fontSize: 16 }} /></Box>
        <Box><Typography
          variant="questionListTitle"
          color="text.secondary"
          fontWeight={500}
        >
          {filter.name}
        </Typography>
          <Typography
            variant="questionListDescription"
            color="text.primary"
            fontWeight={400}
          >
            {filter.description}
          </Typography></Box>

      </Box>
      <Box>
        <FormControlLabel control={<Checkbox checked={allOptionsSelected()} indeterminate={someOptionsSelected()}
          onClick={optionsCheckboxClicked} />} label="Options"
          sx={{ pl: 1, pr: 1, width: 12, height: 12, fontSize: 14, 'svg': { width: '0.7em', height: '0.7em' }}}/>
      </Box>
      <List dense sx={{ maxHeight: 200, overflowY: "auto", pl: 2 }}>
        {options.map((option, idx) => {
          return (
            <ListItem key={idx} disablePadding sx={{ mb: 0, pr: 0, pl: 1 }}>
              <ListItemIcon sx={{ minWidth: '0' }}>
                <Checkbox
                  edge="start"
                  tabIndex={-1}
                  disableRipple
                  checked={option.isSelected}
                  aria-label={option.name}
                  onClick={() => handleOptionCheckboxClick(idx)}
                  sx={{p: 0, pt:0.2, width: 12, height: 12, fontSize: 14, 'svg': { width: '0.7em', height: '0.7em' }}}
                />
              </ListItemIcon>
              <ListItemText
                primary={
                  <NamedRowWithPercentage
                    name={option.name}
                    percent={option.percent}
                  />
                }
                secondary={null}
                sx={{ pr: 1, pl: 1 }}
                onClick={() => { handleOptionCheckboxClick(idx) }}
              />
            </ListItem>
          );
        })}
      </List>
      <Box sx={{ pl: 2, pr: 2, pt: 1, m: 1 }}>
        <ResponsesPercentageBar
          responsesCount={count}
          totalCount={totalCountOfRespondents}
          isLoading={loading}
          errorMessage={countError}
        />
      </Box>
      <Box sx={{ display: "flex", gap: 2, mt: 3 }}>
        <Button
          variant="contained"
          color="primary"
          disabled={!selectionHasChanged()}
          onClick={handleAddClick}
        >
          Add options
        </Button>
      </Box>
    </>);
};


export default GroupFilterOptionsDialog;