import chroma from "chroma-js";
import { EntityInstance } from "../../BrandVueApi";
import { Metric } from "../../metrics/metric";

const _defaultColourScale = ['#FDE746', '#2DB396', '#535B97'];
const _reversedDefaultColourScale = ['#535B97', '#2DB396', '#FDE746'];

export const Chalk = '#E0E2E5';
export const ChalkLight = '#EEEFF0'
export const SlateDark = '#42484D';
export const Slate = '#6E7881';

export function getColourMapFromEntityInstances(instances: (EntityInstance | undefined)[], metric: Metric | undefined): Map<string, string> {
    const orderedCategories = instances
        .map((entityInstance, index) => ({
            name: entityInstance?.name ?? metric?.displayName ?? '',
            id: entityInstance?.id ?? index
        }))
        .sort((a, b) => a.id - b.id)
        .map(result => result.name);
    return getColourMap(orderedCategories);
}

export function getColourMap(keys: string[]): Map<string, string> {
    return getColourMapFromArray(keys, _defaultColourScale);
}

export function getColourMapReverse(keys: string[]): Map<string, string> {
    return getColourMapFromArray(keys, _reversedDefaultColourScale);
}

function getColourMapFromArray(keys: string[], colourScale: string[]): Map<string, string> {
    const colours: string[] = chroma.scale(colourScale).colors(keys.length);
    return new Map(keys.map((key, index) => [key, colours[index]]));
}

export function getLabelTextColor(color: string | undefined): string | undefined {
    const darkTextColor = "#26292C"; //slate-darker
    const lightTextColor = "#F8F9F9"; //chalk-lighter
    const contrastThreshold = 4.5;

    var darkContrastRatio = chroma.contrast(color, darkTextColor);
    var lightContrastRatio = chroma.contrast(color, lightTextColor);

    if (darkContrastRatio > lightContrastRatio) {
        if (darkContrastRatio > contrastThreshold) {
            return darkTextColor;
        }
    } else {
        if (lightContrastRatio > contrastThreshold) {
            return lightTextColor;
        }
    }
}

const categoryComparisonColors = new Map(
    [
        ["Gold", "rgba(255, 162, 57, 1)"],
        ["Purple", "rgba(128, 95, 197, 1)"],
        ["Green", "rgba(72, 154, 146, 1)"],
        ["Red", "rgba(202, 62, 67, 1)"],
        ["Blue", "rgba(67, 131, 161, 1)"],
        ["Navy", "rgba(33, 74, 87, 1)"],
        ["LightBlue", "rgba(67, 131, 161, 0.3)"],
    ]
);

const categoryComparisonSecondaryColors = new Map(
    [
        ["Grey", "rgba(25, 25, 46, 0.25)"],
        ["Gold", "rgba(248, 192, 129, 1)"],
        ["Purple", "rgba(127, 107, 168, 0.75)"],
        ["Green", "rgba(94, 161, 155, 0.75)"],
        ["Red", "rgba(226, 88, 92, 0.75)"],
        ["Blue", "rgba(93, 174, 212, 0.75)"],
        ["Navy", "rgba(14, 80, 102, 0.75)"],
        ["LightGrey", "rgba(0, 0, 0, 0.1)"],
    ]
);

export const getCategoryComparisonColorByName = (colorName: string) => { return categoryComparisonColors.get(colorName) ?? categoryComparisonColors.values()[0]; }

export const getCategoryComparisonSecondaryColorByName = (colorName: string) => { return categoryComparisonSecondaryColors.get(colorName) ?? categoryComparisonSecondaryColors.values()[0]; }
