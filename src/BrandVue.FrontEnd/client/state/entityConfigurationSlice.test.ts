import { Factory, IEntityTypeConfiguration } from "../BrandVueApi";
import reducer, { 
  fetchEntityTypeConfigurations, 
  saveEntityType,
  setActiveEntityTypeByIdentifier} from './entityConfigurationSlice';
import {
    selectActiveEntityType,
    selectConfigurations
} from './entityConfigurationSelectors';
import { setupStore } from './store';
// Import MockApplication to reuse existing entities
import { MockApplication, createEntityConfiguration } from '../helpers/MockApp';

// Mock the Factory.EntitiesClient
jest.mock('../BrandVueApi', () => {
  const actual = jest.requireActual('../BrandVueApi');
  const mockGetEntityTypeConfigurations = jest.fn();
  const mockSaveEntityType = jest.fn();

  return {
    ...actual,
    Factory: {
      EntitiesClient: jest.fn(() => ({
        getEntityTypeConfigurations: mockGetEntityTypeConfigurations,
        saveEntityType: mockSaveEntityType
      }))
    }
  };
});

describe('entityConfigurationSlice', () => {
  // Use existing entity types from MockApp
  const mockEntityTypeConfigurations = [
    MockApplication.brandEntityType,
    {
      identifier: 'product',
      displayNameSingular: 'Product',
      displayNamePlural: 'Products',
      toJSON: function() { return this; }
    }
  ] as unknown as IEntityTypeConfiguration[];

  // Reset mocks before each test
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('reducer', () => {
    it('should return the initial state', () => {
      const initialState = reducer(undefined, { type: '' });
      expect(initialState).toEqual({
        activeEntityTypeIdentifier: null,
        configurationByIdentifier: {},
        loading: false,
        error: null
      });
    });

    it('should handle setActiveEntityTypeByIdentifier', () => {
      const initialState = {
        activeEntityTypeIdentifier: null,
        configurationByIdentifier: {},
        loading: false,
        error: null
      };

      const newState = reducer(
        initialState, 
        setActiveEntityTypeByIdentifier('brand')
      );

      expect(newState.activeEntityTypeIdentifier).toBe('brand');
    });
  });

  describe('async thunks', () => {
    describe('fetchEntityTypeConfigurations', () => {
      it('should handle fetchEntityTypeConfigurations.pending', () => {
        const initialState = {
          activeEntityTypeIdentifier: null,
          configurationByIdentifier: {},
          loading: false,
          error: null
        };

        const action = { type: fetchEntityTypeConfigurations.pending.type };
        const state = reducer(initialState, action);

        expect(state.loading).toBe(true);
        expect(state.error).toBe(null);
      });

      it('should handle fetchEntityTypeConfigurations.fulfilled', () => {
        const initialState = {
          activeEntityTypeIdentifier: null,
          configurationByIdentifier: {},
          loading: true,
          error: null
        };

        const action = { 
          type: fetchEntityTypeConfigurations.fulfilled.type,
          payload: mockEntityTypeConfigurations
        };
        
        const state = reducer(initialState, action);

        expect(state.loading).toBe(false);
        expect(state.configurationByIdentifier).toEqual({
          'brand': mockEntityTypeConfigurations[0],
          'product': mockEntityTypeConfigurations[1]
        });
      });

      it('should handle fetchEntityTypeConfigurations.rejected', () => {
        const initialState = {
          activeEntityTypeIdentifier: null,
          configurationByIdentifier: {},
          loading: true,
          error: null
        };

        const action = { 
          type: fetchEntityTypeConfigurations.rejected.type,
          error: { message: 'Failed to fetch' }
        };
        
        const state = reducer(initialState, action);

        expect(state.loading).toBe(false);
        expect(state.error).toBe('Failed to fetch');
      });

      it('should fetch entity type configurations successfully', async () => {
        // Setup mock
        const mockClient = Factory.EntitiesClient(() => {});
        (mockClient.getEntityTypeConfigurations as jest.Mock).mockResolvedValue(mockEntityTypeConfigurations);

        // Create store
        const store = setupStore();
        const result = await store.dispatch(fetchEntityTypeConfigurations());

        expect(result.type).toBe(fetchEntityTypeConfigurations.fulfilled.type);
        expect(result.payload).toEqual(mockEntityTypeConfigurations);
        expect(mockClient.getEntityTypeConfigurations).toHaveBeenCalled();
      });

      it('should handle error when fetching configurations', async () => {
        // Setup mock to reject
        const mockError = new Error('API error');
        const mockClient = Factory.EntitiesClient(() => {});
        (mockClient.getEntityTypeConfigurations as jest.Mock).mockRejectedValue(mockError);

        // Create store
        const store = setupStore();
        const result = await store.dispatch(fetchEntityTypeConfigurations());

        expect(result.type).toBe(fetchEntityTypeConfigurations.rejected.type);
      });
    });

    describe('saveEntityType', () => {
      it('should update state when entityType is saved successfully', async () => {
        const updatedEntityType = {
          identifier: 'brand',
          displayNameSingular: 'Updated Brand',
          displayNamePlural: 'Updated Brands'
        };

        // Setup mock
        const mockClient = Factory.EntitiesClient(() => {});
        (mockClient.saveEntityType as jest.Mock).mockResolvedValue(updatedEntityType);

        // Create store with initial state using MockApplication's brandEntityType
        const store = setupStore({
          entityConfiguration: {
            activeEntityTypeIdentifier: null,
            configurationByIdentifier: {
              'brand': createEntityConfiguration(MockApplication.brandEntityType)
            },
            loading: false,
            error: null
          }
        });

        // Dispatch action
        const result = await store.dispatch(saveEntityType({
          identifier: 'brand',
          displayNameSingular: 'Updated Brand',
          displayNamePlural: 'Updated Brands'
        }));

        expect(result.type).toBe(saveEntityType.fulfilled.type);
        expect(result.payload).toEqual(updatedEntityType);
        
        // Verify state was updated
        const finalState = store.getState().entityConfiguration;
        expect(finalState.configurationByIdentifier['brand']).toEqual(updatedEntityType);
      });
    });
  });

  describe('selectors', () => {
    it('selectActiveEntityType should return null when no active entity', () => {
      const store = setupStore({
        entityConfiguration: {
          activeEntityTypeIdentifier: null,
          configurationByIdentifier: {
            'brand': mockEntityTypeConfigurations[0]
          },
          loading: false,
          error: null
        }
      });

      const result = selectActiveEntityType(store.getState());
      expect(result).toBeNull();
    });

    it('selectActiveEntityType should return the active entity configuration', () => {
      const store = setupStore({
        entityConfiguration: {
          activeEntityTypeIdentifier: 'brand',
          configurationByIdentifier: {
            'brand': mockEntityTypeConfigurations[0],
            'product': mockEntityTypeConfigurations[1]
          },
          loading: false,
          error: null
        }
      });

      const result = selectActiveEntityType(store.getState());
      expect(result).toEqual(mockEntityTypeConfigurations[0]);
    });

    it('selectConfigurations should return all configurations as array', () => {
      const store = setupStore({
        entityConfiguration: {
          activeEntityTypeIdentifier: null,
          configurationByIdentifier: {
            'brand': mockEntityTypeConfigurations[0],
            'product': mockEntityTypeConfigurations[1]
          },
          loading: false,
          error: null
        }
      });

      const result = selectConfigurations(store.getState());
      expect(result).toEqual([
        mockEntityTypeConfigurations[0],
        mockEntityTypeConfigurations[1]
      ]);
    });
  });
});
