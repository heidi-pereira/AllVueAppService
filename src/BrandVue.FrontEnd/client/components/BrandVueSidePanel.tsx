import React from "react";
import { TabContent, TabPane, Nav, NavItem, NavLink } from 'reactstrap';
import { useAppSelector } from "../state/store";
import { BrandVueSidePanelContent, Panel, NavTab, TabSelection } from './helpers/PanelHelper';
import { UsefulnessFeedbackSection } from "./UsefulnessFeedbackSection";

interface IBrandVueSidePanelProps {
    isOpen: boolean;
    close(): void;
    panelContent: BrandVueSidePanelContent;
}

const BrandVueSidePanel = (props: IBrandVueSidePanelProps) => {
    const [activeTab, setActiveTab] = React.useState<TabSelection>(props.panelContent.panels[0]?.navTab?.tab);

    const isLoadingLlmInsights = useAppSelector((state) => state.llmInsights.loading);
    React.useEffect(() => {
        if (props.isOpen) {
            setActiveTab(props.panelContent.panels[0]?.navTab?.tab);
        }
    }, [props.isOpen, props.panelContent.contentType]);

    const navItem = (navTab: NavTab) => {
        return (
            <NavItem key={`nav-item-${navTab.name}`}>
                <NavLink className={activeTab === navTab.tab ? 'tab-active' : 'tab-item'} onClick={() => setActiveTab(navTab.tab)}>
                    {navTab.name === 'AI Summary' ? <div className={activeTab === navTab.tab ? 'sparkle-icon' : 'sparkle-icon-inactive'}/> : null}
                    <div className='inline-text'>{navTab.name}</div>
                </NavLink>
            </NavItem>
        );
    }

    const getPane = (panel: Panel) => {
        return (
            <TabPane key={`nav-item-${panel.navTab.name}`} tabId={panel.navTab.tab}>
                {panel.content}
            </TabPane>
            );
    }

    const currentTab = props.panelContent.panels.find(p => p.navTab.tab === activeTab)?.navTab.name;

    return (
        <div className={`side-panel ${props.isOpen ? "visible" : ""}`}>
            <Nav tabs>
                {props.panelContent.panels.map(p => navItem(p.navTab))}
                <div className="close-side-panel-button" onClick={props.close}>
                    <i className="material-symbols-outlined">close</i>
                </div>
            </Nav>
            <TabContent activeTab={activeTab}>
                {props.panelContent.panels.map(getPane)}
            </TabContent>
            {currentTab === 'AI Summary' && !isLoadingLlmInsights && <UsefulnessFeedbackSection />}
        </div>
    );
}

export default BrandVueSidePanel;