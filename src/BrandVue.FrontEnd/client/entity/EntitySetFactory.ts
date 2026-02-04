import * as BrandVueApi from "../BrandVueApi";
import { IEntityType } from "../BrandVueApi";
import { EntityInstanceGroup } from "./EntityInstanceGroup";
import { EntityInstance } from "./EntityInstance";
import { EntitySet } from "./EntitySet";
import { EntitySetAverage } from "./EntitySetAverage";
import { EntitySetAverageGroup } from "./EntitySetAverageGroup";
import { IEntityInstanceGroup } from "./IEntityInstanceGroup";
import { IEntityInstanceColourRepository } from "./EntityInstanceColourRepository";
import EntitySetBuilder from "./EntitySetBuilder";

export interface IEntitySetFactory {
    fromApiEntitySet(type: IEntityType, instanceFromId: Map<number, EntityInstance>, entitySet: BrandVueApi.EntitySetModel): EntitySet;
    getSetFromInstances(entitySets: EntitySet[], instances: IEntityInstanceGroup, mainInstance: EntityInstance | undefined, type: IEntityType): EntitySet;
    getBuilder(): EntitySetBuilder;
}

export class EntitySetFactory implements IEntitySetFactory {
    private readonly _customSetName = "Custom group";
    private readonly _entityInstanceColourRepository: IEntityInstanceColourRepository;


    constructor(entityInstanceColourRepository: IEntityInstanceColourRepository) {
        this._entityInstanceColourRepository = entityInstanceColourRepository;
    }

    public fromApiEntitySet(type: IEntityType, instanceFromId: Map<number, EntityInstance>, entitySet: BrandVueApi.EntitySetModel): EntitySet {

        const mainInstance = this.getMainInstance(entitySet, instanceFromId);
        const instances = entitySet.instanceIds.map(id => instanceFromId.get(id)).filter(EntitySetFactory.notNull);

        const instanceGroup = new EntityInstanceGroup(instances);

        const averages = entitySet.averageMappings.map(am => new EntitySetAverage(am.childEntitySetId, am.excludeMainInstance, am.id));
        const averageGroup = new EntitySetAverageGroup(averages);

        return new EntitySet(entitySet.id, type, entitySet.name, instanceGroup, entitySet.isSectorSet, entitySet.isDefault, mainInstance, averageGroup, false, entitySet.isFallback, this._entityInstanceColourRepository);
    }

    public getSetFromInstances(entitySets: EntitySet[], instances: IEntityInstanceGroup, mainInstance: EntityInstance, type: IEntityType): EntitySet {

        const matchingSets = entitySets.filter(s => s.InstanceEquals(type, instances));

        if (matchingSets.length < 1) {
            return this.createCustomSet(type, instances, mainInstance);
        }

        if (!mainInstance) {
            return matchingSets[0].cloneSet();
        }

        const matchingSetWithMatchingMainInstance = matchingSets.find(entitySet => entitySet.mainInstance?.id === mainInstance.id);
        if (matchingSetWithMatchingMainInstance) {
            return matchingSetWithMatchingMainInstance.cloneSet();
        }

        // We want the entity set to have the main instance but we don't want the main instance to be in peer or highlighted.
        const newSet = matchingSets[0].cloneSet();
        newSet.mainInstance = mainInstance;
        return newSet;
    }

    public getBuilder() {
        return new EntitySetBuilder(this._entityInstanceColourRepository)
    }

    private createCustomSet(type: IEntityType, instances: IEntityInstanceGroup, mainInstance: EntityInstance): EntitySet {
        instances = instances.clone();
        const newSetId = 0;
        // Average to point to the new set
        const averages = new EntitySetAverageGroup([new EntitySetAverage(newSetId)]);
        return new EntitySet(newSetId, type, this._customSetName, instances, false, false, mainInstance, averages, true, false, this._entityInstanceColourRepository);
    }

    private getMainInstance(entitySet: BrandVueApi.EntitySetModel, instanceFromId: Map<number, EntityInstance>): EntityInstance | undefined {
        if (instanceFromId.size > 0) {
            if (entitySet.mainInstanceId != null) {
                return instanceFromId.get(entitySet.mainInstanceId) ?? instanceFromId.values().next().value;
            }
            return instanceFromId.values().next().value;
        }
    }

    private static notNull<TValue>(value: TValue | null | undefined): value is TValue {
        if (value === null || value === undefined) return false;
        return true;
    }
}