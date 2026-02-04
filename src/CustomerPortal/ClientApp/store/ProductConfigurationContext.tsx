import React from 'react';
import { useEffect } from 'react';
import { useState } from 'react';
import { ProductConfigurationResult, VueClient } from '../CustomerPortalApi';

interface IProductConfigurationContextState {
    productConfiguration: ProductConfigurationResult,
    getDataPageUrl(subProductId: string): string;
    getReportsPageUrl(subProductId: string): string;
    getSettingsPageUrl(subProductId: string): string;
    getOpenEndsPageUrl(subProductId: string): string;
}

const defaultState: IProductConfigurationContextState = {
    productConfiguration: undefined,
    getDataPageUrl: () => '',
    getReportsPageUrl: () => '',
    getSettingsPageUrl: () => '',
    getOpenEndsPageUrl: () => ''
}

const Context = React.createContext<IProductConfigurationContextState>(defaultState);

export const useProductConfigurationContext = () => React.useContext(Context);

export const ProductConfigurationProvider = ({ children }: any) => {
    const [productConfiguration, setProductConfiguration] = useState<ProductConfigurationResult>();

    useEffect(() => {
        const vueClient = new VueClient();
        vueClient.getProductConfiguration().then(config => setProductConfiguration(config));
    }, []);
    const vueUrl = productConfiguration?.vueContext?.vueUrl?.replace(/survey\/$/, '')
    const getBaseUrl = (subProductId: string) => `${productConfiguration?.vueContext?.vueUrl}${subProductId}`;
    const getDataPageUrl = (subProductId: string) => `${getBaseUrl(subProductId)}/ui/crosstabbing`;
    const getReportsPageUrl = (subProductId: string) => `${getBaseUrl(subProductId)}/ui/reports`;
    const getSettingsPageUrl = (subProductId: string) => `${getBaseUrl(subProductId)}/ui/settings`;
    const getOpenEndsPageUrl = (subProductId: string) => `${vueUrl}openends/survey/${subProductId}`;
    return (
        <Context.Provider value={{ productConfiguration, getDataPageUrl, getReportsPageUrl, getSettingsPageUrl, getOpenEndsPageUrl }}>
            {children}
        </Context.Provider>
    );
};