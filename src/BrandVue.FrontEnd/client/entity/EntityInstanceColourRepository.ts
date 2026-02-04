import { IMetaDataClient } from "../BrandVueApi";
import * as BrandVueApi from "../BrandVueApi";
import IEntityType = BrandVueApi.IEntityType;

export interface IEntityInstanceColourRepository {
    get(type: IEntityType, instanceId: number): string | undefined;
}

export class EntityInstanceColourRepository implements IEntityInstanceColourRepository {

    public static readonly emptyRepository = new EntityInstanceColourRepository(new Map<string, Map<number, string>>());
    private readonly typeToInstanceIdToColour: Map<string, Map<number, string>>;

    public static async create() : Promise<IEntityInstanceColourRepository> {
        const metadataClient: IMetaDataClient = BrandVueApi.Factory.MetaDataClient(throwErr => throwErr());
        const colourConfigurations = await metadataClient.getColourConfiguration();
        const typeToInstanceIdToColour = new Map(colourConfigurations.map(config => [config.entityType.identifier, new Map(config.instanceColours.map(c => [c.instanceId, c.colour]))]));
        return new EntityInstanceColourRepository(typeToInstanceIdToColour);
    }

    public static empty(): IEntityInstanceColourRepository {
        return EntityInstanceColourRepository.emptyRepository;
    }

    private constructor(typeToInstanceIdToColour: Map<string, Map<number, string>>) {
        this.typeToInstanceIdToColour = typeToInstanceIdToColour;
    }

    public get(type: IEntityType, instanceId: number) {
        return this.typeToInstanceIdToColour.get(type?.identifier)?.get(instanceId);
    }
}