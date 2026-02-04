import React from "react";
import {
    BaseDefinitionType, BaseFieldExpressionVariableDefinition, ReportVariableAppendType, IEntityType, VariableConfigurationModel,
} from "../../../../BrandVueApi";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { baseTypeDisplayName } from "../../../../components/helpers/SurveyVueUtils";
import { Metric } from "../../../../metrics/metric";
import VariableContentModal from "../../Variables/VariableModal/VariableContentModal";
import BaseOptionDropdownItem from "./BaseOptionDropdownItem";
import {useContext, useState} from "react";
import { BaseVariableContext } from "../../Variables/BaseVariableContext";
import SearchInput from "../../../SearchInput";
import style from "./BaseTypeDropdownMenu.module.less";
import { ProductConfiguration } from "../../../../ProductConfiguration";
import { Tooltip } from "@mui/material";
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from "client/state/subsetSlice";

interface IBaseTypeDropdownMenuProps {
    metric: Metric | undefined;
    baseType: BaseDefinitionType | undefined;
    baseVariableId: number | undefined;
    selectDefaultBase(): void;
    setBaseProperties(baseType: BaseDefinitionType | undefined, baseVariableId: number | undefined): void;
    defaultBaseType?: BaseDefinitionType;
    defaultBaseVariableId?: number;
    canCreateNewBase: boolean | undefined;
    selectedPart?: string;
    updateLocalMetricBase(variableId: number): void;
    productConfiguration?: ProductConfiguration;
    showPadlock?: boolean;
}

export interface IVariableModalState {
    isVariableModalOpen: boolean;
    baseVariableIdToEdit: number | undefined;
    baseVariableIdToCopy: number | undefined;
}
const Padlock = (props: {title?: string}) => <i className={`material-symbols-outlined ${style.materialSymbolsOutlined}`} title={props.title}>lock</i>;

