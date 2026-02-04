import React from 'react';
import Comparison from "../visualisations/MetricComparison/Comparison";

export interface IComparisonContextState {
    getComparisons(): Comparison[];
    addComparison(result: Comparison): void;
    clearComparisons(): void;
    setComparisons(comparisons: Comparison[]): void;
}

export const ComparisonContextProvider = (props: { children: any }) => {
    const [data, setData] = React.useState<Comparison[]>([]);

    const getComparisons = (): Comparison[] => {
        return data;
    }

    const addComparison = (comparison: Comparison) => {
        setData(r => r.concat(comparison));
    }

    const clearComparisons = () => {
        setData([]);
    }

    const setComparisons = (comparisons: Comparison[]) => {
        setData(comparisons);
    }

    return (
        <ComparisonContext.Provider
            value={{
                getComparisons: getComparisons,
                addComparison: addComparison,
                clearComparisons: clearComparisons,
                setComparisons: setComparisons
            }}>
            {props.children}
        </ComparisonContext.Provider>
    );
};

export const ComparisonContext = React.createContext<IComparisonContextState>({} as IComparisonContextState);