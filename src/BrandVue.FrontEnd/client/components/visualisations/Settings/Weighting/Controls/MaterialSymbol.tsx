import React from 'react';

export enum MaterialSymbolType {
    info = "info",
    delete = "delete",
    download = "download",
    report = "report",
    weight = "weight",
    public = "public",
    warning = "warning",
    error = "error",
    done = "done",
    arrow_drop_down = "arrow_drop_down",
    arrow_drop_up = "arrow_drop_up",
    edit = "edit",
    file_copy = "file_copy",
    check_circle = "check_circle",
}

export enum MaterialSymbolStyle {
    standard = "material-symbols",
    outlined = "material-symbols-outlined",
    rounded = "material-symbols-round"
}

interface IMaterialSymbolProps {
    symbolType: MaterialSymbolType;
    symbolStyle?: MaterialSymbolStyle;
    noFill?: boolean;
    className?: string;
}

const MaterialSymbol = (props: IMaterialSymbolProps) => {
    const styleClass = props.symbolStyle ?? MaterialSymbolStyle.standard;
    const fillClass = props.noFill ? "no-symbol-fill" : "";
    return (
        <i className={`${styleClass} ${fillClass} ${props.className}`}>{props.symbolType}</i>
    );
};

export default MaterialSymbol;