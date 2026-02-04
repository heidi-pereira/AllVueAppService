import React from "react";
import {
  Box,
  Typography,
  Checkbox,
  FormControlLabel,
  List,
  ListItemText,
  ListItemIcon,
  Button,
  Paper,
  IconButton,
  Skeleton,
  ListItemButton
} from "@mui/material";
import CloseIcon from '@mui/icons-material/Close';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import ResponsesPercentageBar from "../shared/ResponsesPercentageBar";
import {CustomDropdownChildProps} from "../shared/CustomDropdown";
import SearchInput from "../shared/SearchInput";
import NamedRowWithPercentage from './NamedRowWithPercentage';
export interface Question {
  id: number;
  title: string;
  description: string;
  percent: number;
  isSelected: boolean;
  numberOfResponses: number;
  isHiddenInAllVue: boolean;
}

interface ExtendedQuestion extends Question {
  index: number;
  searchText: string;
}

interface GroupQuestionsDialogProps extends CustomDropdownChildProps {
  questions: Array<Question>;
  onCancel?: () => void;
  onChange?: (selectedQuestions: Array<{ index: number; isSelected: boolean }>) => void;
}

const addIndexAndSearchTextToQuestions = (questions: Array<Question>): Array<ExtendedQuestion> => {
  return questions.map((q, idx) => ({ ...q, index: idx, searchText: `${q.title.toLowerCase()} ${q.description.toLowerCase()}` }));
};

