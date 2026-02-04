import React from "react";

interface IDeltaPresentation {
    cssClass: string;
    icon: string;
    formattedValue: string;
}

function getDeltaPresentation(delta: number, downIsGood: boolean, formatter: (value: number) => string) : IDeltaPresentation {

    const neutral = formatter(0);
    const absoluteDelta = isNaN(delta) ? 0 : Math.abs(delta);
    if (neutral === formatter(absoluteDelta)) {
        return {
            cssClass: "neutral",
            icon: "remove",
            formattedValue: formatter(absoluteDelta)
        }
    }

    if (delta < 0) {
        return  {
            cssClass: downIsGood ? "positive" : "negative",
            icon: "arrow_downward",
            formattedValue: formatter(absoluteDelta)
        };
    }

    return {
        cssClass: downIsGood ? "negative" : "positive",
        icon: "arrow_upward",
        formattedValue: formatter(absoluteDelta)
    };
}

interface IProps {
    delta: number
    formatter: (value: number) => string;
    downIsGood: boolean;
}

const MetricDelta: React.FunctionComponent<IProps> = (props: IProps) => {

    const presentation: IDeltaPresentation = getDeltaPresentation(props.delta, props.downIsGood, props.formatter);

    return (
        <span className={presentation.cssClass}>
            <i className="material-symbols-outlined">{presentation.icon}</i><span>{presentation.formattedValue}</span>
        </span>
    )
}

export default MetricDelta;