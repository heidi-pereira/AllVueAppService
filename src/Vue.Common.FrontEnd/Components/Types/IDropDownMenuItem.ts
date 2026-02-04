export interface IDropDownMenuItem {
    url?: string;
    title?: string;
    text: string;
    showLockIcon?: boolean;
    eventName?: string;
    children?: Array<IDropDownMenuItem>;
}