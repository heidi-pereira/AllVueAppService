import React from 'react';
import { CategoryDisplayResult } from './CategoryComparison';

export interface ICategoryTileBaseProps {
    title: string;
    results: CategoryDisplayResult[];
    topResults: CategoryDisplayResult[];
    activeBrandName: string;
    color: string;
    getTooltip: (CategoryDisplayResult) => NonNullable<React.ReactNode>;
    containsMarketAverage: boolean;
    paneIndex: number;
}
