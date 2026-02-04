import React from "react";
import { PropsWithChildren } from "react";
import {useNavigate} from "react-router-dom";

export interface ITileTemplateProps {
    nextPageUrl?: string;
    linkText?: string;
    description?: string;
    descriptionNode?: React.ReactNode;
    className?: string;
    noScroll?: boolean;
}

const TileTemplate: React.FunctionComponent<React.PropsWithChildren & ITileTemplateProps> = (props: PropsWithChildren<ITileTemplateProps>) => {
    const navigate = useNavigate();
    const content = (
        <>
            {props.descriptionNode ?? (props.description && <p className="description">{props.description}</p>)}
            
            <div className="card-content">
                {props.children}
            </div>
            {props.linkText &&
            <div className="next">
                <span>{props.linkText}</span>
                <div className="icon-border"><i className="material-symbols-outlined">arrow_forward</i></div>
            </div>
            }
        </>
    );

    const className = `card-multi ${props.nextPageUrl ? 'has-link' : ''} ${props.noScroll ? 'no-scroll' : ''}`;
    const wrappedContent = props.nextPageUrl ?
        <div className={className} onClick={() => navigate(props.nextPageUrl!)}>{content}</div>
        : <div className={className}>{content}</div>

    return (
        <div className={props.className ?? "tile"}>
            {wrappedContent}
        </div>
    );
}

export default TileTemplate;