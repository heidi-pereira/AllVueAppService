import React from 'react';
import PageTitle from "./PageTitle";
import HelpButton from "./HelpButton";
import styles from './TopMenu.module.scss';
import AccountOptions from "./AccountOptions";
import { IDropDownMenuItem } from "../Types/IDropDownMenuItem";

interface TopMenuProps {
    track: (eventName: string) => void;
    username: string;
    menuItems?: Array<IDropDownMenuItem>;
    pageTitle: string;
    homeUrl: string;
    helpUrl?: string;
    runningEnvironment: string;
    runningEnvironmentDescription: string;

}

const TopMenu = (props: TopMenuProps) => {
    const sessionKey = `banner_bar`;

    const isVisible = () => {
        return sessionStorage.getItem(sessionKey) == null;;
    }
    const [isBannerVisible, setIsBannerVisible] = React.useState<boolean>(isVisible());


    const setBanner = (flag: boolean) => {
        sessionStorage.setItem(sessionKey, flag.toString());
        setIsBannerVisible(flag);
    }


    return (
        <nav className={styles.topmenu}>
            {props.runningEnvironment != "Live" && props.runningEnvironmentDescription && isBannerVisible &&
                <span className={styles["env-banner"]} onClick={() => setBanner(false)}>{props.runningEnvironmentDescription}</span>
            }
            <PageTitle title={props.pageTitle} url={props.homeUrl}></PageTitle>
            <div className={styles["menu-icons-container"]}>
                <HelpButton track={props.track} url={props.helpUrl!} />
                <AccountOptions username={props.username} menuItems={props.menuItems} track={props.track} />
            </div>
        </nav>
    );
}

export default TopMenu;