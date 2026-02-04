import { PageHandler } from "./components/PageHandler";
import React from 'react';
import { EntitySetFactory, IEntitySetFactory } from "./entity/EntitySetFactory";
import { EntityInstanceColourRepository } from "./entity/EntityInstanceColourRepository";
import { IApplicationUser } from "./BrandVueApi";

export interface GlobalParameters {
    pageHandler?: PageHandler
};

export const GlobalContext = React.createContext<GlobalParameters>({});

export const EntitySetFactoryContext = React.createContext<IEntitySetFactory>(new EntitySetFactory(EntityInstanceColourRepository.empty()));

export const UserContext = React.createContext<IApplicationUser | null>(null);