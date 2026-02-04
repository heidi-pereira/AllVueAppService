import { EntitySet } from "./EntitySet";

export class EntitySetAverage {
    public readonly id: number | undefined;
    public readonly entitySetId: number;
    public readonly excludeMainInstance: boolean;

    constructor(entitySetId: number, excludeMainInstance: boolean = false, id?: number) {
        this.id = id;
        this.entitySetId = entitySetId;
        this.excludeMainInstance = excludeMainInstance;
    }

    public clone(): EntitySetAverage {
        const clone = new EntitySetAverage(this.entitySetId, this.excludeMainInstance, this.id);
        return clone;
    }

    public getEntitySet(selectedEntitySet: EntitySet, availableEntitySets: EntitySet[]): EntitySet {
        return this.entitySetId == selectedEntitySet.id ? selectedEntitySet : availableEntitySets.find(es => es.id === this.entitySetId)!;
    }

    public static getChartDisplayName(name: string) {
        return name.toLowerCase().includes("competitive average") ? name : name + " (competitive average)"
    }
}