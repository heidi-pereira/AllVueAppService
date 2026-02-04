import { IEntityType, PermissionFeaturesOptions } from "../../../BrandVueApi";
import { ProductConfigurationContext } from "../../../ProductConfigurationContext";
import { PageHandler } from "../../../components/PageHandler";
import { isCrosstabAdministrator, hasAllVuePermissionsOrSystemAdmin} from "../../../components/helpers/FeaturesHelper";
import { MixPanel } from "../../../components/mixpanel/MixPanel";
import { IGoogleTagManager } from "../../../googleTagManager";
import { Metric } from "../../../metrics/metric";
import { useState, useEffect, useContext } from "react";
import toast from "react-hot-toast";
import MetricListItemContextMenu from "./MetricListItemContextMenu";
import { getMetricDisplayText } from "../../../components/helpers/SurveyVueUtils";
import { VariableListItem } from './VariableListItem';
const selectedQuestionId = "selectedQuestion";

export interface IMetricListItemProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    variableListItem: VariableListItem;
    splitByEntityType: IEntityType | undefined;
    isSelected: boolean;
    canEditMetrics: boolean;
    showHamburger: boolean;
    groupCustomVariables: boolean;
    subsetId: string;
    selectMetric(): void;
    setEligibleForCrosstabOrAllVue(isEligible: boolean): Promise<void>;
    setMetricEnabled(isEligible: boolean): Promise<void>;
    setFilterForMetricEnabled(isEligible: boolean): Promise<void>;
    setMetricDefaultSplitBy(entityType: IEntityType): Promise<void>;
    setConvertCalculationTypeModalVisible: () => void;
}