const GroupQuestionsDialog: React.FC<GroupQuestionsDialogProps> = ({ questions, onCancel, onChange, onClose, isLoading }) => {
  const SKELETON_ITEM_INDEX = -1;

  const [questionStates, setQuestionStates] = React.useState([] as Array<ExtendedQuestion>);
  const [originalSelection, setOriginalSelection] = React.useState([] as Array<ExtendedQuestion>);
  const [showSelection, setShowSelection] = React.useState(false);
  const [filterText, setFilterText] = React.useState("");
  
  React.useEffect(() => {
    const amendedQuestions = addIndexAndSearchTextToQuestions(questions);
    setOriginalSelection(getSelectedQuestionsForCompare(amendedQuestions));
    setQuestionStates(amendedQuestions);
  }, [questions]);

  const allQuestionsSelected = () => {
    return questionStates.length > 0 && questionStates.every((q) => q.isSelected);
  };

  const someQuestionsSelected = () => {
    return !allQuestionsSelected() && questionStates.some((q) => q.isSelected);
  };

  const questionsCheckboxClicked = () => {
    const updated = questionStates.map((q) => ({ ...q, isSelected: !allQuestionsSelected() }));
    setQuestionStates(updated);
  };

  function getSelectedQuestionsForCompare(questions: Array<ExtendedQuestion>): Array<ExtendedQuestion> {
    return questions.filter(q => q.isSelected);
  };

  const selectionHasChanged = () => {
    const selectedQuestions = getSelectedQuestionsForCompare(questionStates);
    if (selectedQuestions.length !== originalSelection.length) return true;
    if (!selectedQuestions.every((q, idx) => q.index === originalSelection[idx].index)) return true;
    return false;
  };
    const totalCountThatWillComeFromAPI = () => {
        if (questionStates.length === 0) {
            return 0;
        }
        return Math.max(...questionStates.map(q => q.numberOfResponses));
    }
    const responseCountThatWillComeFromAPI = () => {
        const filteredQuestions = questionStates.filter(q => q.isSelected);
        if (filteredQuestions.length === 0) {
            return totalCountThatWillComeFromAPI();
        }

        return Math.max(...filteredQuestions.map(q => q.numberOfResponses));
    }

  const handleQuestionCheckboxClick = (idx: number) => {
    const updated = questionStates.map((q, i) =>
      i === idx ? { ...q, isSelected: !q.isSelected } : { ...q }
    );
    setQuestionStates(updated);
  };

  const handleAddClick = () => {
    if (onChange) {
      onChange(questionStates);
    }
    onClose();
  };

  const handleCancelClick = () => {
    if (onCancel) {
      onCancel();
    }
    onClose();
  };

  const getListItem = (q: ExtendedQuestion) => {
    if (showSelection && !q.isSelected) return null;
      return getFullListItem(q.index, q.title, q.description, q.percent, q.isSelected, false, q.isHiddenInAllVue);
  };

    const getFullListItem = (index: number, title: string | React.ReactNode, description: string | React.ReactNode | null, percent: number | React.ReactNode, isSelected: boolean, isDisabled: boolean, isHiddenInAllVue: boolean) => {
    return (
      <ListItemButton key={index} aria-label={typeof title === "string" ? title : undefined} alignItems="flex-start" disableRipple disablePadding sx={{ p: showSelection ? 0 : 1, pt: 0, pb: 0, mb: 0,
          '&:hover': { backgroundColor: 'transparent' } }}
      disabled={isLoading} onClick={() => {
        handleQuestionCheckboxClick(index);
      }}>
        {!showSelection && <ListItemIcon sx={{ minWidth: 0, mr: 1 }}>
          <Checkbox
            edge="start"
            tabIndex={-1}
            disableRipple
            checked={isSelected}
            disabled={isDisabled}
            aria-label={typeof title === "string" ? title : undefined}
            onClick={() => handleQuestionCheckboxClick(index)}
            sx={{p: 0, pt: 0.2, width: 12, height: 12, 'svg': { width: '0.7em', height: '0.7em' }}}
          />
        </ListItemIcon>
        }
        <ListItemText
          primary={
            <NamedRowWithPercentage
                name={isHiddenInAllVue ? `${title} (Question is hidden in AllVue)` : title}
                percent={percent}
            />

          }
          secondary={
            description && (
                  <Typography variant="questionListDescription"
                      color={isHiddenInAllVue ? "text.disabled" : "text.primary"}
                      sx={isHiddenInAllVue ? { opacity: 0.7 } : undefined} >
                {description}
              </Typography>
            )
          }
          sx={{ pr: 1 }}
          onClick={() => { if (!showSelection && !isLoading) handleQuestionCheckboxClick(index) }}
        />
        {showSelection && <ListItemIcon sx={{ minWidth: 0, mr: 1 }}>
          <IconButton disableRipple disableTouchRipple onClick={() => handleQuestionCheckboxClick(index)} sx={{p: 0}}>
            <CloseIcon sx={{ width: 12, height: 12 }} />
          </IconButton>
        </ListItemIcon>
        }
      </ListItemButton>
    );
  }

  const skeletonListItem = (titleWidth: number, descriptionWidth: number) => {
    const title = <Skeleton variant="text" width={titleWidth} />;
    const description = <Skeleton variant="text" width={descriptionWidth} />;
    const percent = <Skeleton variant="text" width={30} />;
    return getFullListItem(SKELETON_ITEM_INDEX, title, description, percent, false, true, false);
  };

  const skeletonListItems = () => {
    if (!isLoading) return null;
    const items = [[150, 200], [120, 250], [180, 220], [130, 230], [160, 210]];
    return items.map(item => skeletonListItem(item[0], item[1]));
  };

  return (
    <>
    <Paper sx={{ p: 2}}>
      { showSelection ?
        <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 2 }} onClick={() => setShowSelection(false)}>
          <Box sx={{ display: "flex", justifyContent:"flex-start", alignItems: "center" }}><ChevronLeftIcon sx={{fontSize: 16}} /><Typography variant="backButton"> Back</Typography></Box>
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
      { showSelection ?
            <>
            <Typography variant="body2" color="textSecondary">
              Selected questions
            </Typography>
            <Typography variant="body2" color="textSecondary" sx={{ fontSize: 12 }}>
              {questionStates.filter(q => q.isSelected).length} questions
            </Typography>
            </>
          :
            <>
      <FormControlLabel control={<Checkbox checked={allQuestionsSelected()} indeterminate={someQuestionsSelected()} 
        onClick={questionsCheckboxClicked}/>} disabled={isLoading} label="Questions" 
            sx={{p: 0, pt:0.2, width: 12, height: 12, fontSize: 14, 'svg': { width: '0.7em', height: '0.7em' }}} />
        <Button sx={{ textTransform: "none", fontSize: 12 }} disableRipple disableFocusRipple disableTouchRipple onClick={() => setShowSelection(true)}>
          view {questionStates.filter(q => q.isSelected).length} / {questionStates.length} 
        </Button>
        </>
      }
      </Box>
      <List dense sx={{ maxHeight: 200, overflowY: "auto", pl:1 }}>
        {skeletonListItems()}
        {questionStates.filter(q => q.searchText.includes(filterText)).map(getListItem)}
      </List>
      <Box sx={{ px: 0.5, mt: 1, pl: 3, pr: 3 }}>
        <ResponsesPercentageBar
          responsesCount={responseCountThatWillComeFromAPI()}
          totalCount={totalCountThatWillComeFromAPI()}
        />
      </Box>
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

export default GroupQuestionsDialog;