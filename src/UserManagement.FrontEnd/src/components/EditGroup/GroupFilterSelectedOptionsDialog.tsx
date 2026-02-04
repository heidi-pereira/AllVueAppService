import React from "react";
import { Box, IconButton, List, ListItem, ListItemIcon, ListItemText, ListSubheader, Typography } from "@mui/material";
import CloseIcon from '@mui/icons-material/Close';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import { ExtendedFilter } from "./GroupFiltersDialog";
import ResponsesPercentageBar from "../shared/ResponsesPercentageBar";
import NamedRowWithPercentage from './NamedRowWithPercentage';
interface GroupFilterSelectedOptionsDialogProps {
    filters: Array<ExtendedFilter>;
    onRemove: (filterIndex: number, optionIndex: number) => void;
    onClose: () => void;
    totalCountOfRespondents: number;
    filterCountOfRespondents: number;
    isLoadingFilteredCount: boolean;
    filterCountError: string;
};

const GroupFilterSelectedOptionsDialog: React.FC<GroupFilterSelectedOptionsDialogProps> = ({ filters, onRemove, onClose, totalCountOfRespondents, filterCountOfRespondents, isLoadingFilteredCount, filterCountError }) => {
    const getSelectedOptionCount = () => {
        return filters.flatMap(filter => filter.options).filter(option => option.isSelected).length;
    };

    return (
        <>
            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 2 }} onClick={onClose} role="button" aria-label="Back">
                <Box sx={{ display: "flex", justifyContent: "flex-start", alignItems: "center" }}><ChevronLeftIcon sx={{ fontSize: 16 }} /><Typography variant="backButton"> Back</Typography></Box>
            </Box>
            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 2 }}>
                <Typography variant="dropDownTitle" color="textSecondary" sx={{ fontWeight: 500 }}>
                    Filters selected
                </Typography>
                <Typography variant="body2" color="textSecondary" sx={{ fontSize: 12}}>
                    {getSelectedOptionCount()} Options
                </Typography>
            </Box>
            <List dense sx={{ maxHeight: 300, minHeight: 200, overflowY: "auto", p: 0 }}>
                {filters.filter(filter => filter.options.some(option => option.isSelected)).map(filter => {
                    return (
                        <>
                            <ListSubheader sx={{ pl: 0 }}>
                                <Typography
                                    variant="questionListTitle"
                                    color="text.secondary"
                                    fontWeight={500}
                                    sx={{ lineHeight: 1.2 }}
                                >
                                    {filter.name}
                                </Typography>
                                <Typography
                                    variant="questionListDescription"
                                    color="text.primary"
                                    fontWeight={400}
                                    sx={{ lineHeight: 1.2 }}
                                >
                                    {filter.description}
                                </Typography>
                            </ListSubheader>
                            {filter.options.map((option, idx) => {
                                if (!option.isSelected) return null;

                                return (
                                    <ListItem key={idx} disablePadding sx={{ mb: 0, pr: 0, pl: 1 }}>
                                        <ListItemText
                                            primary={
                                                <NamedRowWithPercentage
                                                    name={option.name}
                                                    percent={option.percent}
                                                />
                                                }
                                            secondary={null}
                                            sx={{ pr: 1 }}
                                        />
                                        <ListItemIcon sx={{ minWidth: 0, mr: 0}}>
                                            <IconButton onClick={() => onRemove(filter.index, idx)} aria-label={`Remove ${option.name}`} >
                                                <CloseIcon sx={{ fontSize: 12}} />
                                            </IconButton>
                                        </ListItemIcon>
                                    </ListItem>);
                            })}
                        </>
                    );
                })}
            </List>
            <Box sx={{ pt: 1, m: 1 }}>
                <ResponsesPercentageBar
                    responsesCount={filterCountOfRespondents}
                    totalCount={totalCountOfRespondents}
                    isLoading={isLoadingFilteredCount}
                    errorMessage={filterCountError}
                />
            </Box>
        </>
    );
};

export default GroupFilterSelectedOptionsDialog;