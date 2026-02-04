import React from "react";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import BaseOptionDropdownItem from "./BaseOptionDropdownItem";
import { VariableConfigurationModel } from "../../../../BrandVueApi";
import style from "./BaseTypeReadonlyDropdownMenu.module.less";
import SearchInput from "../../../SearchInput";

interface IBaseVariableReadonlyDropdownMenuProps {
    baseVariables: VariableConfigurationModel[];
    selectBaseVariable: (variableId: VariableConfigurationModel) => void;
    baseVariableId: number;
    filterBases?: (baseVariable: VariableConfigurationModel) => boolean;
}

const BaseVariableReadonlyDropdownMenu = (props: IBaseVariableReadonlyDropdownMenuProps) => {
    const [isBaseTypeDropdownOpen, setBaseTypeDropdownOpen] = React.useState<boolean>(false);
    const [selectedBaseName, setSelectedBaseName] = React.useState<string>("None set");
    const [searchText, setSearchText] = React.useState<string>("");
    const [subVariables, setSubVariables] = React.useState<VariableConfigurationModel[] | null>(null);
    const [subPaneInUse, setSubPaneInUse] = React.useState<boolean>(false);

    const updateBaseVariable = (variableId: number) => {
        const selectedBase = props.baseVariables.find(v => v.id === variableId);
        if (selectedBase) {
            setSelectedBaseName(selectedBase.displayName);
            props.selectBaseVariable(selectedBase);
            setSearchText("");
            setSubVariables(null);
        }
    }

    const updateSearch = (searchText: string) => {
        setSubVariables(null);
        setSearchText(searchText);
    }

    const filterBases = (baseVariable: VariableConfigurationModel): boolean => {
        if (props.filterBases) {
            return props.filterBases(baseVariable);
        }
        return true;
    }

    React.useEffect(() => {
        if (props.baseVariableId !== -1) {
            updateBaseVariable(props.baseVariableId);
        }
    }, [props.baseVariableId]);

    React.useEffect(() => {
        setTimeout(() => {
            if (!subPaneInUse) {
                setSubVariables(null);
            }
        }, 100)
}, [subPaneInUse]);

    const getGroupedVariables = (variables: VariableConfigurationModel[]) => {
        const groupedVariables: { [key: string]: VariableConfigurationModel[] } = {};

        variables.forEach(v => {
            const nameParts = v.displayName.split(':');
            const groupName = nameParts[0];
            if (!groupedVariables[groupName]) {
                groupedVariables[groupName] = [];
            }
            groupedVariables[groupName].push(v);
        });

        return groupedVariables;
    }

    const getMatchedBaseVariables = () => {
        let matchedBaseVariables = props.baseVariables;

        if (searchText && searchText.trim() != '') {
            matchedBaseVariables = matchedBaseVariables.filter(bv =>
                bv.displayName.toLowerCase().includes(searchText.toLowerCase()));
        }

        const filteredBaseVariables = matchedBaseVariables.filter(filterBases);
        const groupedVariables = getGroupedVariables(filteredBaseVariables);

        return Object.keys(groupedVariables).map(k => {
            const itemKey = `custom-bases-${groupedVariables[k][0].id}`;
            if (groupedVariables[k].length > 1) {
                return <DropdownItem
                    key={itemKey}
                    className={style.groupedItem}
                    onMouseOver={() => setSubVariables(groupedVariables[k])}
                >
                    <span>{k}</span>
                    <i className="material-symbols-outlined">arrow_right</i>
                </DropdownItem>
            }
            return <BaseOptionDropdownItem
                key={itemKey}
                variableConfig={groupedVariables[k][0]}
                setVariableModalState={() => { }}
                selectBaseVariable={updateBaseVariable}
                isDisabled={false}
                canCreateNewBase={false}
            />
        })
    }

    const getSubVariables = (subVariables: VariableConfigurationModel[] | null) => {
        if (!subVariables || subVariables.length === 0) {
            return null;
        }

        const groupLabel = subVariables[0].displayName.split(':')[0];
        const renamedSubVariables = subVariables.map(sv => new VariableConfigurationModel({
            ...sv,
            displayName: sv.displayName.split(':')[1]?.trim() ?? groupLabel.trim()
        }
        ));

        return (
            <div className={`${style.subPane}`}
                onMouseOver={() => setSubPaneInUse(true)}
                onMouseLeave={() => setSubPaneInUse(false)}>
                <DropdownItem header>{groupLabel}</DropdownItem>
                <div className={style.itemList}>
                    {renamedSubVariables.map(sv =>
                        <BaseOptionDropdownItem
                            key={`custom-bases-sub-${sv.id}`}
                            variableConfig={sv}
                            setVariableModalState={() => { }}
                            selectBaseVariable={updateBaseVariable}
                            isDisabled={false}
                            canCreateNewBase={false}
                        />)
                    }
                </div>
            </div>
        )
    }

    const toggleDropdown = (toggleValue: boolean) => {
        setSubVariables(null);
        setBaseTypeDropdownOpen(toggleValue);
    }

    return (
        <ButtonDropdown
            isOpen={isBaseTypeDropdownOpen}
            toggle={() => toggleDropdown(!isBaseTypeDropdownOpen)}
            className={`configure-option-dropdown base-type-dropdown ${style.dropdown}`}
        >
            <DropdownToggle className={`toggle-button ${style.toggle}`}>
                <span>{selectedBaseName}</span>
                <i className="material-symbols-outlined">arrow_drop_down</i>
            </DropdownToggle>
            <DropdownMenu className={`${style.dropdownMenu} ${subVariables ? style.expand : ""}`}>
                <div className={style.menuPanes}>
                    <div className={style.mainPane}>
                        <SearchInput id="metric-search-input" className={style.search} onChange={(text) => updateSearch(text)} autoFocus={true} text={searchText} />
                        {getMatchedBaseVariables()}
                    </div>
                    {getSubVariables(subVariables)}
                </div>
            </DropdownMenu>
        </ButtonDropdown>
    )
}

export default BaseVariableReadonlyDropdownMenu;