import { CategorySortKey } from '../BrandVueApi';
import { Provider } from 'react-redux';
import { useActiveEntitySet, useActiveBrandSetWithDefault } from "./entitySelectionHooks";
import { brandEntityType } from '../helpers/MockApp';
import { MockApplication } from '../helpers/MockApp';
import { act, renderHook } from '@testing-library/react';
import * as EntityConfigurationStateContext from 'client/entity/EntityConfigurationStateContext';
import { setupStore } from './store';

jest.spyOn(EntityConfigurationStateContext, "useEntityConfigurationStateContext")
    .mockImplementation(() => ({
        entityConfiguration: MockApplication.mockEntityConfiguration,
        hasEntityConfigurationLoaded: true,
        entityConfigurationDispatch: () => Promise.resolve() })
    );

describe('entitySelectionSelectors hooks', () => {
    it('Should load named default brand set by default', () => {
        const store = setupStore({
            entitySelection: {
                activeBreaks: {},
                entitySets: {
                    brand: { entitySetId: MockApplication.defaultBrandSet.id }
                },
                priorityOrderedEntityTypes: [brandEntityType],
                categorySortKey: CategorySortKey.None
            }
        });

        const wrapper = ({ children }: any) => <Provider store={store}>{children}</Provider>;
        const { result } = renderHook(() => useActiveBrandSetWithDefault(), { wrapper });

        expect(result.current?.id).toEqual(MockApplication.defaultBrandSet.id);
    });

    it('Changing priority on entity types should select the default entity sets when there is no prior user selection', () => {
        const store = setupStore({
            entitySelection: {
                activeBreaks: {},
                entitySets: {
                    brand: { entitySetId: MockApplication.defaultBrandSet.id },
                    product: { entitySetId: MockApplication.defaultProductSet.id }
                },
                priorityOrderedEntityTypes: [brandEntityType],
                categorySortKey: CategorySortKey.None
            }
        });

        const wrapper = ({ children }: any) => <Provider store={store}>{children}</Provider>;
        const { result, rerender } = renderHook(() => useActiveEntitySet(), { wrapper });

        expect(result.current.id).toEqual(MockApplication.defaultBrandSet.id);

        act(() => {
            store.dispatch({
                type: 'entityState/setChartAxes',
                payload: [MockApplication.productEntityType]
            });
        });

        rerender();
        expect(result.current.id).toEqual(MockApplication.defaultProductSet.id);
    });

    it('Changing selected entity type should select the last entity set for this entity type chosen by the user', () => {
        const store = setupStore({
            entitySelection: {
                activeBreaks: {},
                entitySets: {
                    brand: { entitySetId: MockApplication.otherBrandSet.id },
                    product: { entitySetId: MockApplication.defaultProductSet.id }
                },
                priorityOrderedEntityTypes: [brandEntityType],
                categorySortKey: CategorySortKey.None
            }
        });

        const wrapper = ({ children }: any) => <Provider store={store}>{children}</Provider>;
        const { result, rerender } = renderHook(() => useActiveEntitySet(), { wrapper });

        expect(result.current.id).toEqual(MockApplication.otherBrandSet.id);

        act(() => {
            store.dispatch({
                type: 'entityState/setChartAxes',
                payload: [MockApplication.productEntityType]
            });
        });

        rerender();
        expect(result.current.id).toEqual(MockApplication.defaultProductSet.id);

        act(() => {
            store.dispatch({
                type: 'entityState/setChartAxes',
                payload: [brandEntityType]
            });
        });

        rerender();
        expect(result.current.id).toEqual(MockApplication.otherBrandSet.id);
    });

    it('Should return the default entity when the active brand has no selected entity set', () => {
        const store = setupStore({
            entitySelection: {
                activeBreaks: {},
                entitySets: {
                    brand: { active: MockApplication.mainBrand.id },
                    product: {}
                },
                priorityOrderedEntityTypes: [MockApplication.productEntityType],
                categorySortKey: CategorySortKey.None
            }
        });

        const wrapper = ({ children }: any) => <Provider store={store}>{children}</Provider>;
        const { result } = renderHook(() => useActiveEntitySet(), { wrapper });

        expect(result.current.getMainInstance().id).toEqual(MockApplication.defaultProductSet.getMainInstance().id);
    });

    it('Active entity set main instance should be correct', () => {
        const store = setupStore({
            entitySelection: {
                activeBreaks: {},
                entitySets: {
                    product: { active: MockApplication.product1.id }
                },
                priorityOrderedEntityTypes: [MockApplication.productEntityType],
                categorySortKey: CategorySortKey.None
            }
        });

        const wrapper = ({ children }: any) => <Provider store={store}>{children}</Provider>;
        const { result } = renderHook(() => useActiveEntitySet(), { wrapper });

        expect(result.current.getMainInstance().id).toEqual(MockApplication.product1.id);
    });
});