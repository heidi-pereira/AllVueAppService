import AuthorizedLayout from '@Layouts/AuthorizedLayout';
import React from 'react';
import { Routes, Outlet } from 'react-router-dom';
import SurveyPage from '@Pages/SurveyPage';
import ProjectDetailsContainerPage from "@Pages/ProjectDetailsContainerPage";
import {Route} from "react-router";
import SurveyQuotasPage from "@Pages/SurveyQuotasPage";
import SurveyDocumentsPage from "@Pages/SurveyDocumentsPage";
import SurveyGroupStatusPage from "@Pages/SurveyGroupStatusPage";
import { NavLink } from 'react-router-dom';
import { GoogleTagManager } from './util/googleTagManager';
import withPerformanceMeasurement from './WithPerformanceMeasurement';

export const RoutesComponent = (props: {googleTagManager: GoogleTagManager}) => {
    const survey = 'Survey';
    const quotas = 'Quotas';
    const documents = 'Documents';
    const status = 'Status';
    const SurveyPagePerf = withPerformanceMeasurement(SurveyPage, survey); 
    const QuotasPagePerf = withPerformanceMeasurement(SurveyQuotasPage, quotas); 
    const DocumentsPagePerf = withPerformanceMeasurement(SurveyDocumentsPage, documents); 
    const StatusPagePerf = withPerformanceMeasurement(SurveyGroupStatusPage, status); 
    return <Routes>
        <Route element={<AuthorizedLayout><Outlet /></AuthorizedLayout>}>
            <Route index element={<SurveyPagePerf googleTagManager={props.googleTagManager} />} />
            <Route path="Survey" element={<ProjectDetailsContainerPage/>}>
                <Route path="Quotas/:id" element={<QuotasPagePerf googleTagManager={props.googleTagManager} />}/>
                <Route path="Documents/:id" element={<DocumentsPagePerf googleTagManager={props.googleTagManager} />}/>
                <Route path="Status/:id" element={<StatusPagePerf googleTagManager={props.googleTagManager} />}/>
            </Route>
        </Route>
    </Routes>;
};

export const NavLinkExt = (props: {to: string, children: React.ReactNode}) => {
    return props.to.toLowerCase().startsWith('http')
        ? <a href={props.to}>{props.children}</a>
        : <NavLink to={props.to}>{props.children}</NavLink>;
}