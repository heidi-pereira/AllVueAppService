import React from 'react';
import { Provider } from 'react-redux';
import { render } from "@testing-library/react";
import { setupStore } from '../../../../../state/store';
import { UserContext } from 'client/GlobalContext';
import { Metric } from '../../../../../metrics/metric';
import * as BrandVueApi from "../../../../../BrandVueApi";
import { MetricSet } from 'client/metrics/metricSet';
import * as MetricStateContext from 'client/metrics/MetricStateContext';
import { MockRouter } from 'client/helpers/MockRouter';
import { EntityConfigurationStateProvider } from 'client/entity/EntityConfigurationStateContext';
import { EntitySetFactory } from 'client/entity/EntitySetFactory';
import { EntityInstanceColourRepository } from 'client/entity/EntityInstanceColourRepository';
import { EntityConfigurationLoader } from 'client/entity/EntityConfigurationLoader';
import { MockApplication } from 'client/helpers/MockApp';
import { VariableProvider } from './VariableContext';
import { IGoogleTagManager } from 'client/googleTagManager';
import { PageHandler } from 'client/components/PageHandler';
import { ProductConfigurationContext } from "../../../../../ProductConfigurationContext";
import { ProductConfiguration } from '../../../../../ProductConfiguration';
export const initialState = {
    variableConfiguration: {
        variables: [
            {
                id: 2, productShortCode: 'retail', identifier: 'Age', displayName: 'Age',
                definition: {
                    questionVarCode: 'Age', entityTypeNames: [],
                    roundingType: 'Round', discriminator: 'QuestionVariableDefinition'
                }
            },
            {
                id: 3, productShortCode: 'retail', identifier: 'AgeWeightingUk', displayName: 'Age (Weighting)',
                definition: {
                    toEntityTypeName: 'WeightingAge', toEntityTypeDisplayNamePlural: 'Age groups', groups: [
                        {
                            toEntityInstanceName: '16-24', toEntityInstanceId: 1,
                            component: {
                                min: 16, max: 24, exactValues: [], inverted: false, fromVariableIdentifier: 'Age',
                                operator: 'Between', resultEntityTypeNames: [], discriminator: 'InclusiveRangeVariableComponent'
                            }
                        },
                        {
                            toEntityInstanceName: '25-34', toEntityInstanceId: 2,
                            component: {
                                min: 25, max: 34, exactValues: [], inverted: false, fromVariableIdentifier: 'Age',
                                operator: 'Between', resultEntityTypeNames: [], discriminator: 'InclusiveRangeVariableComponent'
                            }
                        }
                    ],
                    discriminator: 'GroupedVariableDefinition'
                }
            },
            {
                id: 4,
                productShortCode: 'finance', identifier: '_DetractorsOverall_fieldExpressionVariable',
                displayName: 'Detractors Overall (Field Expression)',
                definition: {
                    expression: 'sum(response.Recommendation_All(brand=result.brand))/len(response.Recommendation_All(brand=result.brand)) < 7 and sum(response.Recommendation_All(brand=result.brand))/len(response.Recommendation_All(brand=result.brand)) >= 0',
                    discriminator: 'FieldExpressionVariableDefinition'
                }
            }
        ],
        loading: false,
        error: null
    },
    subset: {
        subsetId: 'all',
        subsetConfigurations: [],
    },
    report: {
        allReports: [{
            savedReportId: 1,
            pageId: 1,
            breaks: [],
            name: 'Test Report',
            isPublic: false,
            description: ''
        }],
        currentReportId: 1,
        reportsPageOverride: {
            id: 1,
            name: 'test-page',
            displayName: 'Test Page'
        },
        errorState: { isError: false },
        isLoading: false,
        isSettingsChange: false,
        isDataInSyncWithDatabase: true
    }
};

export const testMetric = new Metric(null, { entityCombination: [], displayName: "Test" });

export const createMockUser = (): BrandVueApi.IApplicationUser => ({
    userId: 'test-user',
    userName: 'testuser',
    name: 'Test',
    surname: 'User',
    accountName: 'TestAccount',
    products: ['BrandVue'],
    isAdministrator: false,
    isSystemAdministrator: false,
    isThirdPartyLoginAuth: false,
    isReportViewer: false,
    isTrialUser: false,
    canEditMetricAbouts: false,
    canAccessRespondentLevelDownload: false,
    runningEnvironmentDescription: 'Test Environment',
    runningEnvironment: BrandVueApi.RunningEnvironment.Development,
    doesUserHaveAccessToInternalSavantaSystems: false,
    featurePermissions: [
        { id: 2, name: 'VariablesCreate', code: BrandVueApi.PermissionFeaturesOptions.VariablesCreate },
        { id: 3, name: 'VariablesEdit', code: BrandVueApi.PermissionFeaturesOptions.VariablesEdit },
        { id: 4, name: 'VariablesDelete', code: BrandVueApi.PermissionFeaturesOptions.VariablesDelete },
    ],
    dataPermission: {
        name: 'Full Access',
        variableIds: [],
        filters: []
    },
});

export const metrics = [
    testMetric,
    new Metric(null, {
        entityCombination: [],
        displayName: "Test2",
        name: "Age",
        primaryVariableIdentifier: "Age",
        variableConfigurationId: 2
    })
];

export const metricContextState: Partial<MetricStateContext.MetricContextState> = {
    selectableMetricsForUser: metrics,
    enabledMetricSet: new MetricSet({ metrics: metrics }),
};

export function mockMetricStateContext() {
    jest.spyOn(MetricStateContext, 'useMetricStateContext').mockReturnValue(metricContextState as any);
}

const productConfiguration = new ProductConfiguration();
productConfiguration.isSurveyVue = () => true;
productConfiguration.productName = "survey";

export async function renderVariableModalComponent(
    Component: React.ComponentType<any>,
    props: any,
    state = initialState,
    userOverrides: Partial<BrandVueApi.IApplicationUser> = {}
) {
    const store = setupStore(state as any);
    mockMetricStateContext();

    const baseUser = createMockUser();
    const user = { ...baseUser, ...userOverrides };

    return await render(
        <Provider store={store}>
            <UserContext.Provider value={user}>
                <MockRouter initialEntries={['/']}>
                    <ProductConfigurationContext.Provider value={{ productConfiguration: productConfiguration }}>
                        <EntityConfigurationStateProvider
                            entitySetFactory={new EntitySetFactory(EntityInstanceColourRepository.empty())}
                            loader={new EntityConfigurationLoader()}
                            initialConfiguration={MockApplication.mockEntityConfiguration}
                        >
                            <VariableProvider
                                googleTagManager={jest.fn() as unknown as IGoogleTagManager}
                                pageHandler={jest.fn() as unknown as PageHandler}
                                user={user}
                                nonMapFileSurveys={[]}
                                isSurveyGroup={false}
                            >
                                <Component {...props} />
                            </VariableProvider>
                        </EntityConfigurationStateProvider>
                     </ProductConfigurationContext.Provider>
                </MockRouter>
            </UserContext.Provider>
        </Provider>
    );
}