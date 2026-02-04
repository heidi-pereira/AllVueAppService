import React from 'react';
import { createBrowserHistory } from 'history';
import * as RoutesModule from '../routes';
import { Router } from 'react-router';
import { SurveyClient } from '../CustomerPortalApi';
import { render, screen } from '@testing-library/react';
let routes = RoutesModule.RoutesComponent;

test('renders without crashing', () => {

    // This will mock the whole exported class
    //jest.mock('../CustomerPortalApi', () => {
    //    return {
    //        SearchClient: jest.fn().mockImplementation(() => {
    //            return { getSurveys: [] };
    //        })
    //    };
    //});

    // This will mock only the one method (for each instance created)
    jest.spyOn(SurveyClient.prototype, 'getProjects').mockImplementation(() => Promise.resolve([]));

    const history = createBrowserHistory({ basename: '/' });

    render(
        <Router basename={'/'} location={'/'} navigator={history} children={routes} />,
    );

});
