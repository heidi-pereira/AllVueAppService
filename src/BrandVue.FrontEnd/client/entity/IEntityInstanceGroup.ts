import { EntityInstance } from "./EntityInstance";

export interface IEntityInstanceGroup {
    getAll(): EntityInstance[];
    getById(id: number): EntityInstance | undefined;
    addInstances(instances: EntityInstance[]): void;
    addInstance(instance: EntityInstance): void;
    removeInstance(instance: EntityInstance): void;
    containsSameInstances(otherGroup: IEntityInstanceGroup): boolean;
    clone(): IEntityInstanceGroup;
}