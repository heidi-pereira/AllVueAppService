import React from "react";
import { AverageTotalRequestModel, PageDescriptor, IEntityType } from "../../../BrandVueApi";
import { viewBase } from "../../../core/viewBase";
import { IEntityConfiguration } from "../../../entity/EntityConfiguration";
import { EntityInstance } from "../../../entity/EntityInstance";
import { EntitySet } from "../../../entity/EntitySet";
import { IGoogleTagManager } from "../../../googleTagManager";
import DropdownSelector from "../../dropdown/DropdownSelector";
import EntitySetDropdownSelector from "../../EntitySetDropdownSelector";
import { PageHandler } from "../../PageHandler";
import { matchesSearch } from "../../FilterInstanceDropdownSelector";
import { MetricSet } from "../../../metrics/metricSet";
import { filterSet } from "../../../filter/filterSet";
import Comparison from "../MetricComparison/Comparison";
import { ContentType } from "../../helpers/PanelHelper";
import { userCanEditAbouts, pageHasPageAbouts, aboutLink } from "../../helpers/AboutHelper";
import { ProductConfiguration } from "../../../ProductConfiguration";
import style from "./BrandAnalysis.module.less";
import {useDispatch} from "react-redux";
import {setActiveEntitySet} from "../../../state/entitySelectionSlice";
import EntitySetBuilder from "../../../entity/EntitySetBuilder";
import { EntityInstanceColourRepository } from "../../../entity/EntityInstanceColourRepository";
import { useActiveEntitySet } from "client/state/entitySelectionHooks";

export interface BrandAnalysisSelectorOptions {
    selectedSubsetId: string;
    coreViewType: number;
    googleTagManager: IGoogleTagManager;
    focusInstance: EntityInstance;
    availableEntityInstances: EntityInstance[];
    pageHandler: PageHandler;
    activeDashPage: PageDescriptor;
    averageRequests: AverageTotalRequestModel[] | null;
    filters: filterSet;
    comparisons: Comparison[];
    metrics: MetricSet;
    viewBase: viewBase;
    availableEntitySets: EntitySet[];
    entityConfiguration: IEntityConfiguration;
    splitByEntityType: IEntityType;
    toggleSidePanel(contentType: ContentType): void;
    productConfiguration: ProductConfiguration;
}

const BrandAnalysisSelector: React.FC<BrandAnalysisSelectorOptions> = (props: BrandAnalysisSelectorOptions) => {
    const activeEntitySet = useActiveEntitySet();
    const [hasPageAbouts, setHasPageAbouts] = React.useState<boolean>(false);
    const dispatch = useDispatch();
    

    React.useEffect(() => {
        pageHasPageAbouts(props.activeDashPage).then(setHasPageAbouts);
    }, [props.activeDashPage]);

    const sortedAvailableEntityInstances = React.useMemo(
        () => props.availableEntityInstances.toSorted((a, b) => a.name.localeCompare(b.name)),
        [props.availableEntityInstances]
    );

    const isEntityInSet = (entitySet: EntitySet, instance:EntityInstance): boolean => {
        return entitySet.getInstances().getAll().some(i=> i == instance)
    }
    const setActiveEntitySetPreserveFocus = (entitySet: EntitySet) => {
        if(isEntityInSet(entitySet, props.focusInstance)) {
            dispatch(setActiveEntitySet({ entitySet }));
        } else {
            //changed to entityset, but by default can't maintain current focus
            const entitySetBuilder = new EntitySetBuilder(EntityInstanceColourRepository.empty());
            const newSet = entitySetBuilder.fromEntitySet(entitySet).withMainInstance(props.focusInstance);
            dispatch(setActiveEntitySet({ entitySet: newSet.build() }));
        }
    }
    
    //We need to be able to select brands outside of the entity set
    const setActiveBrand = (e: EntityInstance) => {
        //to preserve entityset integrity elsewhere, we can't set the focus brand. Instead append focus brand to the other parameter
        const entitySetBuilder = new EntitySetBuilder(EntityInstanceColourRepository.empty());
        if(!isEntityInSet(activeEntitySet, e)) {
            const newSet = entitySetBuilder.fromEntitySet(activeEntitySet).withBothInstances([...activeEntitySet.getInstances().getAll(), e]).withMainInstance(e).build();
            dispatch(setActiveEntitySet({ entitySet: newSet }));
        } else {
            const newSet = entitySetBuilder.fromEntitySet(activeEntitySet).withMainInstance(e).build();
            dispatch(setActiveEntitySet({ entitySet: newSet }));
        }
    }

    const isValidForPageAbouts = () => {
        return userCanEditAbouts(props.productConfiguration) || hasPageAbouts;
    }
    return (
        <div className="brand-analysis-controls">
            <div className={`selectors ${style.selectors}`}>
                <DropdownSelector<EntityInstance>
                    label="Brand"
                    items={sortedAvailableEntityInstances}
                    selectedItem={props.focusInstance}
                    onSelected={setActiveBrand}
                    itemKey={instance => instance.name}
                    itemDisplayText={selected => selected.name}
                    asButton={false}
                    showLabel={true}
                    filterPredicate={matchesSearch}
                />
                <EntitySetDropdownSelector
                    label="Competitor Set"
                    googleTagManager={props.googleTagManager}
                    activeEntitySet={activeEntitySet}
                    focusInstance={props.focusInstance}
                    updateFocusInstance={setActiveBrand}
                    updateActiveEntitySet={setActiveEntitySetPreserveFocus}
                    availableEntitySets={props.availableEntitySets}
                    availableInstances={props.entityConfiguration.getAllEnabledInstancesForType(props.splitByEntityType)}
                    entityType={props.splitByEntityType!}
                    asButton={false}
                    showLabel={true}
                    preserveFocus={true}
                    dropdownClassName=""
                    pageHandler={props.pageHandler}
                />
                {isValidForPageAbouts() && aboutLink("page", () => props.toggleSidePanel(ContentType.PageAbout))}
            </div>
        </div>
    );
};

export default BrandAnalysisSelector;