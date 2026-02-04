import React from "react";
import { Features, RunningEnvironment } from "../../BrandVueApi";
import { ProductConfiguration } from "../../ProductConfiguration";
import helperStyles from "./EnvironmentBannerBar.module.less";
interface IEnvironmentBannerBar {
    productConfiguration: ProductConfiguration;
}

const EnvironmentBannerBar = (props: IEnvironmentBannerBar) => {

    const isVisible = () => {
        let isVisible = false;
        if ((props.productConfiguration.isSurveyVue()) &&
            (props.productConfiguration.runningEnvironment != RunningEnvironment.Live) &&
            (props.productConfiguration.runningEnvironment != RunningEnvironment.Development)
        ) {
            isVisible = sessionStorage.getItem(sessionKey) == null;
        }
        return isVisible;
    }
    const sessionKey = `banner_${props.productConfiguration.productName}_${props.productConfiguration.subProductId}`;
    const [isBannerVisible, setIsBannerVisible] = React.useState<boolean>(isVisible());

    const setBanner = (flag: boolean) => {
        sessionStorage.setItem(sessionKey, flag.toString());
        setIsBannerVisible(flag);
    }
    if (isBannerVisible) {
        return <span onClick={()=>setBanner(false) } className={helperStyles.envBanner}>{props.productConfiguration.runningEnvironmentDescription}</span>
    }
    return <></>
};

export default EnvironmentBannerBar;