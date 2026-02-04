import React, {useEffect, useState} from 'react';
import {Metric} from "./metric";
import * as BrandVueApi from "../BrandVueApi";
import {MainQuestionType} from "../BrandVueApi";
import {MetricSet} from "./metricSet";
import {getMetricsValidAsBreaks, isInfoPageMetric} from '../components/helpers/SurveyVueUtils';
import * as actionTypes from './metricsActionTypeConstants';
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from 'client/state/subsetSlice';

export type MetricAction =
    | { type: typeof actionTypes.UPDATE_METRIC_DEFAULT_SPLIT_BY; data: {metric: Metric, splitByEntityTypeName: string}}
    | { type: typeof actionTypes.SET_ELIGIBLE_FOR_CROSSTAB_OR_ALLVUE; data: {metric: Metric, isEligible: boolean }}
    | { type: typeof actionTypes.SET_METRIC_ENABLED; data: { metric: Metric, isEnabled: boolean } }
    | { type: typeof actionTypes.SET_METRIC_FILTER_ENABLED; data: { metric: Metric, isEnabled: boolean } }
    | { type: typeof actionTypes.UPDATE_CALCULATION_TYPE; data: {metricName: string, calculationType: BrandVueApi.CalculationType, subsetId: string}}
    | { type: typeof actionTypes.UPDATE_BASE_VARIABLE; data: {metricName: string, baseVariableId: number | null}}
    | { type: typeof actionTypes.RELOAD_METRICS }
    | { type: typeof actionTypes.UPDATE_MODAL_DATA; data: {metric: Metric, newDisplayName: string, newHelpText: string, newEntityMeanMap: string | undefined} }

export interface MetricContextState {
    selectableMetricsForUser: Metric[];
    crosstabPageMetrics: Metric[];
    metricsForBreaks: Metric[];
    metricsForReports: Metric[];
    enabledMetricSet: MetricSet;
    hasMetricsLoaded: boolean;
    questionTypeLookup: { [key: string]: MainQuestionType; };
    metricsDispatch: (action: MetricAction) => Promise<void>;
}

const MetricStateContext = React.createContext<MetricContextState>({ 
    hasMetricsLoaded: false,
    selectableMetricsForUser: [],
    crosstabPageMetrics: [],
    metricsForBreaks: [],
    metricsForReports: [],
    questionTypeLookup: {},
    enabledMetricSet: new MetricSet(),
    metricsDispatch: () => Promise.resolve()
});

export const useMetricStateContext = () => React.useContext(MetricStateContext);

interface IProps {
    children: any;
    isSurveyVue: boolean;
    userCanSeeAllMetrics: boolean;
    initialMetrics?: Metric[]
}

