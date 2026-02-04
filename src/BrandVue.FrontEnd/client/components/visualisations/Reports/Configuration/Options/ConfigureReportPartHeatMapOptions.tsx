import React from 'react';
import { PartDescriptor, CustomConfigurationOptions, HeatMapReportOptions, HeatMapKeyPosition, HeatMapOptions } from "../../../../../BrandVueApi";
import { defaultHeatMapOptions } from '../../../../helpers/HeatMapHelper';
import HeatMapConfiguration from './HeatMapConfiguration';

interface IConfigureReportPartHeatMapOptions {
    part: PartDescriptor;
    options: CustomConfigurationOptions | undefined;
    savePartChanges(newPart: PartDescriptor): void;
}

const ConfigureReportPartHeatMapOptions = (props: IConfigureReportPartHeatMapOptions) => {
    /*
    This code is duplicated with the backend
    https://app.shortcut.com/mig-global/story/90476/heatmap-report-tab-duplicate-default-options
    and needs to be unified
    the radius in the back end is taken from the metric's min value!
    */

    const defaultOptions = defaultHeatMapOptions();
    const heatMapOptions = (props.options instanceof HeatMapReportOptions) ? 
        new HeatMapOptions({
            intensity: props.options.intensity,
            overlayTransparency: props.options.overlayTransparency,
            radius: props.options.radiusInPixels,
            keyPosition: props.options.keyPosition,
            displayKey: props.options.displayKey,
            displayClickCounts: props.options.displayClickCounts
        })
        : defaultOptions;

    const saveOptionChanges = (options: HeatMapOptions) => {
        if (JSON.stringify(props.part.customConfigurationOptions) !== JSON.stringify(options)) {
            const modifiedPart = new PartDescriptor(props.part);
            modifiedPart.customConfigurationOptions = new HeatMapReportOptions({
                intensity: options.intensity ?? defaultOptions.intensity!,
                overlayTransparency: options.overlayTransparency ?? defaultOptions.overlayTransparency!,
                radiusInPixels: options.radius ?? defaultOptions.radius!,
                keyPosition: options.keyPosition ?? defaultOptions.keyPosition!,
                displayKey: options.displayKey ?? defaultOptions.displayKey!,
                displayClickCounts: options.displayClickCounts ?? defaultOptions.displayClickCounts!,
            });
            props.savePartChanges(modifiedPart);
        }
    }

    return (<HeatMapConfiguration options={heatMapOptions} saveOptionChanges={saveOptionChanges} />);
}

export default ConfigureReportPartHeatMapOptions