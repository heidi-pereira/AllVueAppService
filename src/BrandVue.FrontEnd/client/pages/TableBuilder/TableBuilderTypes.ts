import { IEntityType } from "../../BrandVueApi";
import { Metric } from "../../metrics/metric";

export interface TableItem {
    metric: Metric;
    label: string;
    primaryInstances: TableItemInstances;
    filterInstances: TableItemInstances[];
}

export interface TableItemInstances {
    entityType: IEntityType;
    instances: TableItemInstance[];
}

export interface TableItemInstance {
    instanceIds: number[];
    label: string;
    originalLabel: string;
    enabled: boolean;
    isNet: boolean;
    originalIndex: number; //used for unique ID in mapping
}

export interface TableOptions {
    showValues: boolean;
    showCounts: boolean;
    showIndexScores: boolean;
    showTotalColumn: boolean;
    highlightSignificance: boolean;
    decimalPlaces: number;
}

export type MappedTableGrouping = {
    metric: Metric;
    groupedLabel: string;
    items: TableItemInstances;
    filterInstances: TableFilterInstance[];
};

export type TableFilterInstance = {
    entityType: IEntityType;
    instance: TableItemInstance;
};
