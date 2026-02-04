import React from 'react';

export enum MaterialIconType {
    scale = "scale"
}

export enum MaterialIconStyle {
    standard = "material-icons",
    outlined = "material-icons-outlined",
    round = "material-icons-round",
    sharp = "material-icons-sharp",
    twoTone = "material-icons-two-tone",
}

interface IMaterialIconProps {
    iconType: MaterialIconType;
    iconStyle?: MaterialIconStyle;
    noFill?: boolean;
}

const MaterialIcon = (props: IMaterialIconProps) => {
    const styleClass = props.iconStyle ?? MaterialIconStyle.standard;
    const fillClass = props.noFill ? "no-symbol-fill" : "";
    return (
        <i className={`${styleClass} ${fillClass}`}>{props.iconType}</i>
    );
};

export default MaterialIcon;