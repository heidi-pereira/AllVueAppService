import { Metric } from '../../../../metrics/metric';
import { CrossMeasure, CrossMeasureFilterInstance, MainQuestionType, IEntityType } from '../../../../BrandVueApi';
import React from 'react';
import { FilterValueMapping } from '../../../../metrics/metricSet';
import { canUseFilterValueMappingAsBreak, getAvailableCrossMeasureFilterInstances, shouldUseFilterValueMappingAsBreak } from '../../../helpers/SurveyVueUtils';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { sortEntityInstances } from "../../../../entity/EntityInstance";
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { ProductConfigurationContext } from '../../../../ProductConfigurationContext';
import { MixPanel } from '../../../mixpanel/MixPanel';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import style from "./CrossMeasureInstanceSelector.module.less";

export interface ICrossMeasureInstanceSelectorProps {
    selectedCrossMeasure: CrossMeasure;
    selectedMetric: Metric;
    activeEntityType: IEntityType | undefined;
    setCrossMeasures: (crossMeasures: CrossMeasure[]) => void;
    disabled: boolean;
    forceablySelectTwo?: boolean;
    includeSelectAll: boolean;
}

interface IFilterInstance {
    displayName: string;
    filterValueMappingName: string;
    instanceId: number;
    checked: boolean;
    toggle(): void;
}

