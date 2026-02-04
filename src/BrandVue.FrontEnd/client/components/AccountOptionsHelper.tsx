import {
    isSubsetConfigEnabled,
    isEntityTypeConfigEnabled,
    isColourConfigurationEnabled,
    isPagesAndMetricsConfigEnabled,
    isWeightingsConfigEnabled,
    isFeaturesConfigEnabled,
    isAverageConfigurationEnabled,
    isBrandVueAudiencesConfigEnabled,
    isQuestionVariableDefinitionConfigurationEnabled, isExportDataEnabled,
    isTableBuilderFeatureEnabled,
} from "./helpers/FeaturesHelper";
import { ProductConfiguration } from '../ProductConfiguration';
import { MixPanel } from './mixpanel/MixPanel';

interface IProductConfigProps {
    productConfiguration: ProductConfiguration;
}

export const configureSubsets = (props: IProductConfigProps) => {
    return isSubsetConfigEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/subset-configuration`}
                title="Subsets"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Subsets
            </a>
        </li>
    );
};

export const configureColours = (props: IProductConfigProps) => {
    return isColourConfigurationEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/colour-configuration`}
                title="Configure colours"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Colours
            </a>
        </li>
    );
};

export const configureEntityTypes = (props: IProductConfigProps) => {
    return isEntityTypeConfigEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/entity-type-configuration`}
                title="Configure entity types"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Entity types
            </a>
        </li>
    );
};

export const configurePages = (props: IProductConfigProps) => {
    return isPagesAndMetricsConfigEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/page-configuration`}
                title="Configure Page/Parts/Panes"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Pages
            </a>
        </li>
    );
};

export const configureMetrics = (props: IProductConfigProps) => {
    return isPagesAndMetricsConfigEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/metric-configuration`}
                title="Configure metrics"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Metrics
            </a>
        </li>
    );
};

export const configureAverages = (props: IProductConfigProps) => {
    return isAverageConfigurationEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/average-configuration`}
                title="Configure averages"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Averages
            </a>
        </li>
    );
};

export const configureQuestionVariableDefinitions = (props: IProductConfigProps) => {
    return isQuestionVariableDefinitionConfigurationEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/question-variable-definition-configuration`}
                title="Configure question variable definitions"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Question variables
            </a>
        </li>
    );
};

export const configureAudiences = (props: IProductConfigProps) => {
    return isBrandVueAudiencesConfigEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/audience-configuration`}
                title="Configure audiences"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Audiences
            </a>
        </li>
    );
};

export const configureWeightings = (props: IProductConfigProps) => {
    return isWeightingsConfigEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/weightings-configuration`}
                title="Configure weightings"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Target weightings
            </a>
        </li>
    );
};

export const configureFeatures = (props: IProductConfigProps) => {
    return isFeaturesConfigEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/features-configuration`}
                title="Configure features"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Features
            </a>
        </li>
    );
};

export const configureExportData = (props: IProductConfigProps) => {
    return isExportDataEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`/exportdata?product=${props.productConfiguration.productName || ''}&subProduct=${props.productConfiguration.subProductId || ''}&subset=${[...new URLSearchParams(location.search)].find(f=>f[0].toLowerCase() === 'subset')?.[1] ?? ''}`}
                title="Export data"
                onClick={() => MixPanel.track("configurationOpened")}
            >
                Export data
            </a>
        </li>
    );
};

export const tableBuilderDropdownItem = (props: IProductConfigProps) => {
    return isTableBuilderFeatureEnabled(props.productConfiguration) && (
        <li>
            <a
                className="dropdown-item"
                href={`${props.productConfiguration.appBasePath}/ui/table-builder`}
                title="Table builder"
            >
                Table builder
            </a>
        </li>
    );
};
