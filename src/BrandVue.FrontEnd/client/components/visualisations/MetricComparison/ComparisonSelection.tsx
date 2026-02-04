import React from "react";
import { EntityInstanceSelector } from "../../filters/EntityInstanceSelector";
import MetricSelector, { MetricTypes } from "../../filters/MetricSelector";
import { EntityInstance } from "../../../entity/EntityInstance";
import { Metric } from "../../../metrics/metric";
import Comparison from "./Comparison";

export interface IIndividualComparisonMethods {
    onBrandChanged: (comparison: Comparison, brand: EntityInstance) => void;
    onMetricChanged: (comparison: Comparison, metric: Metric) => void;
    removeComparison: (comparison: Comparison) => void;
}

interface IComparisonSelectionProps {
    comparison: Comparison;
    methods: IIndividualComparisonMethods;
    brands: EntityInstance[];
    metrics: Metric[];
    processing: boolean;
}

const metricTypes = [MetricTypes.Brand];

const comparisonSelection: React.FunctionComponent<IComparisonSelectionProps> = (props) => {
    const cssClasses = ['comparisonSelection'];
    if (!props.comparison.isValid) {
        cssClasses.push('validationWarning');
    }
    let noDataWarning = null as JSX.Element | null;
    if (props.comparison.isValid && !props.comparison.hasData && !props.processing) {
        cssClasses.push('noDataError');
        noDataWarning = <span className="ms-2 me-2 message"><strong>No data available</strong></span>;
    }
    const onChange = brand => {
        if (brand != null) {
            props.methods.onBrandChanged(props.comparison, brand);
        }
    }
    return (
        <div className={cssClasses.join(' ')}>
            <EntityInstanceSelector optionValues={props.brands} disabled={props.processing} onChange={onChange} activeValue={props.comparison.brand} className="brandSelector dropdown" placeholder="Select a brand..."/>
            <MetricSelector metrics={props.metrics} disabled={props.processing} changeMetric={metric => props.methods.onMetricChanged(props.comparison, metric)} chosenMetric={props.comparison.metric} className="selectMetric dropdown"
                metricTypes={metricTypes} placeholder="Select a metric..." />
            {noDataWarning}
            <button type="button" disabled={props.processing} className="clearMetric" onClick={() => props.methods.removeComparison(props.comparison)}><i className="material-symbols-outlined">cancel</i></button>
        </div>
    );
};

export default comparisonSelection;