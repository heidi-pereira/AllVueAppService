import "@testing-library/jest-dom";
import { NumberFormattingHelper } from "./NumberFormattingHelper";


describe("With the NumberFormattingHelper class", () => {
    const percentageZeroDPConversionTests = [
        [-0.035, "-4%", "-3.5%", "-3.50%"],
        [-0.070, "-7%", "-7.0%", "-7.00%"],
        [-0.140, "-14%", "-14.0%", "-14.00%"],
        [-0.145, "-15%", "-14.5%", "-14.50%"],
        [-0.275, "-28%", "-27.5%", "-27.50%"],
        [-0.280, "-28%", "-28.0%", "-28.00%"],
        [-0.285, "-29%", "-28.5%", "-28.50%"],
        [-0.290, "-29%", "-29.0%", "-29.00%"],
        [-0.545, "-55%", "-54.5%", "-54.50%"],
        [-0.550, "-55%", "-55.0%", "-55.00%"],
        [-0.555, "-56%", "-55.5%", "-55.50%"],
        [-0.560, "-56%", "-56.0%", "-56.00%"],
        [-0.565, "-57%", "-56.5%", "-56.50%"],
        [-0.570, "-57%", "-57.0%", "-57.00%"],
        [-0.575, "-58%", "-57.5%", "-57.50%"],
        [-0.580, "-58%", "-58.0%", "-58.00%"],
        [-0.655, "-66%", "-65.5%", "-65.50%"],

        [0.0049, "0%", "0.5%", "0.49%"],
        [0.00499999, "0%", "0.5%", "0.50%"],
        [0.015, "2%", "1.5%", "1.50%"],
        [0.035, "4%", "3.5%", "3.50%"],
        [0.070, "7%", "7.0%", "7.00%"],
        [0.140, "14%", "14.0%", "14.00%"],
        [0.145, "15%", "14.5%", "14.50%"],
        [0.275, "28%", "27.5%", "27.50%"],
        [0.280, "28%", "28.0%", "28.00%"],
        [0.285, "29%", "28.5%", "28.50%"],
        [0.290, "29%", "29.0%", "29.00%"],
        [0.545, "55%", "54.5%", "54.50%"],
        [0.550, "55%", "55.0%", "55.00%"],
        [0.555, "56%", "55.5%", "55.50%"],
        [0.560, "56%", "56.0%", "56.00%"],
        [0.565, "57%", "56.5%", "56.50%"],
        [0.570, "57%", "57.0%", "57.00%"],
        [0.575, "58%", "57.5%", "57.50%"],
        [0.580, "58%", "58.0%", "58.00%"],
        [0.6234818, "62%", "62.3%", "62.35%"],
        [0.9999999, "100%", "100.0%", "100.00%"],
    ];

    test.each(percentageZeroDPConversionTests)("converting %f to percentage should be %s, %s & %s to 0, 1 & 2 dp", async (decimalValue, expectedZeroDp, expectedOneDp, expectedTwoDp) => {
        const actualZeroDp = NumberFormattingHelper.formatPercentage0Dp(decimalValue as number);
        const actualOneDp = NumberFormattingHelper.formatPercentage1Dp(decimalValue as number);
        const actualTwoDp = NumberFormattingHelper.formatPercentage2Dp(decimalValue as number);
        expect(actualZeroDp).toEqual(expectedZeroDp);
        expect(actualOneDp).toEqual(expectedOneDp);
        expect(actualTwoDp).toEqual(expectedTwoDp);
    });
});