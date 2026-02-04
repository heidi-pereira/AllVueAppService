import { IEntityTypeConfiguration } from '../BrandVueApi';
import { RootState, createAppSelector } from './store';


export const selectActiveEntityType = (state: RootState): IEntityTypeConfiguration | null => {
    const entityConfigurationState = state.entityConfiguration;
    if (entityConfigurationState.activeEntityTypeIdentifier) {
        return entityConfigurationState.configurationByIdentifier[entityConfigurationState.activeEntityTypeIdentifier] || null;
    }
    return null;
};

export const selectConfigurations = createAppSelector(
    (state: RootState) => state.entityConfiguration.configurationByIdentifier,
    (configurations) => Object.values(configurations)
);
