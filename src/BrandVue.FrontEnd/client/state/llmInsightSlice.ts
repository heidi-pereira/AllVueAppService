import {createAsyncThunk, createSlice} from '@reduxjs/toolkit';
import {
    CompetitionRequestData,
    Factory, FunnelRequestData,
    ICompetitionRequestData, IFunnelRequestData,
    IOverTimeRequestData,
    IRankingRequestData,
    IScorecardPerformanceRequestData, LlmInsightResults,
    OverTimeRequestData,
    RankingRequestData,
    ScorecardPerformanceRequestData,
    LlmInsightFeedbackSegmentCorrectnessRequest,
    LlmInsightSegmentFeedbackUsefulnessRequest,
    LlmInsightsFeedbackUserCommentRequest, 
    ProfileRequestData, 
    IProfileRequestData
} from "../BrandVueApi";
import {
    StoreRequestUnion, 
    StoreResultsUnion, 
    WeightedAverageResultWithRequest 
} from "./resultsSlice";

export interface LlmInsightState {
    results: LlmInsightResults | null,
    requested: Date | null,
    loading: boolean,
    error?: string | null,
}

const initialState: LlmInsightState = {
    results: null,
    loading: false,
    requested: null,
};

export const fetchLlmInsightsResult = createAsyncThunk(
    'llmInsightsResults',
    async ({request, results, averages} : {request: StoreRequestUnion, results: StoreResultsUnion, averages: WeightedAverageResultWithRequest[]}) => {
        var insightRequestArgs = {
            request: request,
            results: results,
            averageData: averages
        };
        switch (results.typeName) {
            case 'OverTimeResults':
                return await Factory.LlmInsightsClient(err => err()).overTimeRequestInsight(
                    new OverTimeRequestData(insightRequestArgs as IOverTimeRequestData)
                );
            case 'RankingTableResults':
                return await Factory.LlmInsightsClient(err => err()).rankingRequestInsight(
                    new RankingRequestData(insightRequestArgs as IRankingRequestData)
                );
            case 'ScorecardPerformanceResults':
                return await Factory.LlmInsightsClient(err => err()).scorecardPerformanceRequestInsight(
                    new ScorecardPerformanceRequestData(insightRequestArgs as IScorecardPerformanceRequestData)
                )
            case 'CompetitionResults':
                return await Factory.LlmInsightsClient(err => err()).competitionRequestInsight(
                    new CompetitionRequestData(insightRequestArgs as ICompetitionRequestData)
                );
            case 'FunnelResults':
                return await Factory.LlmInsightsClient(err => err()).funnelRequestInsight(
                    new FunnelRequestData(insightRequestArgs as IFunnelRequestData)
                );
            case 'BreakdownResults':
                return await Factory.LlmInsightsClient(err => err()).profileRequestInsight(
                    new ProfileRequestData(insightRequestArgs as IProfileRequestData)
                );
            default:
                return null;
        }
    }
);

export const updateUsefulness = createAsyncThunk(
    'llmInsights/updateUserFeedbackSegmentUsefulness',
    async (
        { id, isUseful }: { id: string; isUseful: boolean | null },
        { rejectWithValue }
    ) => {
        if (isUseful === null) return;
        try {
            await Factory.LlmInsightsClient(err => err()).llmInsightSegmentFeedbackUsefulness(
                id,
                new LlmInsightSegmentFeedbackUsefulnessRequest({ isUseful })
            );
            return isUseful;
        } catch (error) {
            return rejectWithValue(error);
        }
    }
);

export const updateUserComment = createAsyncThunk(
    'llmInsights/updateUserFeedbackComment',
    async (
        { id, userComment }: { id: string; userComment: string },
        { rejectWithValue }
    ) => {
        try {
            await Factory.LlmInsightsClient(err => err()).llmInsightsFeedbackUserComment(
                id,
                new LlmInsightsFeedbackUserCommentRequest({ userComment })
            );
            return userComment;
        } catch (error) {
            return rejectWithValue(error);
        }
    }
);

export const updateSegmentCorrectness = createAsyncThunk(
    'llmInsights/updateUserFeedbackSegmentCorrectness',
    async (
        { id, segmentId, isCorrect }: { id: string; segmentId: number; isCorrect: boolean },
        { rejectWithValue }
    ) => {
        try {
            await Factory.LlmInsightsClient(err => err()).llmInsightFeedbackSegmentCorrectness(
                id,
                new LlmInsightFeedbackSegmentCorrectnessRequest({ segmentId, isCorrect })
            );
            return { segmentId, isCorrect };
        } catch (error) {
            return rejectWithValue(error);
        }
    }
);

const llmInsightsSlice = createSlice({
    name: 'llmInsights',
    initialState,
    reducers: {
        clearLlmInsightResults(state) {
            state.results = null;
            state.requested = null;
            state.loading = false;
            state.error = null;
        },
        noMoreFeedback(state) {
            if (state.results && state.results.userFeedback) {
                state.results = {...state.results, userFeedback: {...state.results.userFeedback, isUseful: undefined}};
            }
        }
    },
    extraReducers: (builder) => {
        builder
            .addCase(fetchLlmInsightsResult.pending, (state) => {
                state.loading = true;
                state.error = null;
                state.requested = new Date(Date.now());
            })
            .addCase(fetchLlmInsightsResult.fulfilled, (state, action) => {
                state.loading = false;
                state.results = action.payload;
            })
            .addCase(fetchLlmInsightsResult.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'An error occurred';
            })
            .addCase(updateSegmentCorrectness.pending, (state) => {
                state.error = null;
            })
            .addCase(updateSegmentCorrectness.fulfilled, (state, action) => {
                if (!state.results) return;
                const segmentIndex = state.results?.aiSummary.findIndex(item => item.segmentId === action.payload.segmentId);
                
                if (segmentIndex !== undefined) {
                    const newSegment = {...state.results?.aiSummary[segmentIndex], userFeedbackSegmentCorrectness: action.payload.isCorrect};
                    state.results.aiSummary[segmentIndex] = newSegment;
                }
            })
            .addCase(updateSegmentCorrectness.rejected, (state, action) => {
                state.error = action.error.message || 'An error occurred while updating segment correctness';
            })
            .addCase(updateUsefulness.pending, (state) => {
                state.error = null;
            })
            .addCase(updateUsefulness.fulfilled, (state, action) => {
                if (state.results && state.results.userFeedback) {
                    state.results = {...state.results, userFeedback: {...state.results.userFeedback, isUseful: action.payload}};
                }
            })
            .addCase(updateUsefulness.rejected, (state, action) => {
                state.error = action.error.message || 'An error occurred while updating segment usefulness';
            })
            .addCase(updateUserComment.pending, (state) => {
                state.error = null;
            })
            .addCase(updateUserComment.fulfilled, (state, action) => {
                if (state.results && state.results.userFeedback) {
                    state.results = {...state.results, userFeedback: {...state.results.userFeedback, userComment: action.payload}};
                }
            })
            .addCase(updateUserComment.rejected, (state, action) => {
                state.error = action.error.message || 'An error occurred while updating user comment';
            });
    },
});

export const { clearLlmInsightResults, noMoreFeedback } = llmInsightsSlice.actions;

export default llmInsightsSlice.reducer;