import React from 'react';
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from './Controls/MaterialSymbol';
import style from './IconWithPopover.module.less';
import PopoverTooltip, { PopoverType } from './PopoverTooltip';

export enum IconType {
    Info = "info",
    Warning = "warning",
    Error = "error"
}

interface IconWithPopoverProps {
    iconType: IconType;
    popoverContent?: JSX.Element;
    id?: string;
}

const IconWithPopover = ({iconType, popoverContent, id}: IconWithPopoverProps) => {
    const [popoverOpen, setPopoverOpen] = React.useState(false);

    if (popoverContent) {
        const popoverId = id ?? `pop-${iconType}`;
        const capitalisedType = iconType.charAt(0).toUpperCase() + iconType.slice(1);
        return (
            <>
                <div
                    id={popoverId}
                    className={`${style.iconContainer}`}
                    onMouseEnter={() => setPopoverOpen(true)}
                    onMouseLeave={() => setPopoverOpen(false)}
                >
                <MaterialSymbol symbolType={MaterialSymbolType[iconType]} symbolStyle={MaterialSymbolStyle.outlined} className={style[iconType]} noFill />
            </div>
                <PopoverTooltip
                    type={PopoverType[capitalisedType]}
                    popoverContent={popoverContent}
                    id={popoverId}
                    isOpen={popoverOpen}
                    includeHeader={true}
                    limitWidth={true} />
            </>
        )
    }

    return (
        <MaterialSymbol symbolType={MaterialSymbolType[iconType]} symbolStyle={MaterialSymbolStyle.outlined} className={style[iconType]} noFill />
    )
}

export default IconWithPopover;