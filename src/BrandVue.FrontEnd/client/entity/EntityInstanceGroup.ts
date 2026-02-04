import { IEntityInstanceGroup } from "./IEntityInstanceGroup";
import { EntityInstance } from "./EntityInstance";

interface IInstanceIdLookup {
    [id: number]: EntityInstance;
}

export const getEntityInstanceGroupFromIds = (ids: number[], availableInstances: EntityInstance[]): EntityInstanceGroup => {
    return new EntityInstanceGroup(ids.map(id => availableInstances.find(i => i.id == id)).filter((b): b is EntityInstance => b !== undefined)) ?? [];
}

export const isEntityInstanceGroupInUrl = (queryInstanceIds: number[], entityInstanceGroup: IEntityInstanceGroup) => {
    const allInstanceIDs = entityInstanceGroup.getAll().map(e => e.id).sort()
    const sortedUpdatedPeerParams = [...queryInstanceIds].sort()
    return JSON.stringify(sortedUpdatedPeerParams) === JSON.stringify(allInstanceIDs)
}

export const getEntityInstanceGroupIfUrlParamsDifferFromDefault = (queryInstanceIds: number[], entityInstanceGroup: IEntityInstanceGroup, availableInstances: EntityInstance[]) => {
    if (queryInstanceIds.length !== 0) {
        if (!isEntityInstanceGroupInUrl(queryInstanceIds, entityInstanceGroup)) {
            return getEntityInstanceGroupFromIds(queryInstanceIds, availableInstances)
        }
    }
    return null
}

export class EntityInstanceGroup implements IEntityInstanceGroup{
    private readonly _instances: EntityInstance[] = [];
    private readonly _instanceIdLookup: IInstanceIdLookup = {};

    constructor(instances: EntityInstance[]) {
        instances = instances.map(i => i.clone()) //Clone since color currently mutates directly instead of to the entity set
            .sort(EntityInstanceGroup.compareInstances); // PERF: Inserting in order saves a lot of slicing and dicing for big arrays
        this.addInstances(instances);
    }

    getAll(): EntityInstance[] {
        return this._instances;
    }

    getById(id: number): EntityInstance | undefined {
        return this._instanceIdLookup[id];
    }

    public addInstances(instances: EntityInstance[]) {
        if (instances)
            instances.forEach(x => this.addInstance(x));
    }

    public addInstance(i: EntityInstance) {
        if (!this._instanceIdLookup[i.id]) {
            this.binaryInsertBrand(i, 0, this._instances.length - 1);
            this._instanceIdLookup[i.id] = i;
        }
    }

    private binaryInsertBrand(value: EntityInstance, start: number, end: number) {
        var length = this._instances.length;
        var middle = start + Math.floor((end - start) / 2);

        if (length === 0) {
            this._instances.push(value);
            return;
        }

        if (EntityInstanceGroup.compareInstances(value, this._instances[end]) >= 0) {
            this._instances.splice(end + 1, 0, value);
            return;
        }

        if (EntityInstanceGroup.compareInstances(value, this._instances[start]) <= 0) {
            this._instances.splice(start, 0, value);
            return;
        }

        if (start >= end) {
            return;
        }

        if (EntityInstanceGroup.compareInstances(value, this._instances[middle]) > 0) {
            this.binaryInsertBrand(value, middle + 1, end);
            return;
        } else {
            this.binaryInsertBrand(value, start, middle - 1);
            return;
        }
    }

    public removeInstance(i: EntityInstance) {
        let foundBrand = this._instanceIdLookup[i.id];
        if (foundBrand) {
            const index = this._instances.indexOf(foundBrand);
            this._instances.splice(index, 1);
            delete this._instanceIdLookup[i.id];
        }
    }

    private static compareInstances(first: EntityInstance, second: EntityInstance) : number {
        return first.name.localeCompare(
            second.name,
            'en',
            {
                'sensitivity': 'accent',
                'numeric': true,
                'caseFirst': 'upper'
            });
    }

    public containsSameInstances(otherGroup: IEntityInstanceGroup): boolean {
        if (otherGroup.getAll().length !== this._instances.length) {
            return false;
        }
        return this._instances.every((x) => otherGroup.getById(x.id) !== undefined);
    }

    public clone(): IEntityInstanceGroup {
        return new EntityInstanceGroup(this._instances.map(i => i.clone()));
    }
}