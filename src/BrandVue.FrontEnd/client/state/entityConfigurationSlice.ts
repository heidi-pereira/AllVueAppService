import { createAsyncThunk, createSlice, PayloadAction } from '@reduxjs/toolkit';
import { Factory, IEntityTypeConfiguration } from "../BrandVueApi";

interface EntityConfigurationState {
    activeEntityTypeIdentifier: string | null;
    configurationByIdentifier: Record<string, IEntityTypeConfiguration>;
    loading: boolean;
    error: string | null;
}

const initialState: EntityConfigurationState = {
    activeEntityTypeIdentifier: null,
    configurationByIdentifier: {},
    loading: false,
    error: null,
};

export const fetchEntityTypeConfigurations = createAsyncThunk(
    'entityConfiguration/fetchEntityTypeConfigurations',
    async (_, { rejectWithValue }) => {
        try {
            const entitiesClient = Factory.EntitiesClient(() => {});
            const configurations = await entitiesClient.getEntityTypeConfigurations();
            return configurations;
        } catch (error) {
            return rejectWithValue(error);
        }
    }
);

export const saveEntityType = createAsyncThunk(
    'entityConfiguration/saveEntityType',
    async ({ identifier, displayNameSingular, displayNamePlural }: { identifier: string; displayNameSingular: string; displayNamePlural: string }, { rejectWithValue }) => {
        try {
            const entitiesClient = Factory.EntitiesClient(() => {});
            const updatedEntityType = await entitiesClient.saveEntityType(identifier, displayNameSingular, displayNamePlural);
            return updatedEntityType;
        } catch (error) {
            return rejectWithValue(error);
        }
    }
);

const entityTypeConfigurationSlice = createSlice({
    name: 'entityConfiguration',
    initialState,
    reducers: {
        setActiveEntityTypeByIdentifier(state, action: PayloadAction<string>) {
            state.activeEntityTypeIdentifier = action.payload;
        },
    },
    extraReducers: (builder) => {
        builder
            .addCase(fetchEntityTypeConfigurations.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(fetchEntityTypeConfigurations.fulfilled, (state, action) => {
                state.loading = false;
                state.configurationByIdentifier = action.payload.reduce((acc, config) => {
                    // Avoids subtle bugs: Convert the fetched configurations to serializable object for store
                    acc[config.identifier] = config.toJSON();
                    return acc;
                }, {} as Record<string, IEntityTypeConfiguration>);
            })
            .addCase(fetchEntityTypeConfigurations.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'Failed to fetch entity type configurations';
            })
            .addCase(saveEntityType.fulfilled, (state, action) => {
                const updatedEntityType = action.payload;
                if (updatedEntityType) {
                    state.configurationByIdentifier[updatedEntityType.identifier] = updatedEntityType;
                }
            });
    },
});

export const { setActiveEntityTypeByIdentifier } = entityTypeConfigurationSlice.actions;
export default entityTypeConfigurationSlice.reducer;