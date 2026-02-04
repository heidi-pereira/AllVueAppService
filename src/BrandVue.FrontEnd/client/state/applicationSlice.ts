import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { Metric } from '../metrics/metric';

/**
 * Only top level components should know about this.
 * It is *only* for top level state related to the general application.
 */
export interface ApplicationState {
    isSessionLoaded: boolean;
    primaryMetric: string | null;
}

const initialState: ApplicationState = {
    isSessionLoaded: false,
    primaryMetric: null,
};

const applicationSlice = createSlice({
    name: 'application',
    initialState,
    reducers: {
        setSessionLoaded: (state, action: PayloadAction<boolean>) => {
            state.isSessionLoaded = action.payload;
        },
        setPrimaryMetric: (state, action: PayloadAction<Metric>) => {
            state.primaryMetric = action.payload?.name;
        },
    },
});

export const { setSessionLoaded, setPrimaryMetric } = applicationSlice.actions;
export default applicationSlice.reducer;

export const selectPrimaryMetricWithDefaultOrNull = (state: { application: ApplicationState }, metrics: Metric[]) => {
    const primaryMetricName = state.application.primaryMetric;
    return metrics.find(metric => metric.name === primaryMetricName) ?? null;
};