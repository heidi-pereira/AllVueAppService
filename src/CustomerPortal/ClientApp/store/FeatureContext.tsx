import React from 'react';
import { useEffect } from 'react';
import { useState } from 'react';
import { Feature, FeatureCode, VueClient } from '../CustomerPortalApi';

interface IFeatureContextState {
    features: Feature[],
    isFeatureEnabled(feature: FeatureCode): boolean;
}

const defaultState: IFeatureContextState = {
    features: [],
    isFeatureEnabled: (feature: FeatureCode) => false,
}

const Context = React.createContext<IFeatureContextState>(defaultState);

export const useFeatureContext = () => React.useContext(Context);

export const FeatureProvider = ({ children }: any) => {
    const [features, setFeatures] = useState<Feature[]>();

    useEffect(() => {
        const vueClient = new VueClient();
        vueClient.getEnabledFeaturesForCurrentUser().then(config => setFeatures(config));
    }, []);

    const isFeatureEnabled = (feature: FeatureCode) => features?.find(x => x.featureCode === feature) != undefined;
    return (
        <Context.Provider value={{ features: features || [], isFeatureEnabled}}>
            {children}
        </Context.Provider>
    );
};