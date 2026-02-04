import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import * as BrandVueApi from '../BrandVueApi';
import { IFeatureModel, IUserFeatureModel, IOrganisationFeatureModel, ICompanyModel, IUserProjectsModel } from '../BrandVueApi';

interface FeaturesState {
    features: IFeatureModel[];
    userFeaturesByFeatureId: { [featureId: number]: IUserFeatureModel[] };
    orgFeatures: IOrganisationFeatureModel[];
    allUsers: IUserProjectsModel[];
    allOrgs: ICompanyModel[];
    loading: boolean;
    error: string | null;
}

const initialState: FeaturesState = {
    features: [],
    userFeaturesByFeatureId: {},
    orgFeatures: [],
    allUsers: [],
    allOrgs: [],
    loading: false,
    error: null,
};

export const updateFeature = createAsyncThunk<IFeatureModel, IFeatureModel, { rejectValue: string }>(
    'features/updateFeature',
    async (featureToUpdate, { rejectWithValue }) => {
        try {
            const featuresClient = BrandVueApi.Factory.FeaturesClient((error) => error());
            await featuresClient.update(
                featureToUpdate.id,
                featureToUpdate.documentationUrl,
                featureToUpdate.isActive,
                featureToUpdate.featureCode,
                featureToUpdate.name
            );
            return featureToUpdate;
        } catch (error) {
            return rejectWithValue(`Failed to update feature ${featureToUpdate.featureCode}`);
        }
    }
);

export const activateFeature = createAsyncThunk<{oldFeatureID: number, newFeatureID: number}, IFeatureModel, { rejectValue: string }>(
    'features/activateFeature',
    async (featureToActivate, { rejectWithValue }) => {
        try {
            const featuresClient = BrandVueApi.Factory.FeaturesClient((error) => error());
            const newFeatureID = await featuresClient.activateFeature(
                featureToActivate.id,
                featureToActivate.documentationUrl,
                featureToActivate.isActive,
                featureToActivate.featureCode, 
                featureToActivate.name
            );
            return {oldFeatureID: featureToActivate.id, newFeatureID: newFeatureID};
        } catch (error) {
            return rejectWithValue(`Failed to activate feature ${featureToActivate.id}`);
        }
    }
);

export const deactivateFeature = createAsyncThunk<number, number, { rejectValue: string }>(
    'features/deactivateFeature',
    async (featureId, { rejectWithValue }) => {
        try {
            const featuresClient = BrandVueApi.Factory.FeaturesClient((error) => error());
            await featuresClient.deactivateFeature(featureId);
            return featureId;
        } catch (error) {
            return rejectWithValue(`Failed to deactiviate feature ${featureId}`);
        }
    }
);

export const deleteFeature = createAsyncThunk<
    number,
    number,
    { rejectValue: string }
>(
    'features/deleteFeature',
    async (featureId, { rejectWithValue }) => {
        try {
            const featuresClient = BrandVueApi.Factory.FeaturesClient((error) => error());
            const success = await featuresClient.delete(featureId);
            if (!success) {
                throw new Error('Failed to delete feature');
            }
            return featureId;
        } catch (error) {
            return rejectWithValue('Failed to delete feature');
        }
    }
);

export const fetchFeatures = createAsyncThunk<IFeatureModel[], void, { rejectValue: string }>('features/fetchFeatures', async (_, { rejectWithValue }) => {
    try {
        const features = await BrandVueApi.Factory.FeaturesClient((error) => error()).getFeatures();
        return features.map((feature) => feature.toJSON());
    } catch (error) {
        return rejectWithValue('Failed to fetch features');
    }
});

export const fetchUserFeatures = createAsyncThunk<{ featureId: number; features: IUserFeatureModel[] }, number, { rejectValue: string }>(
    'features/fetchUserFeatures', 
    async (featureId: number, { rejectWithValue }) => {
        try {
            const userFeaturesClient = BrandVueApi.Factory.UserFeaturesClient((error) => error());
            const features = (await userFeaturesClient.getUserFeaturesByFeature(featureId)).map((feature) => feature.toJSON());
            return { featureId, features };
        } catch (error) {
            return rejectWithValue('Failed to fetch user features');
        }
    }
);

