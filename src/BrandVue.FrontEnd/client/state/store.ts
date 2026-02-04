import { combineReducers, configureStore, createSelector } from "@reduxjs/toolkit";
import { TypedUseSelectorHook, useDispatch, useSelector } from "react-redux";
import resultsReducer from "./resultsSlice";
import llmInsightsReducer from "./llmInsightSlice";
import llmDiscoveryReducer from "./llmDiscoverySlice";
import reportReducer from "./reportSlice";
import variableConfigurationReducer from "./variableConfigurationsSlice";
import entityConfigurationReducer from "./entityConfigurationSlice";
import entitySelectionReducer from "./entitySelectionSlice";
import timeSelectionReducer from "./timeSelectionSlice";
import featuresReducer from "./featuresSlice";
import applicationReducer from "./applicationSlice";
import subsetReducer from "./subsetSlice";
import averageReducer from "./averageSlice";
import templatesReducer from "./templatesSlice";

const rootReducer = combineReducers({
    results: resultsReducer,
    llmInsights: llmInsightsReducer,
    entitySelection: entitySelectionReducer,
    timeSelection: timeSelectionReducer,
    llmDiscovery: llmDiscoveryReducer,
    report: reportReducer,
    variableConfiguration: variableConfigurationReducer,
    entityConfiguration: entityConfigurationReducer,
    features: featuresReducer,
    application: applicationReducer,
    subset: subsetReducer,
    average: averageReducer,
    templates: templatesReducer,
});

export const setupStore = (preloadedState?: Partial<RootState>) => {
    return configureStore({
        reducer: rootReducer,
        middleware: (getDefaultMiddleware) =>
            getDefaultMiddleware({
                serializableCheck: false,
            }),
        preloadedState,
    });
};

export const store = setupStore();
export type RootState = ReturnType<typeof rootReducer>;
export type AppStore = ReturnType<typeof setupStore>;
export type AppDispatch = AppStore["dispatch"];
export const useAppDispatch = () => useDispatch<AppDispatch>();
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;
export const createAppSelector = createSelector.withTypes<RootState>();
