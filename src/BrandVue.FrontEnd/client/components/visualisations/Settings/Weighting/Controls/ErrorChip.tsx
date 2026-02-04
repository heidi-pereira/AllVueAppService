import React from 'react';
import style from "./ErrorChip.module.less"
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from './MaterialSymbol';
import PopoverTooltip, { PopoverType } from '../PopoverTooltip';

interface IErrorChipProps {
    id: string;
    isWarning?: boolean;
    reasonText: string;
}

const ErrorChip = (props: IErrorChipProps) => {
    const [popoverOpen, setPopoverOpen] = React.useState(false);
    const iconType = props.isWarning ? "warning" : "error";
    const styleClass = props.isWarning ? style.warningColor : style.errorColor;
    const text = props.isWarning ? "Warning" : "Error";
    const popoverId = `chippop-${props.id}`;

    const errorSummary = (): JSX.Element => {
        return <div>
            <div>{props.reasonText}</div>
        </div>;
    }

    return (
        <>
            <div
                id={popoverId}
                className={`${style.errorChip} ${styleClass}`}
                onMouseEnter={() => setPopoverOpen(true)}
                onMouseLeave={() => setPopoverOpen(false)}>
                <MaterialSymbol symbolType={MaterialSymbolType[iconType]} symbolStyle={MaterialSymbolStyle.outlined} noFill={!props.isWarning} className={style.symbol} />
                <span className={style.text}>{`1 ${text}`}</span>
            </div>
            <PopoverTooltip type={props.isWarning ? PopoverType.Warning : PopoverType.Error}
                popoverContent={errorSummary()}
                id={popoverId}
                isOpen={popoverOpen}
                includeHeader={true}
                limitWidth={true}
            />
        </>
    );
};

export default ErrorChip;