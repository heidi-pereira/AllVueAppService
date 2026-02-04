import "@babel/polyfill";
import React from 'react';
import ReactDOM from 'react-dom';
import "@Styles/main.scss";
import Globals from "@Globals";
//
// ToDo: Remove bootstrap https://app.shortcut.com/mig-global/story/88087/remove-bootstrap-from-customerportal#
//
import 'bootstrap';
import { NSerializeJson } from "nserializejson";
import { IndicatorProvider } from "./store/indicatorsProvider";
import ErrorBoundary from "./components/shared/ErrorBoundary";
import CustomerPortalApp from "./CustomerPortalApp";
import { ProductConfigurationProvider } from "./store/ProductConfigurationContext";
import { FeatureProvider } from "./store/FeatureContext";

function setupSession() {
    Globals.reset();
};

function setupGlobalPlugins() {
    // Use dot notation in the name attributes of the form inputs.
    NSerializeJson.options.useDotSeparatorInPath = true;
};

function setupEvents() {
    document.addEventListener('DOMContentLoaded', () => {
        var preloader = document.getElementById("preloader");
        preloader.classList.add("hidden");
    });
};

function renderApp() {
    // This code starts up the React app when it runs in a browser. It sets up the routing configuration
    // and injects the app into a DOM element.
    ReactDOM.render(
        <ErrorBoundary>
            <IndicatorProvider>
                <ProductConfigurationProvider>
                    <FeatureProvider>
                        <CustomerPortalApp />
                    </FeatureProvider>
                </ProductConfigurationProvider>
            </IndicatorProvider>
        </ErrorBoundary>,
        document.getElementById('react-app')
    );
}

setupSession();

setupGlobalPlugins();

setupEvents();

renderApp();