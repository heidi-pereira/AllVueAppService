import React from "react";
import {useState} from "react";
import {
    CrossMeasure, CrossMeasureFilterInstance} from "../../../BrandVueApi";
import {v4 as uuidv4} from "uuid";
import {Metric} from "../../../metrics/metric";
import {CSSTransition} from "react-transition-group";
import toast from "react-hot-toast";
import { IGoogleTagManager } from "../../../googleTagManager";
import {PageHandler} from "../../PageHandler";
import { flattenTree, getParent } from "@atlaskit/tree/dist/cjs/utils/tree";
import Tree, {
    ItemId,
    moveItemOnTree,
    mutateTree,
    RenderItemParams,
    TreeDestinationPosition,
    TreeSourcePosition
} from "@atlaskit/tree";
import CrossMeasureInstanceSelector from "../Reports/Components/CrossMeasureInstanceSelector";
import { MixPanel } from "../../mixpanel/MixPanel";
import { BreakPickerParent } from "./BreaksDropdownHelper";
import { TreeData, TreeItem, TreeItemData } from "@atlaskit/tree/dist/types/types";

interface IMultipleBreaksPickerProps {
    metrics: Metric[];
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    categories: CrossMeasure[];
    onCategoriesChange: (categories: CrossMeasure[]) => void;
    isDisabled: boolean;
    disableNesting?: boolean;
    displayBreakInstanceSelector: boolean;
    parent: BreakPickerParent;
}

interface CustomTreeItemData extends TreeItemData {
    categoryName: string;
    displayName: string;
    isEntitySelectorVisible: boolean;
    filterInstances: CrossMeasureFilterInstance[];
}

interface CustomTreeItem extends Omit<TreeItem, 'data'> {
    data?: CustomTreeItemData;
}

interface CustomTreeData extends TreeData {
    rootId: ItemId;
    items: Record<ItemId, CustomTreeItem>;
}

const emptyTree: CustomTreeData = {
    rootId: "root",
    items: {
        "root": {
            id: "root",
            hasChildren: false,
            isExpanded: true,
            children: []
        }
    }
};

function getNestedCrossMeasureString(categories: CrossMeasure[]): string {
    // this is used to only rebuild the tree when the layout of categories change (e.g. reorder, add/remove)
    // when adding filter instances, we manually update the tree and dont want to rebuild it as it would lose the open/close state of instance picker
    // alternatively we could find some way to map between prev/new better e.g. deterministic IDs
    return '[' + categories.map(c => {
        return `${c.measureName}:${getNestedCrossMeasureString(c.childMeasures)}`;
    }).join(',') + ']';
}

