import { IDropDownMenuItem } from "./IDropDownMenuItem";
import { ITabLink } from "./ITabLink";
import { IExternalLink } from "./IExternalLink";

export interface IHeader {
    username?: string;
    menuItems?: Array<IDropDownMenuItem>;
    tabs?: Array<ITabLink>;
    externalLinks?: Array<IExternalLink>;
    pageTitle: string;
    homeUrl: string;
    helpUrl?: string;
    warningMessage?: string;
    warningIcon?: string;
    runningEnvironment: string;
    runningEnvironmentDescription: string;
}