const CrossMeasureInstanceSelector = (props: ICrossMeasureInstanceSelectorProps) => {
    const [multipleChoiceDropdownOpen, setMultipleChoiceDropdownOpen] = React.useState<boolean>(false);
    const [selectAll, setSelectAll] = React.useState<boolean>(false);
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const { questionTypeLookup } = useMetricStateContext();


    const crossMeasure = props.selectedCrossMeasure;
    const metric = props.selectedMetric;
    const metricBasedOnSingleChoice = questionTypeLookup[metric.name] == MainQuestionType.SingleChoice;
    const filterValueMappingSupported = canUseFilterValueMappingAsBreak(metric);
    const showFilterValueMappings = shouldUseFilterValueMappingAsBreak(metric, crossMeasure?.multipleChoiceByValue, metricBasedOnSingleChoice);
    const showMultipleChoiceByValueToggle = filterValueMappingSupported &&
        !metricBasedOnSingleChoice &&
        props.activeEntityType &&
        metric.entityCombination[0]?.identifier === props.activeEntityType.identifier;

    React.useEffect(() => {
        if(props.selectedCrossMeasure?.filterInstances.length === instanceOptions.length)
            MixPanel.track("selectedAll");
        setSelectAll(props.selectedCrossMeasure?.filterInstances.length === instanceOptions.length)
    }, [props.selectedCrossMeasure]);

    const toggleFilterValueMapping = (instance: FilterValueMapping) => {
        if (crossMeasure && !props.disabled) {
            const indexOfFilterValueName = crossMeasure.filterInstances.findIndex(i => i.filterValueMappingName === instance.fullText);
            let newFilterInstances = [...crossMeasure.filterInstances];

            if (indexOfFilterValueName >= 0) {
                newFilterInstances.splice(indexOfFilterValueName, 1);
            } else {
                newFilterInstances.push(new CrossMeasureFilterInstance({
                    filterValueMappingName: instance.fullText,
                    instanceId: -1,
                }));
            }

            const newCrossMeasure = new CrossMeasure({
                ...crossMeasure,
                filterInstances: newFilterInstances,
            });
            props.setCrossMeasures([newCrossMeasure]);
        }
    }

    const toggleEntityInstance = (instanceId: number) => {
        if (crossMeasure && !props.disabled) {
            const indexOfInstanceId = crossMeasure.filterInstances.findIndex(i => i.instanceId === instanceId);
            let newFilterInstances = [...crossMeasure.filterInstances];

            if (indexOfInstanceId >= 0) {
                newFilterInstances.splice(indexOfInstanceId, 1);

                if (selectAll) {
                    setSelectAll(false);
                }
            } else {
                newFilterInstances.push(new CrossMeasureFilterInstance({
                    filterValueMappingName: "",
                    instanceId: instanceId,
                }));

                if (newFilterInstances.length === instanceOptions.length) {
                    setSelectAll(true);
                }
            }

            const newCrossMeasure = new CrossMeasure({
                ...crossMeasure,
                filterInstances: newFilterInstances,
            });
            props.setCrossMeasures([newCrossMeasure]);
        }
    }

    const toggleMultipleChoiceByValue = (multipleChoiceByValue: boolean) => {
        if (multipleChoiceByValue !== crossMeasure.multipleChoiceByValue) {
            const instances = getAvailableCrossMeasureFilterInstances(metric, entityConfiguration, multipleChoiceByValue, metricBasedOnSingleChoice)
            const newCrossMeasure = new CrossMeasure({
                measureName: crossMeasure.measureName,
                filterInstances: props.forceablySelectTwo ? instances.slice(0, 2) : instances,
                childMeasures: [],
                multipleChoiceByValue: multipleChoiceByValue,
            });
            props.setCrossMeasures([newCrossMeasure]);
        }
    }

    const getAvailableFilterInstances = (metric: Metric): IFilterInstance[] => {
        if (showFilterValueMappings) {
            return metric.filterValueMapping.map(filterValue => {
                return {
                    displayName: filterValue.fullText,
                    filterValueMappingName: filterValue.fullText,
                    instanceId: filterValue.values && filterValue.values.length > 0 ? Number(filterValue.values[0]) : -1,
                    checked: crossMeasure?.filterInstances.some(i => i.filterValueMappingName == filterValue.fullText) ?? false,
                    toggle: () => toggleFilterValueMapping(filterValue),
                };
            });
        } else {
            const entityInstances = entityConfiguration.getAllEnabledInstancesForTypeOrdered(metric.entityCombination[0]);
            if (!productConfiguration.isSurveyVue) {
                entityInstances.sort(sortEntityInstances)
            }
            return entityInstances.map(instance => {
                return {
                    displayName: instance.name,
                    filterValueMappingName: "",
                    instanceId: instance.id,
                    checked: crossMeasure?.filterInstances.some(i => i.instanceId == instance.id) ?? false,
                    toggle: () => toggleEntityInstance(instance.id),
                };
            });
        }
    }

    let instanceOptions: IFilterInstance[] = getAvailableFilterInstances(metric);
    instanceOptions = instanceOptions.sort((a, b) => Number(a.instanceId) - Number(b.instanceId));
    const entityTypeName = metric.entityCombination[0]?.displayNamePlural;
    const multipleChoiceSelectedText = crossMeasure?.multipleChoiceByValue ? 'Choices' : entityTypeName;

    const selectAllHandler = () => {
        if(props.disabled){
            return;
        }

        setSelectAll(!selectAll);
        if (!selectAll) {
            const newCrossMeasure = new CrossMeasure({
                ...crossMeasure,
                filterInstances: instanceOptions,
            });
            props.setCrossMeasures([newCrossMeasure]);
        } else {
            const newCrossMeasure = new CrossMeasure({
                ...crossMeasure,
                filterInstances: [],
            });
            props.setCrossMeasures([newCrossMeasure]);
            MixPanel.track("selectedAll");
        };
    };

    return (
        <div className={`${style.instanceSelector} ${props.disabled && style.disabled}`}>
            {showMultipleChoiceByValueToggle &&
                <ButtonDropdown isOpen={multipleChoiceDropdownOpen} toggle={() => setMultipleChoiceDropdownOpen(!multipleChoiceDropdownOpen)} className="crossmeasure-multiple-choice-dropdown">
                    <DropdownToggle className="toggle-button" disabled={props.disabled}>
                        <div className="title">{multipleChoiceSelectedText}</div>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu className="crossmeasure-multiple-choice-dropdown-menu">
                        <DropdownItem onClick={() => toggleMultipleChoiceByValue(false)}>{entityTypeName}</DropdownItem>
                        <DropdownItem onClick={() => toggleMultipleChoiceByValue(true)}>Choices</DropdownItem>
                    </DropdownMenu>
                </ButtonDropdown>
            }
            <div className={`${style.breakInstanceSelector}  ${props.disabled && style.disabled}`}>
                {props.includeSelectAll && 
                    <>
                    <div className={`instance-checkbox${props.disabled ? " disabled" : ""}`} key="SelectAll">
                        <input type="checkbox" className="checkbox" checked={selectAll} disabled={props.disabled} onChange={selectAllHandler} />
                        <label className={`${style.instanceCheckboxLabel} ${props.disabled && style.disabled}`} onClick={selectAllHandler}>
                            <span>Select All</span>
                        </label>
                    </div>
                    <hr />
                    </>
                }
                {instanceOptions.map((instance, index) =>
                    <div className="instance-checkbox" key={`${metric.name}-${instance.displayName}-${index}`}>
                        <input type="checkbox" className="checkbox"
                            checked={instance.checked}
                            onChange={instance.toggle}
                            disabled={props.disabled}
                        />
                        <label className={`${style.instanceCheckboxLabel} ${props.disabled && style.disabled}`} title={instance.displayName} onClick={instance.toggle}>
                            <span>{instance.displayName}</span>
                        </label>
                    </div>
                )}
            </div>
        </div>
    );
}

export default CrossMeasureInstanceSelector;