export const MetricStateProvider = (props: IProps) => {
    const configureMetricClient = BrandVueApi.Factory.ConfigureMetricClient(error => error());
    const metricClient = BrandVueApi.Factory.MetricsClient(error => error());
    const [hasMetricsLoaded, setHasMetricsLoaded] = useState<boolean>(props.initialMetrics ? true : false);
    const [selectableMetricsForUser, setSelectableMetricsForUser] = useState<Metric[]>(props.initialMetrics ?? []);
    const [enabledMetricSet, setEnabledMetricSet] = useState<MetricSet>(new MetricSet());
    const [questionTypeLookup, setQuestionTypeLookup] = useState<{ [key: string]: MainQuestionType; }>({});
    const [crossTabPageMetrics, setCrossTabPageMetrics] = useState<Metric[]>([]);
    const [metricsForBreaks, setMetricsForBreaks] = useState<Metric[]>([]);
    const subsetId = useAppSelector(selectSubsetId);

    const getMetricsForReports = (m: Metric[]) => {
        return m.filter(m => {
            return m.eligibleForCrosstabOrAllVue
                && !m.disableMeasure
                && m.originalMetricName == undefined
                && !isInfoPageMetric(m, questionTypeLookup)
        });
    }
    const [metricsForReports, setMetricsForReports] = useState<Metric[]>([]);

    async function updateState(m: Metric[]) {
        setMetricsForBreaks(getMetricsValidAsBreaks(m));
        setCrossTabPageMetrics(getCrosstabPageMetrics(m));
        setMetricsForReports(getMetricsForReports(m));
        setSelectableMetricsForUser(m);
    }
    
    const sortMetricsAlphabetically = (metricOne: Metric, metricTwo: Metric) => {
        return metricOne.name.localeCompare(metricTwo.name)
    }

    const sortMetricsBySurveyOrder = (a: Metric, b: Metric) => {
        if (a.isBasedOnCustomVariable && b.isBasedOnCustomVariable) { return a.name.localeCompare(b.name); }
        if (a.isBasedOnCustomVariable) { return -1; }
        if (b.isBasedOnCustomVariable) { return 1; }
        if (!a.primaryFieldDependencies[0].itemNumber && !b.primaryFieldDependencies[0].itemNumber) { return 0; }
        if (!a.primaryFieldDependencies[0].itemNumber) { return 1; }
        if (!b.primaryFieldDependencies[0].itemNumber) { return -1; }
        return (a.primaryFieldDependencies[0].itemNumber - b.primaryFieldDependencies[0].itemNumber);
    }

    function getAvailableMetrics(allMetrics: Metric[]) {
        const result = props.userCanSeeAllMetrics ? allMetrics : allMetrics.filter(m => m.eligibleForCrosstabOrAllVue);
        props.isSurveyVue ? result.sort(sortMetricsBySurveyOrder) : result.sort(sortMetricsAlphabetically);

        return result;
    }

    function getCrosstabPageMetrics(m: Metric[]) {
        return m.filter(m => m.originalMetricName == undefined && !isInfoPageMetric(m, questionTypeLookup));
    }

    const reloadMetrics = async () => {
        try {
            setHasMetricsLoaded(false)
            
            const getMeasures = () => props.isSurveyVue
                ? metricClient.getMetricsWithDisabledAndBaseDescription(subsetId)
                : metricClient.getMetricsWithDisabled(subsetId);

            const measures = await getMeasures();
            const mappedMetrics = MetricSet.mapMeasuresToMetrics(measures);
            setEnabledMetricSet(new MetricSet({
                metrics: mappedMetrics.filter(m => !m.disabled)
            }));
            
            const availableMetrics = getAvailableMetrics(mappedMetrics);
            updateState(availableMetrics);

            const lookup = await BrandVueApi.Factory.MetaDataClient(error => error())
                .getQuestionTypes(subsetId);
            setQuestionTypeLookup(lookup);
        } catch (error) {
            console.log(error);
        } finally {
            setHasMetricsLoaded(true)
        }
    }

    useEffect(() => {
        reloadMetrics()
    }, [subsetId]);

    const asyncDispatch = async (action: MetricAction) => {
        switch (action.type) {
            case actionTypes.SET_METRIC_ENABLED:
                return configureMetricClient.updateMetricDisabled(action.data.metric.name, !action.data.isEnabled)
                    .then(() => reloadMetrics());
            case actionTypes.SET_METRIC_FILTER_ENABLED:
                return configureMetricClient.updateMetricFilterDisabled(action.data.metric.name, !action.data.isEnabled)
                    .then(() => reloadMetrics());
            case actionTypes.SET_ELIGIBLE_FOR_CROSSTAB_OR_ALLVUE:
                return configureMetricClient.updateEligibleForCrosstabOrAllVue(action.data.metric.name, action.data.isEligible)
                    .then(() => reloadMetrics());
            case actionTypes.UPDATE_METRIC_DEFAULT_SPLIT_BY:
                return configureMetricClient.updateMetricDefaultSplitBy(action.data.metric.name, action.data.splitByEntityTypeName)
                    .then(() => reloadMetrics());
            case actionTypes.UPDATE_MODAL_DATA:
                const model = new BrandVueApi.MetricModalDataModel({metricName: action.data.metric.name,
                    displayName: action.data.newDisplayName,
                    displayText: action.data.newHelpText,
                    entityInstanceIdMeanCalculationValueMapping: action.data.newEntityMeanMap ?? ""})
                return configureMetricClient.updateMetricModalData(model)
                    .then(() => reloadMetrics());
            case actionTypes.UPDATE_CALCULATION_TYPE:
                return configureMetricClient.convertCalculationType(action.data.metricName, action.data.calculationType, action.data.subsetId)
                    .then(() => reloadMetrics());
            case actionTypes.UPDATE_BASE_VARIABLE:
                return configureMetricClient.updateBaseVariable(action.data.metricName, action.data.baseVariableId);
            case actionTypes.RELOAD_METRICS:
                return reloadMetrics();
            default:
                throw new Error("Unsupported action type");
        }
    }

    return (
        <MetricStateContext.Provider value={{
            hasMetricsLoaded: hasMetricsLoaded,
            selectableMetricsForUser: selectableMetricsForUser,
            crosstabPageMetrics: crossTabPageMetrics,
            metricsForBreaks: metricsForBreaks,
            metricsForReports: metricsForReports,
            // Use this by default if looking up a metric name
            enabledMetricSet: enabledMetricSet,
            questionTypeLookup: questionTypeLookup,
            metricsDispatch: asyncDispatch
        }}>
            {props.children}
        </MetricStateContext.Provider>
    );
};