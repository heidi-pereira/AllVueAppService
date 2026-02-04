import React from 'react';
import { AverageType, 
    BaseDefinitionType, 
    CrossMeasure,
    CrosstabSignificanceType, 
    ReportOrder, 
    HeatMapOptions, 
    DisplaySignificanceDifferences, 
    SigConfidenceLevel } from '../../../BrandVueApi';
import { Metric } from '../../../metrics/metric';
import { ProductConfiguration } from '../../../ProductConfiguration';
import { defaultHeatMapOptions, getOptionsWithDefaultFallbacks } from '../../helpers/HeatMapHelper';
import { SortAverages } from '../AverageHelper';
import BrandVueOnlyLowSampleHelper from '../BrandVueOnlyLowSampleHelper';

interface ICrosstabPageContextState {
    crosstabPageState: ICrosstabPageState;
    crosstabPageDispatch: (action: CrosstabPageStateAction) => void;
}

interface ICrosstabPageState {
    includeCounts: boolean;
    weightingEnabled: boolean;
    highlightLowSample: boolean;
    highlightSignificance: boolean;
    displaySignificanceDifferences: DisplaySignificanceDifferences;
    displayMeanValues: boolean;
    displayStandardDeviation: boolean;
    significanceType: CrosstabSignificanceType;
    resultSortingOrder: ReportOrder;
    decimalPlaces: number;
    selectedAverages: AverageType[];
    metricBaseLookup: {[metricName: string]: IBaseState};
    categories: CrossMeasure[];
    heatMapOptions: HeatMapOptions;
    sigConfidenceLevel: SigConfidenceLevel;
    hideTotalColumn: boolean;
    showMultipleTablesAsSingle: boolean;
    calculateIndexScores: boolean;
    lowSampleThreshold: number;
}

const defaultState: ICrosstabPageState = {
    includeCounts: true,
    highlightLowSample: true,
    highlightSignificance: false,
    displaySignificanceDifferences: DisplaySignificanceDifferences.ShowBoth,
    displayMeanValues: false,
    displayStandardDeviation: false,
    significanceType: CrosstabSignificanceType.CompareWithinBreak,
    resultSortingOrder: ReportOrder.ScriptOrderDesc,
    decimalPlaces: 1,
    selectedAverages: [],
    metricBaseLookup: {},
    categories: [],
    weightingEnabled: true,
    heatMapOptions: defaultHeatMapOptions(),
    sigConfidenceLevel: SigConfidenceLevel.NinetyFive,
    hideTotalColumn: false,
    showMultipleTablesAsSingle: false,
    calculateIndexScores: false,
    lowSampleThreshold: 75
}

export interface IBaseState {
    baseType: BaseDefinitionType;
    baseVariableId: number | undefined;
}

const defaultBaseState: IBaseState = {
    baseType: BaseDefinitionType.SawThisQuestion,
    baseVariableId: undefined,
}

interface ICrosstabBreaksCategories {
    measureName: string;
    filterInstances: [];
    childMeasures: CrossMeasure[];
    multipleChoiceByValue: boolean;
}

export function getDefaultBaseStateForMetric(metric: Metric | undefined): IBaseState {
    if (metric?.baseVariableConfigurationId) {
        return {
            baseType: BaseDefinitionType.SawThisQuestion,
            baseVariableId: metric.baseVariableConfigurationId,
        };
    }
    if (metric && metric.primaryFieldDependencies && metric.primaryFieldDependencies?.length > 1) {
        return {
            baseType: BaseDefinitionType.AllRespondents,
            baseVariableId: undefined,
        };
    }
    return {
        baseType: BaseDefinitionType.SawThisChoice,
        baseVariableId: undefined,
    };
}

