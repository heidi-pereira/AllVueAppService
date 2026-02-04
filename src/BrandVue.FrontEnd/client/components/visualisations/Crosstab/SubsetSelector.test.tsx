import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import SubsetSelector from './SubsetSelector';
import '@testing-library/jest-dom';
import { MemoryRouter } from "react-router-dom";
import { ProductConfigurationContext } from '../../../ProductConfigurationContext';
import { ProductConfiguration } from '../../../ProductConfiguration';
import { Provider } from 'react-redux';
import { setupStore } from '../../../state/store';
import { SubsetConfiguration } from 'client/BrandVueApi';

const mockGetSubsetConfigurations = jest.fn().mockResolvedValue([
    { id: '1', displayName: 'Subset A', identifier: "SubsetA"},
    { id: '2', displayName: 'Subset B', identifier: "SubsetB"}
]);

jest.mock('../../../BrandVueApi', () => ({
    Factory: {
        SubsetsClient: jest.fn(() => ({
            getSubsetConfigurations: mockGetSubsetConfigurations,
        })),
    },
}));

const mockSetQueryParameter = jest.fn();
jest.mock('../../../components/helpers/UrlHelper', () => ({
    useWriteVueQueryParams: () => ({
        setQueryParameter: mockSetQueryParameter,
    })
}));

jest.mock('../../../BrandVueApi', () => ({
  CategorySortKey: {
    None: 'None',
    BestScores: 'BestScores',
    WorstScores: 'WorstScores',
    OverPerforming: 'OverPerforming',
    UnderPerforming: 'UnderPerforming',
  },
}));

const productConfiguration = new ProductConfiguration();
productConfiguration.isSurveyVue = () => true;


const subsetNameOne = 'Subset 1';
const subsetNameTwo = 'Subset 2';
const subsetIdentifierOne = "Subset1";
const subsetIdentifierTwo = "Subset2";

const subsetConfigurations: SubsetConfiguration[] = [
  {
      id: 1,
      displayName: subsetNameOne,
      identifier: subsetIdentifierOne,
      displayNameShort: subsetNameOne,
      alias: '',
      iso2LetterCountryCode: '',
      description: '',
      order: 0,
      disabled: false,
      surveyIdToAllowedSegmentNames: {},
      enableRawDataApiAccess: false,
      productShortCode: '',
      subProductId: '',
      alwaysShowDataUpToCurrentDate: false,
      pageSubsetConfigurations: [],
      init: function (_data?: any): void {
          throw new Error('Function not implemented.');
      },
      toJSON: function (data?: any) {
          throw new Error('Function not implemented.');
      }
  },
  {
      id: 2,
      displayName: subsetNameTwo,
      identifier: subsetIdentifierTwo,
      displayNameShort: subsetNameTwo,
      alias: '',
      iso2LetterCountryCode: '',
      description: '',
      order: 0,
      disabled: false,
      surveyIdToAllowedSegmentNames: {},
      enableRawDataApiAccess: false,
      productShortCode: '',
      subProductId: '',
      alwaysShowDataUpToCurrentDate: false,
      pageSubsetConfigurations: [],
      init: function (_data?: any): void {
          throw new Error('Function not implemented.');
      },
      toJSON: function (data?: any) {
          throw new Error('Function not implemented.');
      }
  }
];

const validPreloadedState = {
  subset: {
    subsetConfigurations,
    subsetId: subsetIdentifierOne
  },
};

const store = setupStore({
  ...validPreloadedState,
  subset: {
    ...validPreloadedState.subset,
    subsetConfigurations: validPreloadedState.subset.subsetConfigurations as SubsetConfiguration[],
  },
});

describe('SubsetSelector', () => {
    const subsetId = 'testSubsetId';

    beforeEach(() => {
        mockGetSubsetConfigurations.mockResolvedValue(subsetConfigurations);
    });

    test('renders without crashing', async () => {
        render(
          <Provider store={store}>
            <MemoryRouter>
              <ProductConfigurationContext.Provider value={{ productConfiguration: productConfiguration }}>
                <SubsetSelector subsetId={subsetId} updateUrlOnChange={true} onSubsetChange={() => {}}/>
              </ProductConfigurationContext.Provider>
            </MemoryRouter>
          </Provider>
        );
        await waitFor(() => {
            expect(screen.getByText(subsetNameOne)).toBeInTheDocument();
        });
    });

    test('dropdown toggles correctly', async () => {
        render(
          <Provider store={store}>
            <MemoryRouter>
              <ProductConfigurationContext.Provider value={{ productConfiguration: productConfiguration }}>
                <SubsetSelector subsetId={subsetId} updateUrlOnChange={true} onSubsetChange={() => {}}/>
              </ProductConfigurationContext.Provider>
            </MemoryRouter>
          </Provider>
        );
        await waitFor(() => {
            expect(screen.getByText(subsetNameOne)).toBeInTheDocument();
        });
        const toggleButton = screen.getByText(subsetNameOne);
        
        fireEvent.click(toggleButton);
        expect(screen.getByRole('menu')).toBeInTheDocument();
    });

    test('fetches and displays subset configurations', async () => {
        render(
          <Provider store={store}>
            <MemoryRouter>
              <ProductConfigurationContext.Provider value={{ productConfiguration: productConfiguration }}>
                <SubsetSelector subsetId={subsetId} updateUrlOnChange={true} onSubsetChange={() => {}}/>
              </ProductConfigurationContext.Provider>
            </MemoryRouter>
          </Provider>
        );
        await waitFor(() => {
            expect(screen.getByText(subsetNameOne)).toBeInTheDocument();
            expect(screen.getByText(subsetNameTwo)).toBeInTheDocument();
        });
    });

    test('handleOnChange works correctly', async () => {
        render(
          <Provider store={store}>
            <MemoryRouter>
              <ProductConfigurationContext.Provider value={{ productConfiguration: productConfiguration }}>
                <SubsetSelector subsetId={subsetId} updateUrlOnChange={true} onSubsetChange={() => {}} />
              </ProductConfigurationContext.Provider>
            </MemoryRouter>
          </Provider>
        );
        await waitFor(() => {
            expect(screen.getByText(subsetNameOne)).toBeInTheDocument();
        });
        fireEvent.click(screen.getByText(subsetNameOne));
        expect(mockSetQueryParameter).toHaveBeenCalledWith('Subset', subsetIdentifierOne);
    });
});