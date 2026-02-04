import React from 'react';
import { EntityInstance } from "../../entity/EntityInstance";
import { Metric } from "../../metrics/metric";
import {  IAverageDescriptor, AverageDescriptor, SampleSizeMetadata } from "../../BrandVueApi";
import { getSampleSizeDescription } from '../helpers/SampleSizeHelper';

interface IChartFooterInformationProps {
    sampleSizeMeta: SampleSizeMetadata;
    average: IAverageDescriptor;
    activeBrand: EntityInstance;
    metrics: Metric[];
    doesHaveBrandMetric?: boolean;
}

export class ChartFooterInformation extends React.Component<IChartFooterInformationProps> {
    constructor(props) {
        super(props);
    }

    render() {
        const doesHaveBrandMetric = this.props.doesHaveBrandMetric == true || (this.props.metrics && this.props.metrics.find(x => x.isBrandMetric()));
        const sampleSizeFor = doesHaveBrandMetric ? this.props.activeBrand.name : "Market";

        return (
            <div className="sampleN">
                <span>{getSampleSizeDescription(this.props.sampleSizeMeta, this.props.average, sampleSizeFor, this.props.metrics)}</span>
            </div>
        );
    }
}
