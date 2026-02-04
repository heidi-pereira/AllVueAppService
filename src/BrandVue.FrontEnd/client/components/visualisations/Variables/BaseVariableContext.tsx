import React from 'react';
import { useEffect, useState } from "react";
import { Factory, VariableConfigurationModel } from '../../../BrandVueApi';

export type BaseVariableAction =
    | { type: "RELOAD_BASE_VARIABLES";};

interface IBaseVariableContextState {
    baseVariables: VariableConfigurationModel[];
    baseVariableDispatch: (action: BaseVariableAction) => Promise<void>;
    baseVariablesLoading: boolean;
}

export const BaseVariableContextProvider = (props: { children: any }) => {
    const variablesClient = Factory.VariableConfigurationClient(error => error());
    const [baseVariablesLoading, setIsLoading] = useState(true);
    const [baseVariables, setBaseVariables] = useState<VariableConfigurationModel[]>([]);

    useEffect(() => {
        reloadBaseVariables();
    }, []);

    const reloadBaseVariables = () => {
        setIsLoading(true);
        variablesClient.getBaseVariables().then(variables =>
            setBaseVariables(variables?.sort((a, b) => a.displayName.localeCompare(b.displayName)) ?? []))
            .then(() => setIsLoading(false));
    }

    const asyncDispatch = async (action: BaseVariableAction) => {
        switch (action.type) {
            case "RELOAD_BASE_VARIABLES":
                return reloadBaseVariables();
            default:
                throw new Error("Unsupported action type");
        }
    }

    return (
        <BaseVariableContext.Provider 
            value={{ 
                baseVariables: baseVariables,
                baseVariableDispatch: asyncDispatch,
                baseVariablesLoading: baseVariablesLoading,
            }}>
            {props.children}
        </BaseVariableContext.Provider>
    );
};

export const BaseVariableContext = React.createContext<IBaseVariableContextState>({} as IBaseVariableContextState);
