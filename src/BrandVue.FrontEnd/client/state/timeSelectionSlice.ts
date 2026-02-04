import { createSlice, PayloadAction } from "@reduxjs/toolkit";

export interface TimeSelectionState {
    scorecardPeriod: string | null;
}

const initialState: TimeSelectionState = {
    scorecardPeriod: null,
};

const timeSelectionSlice = createSlice({
    name: "time",
    initialState,
    reducers: {
        setScorecardPeriod(state, action: PayloadAction<string>) {
            state.scorecardPeriod = action.payload;
        },
    },
});

export const { setScorecardPeriod } = timeSelectionSlice.actions;
export default timeSelectionSlice.reducer;
