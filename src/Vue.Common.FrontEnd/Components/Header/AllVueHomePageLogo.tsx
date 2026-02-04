import React from "react";
import styles from './AllVueHomePageLogo.module.scss';

interface IAllVueHomePageLogoProps {
    url: string;
}

const AllVueHomePageLogo = (props: IAllVueHomePageLogoProps) => {
    return (
        <a className={styles['logo-link-container']} href={props.url}>
            <img style={{ content: 'var(--header-logo)' }} alt="Logo" className={styles["page-logo"]} />
        </a>
    );
}

export default AllVueHomePageLogo