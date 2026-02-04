import 'whatwg-fetch';
import moment from "moment";
import 'moment/locale/en-gb';
import 'material-symbols/outlined.css';
import 'material-icons/iconfont/material-icons.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap';
import "../less/main.less";
import { store, useAppDispatch } from "./state/store";
import { highchartsGlobalConfiguration } from "./highcharts/highchartsGlobalConfiguration";
import { dsession } from "./dsession";
import Throbber from "./components/throbber/Throbber";
import { EntitySetFactory, IEntitySetFactory } from "./entity/EntitySetFactory";
import { EntityInstanceColourRepository } from "./entity/EntityInstanceColourRepository";
import { EntitySetFactoryContext, GlobalContext, UserContext } from "./GlobalContext";
import { ApplicationConfiguration } from './ApplicationConfiguration';
import { ProductConfiguration } from './ProductConfiguration';
import { SavedBreaksProvider } from './components/visualisations/Crosstab/SavedBreaksContext';
import { MetricStateProvider } from './metrics/MetricStateContext';
import { CatchReportAndDisplayErrors } from './components/CatchReportAndDisplayErrors';
import { hasAllVuePermissionsOrSystemAdmin  } from './components/helpers/FeaturesHelper';
import {
    EntityConfigurationStateProvider
} from './entity/EntityConfigurationStateContext';
import BrandVueContent from './BrandVueContent';
import { CategoryContextProvider } from './components/helpers/CategoryContext';
import { BaseVariableContextProvider } from './components/visualisations/Variables/BaseVariableContext';
import { ComparisonContextProvider } from './components/helpers/ComparisonContext';
import { UserFeaturesProvider } from './features/UserFeaturesContext';
import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useReadVueQueryParams, useWriteVueQueryParams } from "./components/helpers/UrlHelper";
import {Provider} from "react-redux";
import { EntityConfigurationLoader } from "./entity/EntityConfigurationLoader";
import { setSessionLoaded } from './state/applicationSlice';
import { selectSubsetId, updateSubset } from './state/subsetSlice';
import { updateAverages } from './state/averageSlice';
import {PermissionFeaturesOptions} from "./BrandVueApi";
export interface IBrandVueProps {
    productConfiguration: ProductConfiguration,
}

export interface IBrandVueState {
    initComplete: boolean,
    entitySetFactory: IEntitySetFactory,
    session: dsession,
    applicationConfiguration: ApplicationConfiguration
}

export default function BrandVue(props: IBrandVueProps) {
    const [state, setState] = useState<IBrandVueState>(() => ({
        initComplete: false,
        entitySetFactory: new EntitySetFactory(EntityInstanceColourRepository.empty()),
        applicationConfiguration: new ApplicationConfiguration(),
        session: new dsession()
    }));
    const location = useLocation();
    const navigate = useNavigate();
    const readQueryParams = useReadVueQueryParams();
    const writeQueryParams = useWriteVueQueryParams(navigate, location);
    const dispatch = useAppDispatch();
    useEffect(() => {
        async function init() {
            try {
                new highchartsGlobalConfiguration().configure();
                moment.locale(window.navigator["userLanguage"] || window.navigator["language"]);

                // Init all async stuff first
                const entityInstanceColourRepository = await EntityInstanceColourRepository.create();
                const entitySetFactory = new EntitySetFactory(entityInstanceColourRepository);
                await state.session.init(state.applicationConfiguration, location, readQueryParams, writeQueryParams);
                setState(prev => ({
                    ...prev,
                    initComplete: true,
                    entitySetFactory: entitySetFactory
                }));
                dispatch(updateSubset(state.session.selectedSubsetId));
                dispatch(updateAverages(state.session.averages));
                dispatch(setSessionLoaded(true));
            } catch (err) {
                setState(() => { throw err; });
            }
        }

        init();
    }, []);

    if (!state.initComplete) {
        return (
            <div id="ld" className="loading-container">
                <Throbber />
            </div>
        );
    }

    return (
        <CatchReportAndDisplayErrors applicationConfiguration={state.applicationConfiguration} childInfo={{ "Component": "BrandVue" }}>
            <UserContext.Provider value={props.productConfiguration.user}>
                <GlobalContext.Provider value={{ pageHandler: state.session.pageHandler }}>
                    <EntitySetFactoryContext.Provider value={state.entitySetFactory}>
                        <SavedBreaksProvider pageHandler={state.session.pageHandler}>
                            <EntityConfigurationStateProvider entitySetFactory={state.entitySetFactory} loader={new EntityConfigurationLoader()}>
                                <BaseVariableContextProvider>
                                    <CategoryContextProvider>
                                        <ComparisonContextProvider>
                                            <UserFeaturesProvider user={props.productConfiguration.user}>
                                                <MetricStateProvider
                                                    userCanSeeAllMetrics={hasAllVuePermissionsOrSystemAdmin(props.productConfiguration, [PermissionFeaturesOptions.VariablesEdit, PermissionFeaturesOptions.VariablesCreate])}
                                                    isSurveyVue={props.productConfiguration.isSurveyVue()}>
                                                            <Provider store={store}>
                                                                <BrandVueContent {...props} {...state} />
                                                            </Provider>
                                                </MetricStateProvider>
                                            </UserFeaturesProvider>
                                        </ComparisonContextProvider>
                                    </CategoryContextProvider>
                                </BaseVariableContextProvider>
                            </EntityConfigurationStateProvider>
                        </SavedBreaksProvider>
                    </EntitySetFactoryContext.Provider>
                </GlobalContext.Provider>
            </UserContext.Provider>
        </CatchReportAndDisplayErrors>
    );
}