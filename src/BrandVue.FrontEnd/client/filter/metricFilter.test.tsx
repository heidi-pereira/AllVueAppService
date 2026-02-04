import "@testing-library/jest-dom";
import { MetricFilterState } from "./metricFilterState";

const cSharpMinValue = -2147483648;
const cSharpMaxValue = 2147483647;

let mf: MetricFilterState;

describe("Check Metric Filter sets value correctly", () => {

    beforeEach(() => {
        mf = new MetricFilterState();
    });

    it("treatPrimaryValuesAsRange true", async () => {
        mf.values = [0, 1];
        mf.treatPrimaryValuesAsRange = true;
        expect(mf.valueToString()).toBe("0-1");
    });

    it("treatPrimaryValuesAsRange false", async () => {
        mf.values = [0, 1];
        mf.treatPrimaryValuesAsRange = false;
        expect(mf.valueToString()).toBe(`0,1`);
    });

    it("treatPrimaryValuesAsRange true and inverted", async () => {
        mf.values = [0, 1];
        mf.treatPrimaryValuesAsRange = true;
        mf.invert = true;
        expect(mf.valueToString()).toBe("!0-1");
    });

    it("treatPrimaryValuesAsRange false and inverted", async () => {
        mf.values = [0, 1];
        mf.treatPrimaryValuesAsRange = false;
        mf.invert = true;
        expect(mf.valueToString()).toBe(`!0,1`);
    });

    it("should return c# min/max values if no values passed", async () => {
        mf.treatPrimaryValuesAsRange = true;
        expect(mf.valueToString()).toBe(`${cSharpMinValue}-${cSharpMaxValue}`);
    });
});