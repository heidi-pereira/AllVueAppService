import React from 'react';
import { useState } from "react";
import { CategoryExportResultCard } from "../../BrandVueApi";

interface ICategoryContextState {
    getCategoryExportResultCards(): CategoryExportResultCard[];
    addCategoryExportResultCards(result: CategoryExportResultCard): void;
    clearCategoryExportResultCards(): void;
}

export const CategoryContextProvider = (props: { children: any }) => {
    const [results, setResults] = useState<CategoryExportResultCard[]>([]);

    const getCategoryExportResultCards = (): CategoryExportResultCard[] => {
        return results;
    }

    const addCategoryExportResultCards = (result: CategoryExportResultCard) => {
        setResults(r => r.concat(result));
    }

    const clearCategoryExportResultCards = () => {
        setResults([]);
    }

    return (
        <CategoryContext.Provider
            value={{
                getCategoryExportResultCards: getCategoryExportResultCards,
                addCategoryExportResultCards: addCategoryExportResultCards,
                clearCategoryExportResultCards: clearCategoryExportResultCards,
            }}>
            {props.children}
        </CategoryContext.Provider>
    );
};

export const CategoryContext = React.createContext<ICategoryContextState>({} as ICategoryContextState);