const MultipleBreaksPicker = (props: IMultipleBreaksPickerProps) => {
    const [isAddingColumn, setIsAddingColumn] = useState(false);
    const [tree, setTree] = useState<CustomTreeData>(emptyTree);
    const metricDictionary = new Map(props.metrics.map(metric => [metric.name, metric]));

    React.useEffect(() => {
        const currentCategories = updateCategories(tree);
        if (JSON.stringify(currentCategories) !== JSON.stringify(props.categories)) {
            setTree(populateTreeFromCategories(emptyTree, emptyTree.rootId, props.categories));
        }
    }, [getNestedCrossMeasureString(props.categories)]);

    const addMeasureToTree = (tree: TreeData, newId: string, parentId: ItemId, crossMeasure: CrossMeasure): TreeData => {
        const data: CustomTreeItemData = {
            categoryName: crossMeasure.measureName,
            displayName: metricDictionary.get(crossMeasure.measureName)?.displayName ?? crossMeasure.measureName,
            isEntitySelectorVisible: false,
            filterInstances: crossMeasure.filterInstances
        };

        tree.items[newId] = {
            id: newId,
            hasChildren: false,
            isExpanded: true,
            children: [],
            data: data
        };

        const mutation = {
            id: parentId,
            hasChildren: true,
            children: [...tree.items[parentId].children, newId]
        };

        return mutateTree(tree, parentId, mutation);
    };

    function updateTreeItem(item: CustomTreeItem, updater: (data: CustomTreeItemData) => CustomTreeItemData) {
        const newTree = { ...tree };
        newTree.items = { ...tree.items };
        newTree.items[item.id] = {
            ...item,
            data: updater(item.data!)
        };
        setTree(newTree);
        return newTree;
    }

    const toggleItemExpansion = (item: CustomTreeItem) => {
        const isToggled = item.data?.isEntitySelectorVisible ?? false;
        updateTreeItem(item, (data: CustomTreeItemData) => ({
            ...data,
            isEntitySelectorVisible: !isToggled
        }));
    };

    const updateItemFilterInstances = (item: CustomTreeItem, filterInstances: CrossMeasureFilterInstance[]) => {
        const newTree = updateTreeItem(item, (data: CustomTreeItemData) => ({
            ...data,
            filterInstances: filterInstances
        }));
        updateCategoriesFromTree(newTree);
    };

    const populateTreeFromCategories = (treeData: CustomTreeData, parentId: string | number, categories: CrossMeasure[]): CustomTreeData => {
        let tempTree = treeData;

        categories.forEach(c => {
            const newId = uuidv4();
            const newTree = addMeasureToTree(tempTree, newId, parentId, c);
            tempTree = populateTreeFromCategories(newTree, newId, c.childMeasures);
        });

        return tempTree;
    };

    const updateCategoriesFromTree = (updatedTree: CustomTreeData) => {
        const categories = updateCategories(updatedTree);
        props.onCategoriesChange(categories);
    }

    const updateCategories = (treeData: CustomTreeData, itemId?: ItemId): CrossMeasure[] => {
        const id = itemId ?? treeData.rootId;
        return treeData.items[id].children.map(c => {
            const child = treeData.items[c];
            return new CrossMeasure({
                measureName: child.data!.categoryName,
                childMeasures: updateCategories(treeData, c),
                filterInstances: child.data!.filterInstances,
                multipleChoiceByValue: false,
            });
        });
    };

    const removeCategory = (item: TreeItem) => {
        props.googleTagManager.addEvent("removeCrosstabBreak", props.pageHandler, { value: item?.data?.categoryName });
        MixPanel.track("removedCrosstabBreak");

        const itemPath = flattenTree(tree).find(i => i.item.id === item.id)?.path;
        if (itemPath) {
            const parent = getParent(tree, itemPath);
            const children = parent.children.filter(c => c !== item.id);
            const updatedTree =  mutateTree(tree, parent.id, {children: children, hasChildren: children.length > 0});
            updateCategoriesFromTree(updatedTree);
        }
    };

    const renderChildren = (item: CustomTreeItem) => {
        const children = item.children.map(c => tree.items[c]);
        if (children.length === 0) return;
        return (
            <>
                {children.map(c => {
                    return (
                        <div className={"nested"} key={c.id}>
                            <div className="category-item-dragging">
                                <div className="category-name">{c.data!.displayName}</div>
                            </div>
                            {renderChildren(c)}
                        </div>
                    );
                })}
            </>
        );
    };

    const getCrossMeasureInstanceSelector = (item: CustomTreeItem) => {
        const crossMeasure = new CrossMeasure({
            measureName: item.data!.categoryName,
            filterInstances: item.data!.filterInstances,
            multipleChoiceByValue: false,
            childMeasures: [],
        });
        const crossMeasureMetric = props.metrics.find(m => m.name == item.data!.categoryName);

        return <CrossMeasureInstanceSelector
            selectedCrossMeasure={crossMeasure!}
            selectedMetric={crossMeasureMetric!}
            activeEntityType={undefined}
            setCrossMeasures={(m) => updateItemFilterInstances(item, m[0].filterInstances)}
            disabled={false}
            includeSelectAll={false}
        />
    }

    const renderItem = (params: RenderItemParams) => {
        const treeItem: CustomTreeItem = params.item;
        if (params.snapshot.isDragging) {
            return (
                <div ref={params.provided.innerRef} {...params.provided.draggableProps} className="draggable-category">
                    <div className="category-item-dragging">
                        <div {...params.provided.dragHandleProps} className="drag-handle"><i
                            className="material-symbols-outlined">drag_indicator</i></div>
                        <div className="category-name">{treeItem.data!.displayName}</div>
                    </div>
                    {renderChildren(treeItem)}
                </div>
            );
        }

        const entitySelectorVisible = (treeItem?.data?.isEntitySelectorVisible ?? false) && props.displayBreakInstanceSelector;
        return (
            <CSSTransition in={true} appear={isAddingColumn} classNames="draggable-category" timeout={300}>
                <div ref={params.provided.innerRef} {...params.provided.draggableProps} className="draggable-category">
                    <div className="category-wrapper">
                        <div className={"category-item" + (props.isDisabled ? " disabled" : "")}>
                            <div className="title-handle">
                                <div className="title-box" onClick={() => {toggleItemExpansion(treeItem)}}>
                                    <div className="left">
                                        <div {...params.provided.dragHandleProps} className={`drag-handle ${entitySelectorVisible ? "no-drag" : ""}`}>
                                            <i className="material-symbols-outlined">drag_indicator</i>
                                        </div>
                                        <div className="category-name" title={treeItem.data!.displayName}>{treeItem.data!.displayName}</div>
                                    </div>
                                    {entitySelectorVisible &&
                                        <div className="right">
                                            <i className="material-symbols-outlined">{treeItem.data!.isEntitySelectorVisible ? "arrow_drop_up" : "arrow_drop_down"}</i>
                                        </div>
                                    }
                                </div>
                                {!entitySelectorVisible && <div onClick={() => removeCategory(treeItem)} className="remove-category-multi"><i className="material-symbols-outlined">close</i></div>}
                            </div>
                            <div className="instance-selector">
                                {entitySelectorVisible && getCrossMeasureInstanceSelector(treeItem)}
                            </div>
                        </div>
                    </div>
                    <CSSTransition in={params.snapshot.combineTargetFor && treeItem.children.length === 0} timeout={{enter:600, exit:0}} classNames="dummy-column-item" unmountOnExit={true}>
                        <div className="dummy-column-item"></div>
                    </CSSTransition>
                </div>
            </CSSTransition>
        );
    };

    const onDragEnd = (sourcePosition: TreeSourcePosition, destinationPosition?: TreeDestinationPosition) => {
        if (!destinationPosition) {
            return;
        }

        const sourceNode = tree.items[sourcePosition.parentId];
        const destNode = tree.items[destinationPosition.parentId];
        const sourceItem = tree.items[sourceNode.children[sourcePosition.index]];

        const measureName = sourceItem.data!.categoryName;
        const measuresAtDest = destNode.children.map(id => tree.items[id])
            .map(child => child.data!.categoryName);

        if ((sourcePosition.parentId === destinationPosition.parentId) || !measuresAtDest.includes(measureName)) {
            MixPanel.track("nestedCrosstabBreak");
            const moved = moveItemOnTree(tree, sourcePosition, destinationPosition);
            const updatedTree = mutateTree(moved, destinationPosition.parentId, {isExpanded: true});
            setTree(updatedTree);
            updateCategoriesFromTree(updatedTree);
        } else {
            toast.error("Duplicate break is already added here");
        }
    };

    const hasColumns = tree.items["root"].children.length > 0;

    const clearBreaks = () => {
        updateCategoriesFromTree(emptyTree);
    }

    return (
        <div className="category-config" data-testid="multi-breaks-container">
            <div className={"breaks-container" + (props.isDisabled ? " disabled" : "") + " full-height"}>
                <button className={"remove-breaks secondary-button " + (hasColumns && !props.isDisabled ? "" : "hidden")} onClick={() => clearBreaks()} aria-label="Clear all selected breaks">Clear all</button>
                <div>
                    <div className={`selected-categories ${props.parent} ${hasColumns ? "" : "empty"} full-height ${props.isDisabled ? "disabled" : ""}`}>
                        <Tree tree={tree}
                            renderItem={renderItem}
                            onDragEnd={onDragEnd}
                            onDragStart={() => setIsAddingColumn(false)}
                            offsetPerLevel={27}
                            isDragEnabled
                            isNestingEnabled={!props.disableNesting} />
                    </div>
                    <div className="breaks-info">
                        <i className="material-symbols-outlined">subdirectory_arrow_right</i>
                        <div>{props.disableNesting ? "Drag breaks to reorder" : "Drag breaks to reorder or interlock them"}</div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default MultipleBreaksPicker;