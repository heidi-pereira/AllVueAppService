import React from "react";

interface ICancelFilterProps {
    metricName: string;
    enabled: boolean;
    clearFilter: (metricName: string) => void;
};

interface IFilterTitleProps {
    metricName: string;
    enabled: boolean;
    clearFilter: () => void;
}

export const CancelFilter: React.FunctionComponent<ICancelFilterProps> = (props) => {
    let title = `Clear '${props.metricName}' filter`;
    if (!props.enabled) {
        title = `Filter '${props.metricName}' currently not selected`;
    }
    return (
        <a onClick={() => props.clearFilter(props.metricName)} href={undefined} className={`pointer not-exported ${
            props.enabled ? "" : "d-none"}`} title={title}>
            <i className={`material-symbols-outlined small ${props.enabled ? "enabled" : "disabled"}`}>cancel</i>
        </a>
    );
};

export const FilterTitle: React.FunctionComponent<IFilterTitleProps> = (props) => {
    return (
        <div className={`text-sm-right position-relative filterName p-2 ml-sm-1 ${props.enabled ? "" : "disabled"}`
        }>
            <CancelFilter metricName={props.metricName} enabled={props.enabled} clearFilter={props.clearFilter} />
            <span>{props.metricName}</span>
        </div>
    );
};
