import { Navigate, useLocation, useNavigate } from "react-router-dom";
import Throbber from "../../components/throbber/Throbber";
import { UserFeaturesProvider, useUserFeaturesContext } from "../../features/UserFeaturesContext";
import { ProductConfiguration } from "../../ProductConfiguration";
import TableBuilderPage from "./TableBuilderPage";
import { ComparisonPeriodSelection, DemographicFilter, FeatureCode, PermissionFeaturesOptions, WeightingMethod } from "../../BrandVueApi";
import { MetricStateProvider } from "../../metrics/MetricStateContext";
import { hasAllVuePermissionsOrSystemAdmin } from "../../components/helpers/FeaturesHelper";
import { useEffect, useMemo, useState } from "react";
import { EntitySetFactory, IEntitySetFactory } from "../../entity/EntitySetFactory";
import moment from "moment";
import { ApplicationConfiguration } from "../../ApplicationConfiguration";
import { IBrandVueState } from "../../BrandVue";
import { useReadVueQueryParams, useWriteVueQueryParams } from "../../components/helpers/UrlHelper";
import { dsession } from "../../dsession";
import { EntityInstanceColourRepository } from "../../entity/EntityInstanceColourRepository";
import { highchartsGlobalConfiguration } from "../../highcharts/highchartsGlobalConfiguration";
import { setSessionLoaded } from "../../state/applicationSlice";
import { selectAveragesForSubset, updateAverages } from "../../state/averageSlice";
import { store, useAppDispatch, useAppSelector } from "../../state/store";
import { selectSubsetId, updateSubset } from "../../state/subsetSlice";
import { CatchReportAndDisplayErrors } from "../../components/CatchReportAndDisplayErrors";
import { CategoryContextProvider } from "../../components/helpers/CategoryContext";
import { ComparisonContextProvider } from "../../components/helpers/ComparisonContext";
import { SavedBreaksProvider } from "../../components/visualisations/Crosstab/SavedBreaksContext";
import { BaseVariableContextProvider } from "../../components/visualisations/Variables/BaseVariableContext";
import { EntityConfigurationLoader } from "../../entity/EntityConfigurationLoader";
import { EntityConfigurationStateProvider, useEntityConfigurationStateContext } from "../../entity/EntityConfigurationStateContext";
import { UserContext, GlobalContext, EntitySetFactoryContext } from "../../GlobalContext";
import {Provider} from "react-redux";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { isCustomPeriodAverage } from "../../components/helpers/PeriodHelper";

interface IProps {
    nav: React.ReactNode;
    productConfiguration: ProductConfiguration;
}

interface IState {
    initComplete: boolean,
    entitySetFactory: IEntitySetFactory,
    session: dsession,
    applicationConfiguration: ApplicationConfiguration
}

const TableBuilderWrapper = (props: IProps) => {

    /* COPY PASTE FROM BrandVue.tsx - this will go away when the table builder moves to an appropriate place */

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
                moment.locale(window.navigator["language"]);

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
                                                    userCanSeeAllMetrics={hasAllVuePermissionsOrSystemAdmin(props.productConfiguration, [
                                                        PermissionFeaturesOptions.VariablesEdit,
                                                        PermissionFeaturesOptions.VariablesCreate
                                                    ])}
                                                    isSurveyVue={props.productConfiguration.isSurveyVue()}>
                                                            <Provider store={store}>
                                                                <WrappedContent {...props} {...state} />
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
};

const WrappedContent = (props: IProps & IState) => {
    const userFeaturesContext = useUserFeaturesContext();
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const subsetId = useAppSelector(selectSubsetId);
    const availableAveragesForSubset = useAppSelector(state => selectAveragesForSubset(state, subsetId));

    const unchangingFilters = useMemo(() => {
        const demographicFilter = new DemographicFilter();
        const newFilters = CuratedFilters.createWithOptions({
            startDate: props.applicationConfiguration.dateOfFirstDataPoint,
            endDate: props.applicationConfiguration.dateOfLastDataPoint,
            average: availableAveragesForSubset.find(a => isCustomPeriodAverage(a) && a.weightingMethod === WeightingMethod.None),
            comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly,
        }, entityConfiguration);
        newFilters.demographicFilter.ageGroups = demographicFilter.ageGroups;
        newFilters.demographicFilter.genders = demographicFilter.genders;
        newFilters.demographicFilter.regions = demographicFilter.regions;
        newFilters.demographicFilter.socioEconomicGroups = demographicFilter.socioEconomicGroups;
        return newFilters;
    }, [
        availableAveragesForSubset,
        props.applicationConfiguration.dateOfFirstDataPoint,
        props.applicationConfiguration.dateOfLastDataPoint
    ]);

    if (!userFeaturesContext || userFeaturesContext.features.length === 0) {
        return (
            <div id="ld" className="loading-container">
                <Throbber />
            </div>
        );
    } else if (userFeaturesContext.features.some(f => f.FeatureCode === FeatureCode.Table_builder)) {
        return (
            <TableBuilderPage {...props} curatedFilters={unchangingFilters} />
        );
    } else {
        return <Navigate to="/crosstabbing" />;
    }
}
export default TableBuilderWrapper;