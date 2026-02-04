import React from "react";
import styles from './HelpButton.module.scss';

interface IHelpButtonProps {
    track: (event: string) => void;
    url: string;
}

const HelpButton = (props: IHelpButtonProps) => {
    if (!props.url) {
        return <></>;
    }
    return (
        <a href={props.url} className={styles["circular-nav-button"]} title="Help" target="_blank" onClick={() => props.track("helpOpened")}>
            <div className={styles["circle"]}>
                <i className="material-symbols-outlined">help</i>
            </div>
            <span className="text">Help</span>
        </a>
    );
}

export default HelpButton