import { EntitySetAverage } from "./EntitySetAverage";

export interface IEntitySetAverageGroup {
    getAll(): EntitySetAverage[];
    getById(id: number): EntitySetAverage | undefined;
    addAverages(averages: EntitySetAverage[]): void;
    addAverage(average: EntitySetAverage): void;
    removeAverage(average: EntitySetAverage): void;
    clone(): IEntitySetAverageGroup;
}