type CrosstabPageStateAction =
    | { type: 'SET_WEIGHTING'; data: { weightingEnabled: boolean }}
    | { type: 'SET_INCLUDE_COUNTS'; data: { includeCounts: boolean }}
    | { type: 'SET_CALCULATE_INDEX_SCORES'; data: { calculateIndexScores: boolean } }
    | { type: 'SET_HIGHLIGHT_LOW_SAMPLE'; data: { highlightLowSample: boolean }}
    | { type: 'SET_HIGHLIGHT_SIGNFICANCE'; data: { highlightSignificance: boolean } }
    | { type: 'SET_DISPLAYED_SIG_DIFF'; data: { displaySignificanceDifferences: DisplaySignificanceDifferences } }
    | { type: 'SET_DISPLAY_MEAN_VALUES'; data: { displayMeanValues: boolean } }
    | { type: 'SET_DISPLAY_STANDARD_DEVIATION'; data: { displayStandardDeviation: boolean } }
    | { type: 'SET_SIGNIFICANCE_TYPE'; data: { significanceType: CrosstabSignificanceType }}
    | { type: 'SET_SIGNIFICANCE_LEVEL'; data: { sigConfidenceLevel: SigConfidenceLevel }}
    | { type: 'SET_RESULT_SORTING_ORDER'; data: { resultSortingOrder: ReportOrder }}
    | { type: 'SET_DECIMAL_PLACES'; data: { decimalPlaces: number }}
    | { type: 'SET_SELECTED_AVERAGES'; data: { selectedAverages: AverageType[] }}
    | { type: 'SET_METRIC_BASE'; data: { metricName: string, baseTypeOverride: BaseDefinitionType, baseVariableId: number | undefined }}
    | { type: 'SET_CATEGORIES'; data: { categories: CrossMeasure[] }}
    | { type: 'REMOVE_METRIC_BASE'; data: { variableId: number } }
    | { type: 'SET_HEATMAP_OPTIONS'; data: { heatMapOptions: HeatMapOptions } }
    | { type: 'SET_SELECTED_AVERAGES_AND_DISPLAY_MEAN_VALUES'; data: { selectedAverages: AverageType[], displayMeanValues: boolean }}
    | { type: 'SET_HIDE_TOTAL_COLUMN'; data: { hideTotalColumn: boolean } }
    | { type: 'SET_SHOW_MULTIPLE_TABLES_AS_SINGLE'; data: { showMultipleTablesAsSingle: boolean } }
    | { type: 'SET_LOW_SAMPLE_THRESHOLD'; data: { lowSampleThreshold: number } };

const CrosstabPageStateContext = React.createContext<ICrosstabPageContextState>({ crosstabPageState: defaultState, crosstabPageDispatch: () => {} });
export const useCrosstabPageStateContext = () => React.useContext(CrosstabPageStateContext);

const LOCALSTORAGE_CROSSTABPAGE_STATE = "crosstabpage_persisted_state";

const createCategories = (categories: ICrosstabBreaksCategories[]): CrossMeasure[] => 
    categories.map(category => new CrossMeasure(category));

