import React from "react";
import { EntityInstance } from "../../../entity/EntityInstance";
import { Metric } from "../../../metrics/metric";
import Comparison from "./Comparison";
import ComparisonSelection, { IIndividualComparisonMethods } from "./ComparisonSelection";

interface IComparisonSelectionsProps {
    comparisons: Comparison[];
    methods: IIndividualComparisonMethods;
    brands: EntityInstance[];
    metrics: Metric[];
    processing: boolean;
}

const comparisonSelections: React.FunctionComponent<IComparisonSelectionsProps> = (props) => {
    const { brandToMetric, metricToBrand } = createSelectionMaps(props.comparisons);

    return (
        <React.Fragment>
            {props.comparisons.map(c => {
                let filteredBrands = props.brands;
                let filteredMetrics = props.metrics;

                const metricsUsedForBrands = brandToMetric.get(c.brand);
                if (metricsUsedForBrands) {
                    filteredMetrics = props.metrics
                        .filter(m => metricsUsedForBrands.indexOf(m) === -1 || m === c.metric);
                }
                filteredMetrics = filteredMetrics.filter(m => m.entityCombination.length > 0 && m.entityCombination.some(ec => ec.identifier === "brand"));

                const brandsUsedForMetrics = c.metric ? 
                    metricToBrand.get(c.metric) : null;
                if (brandsUsedForMetrics) {
                    filteredBrands = props.brands
                        .filter(b => brandsUsedForMetrics.indexOf(b) === -1 || b === c.brand);
                }

                return (
                    <ComparisonSelection key={c.key} comparison={c} methods={props.methods} brands={
                        filteredBrands} metrics={filteredMetrics} processing={props.processing} />
                );
            }
            )}
        </ React.Fragment>
    );
};

export default comparisonSelections;

const createSelectionMaps = (comparisons: Comparison[]): { brandToMetric: Map<EntityInstance, Metric[]>, metricToBrand: Map<Metric, EntityInstance[]>} => {
    const brandToMetricMap = new Map<EntityInstance, Metric[]>();
    const metricToBrandMap = new Map<Metric, EntityInstance[]>();

    comparisons.forEach(c => {
        if (c.metric) {
            const metrics = brandToMetricMap.get(c.brand) || [];
            metrics.push(c.metric);
            brandToMetricMap.set(c.brand, metrics);

            const brands = metricToBrandMap.get(c.metric) || [];
            brands.push(c.brand);
            metricToBrandMap.set(c.metric, brands);
        }
    });

    return {
        brandToMetric: brandToMetricMap,
        metricToBrand: metricToBrandMap
    };
}