const BaseTypeDropdownMenu = (props: IBaseTypeDropdownMenuProps) => {
    const [isBaseTypeDropdownOpen, setBaseTypeDropdownOpen] = React.useState<boolean>(false);
    const [variableModalState, setVariableModalState] = useState<IVariableModalState>({isVariableModalOpen: false, baseVariableIdToEdit: undefined, baseVariableIdToCopy: undefined})
    const [searchText, setSearchText] = useState<string>("");
    const { baseVariables } = useContext(BaseVariableContext);
    const subsetId = useAppSelector(selectSubsetId);

    const hasCustomBase = props.metric?.hasCustomBase;
    const metricEntityCombination = props.metric?.entityCombination ?? [];
    const baseTypeOptions = [BaseDefinitionType.SawThisChoice, BaseDefinitionType.SawThisQuestion, BaseDefinitionType.AllRespondents];
    const loweredSearchText = searchText.trim().toLowerCase();

    const toggleDropdown = () => {
        setSearchText("");
        setBaseTypeDropdownOpen(!isBaseTypeDropdownOpen);
    }

    const getDefaultDisplayName = () => {
        if (props.defaultBaseVariableId) {
            const baseName = baseVariables.find(v => v.id === props.defaultBaseVariableId)?.displayName ?? "Custom base";
            return `Default (${baseName})`;
        }
        return `Default (${baseTypeDisplayName(props.defaultBaseType)})`;
    }

    const getSelectedDisplayName = () => {
        if (hasCustomBase) {
            const metricBaseDescription = props.metric?.baseDescription?.trim() ?? "";
            return metricBaseDescription.length > 0 ? metricBaseDescription : "Custom base";
        }
        if (props.baseVariableId) {
            return baseVariables.find(v => v.id === props.baseVariableId)?.displayName ?? "Custom base";
        }
        if (props.baseType) {
            return baseTypeDisplayName(props.baseType);
        }
        if (props.defaultBaseType || props.defaultBaseVariableId) {
            return getDefaultDisplayName();
        }
        return "Not set";
    }

    const createNewBaseHandler = () => {
        setVariableModalState(
            {
                isVariableModalOpen: true,
                baseVariableIdToEdit: undefined,
                baseVariableIdToCopy: undefined
            })
    }

    const setIsModalOpen = (isOpen: boolean) => {
        setVariableModalState({...variableModalState, isVariableModalOpen: isOpen})
    }

    const getCreateNewBaseButton = () => {
        if (props.canCreateNewBase) {
            return(
                <div className="create-base-button-container">
                    <button className="hollow-button create-base-button" onClick={createNewBaseHandler}>
                        <i className="material-symbols-outlined">add</i>
                        <div className="new-variable-button-text">Create new base</div>
                    </button>
                </div>
            );
        }
    }

    const isBaseOptionEnabled = (baseOption: BaseDefinitionType | undefined) => {
        if (baseOption == BaseDefinitionType.SawThisChoice) {
            if (props.metric && props.metric.primaryFieldDependencies?.length > 1) {
                return false;
            }
        }
        return true;
    }

    const selectBaseType = (baseType: BaseDefinitionType | undefined) => {
        if (isBaseOptionEnabled(baseType) && (props.baseVariableId || baseType !== props.baseType)) {
            props.setBaseProperties(baseType, undefined);
        }
    }

    const selectBaseVariable = (baseVariableId: number) => {
        if (baseVariableId && baseVariableId === props.defaultBaseVariableId) {
            props.selectDefaultBase();
        } else if (baseVariableId !== props.baseVariableId) {
            props.setBaseProperties(BaseDefinitionType.SawThisQuestion, baseVariableId);
        }
    }

    const matchesSearchText = (baseName: string) => {
        return loweredSearchText.length == 0 || baseName.toLowerCase().includes(loweredSearchText);
    }

    const matchesSearch = (v: VariableConfigurationModel) => {
        return matchesSearchText(v.displayName) || matchesSearchText(v.identifier);
    }

    const createBaseDropdownItem = (v: VariableConfigurationModel, displayNameOverride?: string): React.JSX.Element => {
        return <BaseOptionDropdownItem
            key={`custom-bases-${v.id}`}
            variableConfig={v}
            displayNameOverride={displayNameOverride}
            setVariableModalState={setVariableModalState}
            selectBaseVariable={selectBaseVariable}
            isDisabled={!canBaseVariableBeSelected(metricEntityCombination, v)}
            canCreateNewBase={props.canCreateNewBase} />;
    }

    const getBaseDropdownItems = (): JSX.Element[] => {
        const defaultBaseItem: JSX.Element[] = [];

        if (props.defaultBaseVariableId != undefined) {
            const v = baseVariables.find(v => v.id === props.defaultBaseVariableId)!;
            if (matchesSearch(v)) {
                defaultBaseItem.push(
                    createBaseDropdownItem(v, getDefaultDisplayName())
                );
            }
        } else if (props.defaultBaseType && matchesSearchText(getDefaultDisplayName())) {
            defaultBaseItem.push(
                <DropdownItem key="default-base" onClick={() => props.selectDefaultBase()}>
                    <div className="base-type-name">{getDefaultDisplayName()}</div>
                </DropdownItem>
            );
        }
        const outputItem = (type: BaseDefinitionType, displayPadlock: boolean|undefined) => {
            return (
                <div className={style.baseTypeName} >{displayPadlock && <Padlock />}
                    {baseTypeDisplayName(type)}
                </div>
            );
        }

        const getBaseTypeItems = (baseTypeOptions: BaseDefinitionType[], applyAdminOnlyFormatting?: boolean) => {
            const adminHeaderItem = <DropdownItem key="default-base-header" className={style.adminOption} disabled divider={false}>Savanta Admin Only</DropdownItem>;
            const baseItems = baseTypeOptions.filter(b => matchesSearchText(baseTypeDisplayName(b)))
                .map((type, i) => {
                    const classNames : string[] = [];

                    if (!isBaseOptionEnabled(type)) {
                        classNames.push(style.baseTypeNotSupported)
                    } else if ((applyAdminOnlyFormatting)) {
                        classNames.push(style.adminOption)
                    }

                    return <DropdownItem
                        key={`base-type-${i}`}
                        onClick={() => selectBaseType(type)}
                        className={classNames.join(" ")}>
                            {!isBaseOptionEnabled(type) &&
                                <Tooltip title="This question doesn't support this base type">
                                    {outputItem(type, applyAdminOnlyFormatting)}
                                </Tooltip>
                            }
                            {isBaseOptionEnabled(type) && outputItem(type, applyAdminOnlyFormatting)}
                    </DropdownItem>;
                });

            return applyAdminOnlyFormatting ? [adminHeaderItem, ...baseItems] : baseItems;
        }

        const applyAdminOnlyFormatting = props.productConfiguration ? !props.productConfiguration.isSurveyVue() : false;
        const baseTypeItems = getBaseTypeItems(baseTypeOptions, applyAdminOnlyFormatting);

        const filteredBaseVariables = baseVariables.filter(v => matchesSearch(v) && v.id !== props.defaultBaseVariableId)
            .map(v => createBaseDropdownItem(v));

        const nonEmptySections = [defaultBaseItem, baseTypeItems, filteredBaseVariables].filter(section => section.length > 0);
        nonEmptySections.forEach((section, index) => {
            if (index < nonEmptySections.length - 1) {
                section.push(<DropdownItem divider key={`divider-${index}`} />);
            }
        });
        return nonEmptySections.flat();
    }

    return (
        <>
            <ButtonDropdown
                isOpen={isBaseTypeDropdownOpen}
                toggle={toggleDropdown}
                className={`configure-option-dropdown base-type-dropdown ${style.baseTypeDropdown}`}
            >
                <DropdownToggle className="toggle-button" disabled={hasCustomBase}>
                    <span>{getSelectedDisplayName()}</span>
                    <i className="material-symbols-outlined">arrow_drop_down</i>
                </DropdownToggle>
                <DropdownMenu className={`${style.dropdownMenu}`}>
                    <SearchInput id="base-type-search-input" className="flat-search" onChange={(text) => setSearchText(text)} autoFocus={true} text={searchText} />
                    {getBaseDropdownItems()}
                    {getCreateNewBaseButton()}
                </DropdownMenu>
            {props.showPadlock && <Padlock title="⚠️ This control does not correctly override base in all cases"/>}
            </ButtonDropdown>
            <VariableContentModal isOpen={variableModalState.isVariableModalOpen}
                subsetId={subsetId}
                setIsOpen={setIsModalOpen}
                isBase={true}
                selectedPart={props.selectedPart}
                variableIdToView={variableModalState.baseVariableIdToEdit}
                variableIdToCopy={variableModalState.baseVariableIdToCopy}
                updateLocalMetricBase={(variableId: number) => props.updateLocalMetricBase(variableId)}
                reportAppendType={ReportVariableAppendType.Base}
            />
        </>
    )
}

function canBaseVariableBeSelected(metricEntityCombination: IEntityType[], baseVariable: VariableConfigurationModel) {
    if (baseVariable.definition instanceof BaseFieldExpressionVariableDefinition) {
        return baseVariable.definition.resultEntityTypeNames.every(typeName => metricEntityCombination.some(t => t.identifier === typeName));
    }
    return true;
}

export default BaseTypeDropdownMenu;