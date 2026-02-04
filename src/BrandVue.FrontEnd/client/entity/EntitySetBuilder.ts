import {EntitySet} from "./EntitySet";
import {IEntityType} from "../BrandVueApi";
import {IEntityInstanceGroup} from "./IEntityInstanceGroup";
import {EntityInstance} from "./EntityInstance";
import {EntityInstanceGroup} from "./EntityInstanceGroup";
import {EntitySetAverage} from "./EntitySetAverage";
import { IEntityInstanceColourRepository } from "./EntityInstanceColourRepository";
import {IEntitySetAverageGroup} from "./IEntitySetAverageGroup";
import {EntitySetAverageGroup} from "./EntitySetAverageGroup";
import {IEntitySetSelection} from "../state/entitySelectionSlice";

export default class EntitySetBuilder {
    private readonly _entityInstanceColourRepository: IEntityInstanceColourRepository;
    private _type: IEntityType
    private _name: string
    private _instances: IEntityInstanceGroup
    private _isSectorSet: boolean = false
    private _mainInstance: EntityInstance | undefined
    private _isCustom: boolean = false
    private _isDefaultSet: boolean = false
    private _isFallbackSet: boolean = false
    private _id: number | undefined = undefined
    private _averages: IEntitySetAverageGroup

    public constructor(entityInstanceColourRepository: IEntityInstanceColourRepository) {
        this._entityInstanceColourRepository = entityInstanceColourRepository;
    }
    
    public fromEntitySet(entitySet: EntitySet): EntitySetBuilder{
        this._type = entitySet.type
        this._name = entitySet.name
        this._instances = entitySet.getInstances().clone()
        this._isSectorSet = entitySet.isSectorSet
        this._mainInstance = entitySet.mainInstance
        this._isCustom = entitySet.isCustomSet
        this._id = entitySet.id
        this._isDefaultSet = entitySet.isDefaultSet
        this._isFallbackSet = entitySet.isFallbackSet
        this._averages = entitySet.getAverages().clone()
        return this
    }
    
    public toStateEntitySelection() : IEntitySetSelection {
        return {
            entitySetId: this._id,
            active: this._mainInstance?.id,
            highlighted: this._instances.getAll().map(x => x.id),
            entitySetAverages: this._averages.getAll().filter(a => a.entitySetId).map(a => a.entitySetId)
        }
    }
    
    public asType(type: IEntityType): EntitySetBuilder{
        this._type = type
        return this
    }
    
    public withName(name: string): EntitySetBuilder {
        this._name = name
        return this
    }
    
    public withInstances(instances: EntityInstance[]): EntitySetBuilder{
        this._instances = new EntityInstanceGroup(instances).clone()
        return this
    }

    public withInstanceGroup(instances: IEntityInstanceGroup): EntitySetBuilder{
        this._instances = instances.clone()
        return this
    }

    public withBothInstances(instances: EntityInstance[]): EntitySetBuilder{
        this._instances = new EntityInstanceGroup(instances).clone()
        return this
    }

    public withBothInstanceGroups(instances: IEntityInstanceGroup): EntitySetBuilder{
        this._instances = instances.clone()
        return this
    }
    
    public isSectorSet(isSectorSet: boolean): EntitySetBuilder{
        this._isSectorSet = isSectorSet
        return this
    }
    
    public withMainInstance(mainInstance: EntityInstance | undefined): EntitySetBuilder{
        this._mainInstance = mainInstance
        return this
    }
    
    public withIsDefaultSet(isDefaultSet: boolean): EntitySetBuilder{
        this._isDefaultSet = isDefaultSet
        return this
    }

    public withIsFallbackSet(isFallbackSet: boolean): EntitySetBuilder{
        this._isFallbackSet = isFallbackSet
        return this
    }
    
    public isCustomSet(isCustomSet: boolean): EntitySetBuilder{
        this._isCustom = isCustomSet
        return this
    }

    public isDefaultSet(isDefaultSet: boolean): EntitySetBuilder{
        this._isDefaultSet = isDefaultSet
        return this
    }
    
    public withId(id: number | undefined): EntitySetBuilder{
        this._id = id
        return this
    }

    public withAverages(averages: EntitySetAverage[]): EntitySetBuilder {
        this._averages = new EntitySetAverageGroup(averages);
        return this
    }

    public withNoIdAverages(averages: EntitySetAverage[]): EntitySetBuilder {
        const averagesWithIdRemoved = averages.map(a => new EntitySetAverage(a.entitySetId, a.excludeMainInstance));
        this._averages = new EntitySetAverageGroup(averagesWithIdRemoved);
        return this
    }

    public withResetSelfRefAverage(): EntitySetBuilder {
        this._averages = this.getAveragesWithResetSelfRef(this._id)
        return this
    }

    public build(): EntitySet {
        return new EntitySet(this._id, this._type, this._name, this._instances, this._isSectorSet, this._isDefaultSet, this._mainInstance, this._averages, this._isCustom, this._isFallbackSet, this._entityInstanceColourRepository)
    }

    private getAveragesWithResetSelfRef(entitySetId: number | undefined): IEntitySetAverageGroup {
        const newAverages = this._averages.getAll()
            .map(a => a.entitySetId === entitySetId ? this.getSelfRefAverage(a) : a)
        return new EntitySetAverageGroup(newAverages);
    }

    private getSelfRefAverage(average: EntitySetAverage): EntitySetAverage {
        return new EntitySetAverage(0, average.excludeMainInstance)
    }
}