const MetricListItem = (props: IMetricListItemProps) => {
    const { productConfiguration } = useContext(ProductConfigurationContext);
    const [eligibleForCrosstabOrAllVue, setEligibleForCrosstabOrAllVue] = useState<boolean>(props.variableListItem.metric.eligibleForCrosstabOrAllVue)
    const [metricEnabled, setMetricEnabled] = useState<boolean>(!props.variableListItem.metric.disableMeasure);
    const [filterForMetricEnabled, setFilterForMetricEnabled] = useState<boolean>(!props.variableListItem.metric.disableMeasure);

    useEffect(() => {
        setEligibleForCrosstabOrAllVue(props.variableListItem.metric.eligibleForCrosstabOrAllVue);
        setMetricEnabled(!props.variableListItem.metric.disableMeasure);
        setFilterForMetricEnabled(!props.variableListItem.metric.disableFilter);
    }, [props.variableListItem.metric]);

    const setDisableMeasure = (isDisabled: boolean) => {
        if (!hasAllVuePermissionsOrSystemAdmin(productConfiguration, [PermissionFeaturesOptions.VariablesEdit])) {
            throw new Error("Don't have permission to edit");
        }
        const isEnabled = !isDisabled;
        setMetricEnabled(isEnabled);
        props.setMetricEnabled(isEnabled).then(
            () => {
            },
            (error) => {
                toast.error(`An error occurred trying to ${isEnabled ? 'enable' : 'disable'} this question`);
                console.error(error);
                setMetricEnabled(!props.variableListItem.metric.disableMeasure);
            }
        );
    }

    const setDisableFilterForMeasure = (isDisabled: boolean) => {
        if (!hasAllVuePermissionsOrSystemAdmin(productConfiguration, [PermissionFeaturesOptions.VariablesEdit])) {
            throw new Error("Don't have permission to edit");
        }
        const isEnabled = !isDisabled;
        setFilterForMetricEnabled(isEnabled);
        props.setFilterForMetricEnabled(isEnabled).then(
            () => {
            },
            (error) => {
                toast.error(`An error occurred trying to ${isEnabled ? 'enable' : 'disable'} filter this question`);
                console.error(error);
                setFilterForMetricEnabled(!props.variableListItem.metric.disableFilter);
            }
        );
    }

    const updateIsEligibleForCrosstabOrAllVue = (metric: Metric, isEligible: boolean) => {
        if (!hasAllVuePermissionsOrSystemAdmin(productConfiguration, [PermissionFeaturesOptions.VariablesEdit])) {
            throw new Error("Don't have permission to edit");
        }

        setEligibleForCrosstabOrAllVue(isEligible);
        props.setEligibleForCrosstabOrAllVue(isEligible).then(
            () => {
                props.googleTagManager.addEvent(isEligible ? "surveyVueEnableMetric" : "surveyVueDisableMetric", props.pageHandler, { value: metric.name })
            },
            (error) => {
                props.googleTagManager.addEvent(isEligible ? "surveyVueEnableMetricFailed" : "surveyVueDisableMetricFailed", props.pageHandler, { value: metric.name })
                toast.error(`An error occurred trying to ${isEligible ? 'enable' : 'disable'} this question`);
                console.error(error);
                setEligibleForCrosstabOrAllVue(props.variableListItem.metric.eligibleForCrosstabOrAllVue);
            }
        );
    }

    const setMetricDefaultSplitBy = (entityType: IEntityType) => {
        if (!props.canEditMetrics) throw new Error("Don't have permission to change the metric default split by");

        props.setMetricDefaultSplitBy(entityType).then(
            () => {
                props.googleTagManager.addEvent("surveyVueSwapMetricSplitBy", props.pageHandler, { value: props.variableListItem.metric?.name })
            },
            (error) => {
                props.googleTagManager.addEvent("surveyVueSwapMetricSplitByFailed", props.pageHandler, { value: props.variableListItem.metric?.name })
                toast.error(`An error occurred trying to swap the split by for this question`);
                console.error(error);
            }
        );
    }

    const handleItemClick = () => {
        MixPanel.track("crossTabMetricChanged");
        props.selectMetric();
    }

    const getTooltip = (displayText: string, metric: Metric) => {
        if (isCrosstabAdministrator(productConfiguration) && !productConfiguration.isSurveyVue()) {
            return `Name:${metric.name}\r\n` +
                `Enabled:${metric.disableMeasure ? "Disabled" : "Enabled"}\r\n` +
                `Analysis/Explore Data:${metric.eligibleForCrosstabOrAllVue ? "Enabled" : "Disabled"}\r\n` +
                `Filter :${metric.disableFilter ? "Disabled" : "Enabled \r\n" + metric.filterValueMapping.map(x => `${x.fullText} : ${x.values.join(",")}`).join("\r\n")}\r\n` +
                `\r\n` + displayText;
        }
        return displayText;
    }

    const metricListElement = () => {
        const displayText = getMetricDisplayText(props.variableListItem.metric);
        const postClassName = (props.variableListItem.metric.disableMeasure ? " really-disabled" : '') + (eligibleForCrosstabOrAllVue ? '' : ' disabled');
        return (
            <>
                <div className="name-container">
                    {((props.groupCustomVariables) || props.variableListItem.metric.isAutoGeneratedNumeric()) && <span className={`var-name${postClassName}`}>{props.variableListItem.metric.displayName}</span>}
                    <span className={`title${postClassName} `} title={getTooltip(displayText, props.variableListItem.metric)}>
                        {displayText}
                    </span>
                </div>
                {
                    props.showHamburger &&
                    <span className='buttons'>
                        <MetricListItemContextMenu variableListItem={props.variableListItem}
                            splitByEntityType={props.splitByEntityType}
                            canEditMetrics={props.canEditMetrics}
                            googleTagManager={props.googleTagManager}
                            eligibleForCrosstabOrAllVue={eligibleForCrosstabOrAllVue}
                            metricEnabled={metricEnabled}
                            setEligibleForCrosstabOrAllVue={updateIsEligibleForCrosstabOrAllVue}
                            setMetricDefaultSplitBy={setMetricDefaultSplitBy}
                            setConvertCalculationTypeModalVisible={props.setConvertCalculationTypeModalVisible}
                            setDisableMeasure={setDisableMeasure}
                            setDisableFilterMeasure={setDisableFilterForMeasure}
                            filterEnabled={filterForMetricEnabled}
                            subsetId={props.subsetId}
                        />
                    </span>
                }
            </>
        );
    }

    const containerClass = `metric-list-item${props.isSelected ? ' selected' : ''}`;

    return (
        <div onClick={handleItemClick} className={containerClass} id={props.isSelected ? selectedQuestionId : undefined}>
            <div className="title-container">
                {metricListElement()}
            </div>
        </div>
    );
}

export default MetricListItem;