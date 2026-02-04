import React from 'react';
import {fireEvent, render, screen} from '@testing-library/react';
import { MockRouter } from '../../../helpers/MockRouter';
import { Provider } from 'react-redux';
import '@testing-library/jest-dom';
import MetricListItemContextMenu from './MetricListItemContextMenu';
import {ProductConfigurationContext} from '../../../ProductConfigurationContext';
import { IGoogleTagManager } from '../../../googleTagManager';
import {createMockProductConfiguration} from '../../../helpers/MockSession';
import {generateMetric} from '../../../helpers/ReactTestingLibraryHelpers';
import {IEntityType, PermissionFeaturesOptions, PermissionFeatureOptionWithCode} from "../../../BrandVueApi";
import {mock} from "jest-mock-extended";
import { QuestionVariableDefinition, VariableConfigurationModel } from "../../../BrandVueApi";
import { setupStore, store } from '../../../state/store';
import { MockStoreBuilder } from 'client/helpers/MockStore';
import { EntityConfigurationStateProvider } from 'client/entity/EntityConfigurationStateContext';
import { EntitySetFactory } from 'client/entity/EntitySetFactory';
import { EntityInstanceColourRepository } from 'client/entity/EntityInstanceColourRepository';
import { EntityConfigurationLoader } from 'client/entity/EntityConfigurationLoader';
import { MockApplication } from 'client/helpers/MockApp';
import { UserContext } from '../../../GlobalContext';


const googleTagManager = mock<IGoogleTagManager>();
const mockProductConfiguration = createMockProductConfiguration();
mockProductConfiguration.user.isSystemAdministrator = mockProductConfiguration.user.isAdministrator = true;
mockProductConfiguration.user.featurePermissions = [
    new PermissionFeatureOptionWithCode({
        id: 1,
        name: 'VariablesEdit',
        code: PermissionFeaturesOptions.VariablesEdit
    })
];
mockProductConfiguration.isSurveyVue = jest.fn(() => false);

const createMockVariableListItem = () => {
    const questionVariableDefinition = new QuestionVariableDefinition();
    questionVariableDefinition.questionVarCode = "Test question";
    const variableConfig = new VariableConfigurationModel();
    variableConfig.id = 1;
    variableConfig.definition = questionVariableDefinition;
    var metric = generateMetric("Test metric");
    metric.primaryFieldDependencies = [];
    const entityType = {} as any as IEntityType;
    metric.entityCombination = [ entityType ];
    metric.primaryFieldEntityCombination = [];
    metric.primaryVariableIdentifier = variableConfig.identifier;
    metric.variableConfigurationId = variableConfig.id;
    return {
        metric,
        variable: new VariableConfigurationModel(),
        variableType: 0,
    };
}

const createComponentProps = () => {
    return {
        variableListItem: createMockVariableListItem(),
        splitByEntityType: undefined,
        canEditMetrics: true,
        googleTagManager: googleTagManager,
        eligibleForCrosstabOrAllVue: true,
        metricEnabled: true,
        filterEnabled: true,
        subsetId: '123',
        setIsEditingHelptext: jest.fn(),
        setIsEditingDisplayName: jest.fn(),
        setEligibleForCrosstabOrAllVue: jest.fn(),
        setDisableMeasure: jest.fn(),
        setDisableFilterMeasure: jest.fn(),
        setMetricDefaultSplitBy: jest.fn(),
        setConvertCalculationTypeModalVisible: jest.fn(),
        helpText: '',
        displayName: '',
    };
};

const renderComponent = (productConfiguration) => {
    const props = createComponentProps();
    const mockState = new MockStoreBuilder()
        .setSubset({ subsetId: 'all', subsetConfigurations: [] })
        .build();
    return render(
        <Provider store={setupStore(mockState)}>
            <MockRouter>
                <EntityConfigurationStateProvider
                    entitySetFactory={new EntitySetFactory(EntityInstanceColourRepository.empty())}
                    loader={new EntityConfigurationLoader()}
                    initialConfiguration={MockApplication.mockEntityConfiguration}
                >
                    <UserContext.Provider value={productConfiguration.user}>
                        <ProductConfigurationContext.Provider value={{ productConfiguration }}>
                            <MetricListItemContextMenu {...props} />
                        </ProductConfigurationContext.Provider>
                    </UserContext.Provider>
                </EntityConfigurationStateProvider>
            </MockRouter>
        </Provider>
    );
};

test('renders menu items when canEditVariable is true', () => {
    renderComponent(mockProductConfiguration);

    // Open the dropdown
    fireEvent.click(screen.getByRole('button'));

    // Check for the presence of variable editing items
    expect(screen.getByText('Edit')).toBeInTheDocument();
});
