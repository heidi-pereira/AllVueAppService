import React from "react";

interface IRankDeltaPresentation {
    cssClass: string;
    icon: string;
}

export const getDeltaPresentation = (delta: number, downIsGood: boolean): IRankDeltaPresentation => {
    if (delta === 0) {
        return {
            cssClass: "rank-neutral",
            icon: "remove",
        }
    }

    if (delta > 0) {        
        return  {
            cssClass: downIsGood ? "rank-positive" : "rank-negative",
            icon: "arrow_downward",
        };
    }

    return {
        cssClass: downIsGood ? "rank-negative" : "rank-positive",
        icon: "arrow_upward",
    };
}

interface IProps {
    delta: number;
    downIsGood: boolean;
}

const RankDelta: React.FunctionComponent<IProps> = ({ delta, downIsGood }: IProps) => {

    const presentation: IRankDeltaPresentation = getDeltaPresentation(delta, downIsGood);
    return (
        <span className={presentation.cssClass}><i className="material-symbols-outlined">{presentation.icon}</i></span>
    )
}

export default RankDelta;