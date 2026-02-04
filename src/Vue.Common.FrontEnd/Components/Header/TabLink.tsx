import React from 'react';
import styles from "./TabLink.module.scss";
import { ITabLink } from "../Types/ITabLink";

interface ITabLinkProps extends ITabLink {
    track: (event: string) => void;
}


const TabLink = (props: ITabLinkProps) => {
    const current = props.isActive ? "true" : "false";
    const classes = [styles["tab-link"]];
    if (props.className) {
        classes.push(props.className);
    }
    if (props.isActive) {
        classes.push(styles["active"]);
    }
    const classNames = classes.join(" ");

    const iconClasses = "material-symbols-outlined" + (props.noFill ? " no-symbol-fill" : "");

    const handleClick = (event: React.MouseEvent<HTMLAnchorElement>) => {
        if (props.eventName) {
            props.track(props.eventName);
        }
    };

    return (
        <a className={classNames} href={props.url} aria-current={current} onClick={handleClick}>
            {props.icon && <i className={iconClasses}>{props.icon}</i>}
            <span>{props.text}</span>
        </a>
    );
}

export default TabLink;