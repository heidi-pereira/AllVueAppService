import { HeatMapKeyPosition, HeatMapOptions } from "../../BrandVueApi";

export const defaultHeatMapOptions = () => {
    return new HeatMapOptions({ intensity: 10, overlayTransparency: 0.5, radius: 12, keyPosition: HeatMapKeyPosition.TopRight, displayKey: true, displayClickCounts: true });
};

export const getOptionsWithDefaultFallbacks = (options: HeatMapOptions) => {
    const defaultOptions = defaultHeatMapOptions();
    return new HeatMapOptions({
        intensity: options.intensity ?? defaultOptions.intensity!,
        overlayTransparency: options.overlayTransparency ?? defaultOptions.overlayTransparency!,
        radius: options.radius ?? defaultOptions.radius!,
        keyPosition: options.keyPosition ?? defaultOptions.keyPosition!,
        displayKey: options.displayKey ?? defaultOptions.displayKey!,
        displayClickCounts: options.displayClickCounts ?? defaultOptions.displayClickCounts!,
    });
};