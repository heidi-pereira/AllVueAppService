import React from "react";
import { EntityInstance } from "../../entity/EntityInstance";
import { ViewHelper } from "../visualisations/ViewHelper";
import { Metric } from "../../metrics/metric";
import { EntitySet } from "../../entity/EntitySet";

interface IScorecardKeyProps { 
    mainInstance: EntityInstance, 
    restrictor?: string[], 
    metrics?: Metric[], 
    averages?: (EntitySet | undefined)[] 
}

const ScorecardKey = (props: IScorecardKeyProps) => {
    const getKeyParts = () => {
        var keyItems = [
            { n: "range", d: "Range of competitor scores", style: {}},
            { n: "current", d: props.mainInstance.name + " score", style: {} },

        ];
        if (props.metrics) {
            keyItems.push({ n: "average", d: ViewHelper.createAverageDescription(props.metrics), style: {} });
        }
        if (props.averages) {
            props.averages.forEach((x, i) => keyItems.push({ n: "average" + i, d: "" + x?.name, style: scoreCardKeyStyle(i, props.averages!.length) }));
        }
        keyItems.push(
            { n: "sigdrop", d: "Significant <b>drop</b> from last period", style: {} },
            { n: "sigdropall", d: "Metric with significant drop across all periods", style: {} },
            { n: "siginc", d: "Significant <b>increase</b> from last period", style: {} },
            { n: "sigincall", d: "Metric with significant increase across all periods", style: {} });

        return keyItems.filter(i=> props.restrictor ? props.restrictor.indexOf(i.n)>=0 : true);
    }
    
    return (
        <div className="subsection scorecardKey">
            <header>Key</header>
            <table>
                <tbody>
                    {getKeyParts().map(k =>
                        <tr key={k.n}><td className="key"><div className={"key--" + k.n} style={k.style} ></div></td><td dangerouslySetInnerHTML={{ __html: k.d }}></td></tr>
                    )}
                </tbody>
            </table>
        </div>
    );
}
export default ScorecardKey;

const scoreCardKeyStyle = (i: number, averageCount: number): React.CSSProperties => ({
    ...scorecardAverageStyle(0, i, averageCount),
    marginTop: 0,
    borderRadius: "2px",
    border: "0",
    width: "4px",
});

export const scorecardAverageStyle = (average: number, i: number, averageCount: number): React.CSSProperties => {
    const marginHeight = 16;
    let rMin = 255, rMax = 153, gMin = 178, gMax = 255, bMin = 102, bMax = 51;
    let r = rMin + (rMax - rMin) * (i / (averageCount + 1));
    let g = gMin + (gMax - gMin) * (i / (averageCount + 1));
    let b = bMin + (bMax - bMin) * (i / (averageCount + 1));
    let color = '#' + Math.floor(r).toString(16) + Math.floor(g).toString(16) + Math.floor(b).toString(16);

    return {
        marginTop: (-marginHeight + marginHeight * (i / (averageCount))) - 5 + "px",
        height: (marginHeight / averageCount) + 4 + "px",
        backgroundColor: color,
    }
};
