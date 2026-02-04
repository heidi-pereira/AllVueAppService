export interface ITabLink {
    url: string;
    text: string;
    icon?: string;
    isActive?: boolean;
    className?: string;
    noFill?: boolean;
    eventName?: string;
}