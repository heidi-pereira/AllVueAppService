import React from 'react';
import {NavLink, useLocation} from 'react-router-dom';
import { PageDescriptor } from "../BrandVueApi";
import Tooltip from "./Tooltip";
import { getActiveViewUrl, pageListToUrl } from "./helpers/PagesHelper";
import {useEffect, useState} from "react";
import { PageHandler } from './PageHandler';
import { MetricSet } from '../metrics/metricSet';
import { MixPanel } from './mixpanel/MixPanel';
import { Location } from 'react-router-dom';
import { useReadVueQueryParams } from "./helpers/UrlHelper";
interface IActiveContext {
    activePageUrl: string,
    activePageUrlChanged: (u: string) => void,
    toggleMenu: () => void
}

const ActiveContext = React.createContext<IActiveContext>({
    activePageUrl: "",
    activePageUrlChanged: () => { },
    toggleMenu: () => {}
});

const SideBar = (props: { pages: PageDescriptor[], pageHandler: PageHandler, toggleMenu: () => void, enabledMetricSet: MetricSet }) => {
    const [activePageUrl, setActivePageUrl] = useState<string>("");

    const render = () => {
        return (
            <div className="sidebar-menu">
                <ActiveContext.Provider value={{activePageUrl: activePageUrl, activePageUrlChanged: setActivePageUrl, toggleMenu: props.toggleMenu} as IActiveContext}>
                    <LinkContainerComponent pageHandler={props.pageHandler} pages={props.pages} depth={[]} toggleMenu={props.toggleMenu} enabledMetricSet={props.enabledMetricSet} />
                </ActiveContext.Provider>
            </div>
        );
    }
    return render();
}

export default SideBar

const LinkContainerComponent = (props: { pages: PageDescriptor[], pageHandler: PageHandler, depth: PageDescriptor[], toggleMenu: () => void, enabledMetricSet: MetricSet }) => {
    const location = useLocation();
    
    if (props.pages.length === 0) return null;

    const { isActive } = calculateIsActive(props.depth, location);

    return (
        <ActiveContext.Consumer>
            {ctx => (
                <div className={isActive ? 'active' : ''}>
                    <ul>
                        {props.pages.map(p =>
                            <LinkComponent activeContext={ctx} key={p.name} page={p} pageHandler={props.pageHandler} depth={props.depth} enabledMetricSet={props.enabledMetricSet} />
                        )}
                    </ul>
                </div>
            )}
        </ActiveContext.Consumer>
    );
}

const startsWith = (url: string, root: string): boolean => {
    const segments = url.split("/");
    const startsWith = root.split("/").reduce((previous, seg, segIndex) => previous && segments[segIndex] === seg, true);
    return startsWith;
}

const calculateIsActive = (depth: PageDescriptor[], location: Location, page?: PageDescriptor): { isActive: boolean, newDepth: PageDescriptor[], pageUrl: string } => {
    const newDepth = depth.slice();

    if (page) {
        newDepth.push(page);
    }

    const pageUrl = pageListToUrl(newDepth);

    const isActive = startsWith(location.pathname, pageUrl);

    return { isActive, newDepth, pageUrl };
}

interface LinkComponentProps {
    activeContext: IActiveContext,
    page: PageDescriptor,
    pageHandler: PageHandler,
    depth: PageDescriptor[],
    enabledMetricSet: MetricSet
}

const LinkComponent = (props: LinkComponentProps) => {
    const location = useLocation();
    const activeResult = calculateIsActive(props.depth, location, props.page)
    const [isActive, setIsActive] = useState<boolean>(activeResult.isActive);
    const newDepth: PageDescriptor[] = activeResult.newDepth;
    const pageUrl: string = activeResult.pageUrl;
    const isToggler: boolean = props.page.panes.length === 0;
    const readVueQueryParams = useReadVueQueryParams();

    useEffect(() => {
        let active: boolean;
        if (isToggler) {
            active = startsWith(props.activeContext.activePageUrl ||
                location.pathname, pageUrl);
        } else {
            active = startsWith(location.pathname, pageUrl);
        }
        setIsActive(active)
    }, [props.activeContext.activePageUrl, location.pathname, isToggler, pageUrl]);

    const toggle = (e, hideMenu: boolean) => {
        if (hideMenu) {
            props.activeContext.toggleMenu();
        }

        if (isToggler) {
            let toggleUrl = !startsWith(props.activeContext.activePageUrl ||
                location.pathname,
                pageUrl)
                ? pageUrl
                : pageUrl.substring(0, pageUrl.lastIndexOf("/"));

            if (toggleUrl === "") toggleUrl = "/";
            props.activeContext.activePageUrlChanged(toggleUrl);
            e.preventDefault();
        } else {
            props.activeContext.activePageUrlChanged("");
            MixPanel.trackPage(props.page.displayName);
        }
    }

    const render = () => {
        const className = `navItem-${props.page.name.replace(/[^\- a-zA-Z0-9]/g, '').replace(/\s+/g, '_')}${isActive ? ' active' : ''}`;
        let helpText = props.page.pageSubsetConfiguration.find(p => p.subset == props.pageHandler.session.selectedSubsetId)?.helpText ?? props.page;

        return (
            <li className={className}>
                <NavLink
                    to={{
                        pathname: getActiveViewUrl(pageUrl,
                       location),
                        search: props.pageHandler.getPageQuery(pageUrl, window.location.search, props.enabledMetricSet,readVueQueryParams)
                    }}
                    onClick={e => toggle(e, props.page.panes.length !== 0)}
                    className={({ isActive }) => {
                        const baseClass = isToggler ? "nopage" : "";
                        return isActive ? `${baseClass} active` : baseClass;
                    }}>
                    {props.page.roles.length > 0 && (
                        <Tooltip placement="top" title="Only available to authorised users">
                            <span className="material-symbols-outlined">lock</span>
                        </Tooltip>
                    )}
                    <span>{props.page.displayName}</span>
                    {helpText != null && (
                        <Tooltip placement="top" title={helpText as string}>
                            <span><i className="fa fa-info-circle" /></span>
                        </Tooltip>
                    )}
                </NavLink>
                <LinkContainerComponent pages={props.page.childPages} pageHandler={props.pageHandler} depth={newDepth} toggleMenu={props.activeContext.toggleMenu} enabledMetricSet={props.enabledMetricSet} />
            </li>
        );
    }
    return render();
}