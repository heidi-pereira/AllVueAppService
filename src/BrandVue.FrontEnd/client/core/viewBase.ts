import { CuratedFilters } from "../filter/CuratedFilters";
import {Metric} from "../metrics/metric";
import {IEntityType} from "../BrandVueApi";
export class viewBase {

    // Metrics
    public activeMetrics: Metric[];
    public curatedFilters: CuratedFilters;

    constructor(curatedFilters: CuratedFilters) {
        this.curatedFilters = curatedFilters;
    }

    getEntityCombination(): IEntityType[] {
        const entityCombinations = this.activeMetrics?.flatMap(metric => 
            metric.entityCombination ?? []
        ) ?? [];

        const freq = entityCombinations.reduce((map, entity) => {
            const entry = map.get(entity.identifier) || { entity, count: 0 };
            entry.count++;
            map.set(entity.identifier, entry);
            return map;
        }, new Map<string, { entity: IEntityType; count: number }>());

        const maxCount = Math.max(...Array.from(freq.values()).map(e => e.count));

        return Array.from(freq.values())
            .filter(e => e.count === maxCount)
            .map(e => e.entity);
    }
}
