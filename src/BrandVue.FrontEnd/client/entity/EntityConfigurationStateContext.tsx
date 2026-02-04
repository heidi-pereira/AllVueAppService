import React, { ReactElement, useState, useEffect } from 'react';
import { EntityConfiguration, IEntityConfiguration } from './EntityConfiguration';
import { IEntitySetFactory } from './EntitySetFactory';
import { IEntityConfigurationLoader } from './EntityConfigurationLoader';
import { DataSubsetManager } from '../DataSubsetManager';
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from 'client/state/subsetSlice';

export type EntityConfigurationAction =
    | { type: "RELOAD_ENTITYCONFIGURATION";};


interface EntityConfigurationContextState {
    entityConfiguration: IEntityConfiguration;
    hasEntityConfigurationLoaded: boolean;
    entityConfigurationDispatch: (action: EntityConfigurationAction) => Promise<void>;
}

const defaultEntityConfig = new EntityConfiguration([], "", "");
export const EntityConfigurationStateContext = React.createContext<EntityConfigurationContextState>({ entityConfiguration: defaultEntityConfig, hasEntityConfigurationLoaded: false, entityConfigurationDispatch: () => Promise.resolve() });
export const useEntityConfigurationStateContext = () => React.useContext(EntityConfigurationStateContext);

interface IProps {
    entitySetFactory: IEntitySetFactory;
    loader: IEntityConfigurationLoader;
    initialConfiguration?: IEntityConfiguration;
    children: any;
}

export const EntityConfigurationStateProvider = (props: IProps) => {
    const [entityConfiguration, setEntityConfiguration] = useState<IEntityConfiguration>(props.initialConfiguration ?? new EntityConfiguration([], "brand", "All"));
    const [hasEntityConfigurationLoaded, setEntityConfigurationLoaded] = useState<boolean>(!!props.initialConfiguration);
    const subsetId = useAppSelector(selectSubsetId);

    const reloadEntityConfiguration = async () => {
        setEntityConfigurationLoaded(false);
        await props.loader.load(subsetId, props.entitySetFactory)
            .then((r) => {
                if (r) {
                    setEntityConfiguration(r)
                }
            })
            .finally(() =>
                setEntityConfigurationLoaded(true)
            );
    }

    useEffect(() => {
        if (!hasEntityConfigurationLoaded) {
            reloadEntityConfiguration()
        }
    }, []);

    const asyncDispatch = async (action: EntityConfigurationAction) => {
        switch (action.type) {
            case "RELOAD_ENTITYCONFIGURATION":
                return await reloadEntityConfiguration();
            default:
                throw new Error("Unsupported action type");
        }
    }

    return (
        <EntityConfigurationStateContext.Provider value={{ entityConfiguration: entityConfiguration, hasEntityConfigurationLoaded: hasEntityConfigurationLoaded, entityConfigurationDispatch: asyncDispatch }}>
            {props.children}
        </EntityConfigurationStateContext.Provider>
    );
};