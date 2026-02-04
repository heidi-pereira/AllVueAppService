import React from "react";
import { ProductConfiguration } from "../ProductConfiguration";
import { isBarometer } from "./helpers/FeaturesHelper";
import BarometerProductsMenu from "./BarometerProductsMenu";
import { Link } from "react-router-dom";

interface DesktopLogoProps {
    productConfiguration: ProductConfiguration;
    startPageUrl: string;
}

const DesktopLogo = (props: DesktopLogoProps) => {
    const getSurveyVueLinkComponent = () => {
        return <div className="surveyvue-title">{props.productConfiguration.surveyName}</div>
    }
    const getBrandVueLinkComponent = (logoPathSet: boolean) => {
        return <>
            {!logoPathSet && <div className="logo" >
                <div className="sector"></div>
            </div>}
        </>;
    }

    const getLinkComponent = () => {
        const logo = window.getComputedStyle(document.body).getPropertyValue('--header-logo');
        const logoPathSet = logo?.length > 2; // This includes the double quotes of an empty string
        return <>
            {logoPathSet && <div className="savanta-logo"></div>}
            {props.productConfiguration.isSurveyVue() && getSurveyVueLinkComponent()}
            {!props.productConfiguration.isSurveyVue() && getBrandVueLinkComponent(logoPathSet)}
            </>
    }

    return (
        <Link to={{pathname: props.startPageUrl, search: window.location.search}}
                 className="home-link" >
            {getLinkComponent()}
            {isBarometer(props.productConfiguration) && <BarometerProductsMenu />}
        </Link>
    );
}

export default DesktopLogo