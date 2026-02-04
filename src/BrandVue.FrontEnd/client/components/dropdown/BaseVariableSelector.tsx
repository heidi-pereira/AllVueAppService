import React from 'react';
import BaseVariableReadonlyDropdownMenu from '../visualisations/Reports/Components/BaseTypeReadonlyDropdownMenu';
import style from "./BaseVariableSelector.module.less";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { QueryStringParamNames, useReadVueQueryParams } from '../helpers/UrlHelper';
import { VariableConfigurationModel, BaseFieldExpressionVariableDefinition } from '../../BrandVueApi';
import { EntityInstance } from '../../entity/EntityInstance';
import { BaseVariableContext } from '../visualisations/Variables/BaseVariableContext';
import { StringHelper } from '../../helpers/StringHelper';
import { MixPanel } from '../mixpanel/MixPanel';

export type BaseVariablesForComparison = {
    baseVariable1: VariableConfigurationModel | undefined,
    baseVariable2: VariableConfigurationModel | undefined,
}

interface BaseVariableSelectorProps {
    focusInstance: EntityInstance;
    updateBaseVariables(variableIds: BaseVariablesForComparison): void;
    setHeaderText(header: string): void;
    defaultBaseVariableIdentifier: string | undefined;
}

enum Comparison {
    MetricVsAverage = "Brand Metric vs Average",
    MetricVsMetric = "Brand Metric vs Brand Metric"
}

const BaseVariableSelector = (props: BaseVariableSelectorProps) => {
    const { baseVariables, baseVariablesLoading } = React.useContext(BaseVariableContext);
    const [isComparisonSelectorDropdownOpen, setComparisonSelectorDropdownOpen] = React.useState<boolean>(false);
    const [baseVariable1, setBaseVariable1] = React.useState<VariableConfigurationModel | undefined>(undefined);
    const [baseVariable2, setBaseVariable2] = React.useState<VariableConfigurationModel | undefined>(undefined);
    const [selectedComparison, setSelectedComparison] = React.useState<Comparison>(baseVariable2 ? Comparison.MetricVsMetric : Comparison.MetricVsAverage);
    const [formattedBaseVariables, setFormattedBaseVariables] = React.useState<VariableConfigurationModel[]>([]);
    const { getQueryParameterInt } = useReadVueQueryParams();
    const dropdownClickHandler = (value) => {
        value === Comparison.MetricVsAverage ? MixPanel.track("metricVsAverage") : MixPanel.track("metricVsMetric");
        setSelectedComparison(value)
    }

    const comparisonSelector = (comparison: Comparison) => {
        return (
            <div className={`page-title-menu ${style.control}`}>
                <div className="page-title-label">Comparison</div>
                <ButtonDropdown
                    isOpen={isComparisonSelectorDropdownOpen}
                    toggle={() => setComparisonSelectorDropdownOpen(!isComparisonSelectorDropdownOpen)}
                    className={`configure-option-dropdown base-type-dropdown ${style.comparisonType} ${style.dropdown}`}
                >
                    <DropdownToggle className={`toggle-button ${style.toggle}`}>
                        <span>{comparison}</span>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu>
                        {Object.values(Comparison).map(c => {
                            return (
                                <DropdownItem
                                    key={`comparison-${c}`}
                                    onClick={() => { dropdownClickHandler(c) }}
                                >
                                    {c}
                                </DropdownItem>
                            )
                        })
                        }
                    </DropdownMenu>
                </ButtonDropdown>
            </div>
        )
    }

    const baseVariableHasBrandId = (baseVariable: VariableConfigurationModel): boolean => {
        if (baseVariable.definition instanceof BaseFieldExpressionVariableDefinition) {
            return baseVariable.definition.resultEntityTypeNames.some(typeName => typeName == "brand");
        }
        return false;
    }

    const baseDropdown = (label: string, baseVariables: VariableConfigurationModel[], selectBaseVariable: (variable: VariableConfigurationModel) => void, baseVariableId: number | undefined) => {
        return (
            <div className="page-title-menu">
                <div className="page-title-label">{label}</div>
                <BaseVariableReadonlyDropdownMenu
                    baseVariables={baseVariables}
                    selectBaseVariable={selectBaseVariable}
                    baseVariableId={baseVariableId ?? -1}
                    filterBases={baseVariableHasBrandId}
                />
            </div>
        )
    }

    const compareButton = () => {
        const onClick = () => {
            const baseVariables = {
                baseVariable1: baseVariable1,
                baseVariable2: selectedComparison === Comparison.MetricVsAverage ? undefined : baseVariable2
            };

            props.updateBaseVariables(baseVariables);
        }

        const validForComparison = baseVariable1;

        return (
            <div className={`page-title-menu ${style.controlButton}`}>
                <button className={`primary-button ${style.button} ${!validForComparison && style.disabled}`} onClick={onClick}>
                    Compare
                </button>
            </div>
        )
    }

    const setHeaderText = (_baseVariable1: VariableConfigurationModel, _baseVariable2: VariableConfigurationModel | undefined) => {
        props.setHeaderText(`${_baseVariable1.displayName} vs ${_baseVariable2?.displayName ?? "Average"} - ${props.focusInstance.name}`);
    }

    React.useEffect(() => {
        if (baseVariable1) {
            setHeaderText(baseVariable1, baseVariable2);
        }
    }, [props.focusInstance.name]);

    React.useEffect(() => {
        if (!baseVariablesLoading) {
            const formattedBaseVariables = baseVariables.map(bv => {
                return new VariableConfigurationModel({
                    ...bv,
                    displayName: StringHelper.formatBaseVariableName(bv.displayName)
                });
            });

            setFormattedBaseVariables(formattedBaseVariables);

            const baseVariable1 =
                formattedBaseVariables.find(bv => bv.id === getQueryParameterInt(QueryStringParamNames.baseVariableId1))
                ?? formattedBaseVariables.find(b => b.identifier == props.defaultBaseVariableIdentifier);
            const baseVariable2 = formattedBaseVariables.find(bv => bv.id === getQueryParameterInt(QueryStringParamNames.baseVariableId2));
            setBaseVariable1(baseVariable1);
            setBaseVariable2(baseVariable2);
            setSelectedComparison(baseVariable2 ? Comparison.MetricVsMetric : Comparison.MetricVsAverage);

            if (baseVariable1) {
                setHeaderText(baseVariable1, baseVariable2);
            }
        }
    }, [baseVariablesLoading,
        getQueryParameterInt(QueryStringParamNames.baseVariableId1),
        getQueryParameterInt(QueryStringParamNames.baseVariableId2)]
    );

    return <div className={style.controlGroup}>
        {comparisonSelector(selectedComparison)}
        {baseDropdown("Brand Metric 1",
            formattedBaseVariables.filter(bv => bv.id !== baseVariable2?.id),
            (variable: VariableConfigurationModel) => { 
                if (variable !== baseVariable1) { 
                    MixPanel.track("metric1Changed"); 
                } 
                setBaseVariable1(variable); 
            },
            baseVariable1?.id)}
        {selectedComparison === Comparison.MetricVsMetric && baseDropdown("Brand Metric 2",
            formattedBaseVariables.filter(bv => bv.id !== baseVariable1?.id),
            (variable: VariableConfigurationModel) => { 
                if (variable !== baseVariable2) { 
                    MixPanel.track("metric2Changed"); 
                } 
                setBaseVariable2(variable); 
            },
            baseVariable2?.id)}
        {compareButton()}
    </div>
}

export default BaseVariableSelector;