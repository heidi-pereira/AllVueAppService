import React from "react";
import Tooltip from "../../components/Tooltip";

interface IWeightingButtonSimple {
    className: string;
    toolTipText: string;
    disabled: boolean;
    buttonIcon: string;
    buttonText: string;
    onClick: () => void;
}

interface IWeightingButton extends IWeightingButtonSimple {
    isBusy: boolean;
    setIsBusy: (busy: boolean) => void;
}

const WeightingButton: React.FunctionComponent<IWeightingButton> = (props) => {

    const onClick = () => {
        props.setIsBusy(true);
        props.onClick();
    }

    return (<Tooltip placement="top" title={props.toolTipText} >
        <button className={`${props.className} ${props.isBusy ? "loading" : ""}`}
            disabled={props.disabled || props.isBusy}
            onClick={onClick}>
            <i className="material-symbols-outlined">{props.buttonIcon}</i>
            <div>{props.buttonText}</div></button>
    </Tooltip>);
}

export const WeightingButtonSimple: React.FunctionComponent<IWeightingButtonSimple> = (props) => {
    const [hasBeenClicked, setHasBeenClicked] = React.useState(false);

    return (<WeightingButton
        {...props}
        setIsBusy={setHasBeenClicked}
        isBusy={hasBeenClicked}
        />);
}

export default WeightingButton;

