import React from "react"
import { useLocation, useNavigate } from 'react-router-dom';
import AccountOptions from "./AccountOptions";
import NavSearch from "./NavSearch";
import SubsetSelector from "./filters/SubsetSelector";
import SideBar from './SideBar';
import { getPathByPageName } from "./helpers/UrlHelper";
import { getActiveViewsForPage } from "./helpers/PagesHelper";
import { PageDescriptor, Subset } from "../BrandVueApi";
import { DataSubsetManager } from "../DataSubsetManager";
import { ApplicationConfiguration } from "../ApplicationConfiguration";
import { PageHandler } from "./PageHandler";
import { IGoogleTagManager } from "../googleTagManager";
import { ProductConfiguration } from "../ProductConfiguration";
import DesktopLogo from "./DesktopLogo";
import { isBarometer, isDevEnvironment, showbrandVueHelpLink } from "./helpers/FeaturesHelper";
import { MetricSet } from "../metrics/metricSet";
import { MixPanel } from "./mixpanel/MixPanel";

const NavDropdown = (props: { pages: PageDescriptor[], pageHandler: PageHandler, closeMenu: () => void, enabledMetricSet: MetricSet }) => {
    const menuContainer = React.useRef<HTMLDivElement>(null);

    const handleClickOutside = (e: MouseEvent) => {
        if (menuContainer !== null
            && menuContainer.current !== null
            && menuContainer.current.contains(e.target as Node)) {
            return;
        }

        e.cancelBubble = true;
        props.closeMenu();
    };

    React.useEffect(() => {
        document.addEventListener("click", handleClickOutside);
        return () => {
            document.removeEventListener("click", handleClickOutside);
        };
    });

    return (
        <div>
            <div className="nav-menu-background"></div>
            <div className="nav-menu-container" ref={menuContainer}>
                <SideBar pages={props.pages} pageHandler={props.pageHandler} toggleMenu={props.closeMenu} enabledMetricSet={props.enabledMetricSet} />
            </div>
        </div>
    );
}

interface ITopNavButtonsProps {
    pages: PageDescriptor[];
    startPageUrl: string;
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    showSubsets: boolean;
    enabledMetricSet: MetricSet;
}

const TopNavButtons = (props: ITopNavButtonsProps) => {

    const [isOpen, setIsOpen] = React.useState(false);

    const openMenu = (e: React.MouseEvent) => {
        MixPanel.track("navigationOpened");
        setIsOpen(true);
        setContentDisabled(true);
        e.stopPropagation();
    }

    const closeMenu = () => {
        MixPanel.track("navigationClosed");
        setIsOpen(false);
        setContentDisabled(false);
    }

    const setContentDisabled = (disable: boolean) => {
        var disableBlock = document.getElementsByClassName("page-content-disabled")[0];
        disableBlock.classList.toggle("show", disable);

        var element = document.documentElement;
        element.classList.toggle("hide-overflow", disable);

        var body = document.body;
        body.classList.toggle("hide-overflow", disable);
    }

    const isSurveyVue = props.productConfiguration.isSurveyVue();

    const showAccountMenu = !isBarometer(props.productConfiguration)
        || isDevEnvironment(props.productConfiguration)
        || props.productConfiguration.user.isSystemAdministrator;

    const onSubsetChange = (subset: Subset) => {
        MixPanel.track("subsetChanged");
        window.location.href = window.location.pathname + "?Subset=" + subset.id;
    }

    const surveyVueClass = isSurveyVue ? 'surveyvue' : '';
    return (
        <div className="sticky-header not-exported">
            <div className={surveyVueClass}>
                <div className={isOpen ? 'desktop-nav open' : 'desktop-nav'}>
                    <div className="nav-container">
                        <div className="flex-container">
                            <DesktopLogo productConfiguration={props.productConfiguration} startPageUrl={props.startPageUrl} />
                            {props.showSubsets && <SubsetSelector selectedSubset={DataSubsetManager.selectedSubset} onChange={onSubsetChange} />}
                        </div>
                        {<NavSearch pages={props.pages} googleTagManager={props.googleTagManager} pageHandler={props.pageHandler} enabledMetricSet={props.enabledMetricSet} />}
                        <div className="flex-container">
                            <div className="button-container">
                                {<div onClick={(e)=>isOpen ? closeMenu : openMenu(e)} className="circular-nav-button" title="Choose a metric">
                                    <div className="circle">
                                        <i className="material-symbols-outlined">menu</i>
                                    </div>
                                    <div className="text">Menu</div>
                                </div>}
                                <div className="circular-nav-button" style={{ display: 'none' }}>
                                    <div className="circle">
                                        <i className="material-symbols-outlined">group_work</i>
                                    </div>
                                    <div className="text">Brands</div>
                                </div>
                                {showbrandVueHelpLink(props.productConfiguration) &&
                                    <a className="circular-nav-button" 
                                        target="_blank" 
                                        href={props.productConfiguration.getHelpLink()} 
                                        title="Help"
                                        onClick={() => MixPanel.track("helpOpened")}
                                    >
                                        <div className="circle">
                                            <i className="material-symbols-outlined">help</i>
                                        </div>
                                        <div className="text">Help</div>
                                    </a>}
                                {showAccountMenu && <AccountOptions productConfiguration={props.productConfiguration} />}
                            </div>
                        </div>
                    </div>
                </div>
                <div className={isOpen ? 'mobile-nav open' : 'mobile-nav'}>
                    <div className="nav-container">
                        {!isSurveyVue && <div className="logo">
                            <div className="sector"></div>
                        </div>}
                        {<NavSearch pages={props.pages} googleTagManager={props.googleTagManager} pageHandler={props.pageHandler} enabledMetricSet={props.enabledMetricSet} />}
                        {<div onClick={isOpen ? closeMenu : openMenu} className="button-container">
                            <div className="circular-nav-button">
                                <div className="circle">
                                    <i className="material-symbols-outlined">menu</i>
                                </div>
                            </div>
                        </div>}
                    </div>
                </div>
            </div>
            <div className={`nav-menu-anchor ${surveyVueClass}`}>
                {isOpen && <NavDropdown pages={props.pages} pageHandler={props.pageHandler} closeMenu={closeMenu} enabledMetricSet={props.enabledMetricSet} />}
            </div>
        </div>
    );
};

interface ITopNavProps {
    startPage: PageDescriptor;
    pages: PageDescriptor[];
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    showSubsets?: boolean;
    enabledMetricSet: MetricSet;
}

const TopNav: React.FunctionComponent<ITopNavProps> = (props: ITopNavProps) => {
    const { startPage, pages, applicationConfiguration, productConfiguration, googleTagManager, pageHandler, showSubsets = true } = props;

    const activeView = getActiveViewsForPage(startPage)[0];
    const startPageUrl = getPathByPageName(startPage.name) + (activeView?.url || "");
    const location = useLocation();
    const navigate = useNavigate();

    React.useEffect(() => {
        if (location.pathname === productConfiguration.appBasePath + "/") {
            navigate(startPageUrl + window.location.search, { replace: true });
        }
    }, [location.pathname, productConfiguration.appBasePath, startPageUrl, navigate]);

    return (
        <TopNavButtons
            pages={pages}
            startPageUrl={startPageUrl}
            applicationConfiguration={applicationConfiguration}
            productConfiguration={productConfiguration}
            googleTagManager={googleTagManager}
            pageHandler={pageHandler}
            showSubsets={showSubsets}
            enabledMetricSet={props.enabledMetricSet}
        />
    );
};

export default TopNav;