export const fetchOrgFeatures = createAsyncThunk<IOrganisationFeatureModel[], void, { rejectValue: string }>(
    'features/fetchOrgFeatures', 
    async (_, { rejectWithValue }) => {
        try {
            const orgFeaturesClient = BrandVueApi.Factory.OrganisationFeaturesClient((error) => error());
            const orgFeatures = (await orgFeaturesClient.getAllOrganisationFeatures()).map((of) => of.toJSON());
            return orgFeatures;
        } catch (error) {
            return rejectWithValue('Failed to fetch organization features');
        }
    }
);

export const fetchAllOrgs = createAsyncThunk<ICompanyModel[], void, { rejectValue: string }>('features/fetchAllOrgs', async (_, { rejectWithValue }) => {
    try {
        const orgsClient = BrandVueApi.Factory.ConfigClient((error) => error());
        const orgs = (await orgsClient.getAllAuthCompanies()).map((org) => org.toJSON());
        return orgs;
    } catch (error) {
        return rejectWithValue('Failed to fetch organizations');
    }
});

export const fetchAllUsers = createAsyncThunk<IUserProjectsModel[], void, { rejectValue: string }>('features/fetchAllUsers', async (_, { rejectWithValue }) => {
    try {
        const usersClient = BrandVueApi.Factory.UsersClient((error) => error());
        const users = (await usersClient.allUsers()).toJSON();
        return users.users;
    } catch (error) {
        return rejectWithValue('Failed to fetch users');
    }
});

export const deleteOrgFeature = createAsyncThunk<{ organisationId: string; featureId: number }, { organisationId: string; featureId: number }, { rejectValue: string }>(
    'features/deleteOrgFeature', 
    async ({ organisationId, featureId }: { organisationId: string; featureId: number }, { rejectWithValue }) => {
        try {
            const orgFeaturesClient = BrandVueApi.Factory.OrganisationFeaturesClient((error) => error());
            const success = await orgFeaturesClient.deleteOrganisationFeature(organisationId, featureId);
            if (!success) {
                throw new Error('Failed to delete organization feature');
            }
            return { organisationId, featureId };
        } catch (error) {
            return rejectWithValue('Failed to delete organization feature');
        }
    }
);

export const deleteUserFeature = createAsyncThunk<{ userId: string; featureId: number }, { userId: string; featureId: number }, { rejectValue: string }>(
    'features/deleteUserFeature', 
    async ({ userId, featureId }: { userId: string; featureId: number }, { rejectWithValue }) => {
        try {
            const userFeaturesClient = BrandVueApi.Factory.UserFeaturesClient((error) => error());
            const success = await userFeaturesClient.deleteUserFeature(userId, featureId);
            if (!success) {
                throw new Error('Failed to delete organization feature');
            }
            return { userId, featureId };
        } catch (error) {
            return rejectWithValue('Failed to delete organization feature');
        }
    }
);

export const clearFeaturesCache = createAsyncThunk<boolean, void, { rejectValue: string }>('features/clearFeaturesCache', async (_, { rejectWithValue }) => {
    try {
        const userFeaturesClient = BrandVueApi.Factory.UserFeaturesClient((error) => error());
        await userFeaturesClient.clearCache();
        return true;
    } catch (error) {
        return rejectWithValue('Failed to reload user features cache');
    }
});

export const createOrgFeature = createAsyncThunk<IOrganisationFeatureModel, { organisationId: string; featureId: number }>(
    'features/createOrgFeature',
    async ({ organisationId, featureId }, { rejectWithValue }) => {
        try {
            const orgFeaturesClient = BrandVueApi.Factory.OrganisationFeaturesClient((error) => error());
            const createdOrgFeature = await orgFeaturesClient.setOrganisationFeature(organisationId, featureId);
            if (!createdOrgFeature) {
                throw new Error('Failed to create organization feature');
            }
            return createdOrgFeature.toJSON();
        } catch (error) {
            return rejectWithValue('Failed to create organization feature');
        }
    }
);

