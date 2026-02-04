import "@Styles/main.scss";
import "@Styles/surveyPage.scss"
import "@Styles/filtersearchwrapper.scss";
import React from "react";
import { Helmet } from "react-helmet";
import 'material-symbols/outlined.css';
import moment, { Moment } from 'moment';
import TopMenu from "../components/shared/TopMenu";
import { ScrollableContainer } from "@Components/shared/ScrollableContainer";
import { useProductConfigurationContext } from "../store/ProductConfigurationContext";
import {IProject, SurveyClient} from "../CustomerPortalApi";
import {useEffect, useState} from "react";
import SurveyCard from "@Cards/SurveyCard";
import SurveyFilterSearchAndSort from "@Pages/SurveyFilterSearchAndSort";
import Loader from "@Components/shared/Loader";
import { useSearchParams } from "react-router-dom";
import { NoProjectAccessQueryParam } from "../utils";
import { Modal } from 'react-responsive-modal';
import { SortOrder, UrlParams } from "./SurveyFilterSearchAndSort";
import { GoogleTagManager } from "../util/googleTagManager";

const SurveyRows = (props: { projects: IProject[], errorForLoadingProjects?: string, hasLoadedProjects: boolean}) => {
    const { productConfiguration } = useProductConfigurationContext();
    const [searchParams, setSearchParams] = useSearchParams();

    if (!props.hasLoadedProjects) {
        return <Loader show={!props.hasLoadedProjects}/>;
    }

    if (props.errorForLoadingProjects) {
        return (<div className='error-message'>{props.errorForLoadingProjects}</div>);
    }

    if (props.hasLoadedProjects && props.projects.length === 0) {
        return (<div className='no-search-results'>No projects</div>);
    }

    const now = () => moment();

    const ranges = (completeDate: Moment) => [
        {
            dateRangeHeader: "Today", minimumDateInRange: now()
        },
        {
            dateRangeHeader: "This Week", minimumDateInRange: now().startOf('week')
        },
        {
            dateRangeHeader: "This Month", minimumDateInRange: now().startOf('month')
        },
        {
            dateRangeHeader: completeDate.format("MMMM YYYY"), minimumDateInRange: completeDate
        },
        {
            dateRangeHeader: "Over 100 years", minimumDateInRange: now().subtract({ year: 100 })
        },
    ];

    const sortingByDate = searchParams.get(UrlParams.SortOrder) !== SortOrder.Completes;
    let current = ranges(now())[0];
    let previous = current;

    const getDateHeader = (project: IProject) => {
        const getNext = (completeDate: Moment) => ranges(completeDate).find(r => completeDate >= r.minimumDateInRange);

        let header = null;

        const dateForSorting = project.launchDate;
        if (dateForSorting) {
            const date = moment(dateForSorting);

            if (current.minimumDateInRange >= date) {
                previous = current;
                current = getNext(date);

                if (current.dateRangeHeader != previous.dateRangeHeader) {
                    header = current.dateRangeHeader;
                }
            }
        }
        else {
            previous = current;
            current = { dateRangeHeader: `No launched date`, minimumDateInRange: null };

            if (current.dateRangeHeader != previous.dateRangeHeader) {
                header = current.dateRangeHeader;
            }
        }

        if (header) {
            return <div className='survey-date-header'>{header}</div>;
        }

        return "";
    }

    return <div className='survey-list-rows'>
        {
            props.projects.map(project =>
                <React.Fragment key={project.subProductId}>
                    {sortingByDate && getDateHeader(project)}
                    <SurveyCard project={project} user={productConfiguration.user} />
                </React.Fragment>)
        }
    </div>;
}

const SurveyPage = (props: { googleTagManager: GoogleTagManager }) => {
    const [projects, setProjects] = useState<IProject[]>([]);
    const [isFetchingProjects, setHasFetchedProjects] = useState(false);
    const [errorForLoadingProjects, setErrorForLoadingProjects] = useState("");
    const [filteredProjects, setFilteredProjects] = useState<IProject[]>([]);
    const [searchParams, setSearchParams] = useSearchParams();
    const showNoProjectAccessMessage = searchParams.get(NoProjectAccessQueryParam) != null;

    const hideProjectErrorMessage = () => setSearchParams([], { replace: true });

    useEffect(() => {
        props.googleTagManager.addEvent("projectsView");
        const surveyClient = new SurveyClient();
        surveyClient.getProjects().then(p => {
            if (p) {
                setProjects(p);
                setHasFetchedProjects(true);
            }
        }).catch(error => {
            setHasFetchedProjects(true);
            setErrorForLoadingProjects("Unable to load projects. Please refresh the page or contact support if the issue persists.");
            console.error(error);
        });
    }, []);

    return <>
        <Helmet>
            <title>Savanta Projects</title>
        </Helmet>

        <TopMenu />

        <div className='filter-container'>
            <SurveyFilterSearchAndSort projects={projects} googleTagManager={props.googleTagManager} setFilteredProjects={setFilteredProjects}/>
        </div>

        <ScrollableContainer className='survey-list-container'>
            <SurveyRows projects={filteredProjects} errorForLoadingProjects = {errorForLoadingProjects} hasLoadedProjects={isFetchingProjects}/>
        </ScrollableContainer>

        <Modal open={showNoProjectAccessMessage}
            onClose={() => hideProjectErrorMessage()}
            center
            showCloseIcon={true}
            closeOnOverlayClick={true}
            classNames={{ overlay: 'custom-overlay', modal: 'noProjectAccessModal', closeButton: 'custom-close-button' }}>
            <div className="title">Project not found</div>
            <div className="modal">This project may not have yet been shared, or you do not have permission to access this project.</div>
            <div className="modal">Please contact your project administrator to request access.</div>
        </Modal>

    </>;
}

export default SurveyPage
