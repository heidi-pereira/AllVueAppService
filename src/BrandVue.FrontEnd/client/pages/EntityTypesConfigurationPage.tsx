import React from "react";
import { ChangeEvent, useEffect } from "react";
import _ from "lodash";
import { useAppDispatch, useAppSelector } from "../state/store";
import { fetchEntityTypeConfigurations, saveEntityType, setActiveEntityTypeByIdentifier } from "../state/entityConfigurationSlice";
import { selectActiveEntityType, selectConfigurations } from 'client/state/entityConfigurationSelectors';
import ConfigurationList from "./ConfigurationList";
import { ConfigurationElement } from "./ConfigurationList";
import EntityTypeConfigurationPage from "./EntityTypeConfigurationPage";
import { IEntityTypeConfiguration } from "../BrandVueApi";

interface IPageProps {
    nav: React.ReactNode;
}

const EntityTypesConfigurationPage: React.FunctionComponent<IPageProps> = (props: IPageProps) => {
    const dispatch = useAppDispatch();
    const activeEntityType = useAppSelector(selectActiveEntityType);
    const configurations= useAppSelector(selectConfigurations);
    const loading = useAppSelector(state => state.entityConfiguration.loading);
    const error = useAppSelector(state => state.entityConfiguration.error);

    useEffect(() => {
        if (configurations.length === 0) {
            dispatch(fetchEntityTypeConfigurations());
        }
    }, [dispatch, configurations]);

    const assertValid = (entityType: IEntityTypeConfiguration, displayNameSingular: string, displayNamePlural: string) =>
    {
        if (!displayNameSingular || displayNameSingular.trim().length === 0)
            return "Value cannot be null or empty";

        if (!displayNamePlural || displayNamePlural.trim().length === 0)
            return "Value cannot be null or empty";

        if (entityType.displayNameSingular === displayNameSingular && entityType.displayNamePlural === displayNamePlural)
            return "No change";

        const entitiesNotEditing = configurations.filter(r => r.identifier !== entityType.identifier);

        if (entitiesNotEditing.find(ec => ec.displayNameSingular === displayNameSingular))
            return `"${displayNameSingular}" already in use`;

        if (entitiesNotEditing.find(ec => ec.displayNamePlural === displayNamePlural))
            return `"${displayNamePlural}" already in use`;
    }

    const handleBlur = async (event: ChangeEvent<HTMLInputElement>, entityType: IEntityTypeConfiguration, displayNameSingular: string, displayNamePlural: string): Promise<void> => {

        const validationError = assertValid(entityType, displayNameSingular, displayNamePlural);
        if (validationError)
            return Promise.reject(new Error(validationError));

        dispatch(saveEntityType({ identifier: entityType.identifier, displayNameSingular, displayNamePlural }));
    }

    const getSearchableNames = (entity: IEntityTypeConfiguration) => {
        return [entity.displayNameSingular, entity.identifier]
    }

    const configElements: ConfigurationElement[] = configurations.map((e) =>
        ({ id: e.id, displayName: `${e.displayNameSingular} (${e.identifier})`, configObject: e, enabled: true, searchableNames: getSearchableNames(e) }));
    
    return (
        <div className="configuration-page">
            {props.nav}
            {loading && <p>Loading...</p>}
            {error && <p>Error: {error}</p>}
            <div className="view-chart-configurations">
                <ConfigurationList
                    configTypeName="choice set"
                    configElements={configElements}
                    onSelectElementClick={configurationElement => { 
                        dispatch(setActiveEntityTypeByIdentifier(configurationElement.configObject.identifier));
                    }}
                    includeId={false}
                />
                {activeEntityType && <EntityTypeConfigurationPage entityTypeConfiguration={activeEntityType} handleBlur={handleBlur} />}
            </div>
        </div>
    );
};

export default EntityTypesConfigurationPage;
