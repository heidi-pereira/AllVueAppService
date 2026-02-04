import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import { Factory, IVariableConfigurationModel, VariableConfigurationModel } from "../BrandVueApi";
import { createSelector } from 'reselect'
import { RootState } from './store';

interface VariableConfigurationStateBase {
    loading: boolean;
    error?: string | null;
}

export interface VariableConfigurationState extends VariableConfigurationStateBase {
    variables: IVariableConfigurationModel[];
};
export interface HydratedVariableConfigurationState extends VariableConfigurationStateBase {
    variables: VariableConfigurationModel[];
};
const initialState: VariableConfigurationState = {
    variables: [],
    loading: false,
    error: null,
};

export const fetchVariableConfiguration = createAsyncThunk<IVariableConfigurationModel[]>(
    'variableConfiguration/fetch',
    async (_, { rejectWithValue }) => {
        try {
            const fetchedVariables = await Factory.VariableConfigurationClient((_, error) => {
                throw error;
            }).getVariables();
            return fetchedVariables.map(variable => variable.toJSON());
        } catch (error) {
            return rejectWithValue(error);
        }
    }
);

const variableConfigurationSlice = createSlice({
    name: 'variableConfiguration',
    initialState,
    reducers: {},
    extraReducers: (builder) => {
        builder
            .addCase(fetchVariableConfiguration.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(fetchVariableConfiguration.fulfilled, (state, action) => {
                state.loading = false;
                state.variables = action.payload;
            })
            .addCase(fetchVariableConfiguration.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'An error occurred while fetching variable configurations';
            });
    },
});

/**
 * If your code uses "instanceof" you need to use this function to convert the plain JSON object to a class instance
 * This selector prevents a refresh loop for things that use this as props
 * Usage: useAppSelector(selectHydratedVariableConfiguration)
 */
export const selectHydratedVariableConfiguration = createSelector(
    [(state: RootState) => state.variableConfiguration],
    (variableConfiguration): HydratedVariableConfigurationState => {
        return {
            ...variableConfiguration,
            variables: variableConfiguration?.variables?.map(v => VariableConfigurationModel.fromJS(v)) ?? null
        };
    }
)

export default variableConfigurationSlice.reducer;
