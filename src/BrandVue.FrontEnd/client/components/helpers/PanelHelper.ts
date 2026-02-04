import { DynamicTextHelper } from "./DynamicTextHelper"

export type NavTab = {
    tab: TabSelection,
    name: string,
}

export type Panel = {
    navTab: NavTab,
    content: JSX.Element,
    onSetFeedback?: (feedback: React.ReactNode) => void;
}

export type BrandVueSidePanelContent = {
    contentType: ContentType,
    panels: Panel[],
}

export interface IAboutItemProps {
    userCanEdit: boolean;
    isEditing: boolean;
    dynamicTextHelper: DynamicTextHelper;
}

export interface UiAboutItem {
    displayTitle: string,
    displayContent: string,
    originalTitle: string,
    originalContent: string
}

export enum ContentType {
    None,
    EntitySetSelector,
    AboutInsights,
    PageAbout,
    LlmInsights,
}

export enum TabSelection {
    Brands,
    Audience,
    About,
    Insights,
    LlmInsights
}

export const defaultBrandVueSidePanelContent: BrandVueSidePanelContent = {
    contentType: ContentType.None,
    panels: []
}