import React from "react";
import Switch from "react-switch";

interface IToggleSwitchProps {
    disabled?: boolean;
    checked?: boolean;
    label?: string;
    ariaLabel ?: string;
    height ?: number;
    width ?: number;
    onChange: () => void;
}

// https://www.npmjs.com/package/react-switch
const ToggleSwitch: React.FunctionComponent<IToggleSwitchProps> = (props: IToggleSwitchProps) => {
    return (
        <Switch
            height={props.height}
            width={props.width}
            onColor="#86C2DF" // 75% tint from #5daed4
            offColor="#CBCFD3"
            onHandleColor="#FFFFFF"
            uncheckedIcon={false}
            checkedIcon={false}
            onChange={() => props.onChange()}
            checked={props.checked ?? false}
            aria-label={props.ariaLabel} />
    );
};

export default ToggleSwitch;