import React from "react";
import { ViewHelper } from "../ViewHelper";
import * as BrandVueApi from "../../../BrandVueApi";
import { dsession } from "../../../dsession";
import { EntityInstance } from "../../../entity/EntityInstance";
import { Metric } from "../../../metrics/metric";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import ComparisonSelections from "./ComparisonSelections";
import ChartArea from "./ChartArea";
import Comparison from "./Comparison";
import { IGoogleTagManager } from "../../../googleTagManager";
import { ComparisonContext } from "../../helpers/ComparisonContext";
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from '../../../state/subsetSlice';

import {selectTimeSelection} from "../../../state/timeSelectionStateSelectors";

interface IMetricComparisonProps {
    googleTagManager: IGoogleTagManager;
    height: number;
    brands: EntityInstance[];
    metrics: Metric[];
    curatedFilters: CuratedFilters;
    session: dsession;
    removeFromLowSample(brand: EntityInstance, metric: Metric): void;
}

const MetricComparison = (props: IMetricComparisonProps) => {
    const client = BrandVueApi.Factory.DataClient(err => err());

    const [newComparison, setNewComparison] = React.useState<Comparison | null>(null);
    const [processing, setProcessing] = React.useState(false);

    const { getComparisons, setComparisons } = React.useContext(ComparisonContext);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const createComparison = async (brand: EntityInstance, metric: Metric): Promise<Comparison> => {
        const curatedResultsRequestModel = ViewHelper.createCuratedRequestModel([brand.id],
            [metric],
            props.curatedFilters,
            brand.id,
            { continuousPeriod: true },
            subsetId,
            timeSelection);

        let data: BrandVueApi.MultiMetricResults | null = null;
        try {
            data = await client.getMultiMetricResults(curatedResultsRequestModel);
        } catch (e) {}

        return new Comparison(brand, metric, data);
    }

    React.useEffect(() => {
        setProcessing(true);

        const fetchData = async () => {
            setProcessing(false);
        }

        fetchData();

    }, [JSON.stringify(props.curatedFilters)]);

    const removeComparison = (comparison: Comparison): void => {
        const currentComparisons: Array<Comparison> = getComparisons();
        const existingIndex = currentComparisons.findIndex(c => c.key === comparison.key);
        if (existingIndex > -1) {
            const entry = currentComparisons[existingIndex];
            props.removeFromLowSample(entry.brand, entry.metric!);
            const newComparisons = currentComparisons.filter(c => c.key !== comparison.key);
            setComparisons(newComparisons);
            return;
        }

        setNewComparison(null);
    }

    const onComparisonChanged = async (existingComparison: Comparison, newBrand: EntityInstance, newMetric: Metric | null): Promise<void> => {
        const currentComparisons: Array<Comparison> = getComparisons();
        const newComparison = new Comparison(newBrand, newMetric, null);
        if (newComparison.isValid) {
            setProcessing(true);
            const existingIndex = currentComparisons.findIndex(c => c.key === existingComparison.key);
            if (existingIndex > -1) {
                const existingEntry = currentComparisons[existingIndex];
                props.removeFromLowSample(existingEntry.brand, existingEntry.metric!);
                currentComparisons[existingIndex] = newComparison;
                const comparisonWithData = await createComparison(newComparison.brand, newComparison.metric!);
                currentComparisons[existingIndex] = comparisonWithData;
            } else {
                currentComparisons.push(newComparison);
                setNewComparison(null);
                const comparisonWithData = await createComparison(newComparison.brand, newComparison.metric!);
                currentComparisons[currentComparisons.findIndex(c => c.key === newComparison.key)] = comparisonWithData;
            }

            setComparisons(currentComparisons);
            setProcessing(false);
        } else {
            removeComparison(existingComparison);
            setNewComparison(newComparison);
        }
    };

    const methods = {
        onBrandChanged: async (comparison: Comparison, brand: EntityInstance) => onComparisonChanged(comparison, brand, comparison.metric),
        onMetricChanged: async (comparison: Comparison, metric: Metric) => onComparisonChanged(comparison, comparison.brand, metric),
        removeComparison: removeComparison
    };

    const chartData = getComparisons().map(c => ({ metric: c.metric!, overtimeResults: c.data }));
    const comparisonSelections = getComparisons().map(c => c);
    if (newComparison) {
        comparisonSelections.push(newComparison);
    }

    return (
        <>
            <div>
                <ComparisonSelections comparisons={comparisonSelections} methods={methods} brands={props.brands} metrics={props.metrics} processing={processing} />
                <button type="button" className="primary-button d-inline-block addMetricComparisonButton" onClick={() => setNewComparison(new Comparison(EntityInstance.AllInstances, null, null))}
                    disabled={newComparison != null}>
                    <span>+ Add metric to compare</span>
                </button>
            </div>
            <ChartArea googleTagManager={props.googleTagManager} chartingData={chartData} height={props.height} curatedFilters={props.curatedFilters} />
        </>
    );
};

export default MetricComparison;
