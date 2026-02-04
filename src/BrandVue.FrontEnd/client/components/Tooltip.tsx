import React from "react";
import { PropsWithChildren } from "react";
import { OverlayTrigger, Tooltip } from 'react-bootstrap';
import { Placement } from "popper.js";

interface ITooltipProps {
    title: NonNullable<React.ReactNode>;
    placement?: Placement;
    delay?: number
}

const tooltip = React.forwardRef<HTMLElement, PropsWithChildren<ITooltipProps>>((props, ref) => {
    const toolTip = <Tooltip id={`tooltip`}>{props.title}</Tooltip>;
    //If children not a valid element then just empty fragment
    const overlayChildElement = React.isValidElement(props.children) ? props.children : <></>;
    return <OverlayTrigger placement={props.placement} trigger={["hover", "focus"]} overlay={toolTip} flip={true} delay={props.delay}>
        {overlayChildElement}
    </OverlayTrigger>;
});

export default tooltip;