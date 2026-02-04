import {EntitySet} from "../entity/EntitySet";
import {EntitySetAverage} from "../entity/EntitySetAverage";
import React from "react";
import {Renderable} from "react-hot-toast";
import {toast} from "react-hot-toast";
import {EntitySetAverageMappingModel, EntitySetModel, Factory, IEntityType} from "../BrandVueApi";
import EntitySetBuilder from "../entity/EntitySetBuilder";
import { EntityInstanceColourRepository } from "../entity/EntityInstanceColourRepository";

export interface IEntitySetConfigClient {
    createEntitySet: (entitySet: EntitySet) => Promise<EntitySet>,
    updateEntitySet: (entitySet: EntitySet) => Promise<EntitySet>,
    deleteEntitySet: (entitySet: EntitySet) => Promise<void>
}

const EntitySetConfigClient = (subsetId: string, entityType: IEntityType, setEntitySet: (entitySet: EntitySet) => void ) => {
    const entitySetConfigClient = Factory.EntitiesClient(error => error());

    const toastMessage = (success: boolean, userFriendlyText: Renderable) => {
        if (success)
            return toast.success(userFriendlyText);

        return toast.error(userFriendlyText);
    };

    const getAverageMappingConfigurations = (entitySet: EntitySet) => {
        return entitySet.getAverages().getAll().map(av => new EntitySetAverageMappingModel({
            id: av.id ?? 0,
            parentEntitySetId: entitySet.id!,
            childEntitySetId: av.entitySetId,
            excludeMainInstance: av.excludeMainInstance
        }));
    }

    const getEntitySetModel = (entitySet: EntitySet) => {
        const instances = entitySet.getInstances().getAll();
        return new EntitySetModel({
            id: entitySet.id,
            instanceIds: instances.map(i => i.id),
            mainInstanceId: entitySet.mainInstance?.id ?? instances[0].id,
            isSectorSet: entitySet.isSectorSet,
            name: entitySet.name,
            entityType: entityType,
            isDefault: entitySet.isDefaultSet,
            isFallback: entitySet.isFallbackSet,
            organisation: "", //This is set by the backend to _userInformationProvider.UserOrganisation
            averageMappings: getAverageMappingConfigurations(entitySet)
        });
    }

    const createEntitySet = (entitySet: EntitySet) => {
        return entitySetConfigClient.createEntitySet(subsetId, getEntitySetModel(entitySet))
            .then((esm) => {
                const entitySetBuilder = new EntitySetBuilder(EntityInstanceColourRepository.empty());
                const updatedEntitySet = entitySetBuilder.fromEntitySet(entitySet)
                    .withBothInstanceGroups(entitySet.getInstances())
                    .withAverages(esm.averageMappings.map(am=>new EntitySetAverage(am.childEntitySetId)))
                    .withId(esm.id).build();
                setEntitySet(updatedEntitySet);
                const message = <span>Created <strong>{entitySet.name}</strong> {entitySet.type.displayNameSingular
                    .toLowerCase()} group</span>;
                toastMessage(true, message);
                return updatedEntitySet;
            })
            .catch((e: Error) => {
                const message = <span>An error occurred trying to create {
                    entitySet.type.displayNameSingular.toLowerCase()} group</span>;
                toastMessage(false, message);
                throw e
            });
    }

    const updateEntitySet = (entitySet: EntitySet) => {
        return entitySetConfigClient.saveEntitySet(subsetId, getEntitySetModel(entitySet))
            .then(() => {
                const entitySetBuilder = new EntitySetBuilder(EntityInstanceColourRepository.empty());
                const updatedEntitySet = entitySetBuilder.fromEntitySet(entitySet)
                    .withBothInstanceGroups(entitySet.getInstances()).build()
                setEntitySet(updatedEntitySet);
                const message = <span>Updated <strong>{entitySet.name}</strong> {entitySet.type.displayNameSingular.toLowerCase()} group</span>;
                toastMessage(true, message);
                return updatedEntitySet;
            })
            .catch((e: Error) => {
                const message =  <span>An error occurred trying to save {entitySet.type.displayNameSingular.toLowerCase()} group</span>;
                toastMessage(false, message);
            });
    }

    const deleteEntitySet = (entitySet: EntitySet) => {
        return entitySetConfigClient.deleteEntitySet(subsetId, entitySet.id!)
            .then(() => {
                const message = <span>Deleted <strong>{entitySet.name}</strong> {entitySet.type.displayNameSingular
                    .toLowerCase()} group</span>;
                toastMessage(true, message);
            })
            .catch((e: Error) => {
                const message = <span>An error occurred trying to delete {
                    entitySet.type.displayNameSingular.toLowerCase()} group</span>;
                toastMessage(false, message);
            });
    }

    return {
        createEntitySet : createEntitySet,
        updateEntitySet: updateEntitySet,
        deleteEntitySet: deleteEntitySet
    } as IEntitySetConfigClient
}

export default EntitySetConfigClient;