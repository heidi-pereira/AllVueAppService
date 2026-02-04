import {createAsyncThunk, createSlice} from '@reduxjs/toolkit';
import {
    Factory,
    LlmDiscoveryClient,
    LlmDiscoveryRequest,
    AnnotatedQueryParams
} from "../BrandVueApi";

export interface LlmDiscoveryState {
    results: AnnotatedQueryParams[] | null,
    requested: Date | null,
    loading: boolean,
    error?: string | null,
}

const initialState: LlmDiscoveryState = {
    results: null,
    loading: false,
    requested: null,
};

export const fetchLlmDiscoveryResult = createAsyncThunk(
    'llmDiscoveryResults',
    async ({ request }: { request: LlmDiscoveryRequest }) => {
        
        const response = await Factory.LlmDiscoveryClient(err => err()).discover(request);
        return (await response);
    }
);

const llmDiscoverySlice = createSlice({
    name: 'llmDiscovery',
    initialState,
    reducers: {
        clearLlmDiscoveryResults(state) {
            state.results = null;
            state.requested = null;
            state.loading = false;
            state.error = null;
        }
    },
    extraReducers: (builder) => {
        builder
            .addCase(fetchLlmDiscoveryResult.pending, (state) => {
                state.loading = true;
                state.error = null;
                state.requested = new Date(Date.now());
            })
            .addCase(fetchLlmDiscoveryResult.fulfilled, (state, action) => {
                state.loading = false;
                state.results = action.payload;
            })
            .addCase(fetchLlmDiscoveryResult.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'An error occurred';
            });
    },
});

export const { clearLlmDiscoveryResults } = llmDiscoverySlice.actions;

export default llmDiscoverySlice.reducer;