import React from 'react';
import styles from "./Tabs.module.scss";
import TabLink from "./TabLink";
import ExternalLink from "./ExternalLink";
import { ITabLink } from "../Types/ITabLink";
import { IExternalLink } from "../Types/IExternalLink";

interface ITabsProps {
    track: (event: string) => void;
    tabs?: Array<ITabLink>;
    externalLinks?: Array<IExternalLink>;
}

const Tabs = (props: ITabsProps) => {
    if (!props.tabs && !props.externalLinks) {
        return <></>;
    }

    return (
        <div className={styles["tabs"]}>
            <div className={styles["tab-link-container"]}>
                {props.tabs && props.tabs.length > 0 && (
                    props.tabs.map((tabProps, index) => {
                        return (<TabLink key={index} {...tabProps} track={props.track}></TabLink>)
                    })
                )}
            </div>
            <div className={styles["links-to-savanta"]}>
                {props.externalLinks && props.externalLinks.length > 0 && (
                    props.externalLinks.map((linkProps, index) => {
                        return (<ExternalLink key={index} {...linkProps} track={props.track}></ExternalLink>)
                    })
                )}
            </div>
        </div>
    );
}

export default Tabs;