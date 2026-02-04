import React from 'react';
import { IDropDownMenuItem } from "../Types/IDropDownMenuItem";
import styles from './DropDownMenuItem.module.scss';

interface IDropDownMenuItemProps extends IDropDownMenuItem {
    track: (event: string) => void;
}

const DropDownMenuItem = (props: IDropDownMenuItemProps) => {
    const className = props.children && props.children.length > 0 ? styles["node"] : "";
    const linkTitle = props.title ? props.title : props.text;

    const trackClick = () => {
        if (props.track && props.eventName) {
            props.track(props.eventName);
        }
    }

    return (
        <li className={className}>
            <a className="dropdown-item" href={props.url} title={linkTitle} onClick={trackClick}>
                {props.showLockIcon && 
                    <>
                        <span className={styles["icon"]}><i className="material-symbols-outlined">lock</i></span>
                        <span className={styles["text"]}>{props.text}</span>
                    </>
                }
                {!props.showLockIcon && <>{props.text}</>}
            </a>
            {props.children && props.children.length > 0 && (
                <ul>
                    {props.children.map((child, index) => {
                        return (<DropDownMenuItem key={index} {...child}></DropDownMenuItem>)
                    })}
                </ul>
            )}
        </li>
    );
}

export default DropDownMenuItem