import React from 'react';
import { Factory, PageDescriptor, RunningEnvironment, FeatureCode } from "../../BrandVueApi";
import { ProductConfiguration } from "../../ProductConfiguration";
import { Metric } from '../../metrics/metric';
import { isFeatureEnabled } from '../../components/helpers/FeaturesHelper';

export const userCanEditAbouts = (productConfiguration: ProductConfiguration) => {
    return productConfiguration.runningEnvironment == RunningEnvironment.Development ||
        productConfiguration.user.canEditMetricAbouts;
}

const getPageAbouts = async (page: PageDescriptor) => {
    const pageAboutClient = Factory.PagesClient(error => error());
    return await pageAboutClient.getPageAbouts(page.id);
}

const getMetricAbouts = async (metric: Metric) => {
    const metricAboutClient = Factory.MetricsClient(error => error());
    return await metricAboutClient.getMetricAbouts(metric.name);
}

export const pageHasPageAbouts = async (page: PageDescriptor) => {
    return page.id ? await getPageAbouts(page).then(p => p.length > 0) : false;
}

export const pageHasMetricAbouts = async (metric: Metric) => {
    return metric.name ? await getMetricAbouts(metric).then(p => p.length > 0) : false;
}

export const aboutLink = (aboutType: string, onClick: () => void) => {
    return (
        <div className="help-link" onClick={onClick}>
            {isFeatureEnabled(FeatureCode.Llm_insights) && <span className="sparkle-icon" />}
            <span className="link-text">About this {aboutType}</span>
        </div>
    );
}