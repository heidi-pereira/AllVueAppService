import React from 'react';
import { render, fireEvent, waitFor, findAllByRole, act } from "@testing-library/react";
import { getMetrics } from "../helpers/ReactTestingLibraryHelpers";
import { Metric } from "./metric";
import { MetricSet } from './metricSet';
import { metricsThatMatchSearchText } from './metricHelper';

describe("metric helper test", () => {
    const ms = new MetricSet();
    const metric1 = new Metric(ms);
    metric1.name = "metric1";
    metric1.displayName = "metric1DisplayName";
    metric1.varCode = "metric1VarCode";
    metric1.helpText = "metric1HelpText";

    const metric2 = new Metric(ms);
    metric2.name = "metric2";
    metric2.displayName = "metric2DisplayName";
    metric2.varCode = "metric2VarCode";
    metric2.helpText = "metric2HelpText";

    const metrics = [metric1, metric2];

    it("should return metrics that match search text", () => {
        let result = metricsThatMatchSearchText("", metrics);
        expect(result.length).toBe(2)

        result = metricsThatMatchSearchText("metric1DisplayName", metrics);
        expect(result.length).toBe(1)
        expect(result[0].displayName).toBe("metric1DisplayName")

        result = metricsThatMatchSearchText("metric1VarCode", metrics);
        expect(result.length).toBe(1)
        expect(result[0].varCode).toBe("metric1VarCode")

        result = metricsThatMatchSearchText("metric2HelpText", metrics);
        expect(result.length).toBe(1)
        expect(result[0].helpText).toBe("metric2HelpText")

        result = metricsThatMatchSearchText("adslfkjewlncaskdlugal", metrics);
        expect(result.length).toBe(0)
    })
})
