import "@babel/polyfill";
import React from 'react';
import { createBrowserHistory } from 'history';
import { BrowserRouter } from "react-router-dom";
import { RoutesComponent } from './routes';
import "@Styles/main.scss";
import { useProductConfigurationContext } from "./store/ProductConfigurationContext";
import { GoogleTagManager } from "./util/googleTagManager";
import Loader from "@Components/shared/Loader";
import { MixPanel } from "./mixpanel/MixPanel";
import { MixPanelModel } from "./mixpanel/MixPanelHelper";
import { MixPanelClient } from "./mixpanel/MixPanelClient";
import { MixPanelClientTest } from "./mixpanel/MixPanelClientTest";

const CustomerPortalApp = () => {
    const { productConfiguration } = useProductConfigurationContext();

    // Create browser history to use in the Redux store.
    const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href')!;
    const history = createBrowserHistory({ basename: baseUrl });
    const tagManager = new GoogleTagManager(history, productConfiguration);

    var token = productConfiguration?.mixPanelToken;
    var client = token ? new MixPanelClient() : new MixPanelClientTest();
    const mixPanelModelInstance: MixPanelModel = {
        userId: productConfiguration?.user.userId!,
        projectId: token,
        client: client,
        productName: "survey"
    };
    MixPanel.init(mixPanelModelInstance);
    MixPanel.setPeople(productConfiguration?.user);

    if (!productConfiguration) {
        return <Loader show={true} />
    }
    return (
        <BrowserRouter basename={baseUrl}>
            <RoutesComponent googleTagManager={tagManager} />
        </BrowserRouter>
    )
}

export default CustomerPortalApp;
