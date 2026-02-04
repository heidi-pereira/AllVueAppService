import React from 'react';
import { Popover } from 'reactstrap';
import style from './PopoverTooltip.module.less';
import Popper from 'popper.js';

export enum PopoverType {
    Info = "info",
    Warning = "warning",
    Error = "error",
    OptionsSummary = "optionsSummary"
}

interface IPopoverTooltipProps {
    type: PopoverType;
    popoverContent: JSX.Element;
    id: string;
    isOpen: boolean;
    includeHeader: boolean;
    limitWidth: boolean;
    placement?: Popper.Placement;
}

const PopoverTooltip = (props: IPopoverTooltipProps) => {
    const capitalisedType = props.type.charAt(0).toUpperCase() + props.type.slice(1);


    return (
        <Popover
            popperClassName={`${style.popover} ${props.limitWidth ? style.limitWidth : ''}`}
            placement={props.placement ?? "bottom"}
            isOpen={props.isOpen}
            hideArrow={false}
            target={props.id}>
            <>
                {props.includeHeader && 
                    <div className={style.header}>
                        <div className={`${style.legend} ${style[props.type]}`} />
                        <h4>{capitalisedType}</h4>
                    </div>
                }
                <div>
                    {props.popoverContent}
                </div>
            </>
        </Popover>
    )
}

export default PopoverTooltip;