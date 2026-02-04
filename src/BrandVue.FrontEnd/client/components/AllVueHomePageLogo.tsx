import React from "react";
import {IApplicationUser} from "../BrandVueApi";

interface IAllVueHomePageLogoProps {
    user: IApplicationUser | null;
}

const AllVueHomePageLogo = (props: IAllVueHomePageLogoProps) => {
    const getLogoUrl = () => {
        //replace 'url(', '\' escape chars, any quote chars, and trailing ) or );
        const regex = /(url\()*(\\)*['"`]*(\)$)*(\);$)*/g
        const backgroundImage = getComputedStyle(document.body).getPropertyValue("--header-logo");
        return backgroundImage.replaceAll(regex, '').trim()
    }

    const getHomePageLink = () => {
        if (!props.user || props.user.isReportViewer){
            return undefined
        }
        if (props.user.isAdministrator){
            return `${window.location.origin}/auth`
        }
        return `${window.location.origin}/projects/`
    }
    return (
        <a className={"logo-link-container"} href={getHomePageLink()}>
            <img src={getLogoUrl()} alt="Logo" className="page-logo" />
        </a>
    );
}

export default AllVueHomePageLogo