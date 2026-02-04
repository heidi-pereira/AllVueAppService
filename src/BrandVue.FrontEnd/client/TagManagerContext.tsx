import {createContext, useContext} from "react";
import React from 'react';
import { IGoogleTagManager, useGoogleTagManager } from "./googleTagManager";

const TagManagerContext = createContext<IGoogleTagManager | null>(null);

export const TagManagerProvider = ({ children }: {
    children: React.ReactNode;
}) => {
    const tagManager = useGoogleTagManager();

    return (
        <TagManagerContext.Provider value={tagManager}>
            {children}
        </TagManagerContext.Provider>
    );
};

export const useTagManager = () => {
    const tagManager = useContext(TagManagerContext);
    if (!tagManager) {
        throw new Error('useTagManager must be used within a TagManagerProvider');
    }
    return tagManager;
};