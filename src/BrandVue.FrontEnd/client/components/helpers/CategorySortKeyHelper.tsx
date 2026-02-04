import {CategorySortKey} from "../../BrandVueApi";

export const categorySortMap = {
    "Best scores": CategorySortKey.BestScores,
    "Worst scores": CategorySortKey.WorstScores,
    "Over performing": CategorySortKey.OverPerforming,
    "Under performing": CategorySortKey.UnderPerforming
};

export const getCategorySortKeysQueryString = (sortKey: CategorySortKey): string => {
    const entry = Object.entries(categorySortMap).find(([_, value]) => value === sortKey);
    return entry ? entry[0] : "";
}

export const getCategorySortKey = (sortKey: string): CategorySortKey | null => {
    if (Object.values(CategorySortKey).includes(sortKey as CategorySortKey)) {
        return sortKey as CategorySortKey;
    }
    return null;
}