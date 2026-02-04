import { render, act, renderHook } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import UrlSync, { legacyParamNames } from './UrlSync';
import { AppStore, setupStore } from 'client/state/store';
import { EntityType } from 'client/BrandVueApi';
import { MockApplication } from 'client/helpers/MockApp';
import { setActiveEntitySet, setFilterBy, setSplitBy } from 'client/state/entitySelectionSlice';
import EntitySetBuilder from 'client/entity/EntitySetBuilder';
import { EntityInstanceColourRepository } from 'client/entity/EntityInstanceColourRepository';
import { useActiveEntitySetWithDefaultOrNull } from "client/state/entitySelectionHooks";
import { EntityConfigurationStateProvider } from 'client/entity/EntityConfigurationStateContext';
import { EntitySetFactory } from 'client/entity/EntitySetFactory';
import { EntityConfigurationLoader } from 'client/entity/EntityConfigurationLoader';
import { createMockSession } from "../../helpers/MockSession";
import { MetricStateProvider } from 'client/metrics/MetricStateContext';
import { ProductConfiguration } from '../../ProductConfiguration';
import { PaneType } from '../panes/PaneType';
import defineProperty from '../../helpers/defineProperty';
import { MockCuratedFilters } from '../../helpers/MockApp';
import { Metric } from 'client/metrics/metric';

function ProviderAndRouter({ store, initialEntries, children }: { store: any, initialEntries: string[], children: JSX.Element[] }): JSX.Element {
    return <Provider store={store}>
        <MetricStateProvider isSurveyVue={true} userCanSeeAllMetrics={true} initialMetrics={MockApplication.allMetrics.metrics}>
            <MemoryRouter initialEntries={initialEntries}>
                <EntityConfigurationStateProvider
                    entitySetFactory={new EntitySetFactory(EntityInstanceColourRepository.empty())}
                    loader={new EntityConfigurationLoader()}
                    initialConfiguration={MockApplication.mockEntityConfiguration}>
                    {children}
                </EntityConfigurationStateProvider>
            </MemoryRouter>
        </MetricStateProvider>
    </Provider>;
}

function renderWithStoreAndRouter(store: AppStore, ui: any, initialEntries: string[] = ['/']): ReturnType<typeof render> {
    return render(
        <ProviderAndRouter store={store} initialEntries={initialEntries}>{ui}</ProviderAndRouter>
    );
}

const mockProductConfiguration = new ProductConfiguration();
mockProductConfiguration.isSurveyVue = () => false;

async function renderUrlSync(storeToUse: AppStore, activeEntityTypes: EntityType[] = [MockApplication.brandEntityType], activeMetrics: any[] = []) {
    const session = createMockSession();
    session.activeView.activeMetrics = activeMetrics;
    defineProperty(session.activeView, MockCuratedFilters, 'curatedFilters');
    jest.spyOn(session.activeView, 'getEntityCombination').mockImplementation(() => activeEntityTypes);
    await act(async () => {
        renderWithStoreAndRouter(
            storeToUse,
            <UrlSync session={session}><></></UrlSync>
        );
    });
}

let currentMockSearchParams = new URLSearchParams();
const mockSetSearchParams = jest.fn();

jest.mock('react-router-dom', () => ({
    ...jest.requireActual('react-router-dom'),
    useSearchParams: () => [
        currentMockSearchParams,
        (params) => {
            mockSetSearchParams(params);
            currentMockSearchParams = new URLSearchParams(params);
        }
    ]
}));

function getHighlightedIdsInUrlParameter() {
    const highlighted = currentMockSearchParams.get('highlighted');
    return highlighted?.split('.').map(Number);
}

function getActiveIdInUrlParameter() {
    const active = currentMockSearchParams?.get('active');
    return active ? Number(active) : undefined;
}

