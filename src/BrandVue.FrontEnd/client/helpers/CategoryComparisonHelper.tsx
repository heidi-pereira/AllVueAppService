import { StringHelper } from "./StringHelper";
import { NumberFormattingHelper } from "./NumberFormattingHelper";

export const getFirstLegendDescription = (displayName1: string | undefined, activeBrand: string | undefined) =>
    displayName1 ? StringHelper.formatBaseVariableName(displayName1) : activeBrand;

export const getSecondLegendDescription = (displayName1: string | undefined, displayName2: string | undefined) =>
    displayName2 ? StringHelper.formatBaseVariableName(displayName2) : displayName1 ? "Average" : "Average of competitor brands";

export const getDisplayValue = (input: number) => NumberFormattingHelper.format1Dp(input * 100);
