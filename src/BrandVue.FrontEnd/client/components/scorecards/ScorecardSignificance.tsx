import React from "react";
import { Metric } from "../../metrics/metric";

const ScorecardSignificance = (props: { metricResults: Metric[]}) => {
    var lookup: { [id: string]: Metric[]; } = {};
    for (let metric of props.metricResults) {
        if (lookup[metric.getSignificanceValue()] === undefined) {
            lookup[metric.getSignificanceValue()] = [];
        }
        lookup[metric.getSignificanceValue()].push(metric);
    }
    var items : { id: string, text: string} []= [];
    for (const key in lookup) {
        if (lookup.hasOwnProperty(key)) {
            let metrics = lookup[key];
            let values: string[] = [];
            metrics.map(m => values.push(m.name));
            let text = "Significance for " + values.join(", ") + " is " + metrics[0].getSignificanceValue();
            items.push({ id: key, text: text });
        };
    };

    return (
        <div className="subsection scorecardSignifcance">
            <header>Significance</header>
            <table>
                <tbody>
                    {items.map(m =>
                        <tr key={m.id}><td>{m.text}</td></tr>
                        )}
                </tbody>
            </table>
        </div>
    );
}
export default ScorecardSignificance;

