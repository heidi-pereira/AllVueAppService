import React from 'react';
import { IAverageDescriptor, LowSampleSummary, IEntityType } from "../BrandVueApi";
import BrandVueOnlyLowSampleHelper from './visualisations/BrandVueOnlyLowSampleHelper';
import { Collapse } from 'reactstrap';
import { DateFormattingHelper } from "../helpers/DateFormattingHelper";
import { IEntityInstanceGroup } from "../entity/IEntityInstanceGroup";
import { useState } from "react";

interface ISidePanel {
    activeEntityType: IEntityType,
    currentAverage: IAverageDescriptor,
    entityInstances: IEntityInstanceGroup,
    toggleVisibility(): void,
    lowSampleSummaries: LowSampleSummary[],
    sidePanelType: SidePanelType,
    noSampleInstanceName?: string
}

export enum SidePanelType {
    LowSample,
    NoSampleForFocusInstance
}

export type SidePanelObject = {
    sidePanelType: SidePanelType,
    localStorageKey: string,
    getToggleValue: () => boolean
}

const SidePanel = (props: ISidePanel) : JSX.Element => {
    const entityName = props.activeEntityType
        ? props.activeEntityType.displayNameSingular.toLowerCase()
        : "metric";

    const sidePanelContent = (title: string, content: JSX.Element) => {
        return <div className="sidePanel">
            <div className="sidePanel-header">
                <div>
                    <i className="material-symbols-outlined">warning</i>
                    <span>{title}</span>
                </div>
                <a onClick={() => props.toggleVisibility()} className="not-exported sidePanel-close">
                    <i className="material-symbols-outlined">close</i>
                </a>
            </div>
            {content}
        </div>;
    }

    let title: string;
    let names: string | undefined;
    let content: JSX.Element;

    switch (props.sidePanelType) {
        case SidePanelType.LowSample:
            if (props.lowSampleSummaries.length < 1) {
                return <></>;
            }
            const summarized = new SummarizedData(props.currentAverage, props.entityInstances);
            props.lowSampleSummaries.map(x => summarized.addSummaryPoint(x));
            const summarizedData = summarized.validSummarizedData();

            const hasSummary = !(summarizedData == null || summarizedData.length === 0);

            const lowSampleNames = props.lowSampleSummaries.map(lss => {
                return SummarizedData.GetNameForLowSampleSummary(lss, props.entityInstances);
            });

            const uniqueLowSampleNames = Array.from(new Set(lowSampleNames));

            title = "Low sample";
            names = uniqueLowSampleNames.join(", ");
            content = <>
                <p className={hasSummary ? "sidePanel-brands not-exported gentle-highlight" : "sidePanel-brands"}>{
                    names}</p>
                <p className="not-exported">This may be a result of filters or the chosen reporting period.</p>
                <p className="not-exported">Any {entityName} with a sample of less than {
                    BrandVueOnlyLowSampleHelper.lowSampleForEntity} in a given period is marked as having a low sample.</p>
                {
                    hasSummary &&
                    <SidePanelMoreDetails lowSampleSummaries={summarizedData} />
                }
            </>;
            break;
        case SidePanelType.NoSampleForFocusInstance:
            title = "No sample for focus " + entityName;
            names = props.noSampleInstanceName;
            content = <>
                <p className={"sidePanel-brands not-exported gentle-highlight"}>{names}</p>
                <p className="not-exported">This may be a result of filters, the chosen reporting period, or the {entityName} not being relevant to the question.</p>
            </>;
            break;
    };

    return sidePanelContent(title, content);
};

class SummaryData {
    name: string;
    private whenLowSamples: string[];
    private avgDescriptor: IAverageDescriptor;

    constructor(name: string, avgDescriptor: IAverageDescriptor) {
        this.name = name;
        this.avgDescriptor = avgDescriptor;
        this.whenLowSamples = [];
    }

    addDate(date?: Date) {
        if (date != null) {

            this.whenLowSamples.push(DateFormattingHelper.formatDateRange(date, this.avgDescriptor));
        }
    }

    hasDates(): boolean {
        return this.whenLowSamples.length > 0;
    }

    dates(): string[] {
        return Array.from(new Set(this.whenLowSamples));
    }
}

class SummarizedData {
    datum: SummaryData[];
    private readonly currentAverage: IAverageDescriptor;
    private readonly entityInstances: IEntityInstanceGroup;

    constructor(currentAverage: IAverageDescriptor, entityInstances: IEntityInstanceGroup) {
        this.datum = [];
        this.entityInstances = entityInstances;
        this.currentAverage = currentAverage;
    }

    addSummaryPoint(lowSummary: LowSampleSummary) {

        const name = SummarizedData.GetNameForLowSampleSummary(lowSummary, this.entityInstances);
        const metricToAppend = lowSummary.metric == null
            ? ""
            : (name.length > 0 ? "-" : "") + lowSummary.metric;
        const fullName = name + metricToAppend; //Append metric name

        const items = this.datum.filter(x => x.name === fullName);
        let currentItem: SummaryData;
        if (items == null || items.length === 0) {
            currentItem = new SummaryData(fullName, this.currentAverage);
            this.datum.push(currentItem);
        } else {
            currentItem = items[0];
        }
        currentItem.addDate(lowSummary.dateTime);
    }

    public static GetNameForLowSampleSummary(lowSummary: LowSampleSummary, instances: IEntityInstanceGroup) {
        if (lowSummary.entityInstanceId) {
            const instance = instances.getById(lowSummary.entityInstanceId);
            if (instance) {
                return instance.name;
            }

            return "";

        } else {
            if (lowSummary.name) {
                return lowSummary.name;
            }

            return "";
        }
    }

    validSummarizedData(): SummaryData[] {
        return this.datum.filter(x => x.hasDates());
    }

}

const SidePanelMoreDetails = (props: { lowSampleSummaries: SummaryData[] }) => {
    const [isMoreDetailsOpen, setIsMoreDetailsOpen] = useState<boolean>(false);

    const toggleMoreDetails = () => {
        setIsMoreDetailsOpen(!isMoreDetailsOpen);
    }

    return (
        <span>
            <div className="sidePanel-more-details mt-3 clickable not-exported" onClick={toggleMoreDetails}>
                <span className="float-end"><i className="material-symbols-outlined align-top">{isMoreDetailsOpen
                    ? "keyboard_arrow_down"
                    : "keyboard_arrow_right"}</i></span>
                <div>More detail</div>
            </div>
            <Collapse className="mt-2" isOpen={isMoreDetailsOpen}>
                {props.lowSampleSummaries.map(summary =>
                    <span key={summary.name}><b>{summary.name}</b>
                        <span className="small">({summary.dates()
                            .join(", ")})</span>, </span>)
                }
            </Collapse>
            <div className="mb-3 not-exported" />
        </span>
    );
}

export default SidePanel;