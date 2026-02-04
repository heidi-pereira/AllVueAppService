import { selectActiveEntityType, selectActiveEntitySelection, selectEntitySelectionReady } from './entitySelectionSelectors';
import { RootState, setupStore } from './store';
import { CategorySortKey } from '../BrandVueApi';
import { Provider } from 'react-redux';
import { useActiveBrandSetWithDefault } from "./entitySelectionHooks";
import { MockApplication } from '../helpers/MockApp';
import { renderHook } from '@testing-library/react';
import { MockStoreBuilder } from 'client/helpers/MockStore';
import { EntitySelectionState } from "./entitySelectionSlice";
import { EntityConfigurationStateProvider } from 'client/entity/EntityConfigurationStateContext';
import { EntitySetFactory } from 'client/entity/EntitySetFactory';
import { EntityInstanceColourRepository } from 'client/entity/EntityInstanceColourRepository';
import { EntityConfigurationLoader } from 'client/entity/EntityConfigurationLoader';
import React from "react";

const defaultEntitySelection: EntitySelectionState = {
    activeBreaks: {},
    entitySets: {
        [MockApplication.productEntityType.identifier]: {
            active: MockApplication.product1.id,
            highlighted: [MockApplication.product1.id, MockApplication.mainProduct.id],
            entitySetId: 10,
            entitySetAverages: [100]
        },
        [MockApplication.brandEntityType.identifier]: {
            active: MockApplication.defaultBrandSet.id,
            highlighted: [MockApplication.brand1.id],
            entitySetId: 20,
            entitySetAverages: [200]
        }
    },
    priorityOrderedEntityTypes: [MockApplication.productEntityType, MockApplication.brandEntityType],
    categorySortKey: CategorySortKey.None,
};

describe('entitySelectionSelectors', () => {
    const mockState = new MockStoreBuilder()
        .setEntitySelection(defaultEntitySelection)
        .setSubset({ subsetId: 'all', subsetConfigurations: [] })
        .build() as RootState;

    it('selectActiveEntityType should return the active entity type', () => {
        const result = selectActiveEntityType(mockState);
        expect(result).toEqual(MockApplication.productEntityType);
    });

    it('selectActiveEntitySelection should return the active entity selection', () => {
        const result = selectActiveEntitySelection(mockState);
        expect(result).toEqual(mockState.entitySelection.entitySets[MockApplication.productEntityType.identifier]);
    });

    it('selectEntitySelectionReady should return true if entity selection is ready', () => {
        const result = selectEntitySelectionReady(mockState);
        expect(result).toBe(true);
    });

    it('selectEntitySelectionReady should return false if priorityOrderedEntityTypes is empty', () => {
        const stateWithEmptyTypes = {
            ...mockState,
            entitySelection: { ...mockState.entitySelection, priorityOrderedEntityTypes: [] }
        };
        const result = selectEntitySelectionReady(stateWithEmptyTypes);
        expect(result).toBe(false);
    });
});

describe('entitySelectionSelectors hooks', () => {
    const mockState = new MockStoreBuilder()
        .setEntitySelection(defaultEntitySelection)
        .setSubset({ subsetId: 'all', subsetConfigurations: [] }) // <-- Ensure subset is present
        .build() as RootState;
    let mockStore = setupStore(mockState);
    
    const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={mockStore}>
            <EntityConfigurationStateProvider
                entitySetFactory={new EntitySetFactory(EntityInstanceColourRepository.empty())}
                loader={new EntityConfigurationLoader()}
                initialConfiguration={MockApplication.mockEntityConfiguration}
            >
                {children}
            </EntityConfigurationStateProvider>
        </Provider>
    );

    it('Should return default brand set by default', () => {
        const {result} = renderHook(() => useActiveBrandSetWithDefault(), { wrapper });
        expect(result.current?.id).toEqual(MockApplication.defaultBrandSet.id);
    });

    it('Should load brand set when set', () => {
        mockStore = setupStore(
            new MockStoreBuilder()
                .setActiveBrandSetId(MockApplication.otherBrandSet.id!)
                .setSubset({ subsetId: 'all', subsetConfigurations: [] })
                .build()
        );

        const { result } = renderHook(() => useActiveBrandSetWithDefault(), { wrapper });
        expect(result.current?.id).toEqual(MockApplication.otherBrandSet.id);
    });
});