function setupInitialState({
    searchParams = {},
    storeState = {
        application: { isSessionLoaded: true, primaryMetric: null },
        subset: { subsetId: 'all', subsetConfigurations: [] }
    }
} = {}) {
    const store = setupStore(storeState);
    Object.entries(searchParams).forEach(([key, value]) => {
        currentMockSearchParams.set(key, String(value));
    });
    return store;
}

function getPriorityOrderedTypeIdentifiers(store) {
    return store.getState().entitySelection.priorityOrderedEntityTypes.map(e => e.identifier);
}

function mockReportsPage(defaultSplitBy?: string) {
    const PagesHelper = require('./PagesHelper');
    return jest.spyOn(PagesHelper, 'getCurrentPageInfo').mockReturnValue({
        page: {
            panes: [{
                parts: [{
                    type: PaneType.reportsPage,
                    ...(defaultSplitBy && { defaultSplitBy })
                }]
            }]
        }
    });
}

describe('UrlSync integration tests', () => {
    let store: AppStore;
    let session: any;
    let activeEntityTypes: any;
    beforeEach(() => {
        jest.resetAllMocks();
        currentMockSearchParams = new URLSearchParams();
        store = setupStore({
            application: { isSessionLoaded: true, primaryMetric: null },
            subset: { subsetId: 'all', subsetConfigurations: [] }
        });
        session = createMockSession();
        defineProperty(session.activeView, MockCuratedFilters, 'curatedFilters');
        activeEntityTypes = [MockApplication.brandEntityType, MockApplication.productEntityType];
        session.activeView.activeMetrics = [new Metric('metric1')];
        jest.spyOn(session.activeView, 'getEntityCombination').mockImplementation(() => activeEntityTypes);
    });

    it('Should leave active undefined if id not set', async () => {
        const newSelection = new EntitySetBuilder(EntityInstanceColourRepository.empty())
            .fromEntitySet(MockApplication.defaultBrandSet)
            .withMainInstance(undefined)
            .build();

        await renderUrlSync(store);

        // Simulate removing active by dispatching setEntitySets
        await act(async () => {
            store.dispatch(setActiveEntitySet({ entitySet: newSelection }));
        });
        expect(getActiveIdInUrlParameter()).toBeUndefined();
    });

    it('Should load named default brand set by default', async () => {
        await act(async () => renderWithStoreAndRouter(
            store,
            <UrlSync session={createMockSession()}><></></UrlSync>
        ));
        const wrapper = ({ children }) => <ProviderAndRouter store={store} initialEntries={['/']}>{children}</ProviderAndRouter>;
        const { result, rerender } = renderHook(() => useActiveEntitySetWithDefaultOrNull(), { wrapper });

        expect(
            result.current?.id
        ).toEqual(MockApplication.defaultBrandSet.id);
    });

    it('Should remove highlighted if equal to default set key instances', async () => {
        const entitySetToSelect = MockApplication.defaultBrandSet;

        await renderUrlSync(store);

        await act(async () => {
            store.dispatch(setActiveEntitySet({ entitySet: entitySetToSelect }));
        });

        const highlighted = getHighlightedIdsInUrlParameter();
        expect(highlighted).toBeUndefined();
    });

    it('Should keep highlighted if different to default set key instances', async () => {
        const newSelection = new EntitySetBuilder(EntityInstanceColourRepository.empty())
            .fromEntitySet(MockApplication.defaultBrandSet)
            .withInstances([...MockApplication.defaultBrandSet.getInstances().getAll(), MockApplication.brand3])
            .build();
        const expectedHighlighted = newSelection.getInstances().getAll().map(i => i.id);

        await renderUrlSync(store);

        await act(async () => {
            store.dispatch(setActiveEntitySet({ entitySet: newSelection }));
        });

        const highlighted = getHighlightedIdsInUrlParameter();
        expect(highlighted).toEqual(expectedHighlighted);
    });

    it('Should override SplitBy if not found in entity combination', async () => {
        await renderUrlSync(store, [MockApplication.productEntityType, MockApplication.brandEntityType]);

        await act(async () => {
            store.dispatch(setSplitBy(MockApplication.productEntityType));
        });

        // Simulates switching to a metric with only brand entity type
        await renderUrlSync(store, [MockApplication.brandEntityType]);

        const state = store.getState();
        expect(state.entitySelection.priorityOrderedEntityTypes).toEqual([MockApplication.brandEntityType]);

        const urlParams = new URLSearchParams(window.location.search);
        expect(urlParams.get(legacyParamNames.splitBy)).toBeNull();
    });

    it('Changing SplitBy should select the default entity sets with no prior user selection', async () => {
        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        const wrapper = ({ children }) => <ProviderAndRouter store={store} initialEntries={['/']}>{children}</ProviderAndRouter>;
        const { result, rerender } = renderHook(() => useActiveEntitySetWithDefaultOrNull(), { wrapper });

        await act(async () => {
            store.dispatch(setSplitBy(MockApplication.productEntityType));
        });
        const state = store.getState();
        expect(state.entitySelection.priorityOrderedEntityTypes[0]).toEqual(MockApplication.productEntityType);
        expect(result.current?.id).toEqual(MockApplication.defaultProductSet.id);
    });

    it('Changing SplitBy should select the last entity set for this entity type chosen by the user', async () => {
        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        await act(async () => {
            store.dispatch(setSplitBy(MockApplication.brandEntityType));
            store.dispatch(setActiveEntitySet({ entitySet: MockApplication.otherBrandSet }));
            store.dispatch(setSplitBy(MockApplication.productEntityType));
            store.dispatch(setSplitBy(MockApplication.brandEntityType));
        });

        const state = store.getState();
        expect(state.entitySelection.entitySets.brand.entitySetId).toEqual(MockApplication.otherBrandSet.id);
    });

    it('Changing SplitBy should select the last filter entity for this type chosen by the user', async () => {
        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        await act(async () => {
            store.dispatch(setFilterBy({ filterByType: MockApplication.brandEntityType, filterBy: MockApplication.otherBrandSet.getMainInstance() }));
            store.dispatch(setFilterBy({ filterByType: MockApplication.productEntityType, filterBy: MockApplication.nonOverlappingProductSet.getMainInstance() }));
            store.dispatch(setSplitBy(MockApplication.productEntityType));
        });

        const state = store.getState();
        expect(state.entitySelection.entitySets.brand.active).toEqual(MockApplication.otherBrandSet.getMainInstance().id);
    });

    it('Should set filter instance to the active of last known entity set if none set', async () => {
        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        await act(async () => {
            store.dispatch(setActiveEntitySet({ entitySet: MockApplication.otherBrandSet }));
            store.dispatch(setSplitBy(MockApplication.productEntityType));
        });
        expect(store.getState().entitySelection.entitySets.brand.active).toEqual(MockApplication.otherBrandSet.getMainInstance().id);
    });

    it('Should set filter instance to the valid one specified in the query', async () => {

        currentMockSearchParams.set("active2", `${MockApplication.nonOverlappingProduct2.id}`);

        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        const state = store.getState();
        expect(state.entitySelection.entitySets.product.active).toEqual(MockApplication.nonOverlappingProduct2.id);

    });

    it('Should set multiple parameters from query', async () => {

        currentMockSearchParams.set("active2", `${MockApplication.nonOverlappingProduct2.id}`);
        currentMockSearchParams.set("active", `${MockApplication.brand2.id}`);
        currentMockSearchParams.set("highlighted", "1.2.3");
        currentMockSearchParams.set("entitySetAverages", "2.4.5.6");

        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        const state = store.getState();
        expect(state.entitySelection.entitySets.product.active).toEqual(MockApplication.nonOverlappingProduct2.id);
        expect(state.entitySelection.entitySets.brand.active).toEqual(MockApplication.brand2.id);
        expect(state.entitySelection.entitySets.brand.highlighted).toEqual([1, 2, 3]);
        expect(state.entitySelection.entitySets.brand.entitySetAverages).toEqual([2, 5]); // Only 2 & 5 are valid brand sets
    });

    it('Should not include invalid IDs from URL parameters in the store', async () => {
        // Set invalid IDs in URL parameters
        const invalidBrandId = 999; // Assuming this ID doesn't exist in the mock data
        const invalidProductId = 888; // Assuming this ID doesn't exist in the mock data
        const invalidHighlightedIds = [777, 666, 555]; // Assuming these IDs don't exist in the mock data
        const invalidEntitySetAverages = [444, 333, 222]; // Assuming these IDs don't exist in the mock data

        currentMockSearchParams.set("active", `${invalidBrandId}`);
        currentMockSearchParams.set("active2", `${invalidProductId}`);
        currentMockSearchParams.set("highlighted", invalidHighlightedIds.join('.'));
        currentMockSearchParams.set("entitySetAverages", invalidEntitySetAverages.join('.'));

        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        const state = store.getState();

        // Check that invalid IDs are not present in the store
        expect(state.entitySelection.entitySets.brand.active).not.toEqual(invalidBrandId);
        expect(state.entitySelection.entitySets.product.active).not.toEqual(invalidProductId);

        // Check that highlighted contains no invalid IDs
        const storeHighlightedIds = state.entitySelection.entitySets.brand.highlighted || [];
        invalidHighlightedIds.forEach(id => {
            expect(storeHighlightedIds).not.toContain(id);
        });

        // Check that entitySetAverages contains no invalid IDs
        const storeEntitySetAverages = state.entitySelection.entitySets.brand.entitySetAverages || [];
        invalidEntitySetAverages.forEach(id => {
            expect(storeEntitySetAverages).not.toContain(id);
        });
    });

    it('Should handle splitBy parameter setting brand when brand is default', async () => {
        currentMockSearchParams.set(legacyParamNames.splitBy, MockApplication.brandEntityType.identifier);

        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        const state = store.getState();
        const activeEntityTypes = state.entitySelection.priorityOrderedEntityTypes;

        expect(activeEntityTypes[0].identifier).toEqual(MockApplication.brandEntityType.identifier);
        expect(activeEntityTypes[1].identifier).toEqual(MockApplication.productEntityType.identifier);
    });

    it('Should handle splitBy parameter setting product when brand is default', async () => {
        currentMockSearchParams.set(legacyParamNames.splitBy, MockApplication.productEntityType.identifier);

        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        const state = store.getState();
        const activeEntityTypes = state.entitySelection.priorityOrderedEntityTypes;

        expect(activeEntityTypes[0].identifier).toEqual(MockApplication.productEntityType.identifier);
        expect(activeEntityTypes[1].identifier).toEqual(MockApplication.brandEntityType.identifier);
    });

    it('Should fallback to default splitBy if invalid splitBy is provided', async () => {
        currentMockSearchParams.set(legacyParamNames.splitBy, "foobar");

        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        const state = store.getState();
        const activeEntityTypes = state.entitySelection.priorityOrderedEntityTypes;

        expect(activeEntityTypes[0].identifier).toEqual(MockApplication.brandEntityType.identifier); // Default fallback
    });

    it('Should correctly handle filterBy parameter in createEntitySelectionFromParams', async () => {
        currentMockSearchParams.set(legacyParamNames.filterBy, `${MockApplication.nonOverlappingProduct2.id}`);

        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);

        const state = store.getState();
        const productSelection = state.entitySelection.entitySets[MockApplication.productEntityType.identifier];

        expect(productSelection.active).toEqual(MockApplication.nonOverlappingProduct2.id);
    });

    it('Should ignore invalid filterBy parameter in createEntitySelectionFromParams', async () => {
        currentMockSearchParams.set(legacyParamNames.filterBy, "999"); // Invalid ID

        await renderUrlSync(store, [MockApplication.brandEntityType]);

        const state = store.getState();
        const brandSelection = state.entitySelection.entitySets[MockApplication.brandEntityType.identifier];

        expect(brandSelection.active).toBeUndefined();
    });

    it('Should use default order if it is reports page', async () => {
        // Mock getCurrentPageInfo to return a reports page without specific defaultSplitBy
        const mockGetCurrentPageInfo = mockReportsPage();

        await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);
        expect(getPriorityOrderedTypeIdentifiers(store)).toEqual(['brand', 'product']);

        // Clean up the mock
        mockGetCurrentPageInfo.mockRestore();
    });

    it('Should use defaultSplitBy from reports page if set to product', async () => {
        // Mock getCurrentPageInfo to return a reports page with product as defaultSplitBy
        const mockGetCurrentPageInfo = mockReportsPage(MockApplication.productEntityType.identifier);

        // The test should verify that when defaultSplitBy is set to product, product comes first
        await renderUrlSync(store, [MockApplication.productEntityType, MockApplication.brandEntityType], [new Metric('metric1')]);
        expect(getPriorityOrderedTypeIdentifiers(store)).toEqual(['product', 'brand']);

        // Clean up the mock
        mockGetCurrentPageInfo.mockRestore();
    });
});

