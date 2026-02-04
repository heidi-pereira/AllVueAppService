import React from "react";
import { BrandVueSidePanelContent, ContentType, NavTab, Panel, TabSelection } from "./helpers/PanelHelper";
import { Metric } from "../metrics/metric";
import MetricAboutPanel from "./MetricAboutPanel";
import MetricInsightsPanel from "./MetricInsightsPanel";
import { EntitySet } from "../entity/EntitySet";
import { MetricResultsSummary, ViewTypesValidForMetricInsights } from "./helpers/MetricInsightsHelper";
import { ViewType } from "./helpers/ViewTypeHelper";
import { parseSampleSizeDescription } from "./helpers/SampleSizeHelper";
import PageAboutPanel from "./PageAboutPanel";
import {FeatureCode, PageDescriptor, PartDescriptor} from "../BrandVueApi";
import LlmInsightsPanel from "./LlmInsightsPanel";
import { isFeatureEnabled } from "../components/helpers/FeaturesHelper";

export interface IBrandVueSidePanelAboutProps {
    parts: PartDescriptor[];
    isOpen: boolean;
    close(): void;
    metric: Metric;
    userCanEdit: boolean;
    brand: string | undefined;
    activeEntitySet: EntitySet | undefined;
    metricResultsSummary: MetricResultsSummary | undefined;
    page: PageDescriptor;
    contentType: ContentType;
}

const BrandVueSidePanelAbout = (props: IBrandVueSidePanelAboutProps, viewType: ViewType | undefined): BrandVueSidePanelContent => {
    const getMetricAboutPanel = (metric: Metric, brand: string | undefined, metricResultsSummary: MetricResultsSummary | undefined): Panel => {
        const navTab: NavTab = { tab: TabSelection.About, name: TabSelection[TabSelection.About] }

        const sampleSizeDescription = metricResultsSummary ? parseSampleSizeDescription(metricResultsSummary.sampleSizeDescription) : "";

        const content: JSX.Element = <MetricAboutPanel
            metric={metric}
            userCanEdit={props.userCanEdit}
            brand={brand}
            sampleSizeDescription={sampleSizeDescription}
            visible={props.isOpen} />;

        return { navTab: navTab, content: content };
    }

    const getInsightsPanel = (metric: Metric, activeEntitySet: EntitySet, metricResultsSummary: MetricResultsSummary, viewType: ViewType): Panel => {
        const navTab: NavTab = { tab: TabSelection.Insights, name: TabSelection[TabSelection.Insights] }

        const content: JSX.Element = <MetricInsightsPanel metric={metric} activeEntitySet={activeEntitySet} metricResultsSummary={metricResultsSummary} viewType={viewType} />;

        return { navTab: navTab, content: content };
    }

    const getLlmInsightsPanel = (partId: number): Panel => {
        const navTab: NavTab = { tab: TabSelection.LlmInsights, name: "AI Summary" }

        const content: JSX.Element = <LlmInsightsPanel partId={partId} />;

        return { navTab: navTab, content: content };
    }

    const getPageAboutPanel = (page: PageDescriptor, brand: string | undefined, metricResultsSummary: MetricResultsSummary | undefined): Panel => {
        const navTab: NavTab = { tab: TabSelection.About, name: TabSelection[TabSelection.About] }

        const sampleSizeDescription = metricResultsSummary ? parseSampleSizeDescription(metricResultsSummary.sampleSizeDescription) : "";

        const content: JSX.Element = <PageAboutPanel
            page={page}
            userCanEdit={props.userCanEdit}
            brand={brand}
            sampleSizeDescription={sampleSizeDescription}
            visible={props.isOpen} />;

        return { navTab: navTab, content: content };
    }

    const getPanels = (brand: string | undefined, metric: Metric, activeEntitySet: EntitySet | undefined, page: PageDescriptor, parts: PartDescriptor[], contentType: ContentType, metricResultsSummary: MetricResultsSummary | undefined, viewType: ViewType | undefined): Panel[] => {
        const panels: Panel[] = [];

        switch (contentType) {
            case ContentType.AboutInsights:
                panels.push(getMetricAboutPanel(metric, brand, metricResultsSummary));

                const validPageForMetricInsights = metric.entityCombination.length === 1 &&
                    viewType && ViewTypesValidForMetricInsights.includes(viewType.id);

                if (metric && activeEntitySet && metricResultsSummary && metricResultsSummary.results.length > 0 && viewType && validPageForMetricInsights) {
                    panels.push(getInsightsPanel(metric, activeEntitySet, metricResultsSummary, viewType));
                }
                break;
            case ContentType.PageAbout:
                panels.push(getPageAboutPanel(page, brand, metricResultsSummary));
                break;
            case ContentType.LlmInsights:
            default:
                break;
        }
        const partIds = parts.map(x=>x.id);
        //insights only showing if a single part is rendering
        if (partIds.length == 1 && llmInsightsEnabled) {
            panels.push(getLlmInsightsPanel(partIds[0]));
        }
        return panels;
    }

    const llmInsightsEnabled = isFeatureEnabled(FeatureCode.Llm_insights);

    return {
        contentType: props.contentType,
        panels: getPanels(props.brand, props.metric, props.activeEntitySet, props.page, props.parts, props.contentType, props.metricResultsSummary, viewType)
    };
}

export default BrandVueSidePanelAbout;