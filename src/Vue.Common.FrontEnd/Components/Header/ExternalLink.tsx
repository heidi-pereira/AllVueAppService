import React from 'react';
import styles from "./ExternalLink.module.scss";
import { IExternalLink } from "../Types/IExternalLink";

interface IExternalLinkProps extends IExternalLink {
    track: (event: string) => void;
}

const ExternalLink = (props: IExternalLinkProps) => {
    const handleClick = (event: React.MouseEvent<HTMLAnchorElement>) => {
        if (props.eventName) {
            props.track(props.eventName);
        }
    };

    return (
        <a href={props.url} className={styles["open-survey-link"]} target="_blank" rel="noopener noreferrer" onClick={handleClick}>
            <span className={styles["link-text"]}>{props.text}</span>
            <i className="material-symbols-outlined">open_in_new</i>
        </a>
    );
}

export default ExternalLink;