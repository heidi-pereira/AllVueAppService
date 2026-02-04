import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { ReportTemplate } from 'client/BrandVueApi';
import * as BrandVueApi from '../BrandVueApi';

interface TemplatesState {
    templates: ReportTemplate[];
    isLoading: boolean;
    error: string | null;
}

const initialState: TemplatesState = {
    templates: [],
    isLoading: false,
    error: null,
};

export const fetchTemplatesForUser = createAsyncThunk<ReportTemplate[], void, { rejectValue: string }>(
    'templates/fetchTemplatesForUser',
    async (_, { rejectWithValue }) => {
        try {
            const client = BrandVueApi.Factory.ReportTemplateClient(error => error());
            const templates = await client.getAllTemplatesForUser();
            return templates;
        } catch (err: any) {
            return rejectWithValue(err?.message || 'Failed to fetch templates');
        }
    }
);

export const deleteTemplateById = createAsyncThunk<number, number, { rejectValue: string }>(
    'templates/deleteTemplateById',
    async (templateId, { rejectWithValue }) => {
        try {
            const client = BrandVueApi.Factory.ReportTemplateClient(error => error());
            await client.deleteTemplate(templateId);
            return templateId;
        } catch (err: any) {
            return rejectWithValue(err?.message || 'Failed to delete template');
        }
    }
);

const templatesSlice = createSlice({
    name: 'templates',
    initialState,
    reducers: {},
    extraReducers: builder => {
        builder
            .addCase(fetchTemplatesForUser.pending, state => {
                state.isLoading = true;
                state.error = null;
            })
            .addCase(fetchTemplatesForUser.fulfilled, (state, action: PayloadAction<ReportTemplate[]>) => {
                state.templates = action.payload;
                state.isLoading = false;
            })
            .addCase(fetchTemplatesForUser.rejected, (state, action) => {
                state.isLoading = false;
                state.error = action.payload || 'Unknown error';
            })
            .addCase(deleteTemplateById.pending, state => {
                state.error = null;
            })
            .addCase(deleteTemplateById.fulfilled, (state, action: PayloadAction<number>) => {
                const id = action.payload;
                state.templates = state.templates.filter(t => t.id !== id);
            })
            .addCase(deleteTemplateById.rejected, (state, action) => {
                state.error = action.payload || 'Unknown error';
            });
    },
});

export default templatesSlice.reducer;