export const CrosstabPageStateProvider = (props: { productConfiguration: ProductConfiguration, children: any }) => {
    const storageKey = `${LOCALSTORAGE_CROSSTABPAGE_STATE}-${props.productConfiguration.productName ?? ''}-${props.productConfiguration.subProductId ?? ''}`;

    const loadInitialState = (): ICrosstabPageState => {
        const cachedState = localStorage.getItem(storageKey);
        let cachedStateParsed = cachedState ? JSON.parse(cachedState) : {};
        let initialState = {
            ...defaultState,
            ...cachedStateParsed
        };
        const categories = createCategories(initialState.categories ?? []);
        initialState.categories = categories;
        initialState.selectedAverages.sort((a: AverageType,b: AverageType) => SortAverages(a,b));
        initialState.heatMapOptions = getOptionsWithDefaultFallbacks(HeatMapOptions.fromJS(initialState.heatMapOptions));
        return initialState;
    };

    const [state, setState] = React.useState<ICrosstabPageState>(loadInitialState);

    if(!props.productConfiguration.isSurveyVue()){
        const globallyDefinedLowSampleThreshold = BrandVueOnlyLowSampleHelper.lowSampleForEntity;
        if(globallyDefinedLowSampleThreshold && state.lowSampleThreshold !== globallyDefinedLowSampleThreshold){
            setState(prevState => ({
                ...prevState,
                lowSampleThreshold: globallyDefinedLowSampleThreshold
            }));
        }
    }

    React.useEffect(() => localStorage.setItem(storageKey, JSON.stringify(state)), [state, storageKey]);

    const dispatch = (action: CrosstabPageStateAction) => {
        switch (action.type) {
            case 'SET_WEIGHTING': return setState({ ...state, weightingEnabled: action.data.weightingEnabled});
            case 'SET_INCLUDE_COUNTS': return setState({...state, includeCounts: action.data.includeCounts});
            case 'SET_CALCULATE_INDEX_SCORES': return setState({...state, calculateIndexScores: action.data.calculateIndexScores});
            case 'SET_HIGHLIGHT_LOW_SAMPLE': return setState({...state, highlightLowSample: action.data.highlightLowSample});
            case 'SET_HIGHLIGHT_SIGNFICANCE': return setState({ ...state, highlightSignificance: action.data.highlightSignificance });
            case 'SET_DISPLAYED_SIG_DIFF': return setState({ ...state, displaySignificanceDifferences: action.data.displaySignificanceDifferences });
            case 'SET_DISPLAY_MEAN_VALUES': return setState({ ...state, displayMeanValues: action.data.displayMeanValues });
            case 'SET_DISPLAY_STANDARD_DEVIATION': return setState({ ...state, displayStandardDeviation: action.data.displayStandardDeviation });
            case 'SET_SIGNIFICANCE_TYPE': return setState({...state, significanceType: action.data.significanceType});
            case 'SET_SIGNIFICANCE_LEVEL': return setState({...state, sigConfidenceLevel: action.data.sigConfidenceLevel});
            case 'SET_RESULT_SORTING_ORDER': return setState({...state, resultSortingOrder: action.data.resultSortingOrder});
            case 'SET_DECIMAL_PLACES': return setState({ ...state, decimalPlaces: action.data.decimalPlaces });
            case 'SET_SELECTED_AVERAGES': return setState({ ...state, selectedAverages: [...action.data.selectedAverages].sort((a, b) => SortAverages(a, b)) });
            case 'SET_SELECTED_AVERAGES_AND_DISPLAY_MEAN_VALUES':
                return setState({ ...state, selectedAverages: [...action.data.selectedAverages].sort((a, b) => SortAverages(a, b)), displayMeanValues: action.data.displayMeanValues });
            case 'SET_METRIC_BASE':
                const newLookup = {...state.metricBaseLookup};
                newLookup[action.data.metricName] = {
                    baseType: action.data.baseTypeOverride,
                    baseVariableId: action.data.baseVariableId
                }
                return setState({...state, metricBaseLookup: newLookup});
            case 'SET_CATEGORIES': return setState({...state, categories: action.data.categories});
            case 'REMOVE_METRIC_BASE':
                const lookup = {...state.metricBaseLookup};
                const keys = Object.keys(lookup).filter(key => lookup[key].baseVariableId === action.data.variableId);
                if(!keys){
                    return;
                }
                keys.forEach(k => lookup[k] = defaultBaseState);
                return setState({...state, metricBaseLookup: lookup});
            case 'SET_HEATMAP_OPTIONS': return setState({ ...state, heatMapOptions: action.data.heatMapOptions });
            case 'SET_HIDE_TOTAL_COLUMN': return setState({ ...state, hideTotalColumn: action.data.hideTotalColumn });
            case 'SET_SHOW_MULTIPLE_TABLES_AS_SINGLE': return setState({ ...state, showMultipleTablesAsSingle: action.data.showMultipleTablesAsSingle });
            case 'SET_LOW_SAMPLE_THRESHOLD': {
                return setState({ ...state, lowSampleThreshold: action.data.lowSampleThreshold });
            }
        }
    }

    return (
        <CrosstabPageStateContext.Provider value={{crosstabPageState: state, crosstabPageDispatch: dispatch}}>
            {props.children}
        </CrosstabPageStateContext.Provider>
    )
}