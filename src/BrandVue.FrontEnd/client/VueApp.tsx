import 'whatwg-fetch';
import React from 'react';
import 'moment/locale/en-gb';
import 'material-symbols/outlined.css';
import 'material-icons/iconfont/material-icons.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap';
import "../less/main.less";
import Throbber from "./components/throbber/Throbber";
import { ProductConfiguration } from './ProductConfiguration';
import ErrorDisplay from './components/ErrorsDisplay';
import VueRouter from './components/app/VueRouter';
import { InnerErrorReport } from './components/InnerErrorReport';
import { MixPanel } from './components/mixpanel/MixPanel';
import { MixPanelClientTest } from './components/mixpanel/MixPanelClientTest';
import { MixPanelClient } from './components/mixpanel/MixPanelClient';
import { MixPanelModel } from './components/mixpanel/MixPanelHelper';
import { store } from "./state/store";
import { Provider } from 'react-redux';
import InitialStorePopulator from './components/InitialStorePopulator';

function InitialiseMixPanel(productConfiguration: ProductConfiguration) {
    var token = productConfiguration.isSurveyVue() ? productConfiguration.allVueMixpanelToken : productConfiguration.brandVueMixpanelToken;
    const client = token ? new MixPanelClient() : new MixPanelClientTest();
    
    const mixPanelModelInstance: MixPanelModel = {
        userId: productConfiguration.user?.userId,
        projectId: token,
        client: client,
        isAllVue: productConfiguration.isSurveyVue(),
        productName: productConfiguration.productName,
        project: productConfiguration.subProductId,
        kimbleProposalId: productConfiguration.kimbleProposalId,
    };

    MixPanel.init(mixPanelModelInstance);
}

const VueApp = ({callback}) => {
    const [productConfiguration, setProductConfiguration] = React.useState<ProductConfiguration>();
    const [hasError, setHasError] = React.useState<boolean>(false);
    React.useEffect(() => {
        const fetchProductConfiguration = async () => {
            const p = await ProductConfiguration.getAsync();
            setProductConfiguration(p);
        }
        fetchProductConfiguration().catch(r => setHasError(true));
    }, []);

    const title = "There has been a problem loading the dashboard, we’re aware and are working to get it back online. Please try again in a few minutes.";
    const message = "In the meantime if you need any support, please get in touch with BV.Support@savanta.com";
    if (hasError)
        return <ErrorDisplay title={title} message={message} />;

    if (!productConfiguration) {
        return <div id="ld" className="loading-container"><Throbber /></div>;
    }

    InitialiseMixPanel(productConfiguration);
    MixPanel.setPeople(productConfiguration.user);

    const surveyIds = productConfiguration.nonMapFileSurveys.map(x => x.surveyId);
    const surveyNames = productConfiguration.nonMapFileSurveys.map(x => x.surveyName);
    MixPanel.trackSurvey(surveyIds, surveyNames, productConfiguration.subProductId, productConfiguration.surveyUid);

    return (
        <Provider store={store}>
            <InitialStorePopulator />
            <InnerErrorReport childInfo={{ "Component": "VueRouter" }}>
                <VueRouter productConfiguration={productConfiguration} />
            </InnerErrorReport>
        </Provider>
    );
}

export default VueApp;