it('Should ignore defaultSplitBy if not in entityTypesForView', async () => {
    let store = setupStore({
        application: { isSessionLoaded: true, primaryMetric: null },
        subset: { subsetId: 'all', subsetConfigurations: [] }
    });
    // Mock getCurrentPageInfo to return a defaultSplitBy that is not in the view
    const mockGetCurrentPageInfo = mockReportsPage('notARealType');
    await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);
    // Should fallback to brand, product order
    expect(getPriorityOrderedTypeIdentifiers(store)).toEqual(['brand', 'product'])
    mockGetCurrentPageInfo.mockRestore();
});

it('Should fallback to brand if all sources for default order are null/undefined', async () => {
    let store = setupStore({
        application: { isSessionLoaded: true, primaryMetric: null },
        subset: { subsetId: 'all', subsetConfigurations: [] }
    });
    // Mock getCurrentPageInfo to return no defaultSplitBy, and metric has no defaultSplitByEntityTypeName
    const mockGetCurrentPageInfo = mockReportsPage();
    await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType], []);
    expect(getPriorityOrderedTypeIdentifiers(store)).toEqual(['brand', 'product']);
    mockGetCurrentPageInfo.mockRestore();
});

it('Should ignore invalid identifiers in paramEntityTypes', async () => {
    const store = setupInitialState({
        searchParams: { entityTypes: 'brand.notARealType.product' }
    });
    await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);
    expect(getPriorityOrderedTypeIdentifiers(store)).toEqual(['brand', 'product']);
});

