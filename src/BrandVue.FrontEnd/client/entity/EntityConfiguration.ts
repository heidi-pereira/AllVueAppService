import { EntitySet } from "./EntitySet";
import { IEntityType, SelectedEntityInstances } from "../BrandVueApi";
import {EntityInstance} from "./EntityInstance";
import { EntityInstanceGroup } from "./EntityInstanceGroup";
import { EntitySetAverageGroup } from "./EntitySetAverageGroup";

export interface IEntityConfiguration {
    entityTypeConfigurationExists(responseEntityType: IEntityType): boolean;
    getDefaultEntitySetFor(responseEntityType: IEntityType): EntitySet;
    getEntitySet(entityTypeId: string, entitySetId: number | null): EntitySet | null;
    getSetsFor(entityType: IEntityType): EntitySet[];
    getAllEnabledInstancesForType(entityType: IEntityType) : EntityInstance[];
    getAllEnabledInstancesForTypeOrdered(entityType: IEntityType) : EntityInstance[];
    getAllEnabledInstancesOrderedAsSet(entityType: IEntityType): EntitySet;
    defaultEntityType: IEntityType;
    getAllEnabledInstancesForResponseTypeNameOrdered(name: string): EntityInstance[];
    getEnabledInstancesById(entityType: string, ids: number[]): EntityInstance[];
    getEntityType(entityType: string): IEntityType | null;
    getBrandEntityTypeOrNull(): IEntityType | null;
    getSelectedInstancesOrderedAsSet(entityType: IEntityType, instances: SelectedEntityInstances) : EntitySet;
}

export interface IEntityConfigurationModel {
    EntityType: IEntityType;
    EntitySets: EntitySet[];
    DefaultEntitySetName: string;
    AllInstances: EntityInstance[];
}

export class EntityConfiguration implements IEntityConfiguration {

    private readonly _entityTypeToConfiguration: Record<string, IEntityConfigurationModel>;
    private readonly _defaultEntityType: IEntityType;
    private readonly _selectedSubsetId: string;
    public constructor(uiModels: IEntityConfigurationModel[], defaultEntityType: string, selectedSubsetId: string) {
        const map: Record<string, IEntityConfigurationModel> = {};
        uiModels.forEach(m => map[m.EntityType.identifier] = m);

        const defaultEntityTypeConfig = map[defaultEntityType];
        if (defaultEntityTypeConfig === undefined) {
            return;
        }
        this._defaultEntityType = defaultEntityTypeConfig.EntityType;
        this._entityTypeToConfiguration = map;
        this._selectedSubsetId = selectedSubsetId;
    }

    public get defaultEntityType(): IEntityType {
        return this._defaultEntityType;
    }

    private static getNamedEntitySet(entityConfiguration: IEntityConfigurationModel, name: string) {
        const activeSet = entityConfiguration.EntitySets.find(s => s.name == name);
        if (!activeSet) throw new Error(`Set ${name} was not found`);
        return activeSet;
    }

    public entityTypeConfigurationExists(responseEntityType: IEntityType): boolean {
        return responseEntityType?.identifier != null && this._entityTypeToConfiguration[responseEntityType.identifier] != null;
    }

    public getDefaultEntitySetFor(responseEntityType: IEntityType): EntitySet {
        const entityConfig = this._entityTypeToConfiguration[responseEntityType.identifier]!;
        return EntityConfiguration.getNamedEntitySet(entityConfig, entityConfig.DefaultEntitySetName);
    }
    
    public getEntitySet(entityTypeId: string, entitySetId: number | null): EntitySet | null {

        return !entitySetId ? null : this._entityTypeToConfiguration?.[entityTypeId]?.EntitySets.find(x => x.id == entitySetId) ?? null;
    }

    public getSetsFor(entityType: IEntityType): EntitySet[] {
        return this._entityTypeToConfiguration?.[entityType.identifier]?.EntitySets ?? [];
    }

    public getAllEnabledInstancesForType(entityType: IEntityType): EntityInstance[] {
        return this._entityTypeToConfiguration?.[entityType?.identifier]?.AllInstances.filter(e => e.enabledBySubset[this._selectedSubsetId] == undefined || e.enabledBySubset[this._selectedSubsetId]) ?? [];
    }

    public getAllEnabledInstancesForTypeOrdered(entityType: IEntityType) : EntityInstance[] {
        return [...this.getAllEnabledInstancesForType(entityType)].sort((a,b) => a.id - b.id);
    }

    public getAllEnabledInstancesOrderedAsSet(entityType: IEntityType) : EntitySet {
        const instances = this.getAllEnabledInstancesForTypeOrdered(entityType);
        const instanceGroup = new EntityInstanceGroup(instances);
        const averageGroup = new EntitySetAverageGroup([]);

        const mainInstance = instances.length > 0 ? instances[0] : EntityInstance.AllInstances;
        return new EntitySet(undefined, entityType, "All", instanceGroup, false, false, mainInstance, averageGroup);
    }

    public getSelectedInstancesOrderedAsSet(entityType: IEntityType, instances: SelectedEntityInstances) : EntitySet {
        const allInstances = this.getAllEnabledInstancesForTypeOrdered(entityType);
        const selectedInstances = allInstances.filter(a => instances.selectedInstances.some(i => a.id == i))

        const instanceGroup = new EntityInstanceGroup(selectedInstances);
        const averageGroup = new EntitySetAverageGroup([]);

        const mainInstance = selectedInstances.length > 0 ? selectedInstances[0] : EntityInstance.AllInstances;
        return new EntitySet(undefined, entityType, "All", instanceGroup, false, false, mainInstance, averageGroup);
    }

    public getEnabledInstancesById(entityType: string, ids: number[]): EntityInstance[] {
        return this._entityTypeToConfiguration[entityType]!.AllInstances.filter(e => ids.includes(e.id) && (e.enabledBySubset[this._selectedSubsetId] == undefined || e.enabledBySubset[this._selectedSubsetId] == true));
    }
    
    public getAllEnabledInstancesForResponseTypeNameOrdered(entityType: string): EntityInstance[] {
        const enabledTypes = this._entityTypeToConfiguration?.[entityType]?.AllInstances.filter(e => e.enabledBySubset[this._selectedSubsetId] == undefined || e.enabledBySubset[this._selectedSubsetId] == true) ?? [];
        return enabledTypes.sort((a,b) => a.id - b.id);
    }

    public getEntityType(entityType: string): IEntityType | null {
        return this._entityTypeToConfiguration[entityType]?.EntityType ?? null;
    }

    public getBrandEntityTypeOrNull(): IEntityType | null {
        // Forgive any missing configuration so we can use this before checking for NoMetadataNotification
        return this._entityTypeToConfiguration && Object.values(this._entityTypeToConfiguration).find(x=>x.EntityType.isBrand)?.EntityType || null;
    }
}
