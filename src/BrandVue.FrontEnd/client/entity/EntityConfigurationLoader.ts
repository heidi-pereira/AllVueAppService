import * as BrandVueApi from "../BrandVueApi";
import {IMetaDataClient} from "../BrandVueApi";
import { EntityConfiguration, IEntityConfiguration } from "./EntityConfiguration";
import {EntityInstance} from "./EntityInstance";
import { IEntitySetFactory } from "./EntitySetFactory";

export interface IEntityConfigurationLoader {
     load(subsetId: string, entitySetFactory: IEntitySetFactory): Promise<IEntityConfiguration>;
}

export class EntityConfigurationLoader implements IEntityConfigurationLoader {
    private readonly _metadataClient: IMetaDataClient = BrandVueApi.Factory.MetaDataClient(throwErr => throwErr());

    public async load(subsetId: string, entitySetFactory: IEntitySetFactory): Promise<EntityConfiguration> {

        const subsetEntityConfiguration = await this._metadataClient.getEntityTypeConfigurationModels(subsetId);
        const uiEntityTypeConfigModel = subsetEntityConfiguration.entityTypeConfigurationModels.map(apiModel => {
            const allInstances = apiModel.allInstances.map(i => EntityInstance.convertInstanceFromApi(i));
            const instanceFromId = new Map(allInstances.map(i => [i.id, i]));
            return {
                EntityType: apiModel.entityType,
                EntitySets: apiModel.entitySets.map(
                    s => entitySetFactory.fromApiEntitySet(apiModel.entityType, instanceFromId, s)),
                DefaultEntitySetName: apiModel.defaultEntitySetName,
                AllInstances: allInstances
            };
        });
        const entitySetManager = new EntityConfiguration(uiEntityTypeConfigModel, subsetEntityConfiguration.defaultEntityTypeName, subsetId);
        return entitySetManager;
    }
}