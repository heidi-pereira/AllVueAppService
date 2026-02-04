import "@Styles/searchAndFilter.scss";
import React, {useEffect, useState} from "react";
import Dropdown from "@Components/Dropdown";
import SearchBar from "@Components/SearchBar";
import {IProject} from "../CustomerPortalApi";
import moment from "moment";
import {useSearchParams} from "react-router-dom";
import {useProductConfigurationContext} from "@Store/ProductConfigurationContext";
import {SAVANTA_SHORTCODE} from "@Utils";
import { isProjectShared } from "../utils";
import { ActionEventName, GoogleTagManager } from "../util/googleTagManager";
import { debounce } from 'lodash';

enum SurveyStatus {
    Live = "Live",
    All = "All",
    ClosedOrPaused = "ClosedOrPaused"
}

export enum SortOrder {
    Completes = "CompletePercentage",
    LaunchDate = "LaunchDate"
}

enum SurveyVisibility {
    All = "All",
    Shared = "Shared",
    SavantaOnly = "SavantaOnly"
}

export enum UrlParams {
    ProjectStatus = "projectStatus",
    Visibility = "visibility",
    SortOrder = "sortOrder",
}

interface ISurveyFilterSearchAndSortProps {
    projects: IProject[],
    googleTagManager: GoogleTagManager;
    setFilteredProjects: (filteredProjects: IProject[]) => void
}

