import React from "react";
import Tooltip from "@Components/shared/Tooltip";
import { IProject } from "../CustomerPortalApi";
import { isProjectShared } from "../utils";

const SavantaOnlyLock = (props: {project: IProject}) => {

    if (isProjectShared(props.project)){
        return null;
    }

    return (
            <Tooltip placement="top" title={"This project is only visible to Savanta users"}>
                <div className="lock-container">
                    <div className="grey-triangle"/>
                    <i className="lock-icon material-symbols-outlined no-symbol-fill">lock</i>
                </div>
            </Tooltip>
    );
}

export default  SavantaOnlyLock;