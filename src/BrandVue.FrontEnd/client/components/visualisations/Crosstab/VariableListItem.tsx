import { VariableConfigurationModel } from 'client/BrandVueApi';
import { Metric } from 'client/metrics/metric';
import { VariableType } from './VariableType';


export interface VariableListItem {
    metric: Metric;
    variable: VariableConfigurationModel | undefined;
    variableType: VariableType;
}
