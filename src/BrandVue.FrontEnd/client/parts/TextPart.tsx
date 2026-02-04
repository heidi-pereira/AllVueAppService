import React from "react";
import { IDashPartProps } from "../components/DashBoard";
import { BasePart } from "./BasePart";

export class TextPart extends BasePart {
    getPartComponent(props: IDashPartProps): JSX.Element | null {
        return <div className={props.partConfig.descriptor.spec2 ?? ""}><div><span dangerouslySetInnerHTML={{__html: props.partConfig.descriptor.spec1}}/></div></div>;
        //child divs are used for attaching additional after styles
    }
}
