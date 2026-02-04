import React from 'react';
import AllVueHomePageLogo from "./AllVueHomePageLogo";
import styles from './PageTitle.module.scss';

interface IPageTitleProps {
    title: string;
    url: string;
}

const PageTitle = (props: IPageTitleProps) => {
    return (
        <div className={styles["page-title-container"]}>
            <AllVueHomePageLogo url={props.url} />
            <span className={styles["page-title"]}>{ props.title }</span>
        </div>
    );
}

export default PageTitle