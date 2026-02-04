import {createSlice, PayloadAction, Store} from '@reduxjs/toolkit';
import {
    BreakdownResults,
    CompetitionResults, FunnelResults,
    ICuratedResultsModel,
    IMultiEntityRequestModel,
    MultiEntityRequestModel,
    OverTimeAverageResults,
    OverTimeResults,
    RankingTableResults,
    ScorecardPerformanceCompetitorResults,
    ScorecardPerformanceResults
} from "../BrandVueApi";

export type StoreResultsUnion = OverTimeResults | RankingTableResults | ScorecardPerformanceResults | CompetitionResults | FunnelResults | BreakdownResults;
export type StoreAverageResultsUnion = OverTimeAverageResults | RankingTableResults | ScorecardPerformanceCompetitorResults | FunnelResults | BreakdownResults;
export type StoreRequestUnion = IMultiEntityRequestModel | ICuratedResultsModel;
export interface ResultsState {
    results: { [partId: number] : WeightedResultWithRequest };
    averages: { [partId: number] : WeightedAverageResultWithRequest[] };
}
export interface IAverageData {
    name: string,
    results: StoreAverageResultsUnion
}
export interface WeightedAverageResultWithRequest extends IAverageData {
    request: StoreRequestUnion;
    requested?: Date;
}

export interface WeightedResultWithRequest {
    request: StoreRequestUnion;
    results: StoreResultsUnion;
    requested: Date;
    averagesSelected?: number;
}

const initialState: ResultsState = {
    results: {},
    averages: {}
}

interface GenericChartRequestPayload {
    results: StoreResultsUnion, 
    request: StoreRequestUnion, 
    partId: number,  
    averagesSelected?: number,
    averages: WeightedAverageResultWithRequest[]
}

interface OverTimePayload {
    results: OverTimeResults, 
    request: MultiEntityRequestModel, 
    partId: number, 
    averagesSelected?: number,
    focusedInstanceId: number
}

interface OverTimePayloadWithAverages extends OverTimePayload {
    averages: WeightedAverageResultWithRequest[],
}

const weightedResultsSlice = createSlice({
    name: 'weightedResults',
    initialState,
    reducers: {
        setGenericResults(state: ResultsState, action: PayloadAction<GenericChartRequestPayload>) {
            const { results, request, partId, averagesSelected} = action.payload;
            state.results[partId] = {
                request: request,
                results: results,
                averagesSelected: averagesSelected,
                requested: new Date(Date.now()),
            };
            state.averages[partId] = state.averages[partId] ?? [];
        },
        setGenericAverageResults(state: ResultsState, action: PayloadAction<{partId: number, payload: WeightedAverageResultWithRequest[]}>) {
            const {payload, partId} = action.payload;
            state.averages[partId] = payload.map(x=>({...x, requested: new Date(Date.now())}));
        },
        setOverTimeResults(state: ResultsState, action: PayloadAction<OverTimePayload>) {
            const { results, request, partId, averagesSelected} = action.payload;
            request.focusEntityInstanceId = action.payload.focusedInstanceId;
            state.results[partId] = {
                request: request,
                results: results,
                averagesSelected: averagesSelected,
                requested: new Date(Date.now()),
            };
        },
        setOverTimeResultsAndAverages(state: ResultsState, action: PayloadAction<OverTimePayloadWithAverages>) {
            const { results, request, partId, averages} = action.payload;
            request.focusEntityInstanceId = action.payload.focusedInstanceId;
            state.results[partId] = {
                request: request,
                results: results,
                averagesSelected: averages.length,
                requested: new Date(Date.now()),
            };
            state.averages[partId] = averages.map(x=>({...x, requested: new Date(Date.now())}));
        }
    },
});

export const { 
    setOverTimeResults, 
    setOverTimeResultsAndAverages, 
    setGenericResults, 
    setGenericAverageResults } = weightedResultsSlice.actions;
export default weightedResultsSlice.reducer;