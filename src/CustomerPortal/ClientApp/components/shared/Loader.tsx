import "@Styles/loader.scss";
import React from "react";

export interface IProps {
    show: boolean;
}

const Loader = (props: IProps) => {

    const css = { "display": (props.show ? "flex" : "none") }

    // The throbber is loaded from the auth server, from the stylesheet.css
    return <div className="loader-bg" style={css}>
        <div className="custom-spinner" aria-label="Content is loading spinner"/>
    </div>;
}

export default Loader;