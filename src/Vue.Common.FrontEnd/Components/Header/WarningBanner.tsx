import React from "react";
import styles from './WarningBanner.module.scss';

interface IWarningBannerProps {
    message?: string;
    icon?: string;
}

const WarningBanner = (props: IWarningBannerProps) => {
    const [isOpen, setIsOpen] = React.useState(true);

    if (!props.message || !isOpen) {
        return null;
    }

    return (
        <div className={styles["warning-banner"]}>
            <i className="material-symbols-outlined">{props.icon}</i>
            <div className={styles["message"]}>{props.message}</div>
            <div className={styles["remove-button"]} onClick={() => setIsOpen(false)}>
                <i className="material-symbols-outlined">close</i>
            </div>
        </div>
    );
}

export default WarningBanner;