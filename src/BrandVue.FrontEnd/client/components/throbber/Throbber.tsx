import React from "react";
import style from "./Throbber.module.less";

interface IThrobberProps {
    inFixedContainer?: boolean;
}

const Throbber: React.FunctionComponent<IThrobberProps> = (props) => {
    // BrandVue will get the throbber from main.less, but AllVue's throbber is loaded from the auth server, from the stylesheet.css
    const spinner = <div className="custom-spinner" aria-label="Content is loading spinner" />
    if (props.inFixedContainer) {
        return (
            <div className={style.fixedContainer}>
                {spinner}
            </div>
        );
    }
    return spinner;
};

export default Throbber;