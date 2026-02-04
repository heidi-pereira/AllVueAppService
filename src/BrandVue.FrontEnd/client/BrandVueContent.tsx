import React from 'react';
import {useMetricStateContext} from './metrics/MetricStateContext';
import App from './components/App';
import ProductTour from './productTour/ProductTour';
import { IBrandVueProps, IBrandVueState } from './BrandVue';
import { useLocation } from "react-router-dom";
import { useTagManager } from "./TagManagerContext";
import {useEntityConfigurationStateContext} from './entity/EntityConfigurationStateContext';
import UrlSync from "./components/helpers/UrlSync";
import { getCurrentPageInfo } from './components/helpers/PagesHelper';

type BrandVueContentProps = IBrandVueProps & IBrandVueState;

const BrandVueContent = (props: BrandVueContentProps): React.ReactElement => {
    const { enabledMetricSet } = useMetricStateContext();
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const location = useLocation();
    const tagManager = useTagManager();
    const pageHandler = props.session.pageHandler;
    const page = getCurrentPageInfo(location).page;

    React.useEffect(() => {
        if (enabledMetricSet?.metrics) {
            const metricFilters = props.session.pageHandler.getMetricFilters(enabledMetricSet, location);
            props.session.activeView.curatedFilters.initializeMeasureFilters(metricFilters, entityConfiguration);
        }
    }, [enabledMetricSet, entityConfiguration]);
    
    return <UrlSync session={props.session}>
            <App session={props.session}
             applicationConfiguration={props.applicationConfiguration}
             productConfiguration={props.productConfiguration}
             key={location.pathname}
             entityConfiguration={entityConfiguration}
             entitySetFactory={props.entitySetFactory}
             activePage={page} />
            <ProductTour productName={props.productConfiguration.productName}
                         pageHandler={pageHandler}
                         googleTagManager={tagManager} />
        </UrlSync>

}

export default BrandVueContent;