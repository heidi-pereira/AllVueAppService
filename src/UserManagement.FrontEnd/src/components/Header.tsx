import React from 'react';
import AllVueHeader from '@shared/AllVueHeader';
import { IDropDownMenuItem, IHeader, IExternalLink, ITabLink } from "@shared/Types";
import { useSelector } from 'react-redux';
import type { RootState } from '../../store';



const Header: React.FC = () => {
    const track = (eventName: string) => { console.log(eventName); };
    const userContext = useSelector((state: RootState) => state.userDetailsReducer.user);
    const isSystemAdministrator = userContext?.isSystemAdministrator ?? false;
    const emailName = userContext?.userName || "Unknown user";
    const doesUserHaveProjectContext = false;
    const runningEnvironment = userContext?.environment.runningEnvironment;
    const runningEnvironmentDescription = userContext?.environment.runningEnvironmentDescription;

    const themeDetails = useSelector((state: RootState) => state.userDetailsReducer.user?.themeDetails);

    function hexToRgbString(hex: string): string {
        if (hex) {
            hex = hex.replace(/^#/, '');

            // Expand shorthand form (#f00) to full form (#ff0000)
            if (hex.length === 3) {
                hex = hex.split('').map(x => x + x).join('');
            }

            if (hex.length !== 6) {
                return "255,0,0"; 
            }

            const num = parseInt(hex, 16);
            const r = (num >> 16) & 255;
            const g = (num >> 8) & 255;
            const b = num & 255;

            return `${r},${g},${b}`;
        }
        return "0,0,0"; 
    }

    function setCssVariableColour(variableName: string, value: string) {
        if (!value) {
            console.warn(`No value provided for CSS variable: ${variableName}`);
            return;
        }
        document.documentElement.style.setProperty(variableName, value);
        document.documentElement.style.setProperty(variableName+"-rgb", hexToRgbString(value));
    }

    function setFavicon(url: string) {
        if (!url) return;
        let link: HTMLLinkElement | null = document.querySelector("link[rel~='icon']");
        if (!link) {
            link = document.createElement('link');
            link.rel = 'icon';
            document.head.appendChild(link);
        }
        link.href = url;
    }

    if (themeDetails?.headerTextColour) {
        const borderWidth = themeDetails?.showHeaderBorder ? `4px` : `0px`;
        setCssVariableColour('--header-text-colour', themeDetails?.headerTextColour);
        setCssVariableColour('--header-background-colour', themeDetails?.headerBackgroundColour);
        setCssVariableColour('--header-border-colour', themeDetails?.headerBorderColour);
        document.documentElement.style.setProperty('--header-logo', `url(${themeDetails?.logoUrl})`);
        document.documentElement.style.setProperty('--header-border-width', borderWidth);
        setFavicon(themeDetails?.faviconUrl);

    }

    //
    //ToDo
    // https://app.shortcut.com/mig-global/story/95566/usermanagement-setup-header-to-be-project-enabled
    //Add some logic to determine if the user has a project context
    //For now, we will assume they do not have a project context
    //ProjectContext:
    // * menu items will need to be dynamic based on the project
    // * add in the missing menu items for the project context
    //
    //}

    const menuItemsProjectContext: IDropDownMenuItem = 
        {
            url: null,
            title: "Configure",
            text: "Configure",
            showLockIcon: true,
            children: [
                {
                    url: "/ui/subset-configuration",
                    title: "Subsets",
                    text: "Subsets",
                    showLockIcon: false,
                    children: []
                },
                {
                    url: "/ui/colour-configuration",
                    title: "Configure colours",
                    text: "Configure colours",
                    showLockIcon: false,
                    children: []
                }
            ],
            eventName: "Configure"
        };
    const menuItemsSystemAdmin: Array<IDropDownMenuItem> = [
        {
            url: "/usermanagement/",
            title: "Manage users for this company",
            text: "Manage users",
            showLockIcon: true,
            children: [],
            eventName: "ManageUsers"
        },
        {
            url: "/",
            title: "View projects that are available to you",
            text: "Your projects",
            showLockIcon: false,
            children: []
        },
        {
            url: "/usermanagement/logout",
            title: "Logout",
            text: "Logout",
            showLockIcon: false,
            children: []
        }
    ];
    if (doesUserHaveProjectContext) {

        menuItemsSystemAdmin.splice(1, 0, menuItemsProjectContext);
    }
    const menuItemsStandard: Array<IDropDownMenuItem> = [
        {
            url: "/projects",
            title: "View projects that are available to you",
            text: "Your projects",
            showLockIcon: false,
            children: []
        },
        {
            url: "/usermanagement/logout",
            title: "Logout",
            text: "Logout",
            showLockIcon: false,
            children: []
        }
    ];

    const tabs: Array<ITabLink> = [
    ];

    const externalLinks: Array<IExternalLink> = [
    ];

    const headerProps: IHeader = {
        track,
        pageTitle: userContext?.companyDisplayName,
        homeUrl: "/",
        helpUrl: "/help",
        username: emailName,
        menuItems: isSystemAdministrator ? menuItemsSystemAdmin: menuItemsStandard,
        tabs,
        externalLinks,
        warningMessage: null,
        warningIcon: "timelapse",
        runningEnvironment: runningEnvironment,
        runningEnvironmentDescription: runningEnvironmentDescription
    };

    return (
        <AllVueHeader {...headerProps}/>
    );
};

export default Header;