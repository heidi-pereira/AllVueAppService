import { createAsyncThunk, createSlice, PayloadAction } from "@reduxjs/toolkit";
import { createSelector } from "reselect";
import { RootState } from "./store";
import { Factory, IAverageDescriptor } from "../BrandVueApi";

interface AverageState {
    allAverages: IAverageDescriptor[];
}

const initialState: AverageState = {
    allAverages: [],
};

export const fetchAllAverages = createAsyncThunk<any[]>("average/fetchAverages", async (_, { rejectWithValue }) => {
    try {
        const client = Factory.MetaDataClient((error) => {
            throw error;
        });
        const averages = await client.getAllAverages();
        return averages.map((avg: any) => (typeof avg.toJSON === "function" ? avg.toJSON() : { ...avg }));
    } catch (error) {
        return rejectWithValue(error);
    }
});

const averageSlice = createSlice({
    name: "average",
    initialState,
    reducers: {
        updateAverages(state, action: PayloadAction<any[]>) {
            state.allAverages = action.payload;
        },
    },
    extraReducers: (builder) => {
        builder.addCase(fetchAllAverages.fulfilled, (state, action) => {
            state.allAverages = action.payload;
        });
    },
});

export const { updateAverages } = averageSlice.actions;
export default averageSlice.reducer;

export const selectAllAverages = (state: RootState): IAverageDescriptor[] => state.average.allAverages;

export const selectAveragesForSubset = createSelector(
    [(state: RootState) => state.average.allAverages, (_: RootState, subsetId: string) => subsetId],
    (allAverages, subsetId) => allAverages.filter((a) => (a.subset && a.subset.some((s: any) => s.id === subsetId)) || !a.subset || a.subset.length === 0)
);
