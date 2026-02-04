import { PageDescriptor, IEntityType } from "../BrandVueApi";
import { IEntityInstanceGroup } from "./IEntityInstanceGroup";
import { EntityInstance } from "./EntityInstance";
import { IEntitySetAverageGroup } from "./IEntitySetAverageGroup";
import { EntityInstanceColourRepository, IEntityInstanceColourRepository } from "./EntityInstanceColourRepository";
import { IEntitySetSelection } from "../state/entitySelectionSlice";
import { IEntitySetFactory } from "./EntitySetFactory";
import { IEntityConfiguration } from "./EntityConfiguration";
import { getEntityInstanceGroupIfUrlParamsDifferFromDefault } from "./EntityInstanceGroup";
import { getEntitySetAverageGroupIfUrlParamsDifferFromDefault } from "./EntitySetAverageGroup";

const defaultColours = [
    "#18b5f9",
    "#feda10",
    "#3dc14e",
    "#e0a0e9",
    "#ffa800",
    "#00d3d6",
    "#ef3c79",
    "#7e4ade",
    "#bcf60c",
    "#ff703a",
    "#4367e0",
    "#0095aa",
    "#3d922f",
    "#ff5bdf",
];

export const defaultFocusColour = "#ff002b";
const defaultUndefinedColour = "#7F7F7F";

export class EntitySet {
    public readonly id: number | undefined;
    public readonly name: string;
    public readonly type: IEntityType;
    public mainInstance: EntityInstance | undefined;

    public readonly isSectorSet: boolean;
    public readonly isCustomSet: boolean;
    public readonly isDefaultSet: boolean;
    public readonly isFallbackSet: boolean;
    public readonly page?: PageDescriptor;
    protected readonly _instances: IEntityInstanceGroup;
    protected readonly _averages: IEntitySetAverageGroup;

    protected readonly _entityInstanceColourRepository: IEntityInstanceColourRepository;
    protected readonly _instanceColoursCache: Map<number, string>;

    private _colourIndex: number = 0;

    public constructor(
        id: number | undefined,
        type: IEntityType,
        name: string,
        instances: IEntityInstanceGroup,
        isSectorSet: boolean,
        isDefaultSet: boolean,
        mainInstance: EntityInstance | undefined,
        averages: IEntitySetAverageGroup,
        isCustom: boolean = false,
        isFallback = false,
        entityInstanceColourRepository = EntityInstanceColourRepository.empty()
    ) {
        this.id = id;
        this.type = type;
        this.name = name;
        this.mainInstance = mainInstance;
        this._instances = instances;
        this.isSectorSet = isSectorSet;
        this.isCustomSet = isCustom;
        this.isDefaultSet = isDefaultSet;
        this.isFallbackSet = isFallback;
        this._averages = averages;
        this._entityInstanceColourRepository = entityInstanceColourRepository;
        this._instanceColoursCache = new Map<number, string>();
        for (let instance of instances.getAll()) {
            this._instanceColoursCache.set(instance.id, this._entityInstanceColourRepository.get(this.type, instance.id) ?? this.getDefaultColour());
        }
        if (mainInstance) {
            this._instanceColoursCache.set(mainInstance.id, this._entityInstanceColourRepository.get(this.type, mainInstance.id) ?? defaultFocusColour);
        }
    }

    public cloneSet(instances?: IEntityInstanceGroup): EntitySet {
        return new EntitySet(
            this.id,
            this.type,
            this.name,
            instances ?? this._instances,
            this.isSectorSet,
            this.isDefaultSet,
            this.mainInstance,
            this._averages,
            this.isCustomSet,
            this.isFallbackSet,
            this._entityInstanceColourRepository
        );
    }

    public getMainInstance(): EntityInstance {
        return this.mainInstance ?? this._instances.getAll()[0];
    }
    public getInstances() {
        return this._instances;
    }

    public getAverages() {
        return this._averages;
    }

    public getInstanceColor(entityInstance: EntityInstance): string {
        return this._instanceColoursCache.get(entityInstance?.id) ?? defaultUndefinedColour;
    }

    private getDefaultColour(): string {
        const colour = defaultColours[this._colourIndex];
        this._colourIndex++;
        this._colourIndex = this._colourIndex % defaultColours.length;
        return colour;
    }

    public InstanceEquals(type: IEntityType, instances: IEntityInstanceGroup): boolean {
        return this.type.identifier == type.identifier && this._instances.containsSameInstances(instances);
    }

    public GetEntitySetIcon(): string {
        if (this.type.isBrand) {
            return "store";
        }
        return "local_offer";
    }
}

interface IEntitySetSortOptions {
    ignoreCase?: boolean;
}

export const createEntitySetFromSelection = (
    entitySetSelection: IEntitySetSelection,
    allEntitySets: EntitySet[],
    sourceEntitySet: EntitySet,
    entitySetFactory: IEntitySetFactory,
    entityConfiguration: IEntityConfiguration
): EntitySet => {
    if (entitySetSelection == null || Object.values(entitySetSelection).every((v) => v == null)) {
        return sourceEntitySet;
    }

    const entitySetBuilder = entitySetFactory.getBuilder();
    entitySetBuilder.fromEntitySet(sourceEntitySet);

    if (entitySetSelection?.highlighted != undefined) {
        let highlighted = getEntityInstanceGroupIfUrlParamsDifferFromDefault(
            entitySetSelection.highlighted!,
            sourceEntitySet.getInstances(),
            entityConfiguration.getAllEnabledInstancesForType(sourceEntitySet.type)
        );
        entitySetBuilder.withInstanceGroup(highlighted ?? sourceEntitySet.getInstances());
    }

    if (entitySetSelection?.entitySetAverages) {
        const averages = getEntitySetAverageGroupIfUrlParamsDifferFromDefault(entitySetSelection.entitySetAverages, allEntitySets);
        entitySetBuilder.withAverages(averages?.getAll() ?? sourceEntitySet.getAverages().getAll());
    }

    // if the url main instance is different from entity set default main instance update the selected entity set, otherwise remove the url parameter
    if (entitySetSelection?.active !== undefined) {
        if (entitySetSelection.active !== sourceEntitySet.mainInstance?.id) {
            const allEnabledInstances = entityConfiguration.getAllEnabledInstancesForType(sourceEntitySet.type);
            const mainInstance = allEnabledInstances.find((e) => e.id === entitySetSelection.active) ?? allEnabledInstances[0];
            entitySetBuilder.withMainInstance(mainInstance);
        }
    }
    return entitySetBuilder.build();
};

export function sortEntitySets(es1: EntitySet, es2: EntitySet, options?: IEntitySetSortOptions) {
    let name1: string = es1.name;
    let name2: string = es2.name;
    if (!(options?.ignoreCase === false)) {
        name1 = name1.toLowerCase();
        name2 = name2.toLowerCase();
    }

    if (name1 > name2) return 1;
    if (name1 < name2) return -1;
    return 0;
}
