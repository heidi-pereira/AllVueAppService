import { VariableConfigurationModel } from '../BrandVueApi';
import { createAppSelector, RootState } from './store';
import { HydratedVariableConfigurationState } from './variableConfigurationsSlice';

/**
 * If your code uses "instanceof" you need to use this function to convert the plain JSON object to a class instance
 * This selector prevents a refresh loop for things that use this as props
 * Usage: useAppSelector(selectHydratedVariableConfiguration)
 */

export const selectHydratedVariableConfiguration = createAppSelector(
    [(state: RootState) => state.variableConfiguration],
    (variableConfiguration): HydratedVariableConfigurationState => {
        return {
            ...variableConfiguration,
            variables: variableConfiguration?.variables?.map(v => VariableConfigurationModel.fromJS(v)) ?? null
        };
    }
);
