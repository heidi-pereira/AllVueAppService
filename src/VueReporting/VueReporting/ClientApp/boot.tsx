// IE11 Polyfills
import Promise from "es6-promise";
import "isomorphic-fetch";
Promise.polyfill();

// Application imports
import 'bootstrap';
import './less/site.less';
import React from 'react';
import ReactDOM from 'react-dom';
import { Route } from 'react-router-dom';
import { Router } from 'react-router-dom';
import Globals from "./globals";
import {Templates} from "./components/Templates";
import './js/es6-shim.min.js';

function renderApp(siteRoot: string, container: HTMLElement): void {
    Globals.Initialise(siteRoot);
    ReactDOM.render(
        <Router history={Globals.QueryManager.history}>
            <Route path='/templates' component={Templates} />
        </Router>,
        container
    );
}

export default renderApp;
