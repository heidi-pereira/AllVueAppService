import React from "react";
import "@Styles/authorizedLayout.scss";
import Footer from "@Components/shared/Footer";
import Indicators from "@Components/shared/Indicators";

interface IProps {
    children?: React.ReactNode;
}

const AuthorizedLayout = (props: IProps) => {

    return <div id="authorizedLayout" className="layout">
        
        <Indicators />

        {props.children}

        <div className="colon">
            <div className="dot"></div>
            <div className="dot"></div>
        </div>
        <Footer />
    </div>;
}

export default AuthorizedLayout;