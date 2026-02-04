import React from "react";
import style from "./BrandAnalysis.module.less"

interface BrandAnalysisPercentageProps {
    group: StackedPercentageGroup,
}

interface StackedPercentageGroup {
    title?: string,
    rows: {
        name: string, value: number | null, color: string,
    }[]
}

const barStyle = (value: number | null, color: string): React.CSSProperties => ({
    width: value != null ? value! * 100 + "%" : "",
    backgroundColor: color,
});

function SingleBarRow(props: { name: string, value: number | null, color: string }) {
    return <div className={`${style.flexRow} ${style.percentageBarRow}`}>
        <div className={`${style.flexRow} ${style.percentageRowTitle}`}>
            <div className={style.rowTitle}>{props.name}</div>
            <div>{((props.value ?? 0) * 100).toFixed(0)}%</div>
        </div>
        <div className={style.percentageBar}>
            <div style={barStyle(props.value, props.color)}>&nbsp;</div>
        </div>
    </div>;
}
export default function BrandAnalysisPercentage(props: BrandAnalysisPercentageProps) {
    return (
        <div className={style.brandAnalysisPercentage}>
            <div>{props.group.title != null && 
                <div className={style.groupTitle}>{props.group.title?.toUpperCase()}</div>
                }
                {props.group.rows.map((r,j)=><SingleBarRow {...r} key={j}/>)}
                </div>
        </div>
    );
};
