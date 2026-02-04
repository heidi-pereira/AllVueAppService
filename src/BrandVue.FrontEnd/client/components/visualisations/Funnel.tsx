import React from "react";
import { FunnelResults, Factory, MetricResultsForEntity, MetricWeightedDailyResult } from "../../BrandVueApi";
import { EntityInstance } from "../../entity/EntityInstance";
import { Metric } from "../../metrics/metric";
import { ViewHelper } from "./ViewHelper";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { ChartFooterInformation } from "./ChartFooterInformation";
import { getLowSampleThreshold } from "./BrandVueOnlyLowSampleHelper";
import _ from "lodash";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import Tooltip from "../Tooltip";
import { EntitySet } from "../../entity/EntitySet";
import * as BrandVueApi from "../../BrandVueApi";
import CompressClientData = BrandVueApi.Factory;
import {useAppDispatch, useAppSelector} from "../../state/store";
import {setGenericAverageResults, setGenericResults} from "../../state/resultsSlice";
import { selectSubsetId } from '../../state/subsetSlice';

import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

interface IFunnelProps {
    partId: number;
    title: string;
    height: number;
    entitySet: EntitySet;
    availableEntitySets: EntitySet[];
    curatedFilters: CuratedFilters;
    metrics: Metric[];
    activeBrand: EntityInstance;
    showAdoptionSignatureIfAvailable: boolean;
}

enum ConversionType {
    Standard = "standard-conversion",
    Best = "best-conversion",
    Worst = "worst-conversion"
}

const calculateConversionDeltas = (metricResults: MetricWeightedDailyResult[]) => {
    if (metricResults.length <= 1) {
        return new Array<number>();
    }

    const resultValues = metricResults.map(r => r.weightedDailyResult.weightedResult);
    return resultValues.map((r, i) => (i > 0 && resultValues[i - 1] !== 0) ? r > 0 ? r / resultValues[i - 1] : 0 : -1).slice(1);
}

const conversionDeltaBlock = (baseRoundelOffset: number, conversionDelta: number, zIndexValue: number, conversionType: ConversionType, currentMetricName: string, nextMetricName: string, isActiveBrand: boolean) => {

    return (
        <div className={`conversion-delta-bar ${conversionType.toString()}`} style={{
            width: `calc(${NumberFormattingHelper.format0Dp((baseRoundelOffset) * 100)}% + 22px)`,
            zIndex: zIndexValue
        }}>
            <Tooltip placement="top" title={getConversionTooltip(conversionDelta, currentMetricName, nextMetricName, isActiveBrand)}>
                <div className={`conversion-delta-roundel ${conversionType.toString()}`}>
                    <p>{NumberFormattingHelper.formatPercentage0Dp(conversionDelta)}</p>
                </div>
            </Tooltip>
        </div>
    );
}

const getMetricTooltip = (currentValue: number, sampleSize: string, isLowSample: boolean) => {
    return (
        <div className="brandvue-tooltip">
            <div className="tooltip-header">{isLowSample ? "Low sample" : ""}</div>
            <div className="tooltip-label">Brand:</div>
            <div className="tooltip-value">{NumberFormattingHelper.formatPercentage1Dp(currentValue)}</div>
            <div className="tooltip-label">N:</div>
            <div className="tooltip-value">{sampleSize}</div>
        </div>
    );
}

const getMetricAverageTooltip = (average: number, name: string) => {
    return (
        <div className="brandvue-tooltip">
            <div className="tooltip-label">Average:</div>
            <div className="tooltip-value">{name}</div>
            <div className="tooltip-label">Value:</div>
            <div className="tooltip-value">{NumberFormattingHelper.formatPercentage1Dp(average)}</div>
        </div>
    );
}

const getConversionTooltip = (conversionDelta: number, conversionFrom: string, conversionTo: string, isActiveBrand: boolean) => {
    return (
        <div className="brandvue-tooltip">
            <div className="tooltip-header">Conversion rate</div>
            <div className="tooltip-label">
                {isActiveBrand ? "You c" : "C"}onverted <span style={{ color: "white", fontWeight: 500 }}>
                    {NumberFormattingHelper.formatPercentage1Dp(conversionDelta)}
                </span> from {conversionFrom} to {conversionTo}
            </div>
        </div>
    );
}

