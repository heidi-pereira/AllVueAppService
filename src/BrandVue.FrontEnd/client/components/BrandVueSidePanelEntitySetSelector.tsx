import React, { ReactElement } from "react";
import { IEntityType } from "../BrandVueApi";
import { EntityInstance } from "../entity/EntityInstance";
import { EntitySet } from "../entity/EntitySet";
import { IGoogleTagManager } from "../googleTagManager";
import { ProductConfiguration } from "../ProductConfiguration";
import AudienceSelector from "./AudienceSelector";
import EntitySetSelector from "./EntitySetSelector";
import { BrandVueSidePanelContent, ContentType, Panel, NavTab, TabSelection } from './helpers/PanelHelper';
import { PageHandler } from "./PageHandler";
import { IActiveBreaks } from "client/state/entitySelectionSlice";

export interface IBrandVueSidePanelEntitySetSelectorProps {
    isOpen: boolean;
    close(): void;
    entityType: IEntityType;
    entitySets: EntitySet[];
    availableInstances: EntityInstance[];
    isBarometer: boolean;
    isColourConfigEnabled: boolean;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    productConfiguration: ProductConfiguration;
}

const BrandVueSidePanelEntitySetSelector = (props: IBrandVueSidePanelEntitySetSelectorProps, activeBreaks: IActiveBreaks): BrandVueSidePanelContent => {
    const getEntitySetSelectorPanel = (): Panel => {
        
        const navTab: NavTab = { tab: TabSelection.Brands, name: props.entityType.displayNamePlural };

        const content: ReactElement = <EntitySetSelector
                visible={props.isOpen}
                closeSelector={props.close}
                entityType={props.entityType}
                entitySets={props.entitySets}
                availableInstances={props.availableInstances}
                isBarometer={props.isBarometer}
                isColourConfigEnabled={props.isColourConfigEnabled}
                productConfiguration={props.productConfiguration}
                />;

        return {navTab: navTab, content: content };
    }

    const getAudiencesPanel = (): Panel => {
        const navTab: NavTab = { tab: TabSelection.Audience, name: TabSelection[TabSelection.Audience] };

        const content: JSX.Element = <AudienceSelector
            activeBreaks={activeBreaks}
            activeEntityType={props.entityType}
            googleTagManager={props.googleTagManager}
            pageHandler={props.pageHandler}
        />;

        return { navTab: navTab, content: content };
    }

    const getPanels = (): Panel[] => {
        return [getEntitySetSelectorPanel(), getAudiencesPanel()];
    }

    return {
        contentType: ContentType.EntitySetSelector,
        panels: getPanels()
    };
}

export default BrandVueSidePanelEntitySetSelector;