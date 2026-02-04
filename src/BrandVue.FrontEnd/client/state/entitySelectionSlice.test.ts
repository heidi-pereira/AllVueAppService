import reducer, {
    setEntitySets,
    setActiveEntitySet,
    setActiveEntityInstance,
    setChartAxes,
    setSplitBy,
    setFilterBy,
    setCategorySortKey,
    EntitySelectionState
} from './entitySelectionSlice';
import { CategorySortKey } from '../BrandVueApi';
import { setupStore } from './store';
import { MockApplication } from '../helpers/MockApp';
import { MockStoreBuilder } from '../helpers/MockStore';
import { categorySortMap } from "../components/helpers/CategorySortKeyHelper";

describe('entitySelectionSlice', () => {
    // Use existing mock data from MockApp
    const mockBrandEntityType = MockApplication.brandEntityType;
    const mockProductEntityType = MockApplication.mockEntityModels[2].EntityType;
    const mockImageEntityType = MockApplication.mockEntityModels[1].EntityType;

    const mockBrandInstance = MockApplication.mainBrand;
    const mockBrand1 = MockApplication.brand1;
    const mockBrand2 = MockApplication.brand2;
    const mockBrand3 = MockApplication.brand3;

    const mockProduct = MockApplication.mainProduct;
    const mockProduct1 = MockApplication.product1;

    // Reset mocks before each test
    beforeEach(() => {
        jest.clearAllMocks();
    });

    describe('reducer', () => {
        it('should return the initial state', () => {
            const initialState = reducer(undefined, {type: ''});
            expect(initialState).toEqual({
                activeBreaks: {},
                entitySets: {},
                priorityOrderedEntityTypes: [],
                categorySortKey: CategorySortKey.None
            });
        });

        it('should handle setEntitySets', () => {
            // Initial state from MockStoreBuilder
            const initialState = {
                activeBreaks: {},
                entitySets: {},
                priorityOrderedEntityTypes: [],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            const entitySetSelection1 = {
                active: mockBrandInstance.id,
                highlighted: [mockBrandInstance.id, mockBrand1.id],
                entitySetId: MockApplication.defaultBrandSet.id,
            };

            const entitySetSelection2 = {
                active: mockProduct.id,
                highlighted: [mockProduct.id],
                entitySetId: MockApplication.defaultProductSet.id,
            };

            const action = setEntitySets({
                selections: [
                    {entityType: mockBrandEntityType.identifier, entitySet: entitySetSelection1},
                    {entityType: mockProductEntityType.identifier, entitySet: entitySetSelection2}
                ],
                priorityOrderedEntityTypes: [mockBrandEntityType, mockProductEntityType]
            });

            const newState = reducer(initialState, action);

            expect(newState.entitySets[mockBrandEntityType.identifier]).toEqual(entitySetSelection1);
            expect(newState.entitySets[mockProductEntityType.identifier]).toEqual(entitySetSelection2);
            expect(newState.priorityOrderedEntityTypes).toEqual([mockBrandEntityType, mockProductEntityType]);
        });

        it('should handle setActiveEntitySet', () => {
            const initialState = {
                activeBreaks: {},
                entitySets: {},
                priorityOrderedEntityTypes: [],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            // Use MockApplication's defaultBrandSet
            const action = setActiveEntitySet({entitySet: MockApplication.defaultBrandSet});
            const newState = reducer(initialState, action);

            expect(newState.entitySets[mockBrandEntityType.identifier]).toEqual({
                active: MockApplication.defaultBrandSet.mainInstance!.id,
                highlighted: MockApplication.defaultBrandSet.getInstances().getAll().map(x => x.id),
                entitySetId: MockApplication.defaultBrandSet.id,
                entitySetAverages: MockApplication.defaultBrandSet.getAverages().getAll().filter(a => a.entitySetId).map(a => a.entitySetId),
            });
        });

        it('should handle setActiveEntityInstance', () => {
            // Setup initial state with existing brand entity set
            const initialState = {
                activeBreaks: {},
                entitySets: {
                    [mockBrandEntityType.identifier]: {
                        active: mockBrandInstance.id,
                        highlighted: [mockBrandInstance.id, mockBrand1.id, mockBrand2.id],
                        entitySetId: MockApplication.defaultBrandSet.id,
                    }
                },
                priorityOrderedEntityTypes: [mockBrandEntityType],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            // Change active instance to Brand2
            const action = setActiveEntityInstance({
                entityType: mockBrandEntityType,
                instance: mockBrand2
            });

            const newState = reducer(initialState, action);

            expect(newState.entitySets[mockBrandEntityType.identifier].active).toBe(mockBrand2.id);
        });

        it('should not modify state when setActiveEntityInstance is called with non-existing entity type', () => {
            const initialState = {
                activeBreaks: {},
                entitySets: {
                    [mockBrandEntityType.identifier]: {
                        active: mockBrandInstance.id,
                        highlighted: [mockBrandInstance.id, mockBrand1.id, mockBrand2.id],
                        entitySetId: MockApplication.defaultBrandSet.id,
                    }
                },
                priorityOrderedEntityTypes: [mockBrandEntityType],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            // Try to set active instance for an entity type not in the state
            const action = setActiveEntityInstance({
                entityType: mockImageEntityType,
                instance: mockBrand1  // Using a brand instance for an image entity type
            });

            const newState = reducer(initialState, action);

            // State should remain unchanged
            expect(newState).toEqual(initialState);
        });

        it('should handle setChartAxes', () => {
            const initialState = {
                activeBreaks: {},
                entitySets: {},
                priorityOrderedEntityTypes: [mockBrandEntityType],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            // Change the entity type order
            const action = setChartAxes([mockProductEntityType, mockBrandEntityType]);
            const newState = reducer(initialState, action);

            expect(newState.priorityOrderedEntityTypes).toEqual([mockProductEntityType, mockBrandEntityType]);
        });

        it('should handle setSplitBy when entity type already exists', () => {
            const initialState = {
                activeBreaks: {},
                entitySets: {},
                priorityOrderedEntityTypes: [mockBrandEntityType, mockProductEntityType],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            const action = setSplitBy(mockProductEntityType);
            const newState = reducer(initialState, action);

            // Product should be prioritised
            expect(newState.priorityOrderedEntityTypes[0]).toEqual(mockProductEntityType);
            expect(newState.priorityOrderedEntityTypes[1]).toEqual(mockBrandEntityType);
        });

        it('should handle setSplitBy when entity type does not exist', () => {
            const initialState = {
                activeBreaks: {},
                entitySets: {},
                priorityOrderedEntityTypes: [mockBrandEntityType],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            const action = setSplitBy(mockProductEntityType);
            const newState = reducer(initialState, action);

            // Product should be added as the first entity type (index 0)
            expect(newState.priorityOrderedEntityTypes).toHaveLength(2);
            expect(newState.priorityOrderedEntityTypes[0]).toEqual(mockProductEntityType);
            expect(newState.priorityOrderedEntityTypes[1]).toEqual(mockBrandEntityType);
        });

        it('should handle setFilterBy when entity set already exists', () => {
            const initialState = {
                activeBreaks: {},
                entitySets: {
                    [mockProductEntityType.identifier]: {
                        active: mockProduct.id,
                        highlighted: [mockProduct.id, mockProduct1.id],
                        entitySetId: MockApplication.defaultProductSet.id,
                    }
                },
                priorityOrderedEntityTypes: [],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            const action = setFilterBy({
                filterBy: mockProduct1,
                filterByType: mockProductEntityType
            });

            const newState = reducer(initialState, action);

            // Only the active ID should be updated
            expect(newState.entitySets[mockProductEntityType.identifier].active).toEqual(mockProduct1.id);
            expect(newState.entitySets[mockProductEntityType.identifier].highlighted).toEqual([mockProduct.id, mockProduct1.id]);
            expect(newState.entitySets[mockProductEntityType.identifier].entitySetId).toEqual(MockApplication.defaultProductSet.id);
        });

        it('should handle setFilterBy when entity set does not exist', () => {
            const initialState = {
                activeBreaks: {},
                entitySets: {},
                priorityOrderedEntityTypes: [],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            const action = setFilterBy({
                filterBy: mockProduct1,
                filterByType: mockProductEntityType
            });

            const newState = reducer(initialState, action);

            expect(newState.entitySets[mockProductEntityType.identifier]).toEqual({
                active: mockProduct1.id
            });
        });

        it('should handle setCategorySortKey with valid key', () => {
            const initialState = {
                activeBreaks: {},
                entitySets: {},
                priorityOrderedEntityTypes: [],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            // Find a valid key from categorySortMap
            const validSortKey = Object.keys(categorySortMap)[0];
            const expectedCategorySortKey = categorySortMap[validSortKey];

            const action = setCategorySortKey(validSortKey);
            const newState = reducer(initialState, action);

            expect(newState.categorySortKey).toBe(expectedCategorySortKey);
        });

        it('should handle setCategorySortKey with invalid key', () => {
            const initialState = {
                activeBreaks: {},
                entitySets: {},
                priorityOrderedEntityTypes: [],
                categorySortKey: CategorySortKey.None
            } as EntitySelectionState;

            const action = setCategorySortKey('InvalidKey');
            const newState = reducer(initialState, action);

            expect(newState.categorySortKey).toBe(CategorySortKey.None);
        });
    });

    describe('integration with store', () => {
        it('should update store state correctly when dispatching setEntitySets', () => {
            // Use MockStoreBuilder to create initial state
            const initialState = new MockStoreBuilder()
                .setPriorityOrderedEntityTypes([mockBrandEntityType])
                .build();

            const store = setupStore(initialState);

            const entitySetSelection1 = {
                active: mockBrandInstance.id,
                highlighted: [mockBrandInstance.id, mockBrand1.id],
                entitySetId: MockApplication.defaultBrandSet.id,
            };

            store.dispatch(setEntitySets({
                selections: [
                    {entityType: mockBrandEntityType.identifier, entitySet: entitySetSelection1}
                ]
            }));

            const newState = store.getState().entitySelection;
            expect(newState.entitySets[mockBrandEntityType.identifier]).toEqual(entitySetSelection1);
            expect(newState.priorityOrderedEntityTypes).toEqual([mockBrandEntityType]);
        });

        it('should update active entity instance when dispatching setActiveEntityInstance', () => {
            // Start with a store that has a brand entity set
            const initialState = new MockStoreBuilder()
                .setEntitySelection({
                    entitySets: {
                        [mockBrandEntityType.identifier]: {
                            active: mockBrandInstance.id,
                            highlighted: [mockBrandInstance.id, mockBrand1.id, mockBrand2.id],
                            entitySetId: MockApplication.defaultBrandSet.id,
                        }
                    },
                    priorityOrderedEntityTypes: [mockBrandEntityType],
                    categorySortKey: CategorySortKey.None
                })
                .build();

            const store = setupStore(initialState);

            // Change active instance to Brand3
            store.dispatch(setActiveEntityInstance({
                entityType: mockBrandEntityType,
                instance: mockBrand3
            }));

            const newState = store.getState().entitySelection;
            expect(newState.entitySets[mockBrandEntityType.identifier].active).toBe(mockBrand3.id);
        });
    });
});