const getTableContent = (results: MetricResultsForEntity, resultDeltas: number[], metrics: Metric[], dataLoaded: boolean, lowSampleThreshold: number, metricAverage: {name: string, data: MetricWeightedDailyResult[]}[],
    worstConversionDeltas: { [id: number]: number; }, bestConversionDeltas: { [id: number]: number; }, isActiveBrand: boolean) => {
    if (!dataLoaded) {
        return metrics.map((m, i) => {
            return (
                <div key={m.name}>
                    <div className="table-row-placeholder" style={{ width: `${100- (100 / metrics.length * i)}%` }}></div>
                </div>
            );
        });
    }
    
    const showDelta = (i: number, result: MetricWeightedDailyResult, currentValue: number) => {

        const maxAverageValue = Math.max(...metricAverage.map(m=>m.data[i].weightedDailyResult.weightedResult));
        const conversionDelta = i < resultDeltas.length ? resultDeltas[i] : -1;
        const nextMetric = (i + 1) < metrics.length ? metrics[i + 1]!.name : "";
        const isWorstConversion = worstConversionDeltas[i] != undefined && worstConversionDeltas[i] === conversionDelta;
        const isBestConversion = bestConversionDeltas[i] != undefined && bestConversionDeltas[i] === conversionDelta;
        const conversionType = isBestConversion ? ConversionType.Best : isWorstConversion ? ConversionType.Worst : ConversionType.Standard;

        let listOfValuesForBaseRoundelOffsetCalc = [currentValue, maxAverageValue];
        if (i < resultDeltas.length) {
            listOfValuesForBaseRoundelOffsetCalc = listOfValuesForBaseRoundelOffsetCalc.concat([
                results.metricResults[i + 1].weightedDailyResult.weightedResult,
                Math.max(...metricAverage.map(m => m.data[i + 1].weightedDailyResult.weightedResult))
            ]);
        }
        const baseRoundelOffset = Math.max(...listOfValuesForBaseRoundelOffsetCalc);
        const zIndex = resultDeltas.length - i;
        return conversionDelta !== -1 && conversionDeltaBlock(baseRoundelOffset, conversionDelta, zIndex, conversionType, result.metricName, nextMetric, isActiveBrand)
    }

    return results.metricResults.map((result, i) => {
        const metric = metrics.find(m => m.name === result.metricName);
        if (!metric!.isPercentage()) throw new Error(`${metric!.name} is not a percentage metric`);

        const currentValue = result.weightedDailyResult.weightedResult;
        const currentValuePercent = metric!.fmt(currentValue);

        const isLowSample = result.weightedDailyResult.unweightedSampleSize < lowSampleThreshold;

        return (
            <div className="single-bar" key={result.metricName}>
                <div className="funnel-row">
                    <div className="data">{currentValuePercent}</div>
                    <div className="funnel-row-bars">
                        <div className="bar-bg">
                            <Tooltip placement="top" title={getMetricTooltip(currentValue, result.weightedDailyResult.unweightedSampleSize.toString(), isLowSample)}>
                                <div className={`bar ${isLowSample ? "bar-fg-low-sample" : "bar-fg"}`} style={{
                                    width: currentValuePercent
                                }}/>
                            </Tooltip>
                            {metricAverage.map(a => {
                                    const averageValue = a.data[i].weightedDailyResult.weightedResult;
                                    const averageValuePercent = metric!.fmt(averageValue);
                                    return (
                                        <Tooltip key={a.name} placement="top" title={getMetricAverageTooltip(averageValue, a.name)}>
                                            <div className="bar bar-avg" style={{ width: averageValuePercent }}></div>
                                        </Tooltip>);
                                })
                            }
                        </div>
                        {showDelta(i, result, currentValue)}
                    </div>
                </div>
            </div>
        );
    });
}

