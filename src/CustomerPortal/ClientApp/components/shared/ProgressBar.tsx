import React from "react";
import classNames from 'classnames';

export interface IProps {
    value: number | null;
}

const ProgressBar = (props: IProps) => {

    if (props.value != null) {
        
        const progressbarFillerClasses = classNames("progressbar-filler", (props.value < 100 ? "partial" : "full"));
        return (
            <div className="progressbar">
                <div className={progressbarFillerClasses} style={{ width: (props.value > 100 ? 100 : props.value) + "%" }}></div>
            </div>
        )
    } else {
        return (
            <div className="progressbar">
                <div className="progressbar-filler"></div>
            </div>
        )
    }
}

export default ProgressBar;