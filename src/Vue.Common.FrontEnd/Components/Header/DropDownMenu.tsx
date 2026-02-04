import React from 'react';
import DropDownMenuItem from "./DropDownMenuItem";
import { IDropDownMenuItem } from "../Types/IDropDownMenuItem";
import styles from './DropDownMenu.module.scss';

interface IDropDownMenuProps {
    track: (event: string) => void;
    username: string;
    menuItems?: Array<IDropDownMenuItem>;
}

const DropDownMenu = (props: IDropDownMenuProps) => {
    return (
        <ul className={styles["account-menu"]}>
            <DropDownMenuItem key="username" text={props.username} />
            <div className={styles["separator"]}></div>
            {props.menuItems && props.menuItems.length > 0 && (
                props.menuItems.map((itemProps, index) => {
                    return (<DropDownMenuItem key={index} {...itemProps} track={props.track}></DropDownMenuItem>)
                })
            )}
        </ul>
    );
}

export default DropDownMenu