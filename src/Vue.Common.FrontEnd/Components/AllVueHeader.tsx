import React from 'react';
import TopMenu from "./Header/TopMenu";
import { IHeader } from "./Types/IHeader";
import Tabs from "./Header/Tabs";
import styles from "./AllVueHeader.module.scss";
import WarningBanner from "./Header/WarningBanner";

interface AllVueHeaderProps extends IHeader {
    track: (eventName: string) => void;
}

const AllVueHeader = (props: AllVueHeaderProps) => {
    const className = "survey-vue-page-header " + styles["header"];
    return (
        <div className={className} id="survey-vue-page-header">
            <TopMenu
                track={props.track}
                pageTitle={props.pageTitle}
                homeUrl={props.homeUrl}
                helpUrl={props.helpUrl}
                username={props.username}
                menuItems={props.menuItems}
                runningEnvironment={props.runningEnvironment}
                runningEnvironmentDescription={props.runningEnvironmentDescription} />
            <Tabs tabs={props.tabs} externalLinks={props.externalLinks} track={props.track}></Tabs>
            <WarningBanner message={props.warningMessage} icon={props.warningIcon} />
        </div>
    );
}

export default AllVueHeader;