const SurveyFilterSearchAndSort = (props: ISurveyFilterSearchAndSortProps) => {
    const { productConfiguration } = useProductConfigurationContext();
    const user = productConfiguration.user;
    const [searchText, setSearchText] = useState<string>("");
    const [searchParams, setSearchParams] = useSearchParams();
    const shouldShowVisibilityToggle = user?.authCompany !== SAVANTA_SHORTCODE && user?.userCompanyShortCode === SAVANTA_SHORTCODE;
    const surveyGroupSurveyIds = new Set(props.projects.flatMap(p => p.childSurveysIds));

    const statusParam = searchParams.get(UrlParams.ProjectStatus);
    const visibilityParam = searchParams.get(UrlParams.Visibility);
    const sortOrderParam = searchParams.get(UrlParams.SortOrder);
    const status = Object.values(SurveyStatus).find(value => value === statusParam) ?? SurveyStatus.All;
    const visibility = Object.values(SurveyVisibility).find(value => value === visibilityParam) ?? SurveyVisibility.All;
    const sortOrder = Object.values(SortOrder).find(value => value === sortOrderParam) ?? SortOrder.LaunchDate;
    const debouncedTrackSearch = React.useMemo(() => debounce(() => props.googleTagManager.addEvent('projectsSearch'), 1000), [props.googleTagManager]);

    const setStatusUrl = (surveyStatus: SurveyStatus) => {
        searchParams.set(UrlParams.ProjectStatus, surveyStatus);
        setSearchParams(searchParams);
        props.googleTagManager.addEvent(getStatusEventName(surveyStatus));
    }

    const getStatusEventName = (surveyStatus: SurveyStatus): ActionEventName => {
        switch (surveyStatus) {
            case SurveyStatus.Live: return 'projectsFilterLive';
            case SurveyStatus.ClosedOrPaused: return 'projectsFilterClosed';
            case SurveyStatus.All: return 'projectsFilterAll';
        }
    }

    const setVisibilityUrl = (surveyVisibility: SurveyVisibility) => {
        searchParams.set(UrlParams.Visibility, surveyVisibility);
        setSearchParams(searchParams);
        props.googleTagManager.addEvent(getVisibilityEventName(surveyVisibility));
    }

    const getVisibilityEventName = (surveyVisibility: SurveyVisibility): ActionEventName => {
        switch (surveyVisibility) {
            case SurveyVisibility.All: return 'projectsVisibilityAll';
            case SurveyVisibility.Shared: return 'projectsVisibilityShared';
            case SurveyVisibility.SavantaOnly: return 'projectsVisibilitySavanta';
        }
    }

    const setSortOrderUrl = (sortOrder: SortOrder) => {
        searchParams.set(UrlParams.SortOrder, sortOrder);
        setSearchParams(searchParams);
        props.googleTagManager.addEvent(getSortOrderEventName(sortOrder));
    }

    const getSortOrderEventName = (sortOrder: SortOrder): ActionEventName => {
        switch (sortOrder) {
            case SortOrder.LaunchDate: return 'projectsSortLaunchDate';
            case SortOrder.Completes: return 'projectsSortCompletion';
        }
    }

    const updateSearchText = (text: string) => {
        setSearchText(text);
        debouncedTrackSearch();
    }

    const filterByStatus = (projects: IProject []) => {
        switch (status){
            case SurveyStatus.Live: return projects.filter(p => p.isOpen);
            case SurveyStatus.ClosedOrPaused: return projects.filter(p => p.isClosed || p.isPaused);
            default: return projects;
        }
    }

    const filterByVisibility = (projects: IProject []) => {
        switch (visibility){
            case SurveyVisibility.Shared: return projects.filter(p => isProjectShared(p));
            case SurveyVisibility.SavantaOnly: return projects.filter(p => !isProjectShared(p));
            default: return projects;
        }
    }

    const sortBySortOrder = (projects: IProject []) => {
        switch (sortOrder){
            case SortOrder.Completes: return [...projects].sort((p1, p2) => p2.percentComplete - p1.percentComplete);
            case SortOrder.LaunchDate: return [...projects].sort((p1, p2) => dateSortOrdering(p1.launchDate, p2.launchDate));
        }
    }

    const dateSortOrdering = (date1: Date | undefined, date2: Date | undefined): number => {
        if (date1 && date2) {
            return date2.getTime() - date1.getTime();
        }
        if (date2) return 1;
        if (date1) return -1;
        return 0;
    }

    const filterBySearch = (projects: IProject []) => {
        return projects.filter(p => isSearchMatch(p, searchText))
    }

    const isSearchMatch = (project: IProject, searchText: string) => {
        const trimmedText = searchText.toLocaleLowerCase().trim();
        return doesContainSearchText(project.name, trimmedText) ||
            doesContainSearchText(project.subProductId, trimmedText) ||
            (project.launchDate && doesContainSearchText(`Launched ${moment(project.launchDate).format("DD MMM YYYY")}`, trimmedText)) ||
            (project.completeDate && doesContainSearchText(`Closed ${moment(project.completeDate).format("DD MMM YYYY")}`, trimmedText)) ||
            doesContainSearchText(`${project.percentComplete}% complete`, trimmedText) ||
            doesContainSearchText(`${project.complete.toLocaleString()} responses`, trimmedText);
    }

    const doesContainSearchText = (str: string, searchText: string) => {
        return str.toLocaleLowerCase().includes(searchText);
    }

    useEffect(() => {
        let filteredProjects = filterByStatus(props.projects)
        filteredProjects = filterByVisibility(filteredProjects)
        filteredProjects = filterBySearch(filteredProjects)
        filteredProjects = sortBySortOrder(filteredProjects)
        props.setFilteredProjects(filteredProjects)
    },[props.projects, status, visibility, sortOrder, searchText]);

    const getSurveyStatusDisplayName = (surveyStatus: SurveyStatus) => {
        switch (surveyStatus) {
            case SurveyStatus.Live: return "Live surveys";
            case SurveyStatus.ClosedOrPaused: return "Closed or paused surveys";
            case SurveyStatus.All: return "All surveys";
        }
    }

    const getSurveyVisibilityDisplayName = (surveyVisibility: SurveyVisibility) => {
        switch (surveyVisibility) {
            case SurveyVisibility.SavantaOnly: return "Savanta only";
            default: return surveyVisibility;
        }
    }

    const getSortByDisplayName = (sortOrder: SortOrder) => {
        switch (sortOrder) {
            case SortOrder.LaunchDate: return "Launch date";
            case SortOrder.Completes: return "% complete";
        }
    }

    return (
        <div className="search-filter-sort">
            <SearchBar search={searchText} setSearch={updateSearchText} autoFocus={true}/>
            <div className="buttons">
                <div className="left-buttons">
                    <Dropdown buttonTitle={"Status"}
                        options={[SurveyStatus.Live, SurveyStatus.ClosedOrPaused, SurveyStatus.All]}
                        getDisplayText={getSurveyStatusDisplayName}
                        selectedOption={status}
                        setSelectedOption={setStatusUrl}/>
                    {shouldShowVisibilityToggle &&
                        <Dropdown buttonTitle={"Visibility"}
                            defaultValue={SurveyVisibility.All}
                            options={[SurveyVisibility.Shared, SurveyVisibility.SavantaOnly]}
                            getDisplayText={getSurveyVisibilityDisplayName}
                            selectedOption={visibility}
                            setSelectedOption={setVisibilityUrl}/>}
                </div>
                <div className="right-buttons">
                    <Dropdown buttonTitle={"Sort by"}
                        options={[SortOrder.LaunchDate, SortOrder.Completes]}
                        getDisplayText={getSortByDisplayName}
                        selectedOption={sortOrder}
                        setSelectedOption={setSortOrderUrl}/>
                </div>
            </div>
        </div>
    );
}

export default SurveyFilterSearchAndSort;
