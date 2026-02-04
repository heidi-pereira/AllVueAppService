import { MemoryRouter, Route, Routes } from 'react-router-dom';
import React from 'react';
import { TagManagerProvider } from "../TagManagerContext";
import { EntityConfigurationStateProvider } from "../entity/EntityConfigurationStateContext";
import { EntityInstanceColourRepository } from "../entity/EntityInstanceColourRepository";
import { EntitySetFactory } from "../entity/EntitySetFactory";
import { IEntityConfiguration } from 'client/entity/EntityConfiguration';
import { MockApplication } from "./MockApp";
import { DataSubsetManager } from "../DataSubsetManager";
import { Subset } from "../BrandVueApi";
import { IEntityConfigurationLoader } from 'client/entity/EntityConfigurationLoader';
import { createMockProductConfiguration } from "./MockSession";

const mockTagManagerInstance = {
    trackEvent: jest.fn(),
    trackPageView: jest.fn(),
};

jest.mock('../googleTagManager', () => ({
    useGoogleTagManager: function() {
        return mockTagManagerInstance;
    }
}));

interface MockRouterProps {
    children: React.ReactNode;
    initialEntries?: string[];
    entityConfiguration?: IEntityConfiguration;
}

export const MockRouter: React.FunctionComponent<MockRouterProps> = ({
    children,
    initialEntries = ['/'],
    entityConfiguration
}) => {
    const entityInstanceColourRepository = EntityInstanceColourRepository.emptyRepository;
    const entitySetFactory = new EntitySetFactory(entityInstanceColourRepository);
    const initialConfiguration = entityConfiguration ?? MockApplication.mockEntityConfiguration;
    const mockEntityConfigurationLoader: IEntityConfigurationLoader = { load: jest.fn().mockResolvedValue(createMockProductConfiguration()) };
    DataSubsetManager.selectedSubset = new Subset();
    return (
        <MemoryRouter initialEntries={initialEntries}>
            <TagManagerProvider>
                <EntityConfigurationStateProvider entitySetFactory={entitySetFactory} initialConfiguration={initialConfiguration} loader={mockEntityConfigurationLoader}>
                    <Routes>
                        <Route path="*" element={children}/>
                    </Routes>
                </EntityConfigurationStateProvider>
            </TagManagerProvider>
        </MemoryRouter>
    );
};