export const createUserFeature = createAsyncThunk<IUserFeatureModel, { userId: string; featureId: number }>(
    'features/createUserFeature',
    async ({ userId, featureId }, { rejectWithValue }) => {
        try {
            const userFeaturesClient = BrandVueApi.Factory.UserFeaturesClient((error) => error());
            const createdUserFeature = await userFeaturesClient.setUserFeature(userId, featureId);
            if (!createdUserFeature) {
                throw new Error('Failed to create user feature');
            }
            return createdUserFeature.toJSON();
        } catch (error) {
            return rejectWithValue('Failed to create user feature');
        }
    }
);

const featuresSlice = createSlice({
    name: 'features',
    initialState,
    reducers: {},
    extraReducers: (builder) => {
        builder
            .addCase(deleteFeature.fulfilled, (state, action: PayloadAction<number>) => {
                state.features = state.features.filter(f => f.id !== action.payload);
            })
            .addCase(fetchFeatures.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(fetchFeatures.fulfilled, (state, action: PayloadAction<IFeatureModel[]>) => {
                state.loading = false;
                state.features = action.payload;
            })
            .addCase(fetchFeatures.rejected, (state, action) => {
                state.loading = false;
                state.error = action.payload ?? null;
            })
            .addCase(fetchUserFeatures.fulfilled, (state, action: PayloadAction<{ featureId: number; features: IUserFeatureModel[] }>) => {
                const { featureId, features } = action.payload;
                state.userFeaturesByFeatureId[featureId] = features;
            })
            .addCase(fetchOrgFeatures.fulfilled, (state, action: PayloadAction<IOrganisationFeatureModel[]>) => {
                state.orgFeatures = action.payload;
            })
            .addCase(fetchAllOrgs.fulfilled, (state, action: PayloadAction<ICompanyModel[]>) => {
                state.allOrgs = action.payload;
            })
            .addCase(fetchAllUsers.fulfilled, (state, action) => {
                state.allUsers = action.payload;
            })
            .addCase(deleteOrgFeature.fulfilled, (state, action: PayloadAction<{ organisationId: string; featureId: number }>) => {
                const { organisationId, featureId } = action.payload;
                state.orgFeatures = state.orgFeatures.filter((of) => !(of.organisationId === organisationId && of.featureId === featureId));
            })
            .addCase(deleteUserFeature.fulfilled, (state, action: PayloadAction<{ userId: string; featureId: number }>) => {
                const { featureId, userId } = action.payload;
                const userFeatures = state.userFeaturesByFeatureId[featureId] || [];
                state.userFeaturesByFeatureId[featureId] = userFeatures.filter((uf) => uf.userId !== userId);
            })
            .addCase(createOrgFeature.fulfilled, (state, action: PayloadAction<IOrganisationFeatureModel>) => {
                const newFeature = action.payload;
                state.orgFeatures.push(newFeature);
            })
            .addCase(createUserFeature.fulfilled, (state, action: PayloadAction<IUserFeatureModel>) => {
                const newFeature = action.payload;
                if (!state.userFeaturesByFeatureId[newFeature.featureId]) {
                    state.userFeaturesByFeatureId[newFeature.featureId] = [];
                }
                state.userFeaturesByFeatureId[newFeature.featureId].push(newFeature);
            })
            .addCase(updateFeature.fulfilled, (state, action: PayloadAction<IFeatureModel>) => {
                const updated = action.payload;
                const idx = state.features.findIndex(f => f.id === updated.id);
                if (idx !== -1) {
                    state.features[idx] = updated;
                }
            })
            .addCase(activateFeature.fulfilled, (state, action: PayloadAction<{oldFeatureID: number, newFeatureID: number}>) => {
                const { oldFeatureID, newFeatureID } = action.payload;
                const idx = state.features.findIndex(f => f.id === oldFeatureID);
                if (idx !== -1) {
                    state.features[idx].id = newFeatureID;
                    state.features[idx].isActive = true;
                }
            })
            .addCase(deactivateFeature.fulfilled, (state, action: PayloadAction<number>) => {
                const featureId = action.payload;
                const idx = state.features.findIndex(f => f.id === featureId);
                if (idx !== -1) {
                    state.features[idx].isActive = false;
                }
            });
    },
});

export default featuresSlice.reducer;