import { EntitySetAverage } from "./EntitySetAverage";
import { IEntitySetAverageGroup } from "./IEntitySetAverageGroup";
import {EntitySet} from "./EntitySet";

interface IAverageIdLookup {
    [id: number]: EntitySetAverage;
}

export class EntitySetAverageGroup implements IEntitySetAverageGroup {
    private readonly _averages: EntitySetAverage[] = [];
    private readonly _averageIdLookup: IAverageIdLookup = {};

    constructor(averages: EntitySetAverage[]) {
        averages = averages.map(a => a.clone()); //Clone since color currently mutates directly instead of to the entity set
        this.addAverages(averages);
    }

    getAll(): EntitySetAverage[] {
        return this._averages;
    }
    getById(id: number): EntitySetAverage | undefined {
        return this._averages.find(a => a.id === id);
    }
    addAverages(averages: EntitySetAverage[]): void {
        if (averages)
        averages.forEach(x => this.addAverage(x));
    }
    addAverage(average: EntitySetAverage): void {
        if (!this._averageIdLookup[average.entitySetId]) {
            this._averages.push(average);
            this._averageIdLookup[average.entitySetId] = average;
        }
    }
    removeAverage(average: EntitySetAverage): void {
        let foundAverage = this._averageIdLookup[average.entitySetId];
        if (foundAverage) {
            const index = this._averages.indexOf(foundAverage);
            this._averages.splice(index, 1);
            delete this._averageIdLookup[average.entitySetId];
        }
    }
    clone(): IEntitySetAverageGroup {
        return new EntitySetAverageGroup(this._averages.map(a => a.clone()));
    }
    
}

export const getEntitySetAverageGroupIfUrlParamsDifferFromDefault = (queryEntitySetIds: number[] | undefined, availableEntitySetsForAverages: EntitySet[]) : IEntitySetAverageGroup | null => {
    if (queryEntitySetIds == null) {
        return null;
    }
    return getEntitySetAverageGroupFromIds(queryEntitySetIds, availableEntitySetsForAverages)
}

export const getEntitySetAverageGroupFromIds = (ids: number[], availableEntitySets: EntitySet[]): EntitySetAverageGroup => {
    const entitySets = ids.map(id => availableEntitySets.find(i => i.id == id))
        .filter((b): b is EntitySet => b !== undefined);
    const entitySetAverages = entitySets.map(es => new EntitySetAverage(es.id!));
    return new EntitySetAverageGroup(entitySetAverages);
}