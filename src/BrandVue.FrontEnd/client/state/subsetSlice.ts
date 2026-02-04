import { createSlice, PayloadAction, createAsyncThunk } from '@reduxjs/toolkit';
import { RootState } from './store';
import { Factory } from '../BrandVueApi';
import { throwIfNullish } from 'client/components/helpers/ThrowHelper';

interface SubsetState {
    subsetId: string | undefined;
    subsetConfigurations: any[];
}

const initialState: SubsetState = {
    subsetId: undefined,
    subsetConfigurations: [],
};

export const fetchSubsetConfigurations = createAsyncThunk<any[]>(
    'subset/fetchSubsetConfigurations',
    async (_, { rejectWithValue }) => {
        try {
            const client = Factory.SubsetsClient(error => { throw error; });
            const subsets = await client.getSubsetConfigurations();
            return subsets.filter((subset: any) => !subset.disabled).map((subset: any) =>
                typeof subset.toJSON === 'function' ? subset.toJSON() : { ...subset }
            );
        } catch (error) {
            return rejectWithValue(error);
        }
    }
);

const subsetSlice = createSlice({
    name: 'subset',
    initialState,
    reducers: {
        updateSubset(state, action: PayloadAction<string>) {
            state.subsetId = action.payload;
        }
    },
    extraReducers: (builder) => {
        builder
            .addCase(fetchSubsetConfigurations.fulfilled, (state, action) => {
                state.subsetConfigurations = action.payload;
            })
    }
});

export const { updateSubset } = subsetSlice.actions;
export default subsetSlice.reducer;

export const selectSubsetId = (state: RootState) => throwIfNullish(state.subset.subsetId, "subset id");
export const selectSubsetConfigurations = (state: RootState) => state.subset.subsetConfigurations;