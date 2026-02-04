import React from "react";
import { Metric } from "../../metrics/metric";
import SelectPicker, { Option, GroupOption } from '../SelectPicker';
import * as BrandVueApi from "../../BrandVueApi";

export enum MetricTypes {
    Market,
    Brand,
    Text
}

type MetricFilterPredicate = (metric: Metric) => any;

const METRIC_PREDICATES = new Map<MetricTypes, MetricFilterPredicate>([
    [MetricTypes.Market, metric => metric.isProfileMetric() && metric.calcType !== BrandVueApi.CalculationType.Text && !metric.disableMeasure],
    [MetricTypes.Brand, metric => metric.isBrandMetric() && metric.calcType !== BrandVueApi.CalculationType.Text && !metric.disableMeasure],
    [MetricTypes.Text, metric => metric.calcType === BrandVueApi.CalculationType.Text && !metric.disableMeasure]
]);

interface IMetricSelectorProps {
    metrics: Metric[];
    disabled: boolean;
    changeMetric: (metric: Metric) => void;
    chosenMetric: Metric | null;
    className: string;
    metricTypes: MetricTypes[];
    placeholder: string;
}

class MetricSelectPicker extends SelectPicker<Metric> {}

const MetricSelector = (props: IMetricSelectorProps) => {
    const change = (selectedOption: Metric | null) => {
        if (selectedOption) {
            let metric = props.metrics.find(x => x.name === selectedOption.name);
            if (metric) {
                props.changeMetric(metric);
            }
        }
    }

    const generateMetrics = (mt: MetricTypes): Metric[] => {
        let metrics = [] as Metric[];
        const predicate = METRIC_PREDICATES.get(mt);
        if (predicate) {
            metrics = props.metrics.filter(predicate);
        }
        return metrics;
    }
    
    let allOptions: (Option<Metric> | GroupOption<Metric>)[];

    if (props.metricTypes.length > 1) {
        allOptions = props.metricTypes.map(mt =>
            new GroupOption(
                MetricTypes[mt],
                generateMetrics(mt)
            )
        );
    } else {
        allOptions = props.metrics.slice();
    }

    const activeValue = props.chosenMetric;

    return (
        <MetricSelectPicker
            onChange={change}
            activeValue={activeValue}
            optionValues={allOptions}
            getLabel={(x) => x.name}
            getValue={(x) => x.name}
            className={props.className}
            placeholder={props.placeholder}
            disabled={props.disabled} />
    );
}
export default MetricSelector