it('Should ignore paramSplitBy if not in entityTypesForView', async () => {
    const store = setupInitialState({
        searchParams: { SplitBy: 'notARealType' }
    });
    await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);
    expect(getPriorityOrderedTypeIdentifiers(store)).toEqual(['brand', 'product']);
});

it('Should return default order if all sources are empty', async () => {
    const store = setupInitialState();
    await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType], []);
    expect(getPriorityOrderedTypeIdentifiers(store)).toEqual(['brand', 'product']);
});

it('Should handle reports page with only one entity type', async () => {
    const store = setupInitialState();
    // Mock getCurrentPageInfo to return a reports page
    const mockGetCurrentPageInfo = mockReportsPage();
    await renderUrlSync(store, [MockApplication.brandEntityType], [new Metric('metric1')]);
    expect(getPriorityOrderedTypeIdentifiers(store)).toEqual(['brand']);
    mockGetCurrentPageInfo.mockRestore();
});

it('Should remove duplicate entity types if duplicate entity types in priority list', async () => {
    const store = setupInitialState({
        searchParams: { entityTypes: 'brand.brand.product.product' }
    });
    await renderUrlSync(store, [MockApplication.brandEntityType, MockApplication.productEntityType]);
    expect(getPriorityOrderedTypeIdentifiers(store)).toEqual(['brand', 'product']);
});