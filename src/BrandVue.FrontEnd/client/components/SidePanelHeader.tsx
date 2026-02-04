import React from "react";
import style from "./SidePanelHeader.module.less"

interface ISidePanelHeader {
    returnButtonHandler?: () => void,
    closeButtonHandler?: () => void,
    children: React.ReactNode
}

const SidePanelHeader = (props: ISidePanelHeader) => {
    return (
        <React.Fragment>
            <div className={style.sidePanelHeader}>
                {props.returnButtonHandler &&
                    <div className={style.returnButton} onClick={props.returnButtonHandler}>
                        <i className="material-symbols-outlined">chevron_left</i>
                    </div>
                }
                <div className={style.headerTitle}>{props.children}</div>
                {props.closeButtonHandler &&
                    <div className={style.closeButton} onClick={props.closeButtonHandler}>
                        <i className="material-symbols-outlined">close</i>
                    </div>
                }
            </div>
        </React.Fragment>
    );
}

export default SidePanelHeader;