import React from "react";
import { FunctionComponent } from "react";
import { Link } from "react-router-dom";

interface IPageLinkProps {
    cssClass: string;
    to: string;
    text: string;
}

const PageLink: FunctionComponent<IPageLinkProps> = (props: IPageLinkProps) => (
    <Link className={props.cssClass} to={props.to}>
        <div>{props.text}</div>
        <span className="round-button">
            <i className="material-symbols-outlined">arrow_forward</i>
        </span>
    </Link>
)

export default PageLink;