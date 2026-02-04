import React from "react";
import { Metric } from "../metrics/metric";
import * as BrandVueApi from "../BrandVueApi";
import { MetricAbout } from "../BrandVueApi";
import { UiAboutItem } from "./AboutItem";
import AboutPanel from "./AboutPanel";

interface IMetricAboutPanelProps {
    metric: Metric;
    userCanEdit: boolean;
    brand: string | undefined;
    sampleSizeDescription: string;
    visible: boolean;
}

const MetricAboutPanel: React.FunctionComponent<IMetricAboutPanelProps> = (props) => {
    const metricAboutClient = BrandVueApi.Factory.MetricsClient(error => error());

    const transformToUiMetricAboutItem = (m: MetricAbout) => {
        return {
            about: m,
            displayTitle: m.aboutTitle,
            displayContent: m.aboutContent,
            originalTitle: m.aboutTitle,
            originalContent: m.aboutContent
        }
    }

    const transformMetricAboutsToUiAbouts = (metricAbouts: MetricAbout[]) => {
        return metricAbouts.map(transformToUiMetricAboutItem);
    }

    const getMetricAboutRecords = (): Promise<UiAboutItem[]> => {
        let setMetrics: UiAboutItem[] = [];
        return metricAboutClient.getMetricAbouts(props.metric.name)
            .then(m => setMetrics = transformMetricAboutsToUiAbouts(m))
            .then(_ => metricAboutClient.getLinkedMetrics(props.metric.name)
                .then(joinLinkedMetrics)
                .then(setLinkedMetric)
                .then((m: UiAboutItem) => {
                    if (m.displayContent.length > 0) {
                        setMetrics.push(m);
                    }
                    return setMetrics;
                }));
    };
    

    const joinLinkedMetrics = (linkedMetrics: string[]) => {
        return linkedMetrics.map(m => `[${ m }]`).join(" ");
    }

    const setLinkedMetric = (linkedMetricNames: string): UiAboutItem => {
        return {
            about: BrandVueApi.MetricAbout.fromJS({ editable: false, user: "System", aboutContent: linkedMetricNames }),
            displayTitle: "Linked Metrics",
            displayContent: linkedMetricNames,
            originalTitle: "",
            originalContent: ""
        }
    }

    const addMetricAboutRecord = (): Promise<UiAboutItem> => {
        return metricAboutClient.createMetricAbout(BrandVueApi.MetricAbout.fromJS({
            aboutTitle: "New About Information",
            aboutContent: "",
            metricName: props.metric.name,
            editable: true
        })).then(transformToUiMetricAboutItem);
    }

    const deleteMetricAboutItem = (metricAboutItem: UiAboutItem) => {
        return metricAboutItem.about instanceof MetricAbout
            ? metricAboutClient.deleteMetricAbout(metricAboutItem.about)
            : Promise.resolve(BrandVueApi.HttpStatusCode.OK);
    }

    const extractUpdatedMetricAbout = (metricAboutItem: UiAboutItem) => {
        const newMetricAboutItem = { ...metricAboutItem };
        newMetricAboutItem.about.aboutTitle = newMetricAboutItem.displayTitle;
        newMetricAboutItem.about.aboutContent = newMetricAboutItem.displayContent;
        if ("metricName" in newMetricAboutItem.about)
            return newMetricAboutItem.about;
    }

    const saveChanges = (metricAboutItems: UiAboutItem[]): Promise<UiAboutItem[]> => {
        const newMetricAboutsToSave = metricAboutItems.map(extractUpdatedMetricAbout)
                                        .filter(m => m !== undefined) as MetricAbout[];

        return metricAboutClient.updateMetricAboutList(newMetricAboutsToSave).then(transformMetricAboutsToUiAbouts)
    }

    return <AboutPanel
        userCanEdit={props.userCanEdit}
        brand = {props.brand}
        sampleSizeDescription={props.sampleSizeDescription}
        visible={props.visible}
        getAbouts={getMetricAboutRecords}
        addAbout={addMetricAboutRecord}
        updateAbouts={saveChanges}
        deleteAbout={deleteMetricAboutItem}
    />;
}

export default MetricAboutPanel;