const Funnel = (props: IFunnelProps) => {
    const [results, setResults] = React.useState(new FunnelResults);
    const [averagesPerMeasures, setAveragesPerMeasures] = React.useState<{name: string, data: MetricWeightedDailyResult[]}[]>([]);
    const [dataLoaded, setDataLoaded] = React.useState<boolean>(false);
    const [, handleError] = React.useState();
    const dispatch = useAppDispatch();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const loadAverages = async () => {
        const promises = props.entitySet.getAverages().getAll().map(async average => {
            const activeBrandId = props.activeBrand.id;
            const averageEntitySet = average.getEntitySet(props.entitySet, props.availableEntitySets);
            const curatedResultsModel = ViewHelper.createCuratedRequestModel(averageEntitySet.getInstances().getAll().map(x => x.id),
                props.metrics,
                props.curatedFilters,
                activeBrandId,
                {},
                subsetId,
                timeSelection);
            return await CompressClientData.DataClient(throwErr => throwErr()).getFunnelResults(curatedResultsModel)
                .then(r => {
                    return { name: averageEntitySet.name, results: r, request: curatedResultsModel }; 
                });
        });
        var allAverageSliceData = await Promise.all(promises);
        setAveragesPerMeasures(allAverageSliceData.map(x => ({name: x.name, data: x.results.marketAveragePerMeasures})));
        dispatch(setGenericAverageResults({
            partId: props.partId,
            payload: allAverageSliceData
        }));
    };

    React.useEffect(() => {
        const brandIds = props.entitySet.getInstances().getAll().map(b => b.id);
        const activeBrandId = props.activeBrand.id;
        const curatedResultsModel = ViewHelper.createCuratedRequestModel(brandIds,
            props.metrics,
            props.curatedFilters,
            activeBrandId,
            {},
            subsetId,
            timeSelection);

        setDataLoaded(false);

        Factory.DataClient(throwErr => throwErr()).getFunnelResults(curatedResultsModel)
            .then(r => {
                dispatch(setGenericResults({
                    results: r,
                    request: curatedResultsModel,
                    averages: [],
                    averagesSelected: props.entitySet.getAverages().getAll().length,
                    partId: props.partId,
                }));
                setResults(r);
                setDataLoaded(true);
            }).catch((e: Error) => handleError(() => { throw e }));

        loadAverages();

    }, [
        JSON.stringify(props.curatedFilters),
        props.activeBrand.id,
        JSON.stringify(props.entitySet),
        subsetId,
        timeSelection
    ]);

    const lowSampleThreshold = getLowSampleThreshold();

    const resultsWithActiveBrandFirst = results.results.filter(r => r.entityInstance.id === props.activeBrand.id)
        .concat(results.results.filter(r => r.entityInstance.id !== props.activeBrand.id));

    const resultsWithConversionDeltas = resultsWithActiveBrandFirst.map(r => {
        return {
            entityInstance: r.entityInstance,
            results: r,
            conversionDeltas: calculateConversionDeltas(r.metricResults)
        }
    });

    const worstConversionDeltas: { [id: number]: number; } = {};
    const bestConversionDeltas: { [id: number]: number; } = {};

    resultsWithConversionDeltas.forEach(e =>
        e.conversionDeltas.forEach((cd, i) => {
            if (worstConversionDeltas[i] === undefined || cd < worstConversionDeltas[i]) worstConversionDeltas[i] = cd;
            if (bestConversionDeltas[i] === undefined || cd > bestConversionDeltas[i]) bestConversionDeltas[i] = cd;
        }));

    const resultsWithConversionDeltasChunks = _.chunk(resultsWithConversionDeltas, 3);

    return (
        <div className="chart funnel-page">
            <div className="key">
                <div className="key-title">Key:</div>
                {averagesPerMeasures.length > 0 &&
                    <div className="key-item">
                        <div className="key-graphic average"></div>
                        <div>Average</div>
                    </div>
                }
                <div className="key-item">
                    <div className="key-graphic roundel best-conversion"></div>
                    <div>
                        Best conversion rate
                        </div>
                </div>
                <div className="key-item">
                    <div className="key-graphic roundel standard-conversion"></div>
                    <div>
                        Conversion rate
                        </div>
                </div>
                <div className="key-item">
                    <div className="key-graphic roundel worst-conversion"></div>
                    <div>
                        Worst conversion rate
                        </div>
                </div>
            </div>
            <div className="chart-container">
                {resultsWithConversionDeltasChunks.map((c, index) => {
                    const isActiveBrandInChunk = c.find(b => b.entityInstance.id === props.activeBrand.id);

                    return (
                        <div className={`funnel-result-row ${!isActiveBrandInChunk ? "not-exported" : ""}`} key={`row${index}`}>
                            <div className="metric-names all-bars">
                                {props.metrics.map(m => (<div key={m.name}>{m.name}</div>))}
                            </div>
                            <div className="cols3">
                                {c.map(r => {
                                    const isActiveBrand = r.entityInstance.id === props.activeBrand.id;

                                    return (
                                        <div className={!isActiveBrand ? "not-exported" : ""} key={r.entityInstance.name}>
                                            <div className={`funnel-title ${isActiveBrand ? "funnel-title-active-brand" : ""}`}>{r.entityInstance.name}
                                            </div>
                                            <div className="all-bars">
                                                {getTableContent(r.results, r.conversionDeltas, props.metrics, dataLoaded, lowSampleThreshold, averagesPerMeasures,
                                                    worstConversionDeltas, bestConversionDeltas, isActiveBrand)}
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    );
                })}

            </div>
            <ChartFooterInformation sampleSizeMeta={results.sampleSizeMetadata} average={props.curatedFilters.average} activeBrand={props.activeBrand} metrics={props.metrics} />
        </div>
    );
}
export default Funnel;
