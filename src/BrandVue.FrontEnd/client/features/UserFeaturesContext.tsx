import React from 'react';
import { useEffect, useState, createContext, useContext } from "react";
import * as BrandVueApi from "../BrandVueApi";
import { IApplicationUser } from '../BrandVueApi';

export interface FeaturesModel {
    Id: number;
    Name: string;
    DocumentationUrl: string;
    FeatureCode: BrandVueApi.FeatureCode;
    IsActive: boolean;
};


interface UserFeaturesContextType {
    features: FeaturesModel[];
}

interface UserFeaturesProviderProps {
    children: React.ReactNode;
    user: IApplicationUser | null;
}

export const UserFeaturesContext = createContext<UserFeaturesContextType | undefined>(undefined);


export const UserFeaturesProvider: React.FC<UserFeaturesProviderProps> = ({ children, user }) => {
    const [features, setFeatures] = useState<FeaturesModel[]>([]);
    const userFeaturesClient = BrandVueApi.Factory.UserFeaturesClient(error => error());

    useEffect(() => {
        userFeaturesClient.get().then(f => {
            var result = f.map(f => ({
                Id: f.id,
                Name: f.name,
                DocumentationUrl: f.documentationUrl,
                FeatureCode: f.featureCode,
                IsActive: f.isActive
            }));
            setFeatures(result);
        });
    }, [user]);

    return (
        <UserFeaturesContext.Provider value={{ features: features }}>
            { children }
        </UserFeaturesContext.Provider >
    );
};

export const useUserFeaturesContext = () => useContext(UserFeaturesContext);