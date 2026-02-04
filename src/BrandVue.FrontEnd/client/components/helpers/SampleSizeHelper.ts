import { IAverageDescriptor, SampleSizeMetadata } from "../../BrandVueApi";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { Metric } from "../../metrics/metric";

export function getSampleSizeDescription(sampleSizeMeta: SampleSizeMetadata, average: IAverageDescriptor, sampleSizeFor: string, metrics: Metric[]): string {
    const startOfDescription = sampleSizeFor ? `${sampleSizeFor} sample size` : "Sample size";
    const date = sampleSizeMeta.currentDate ? `${DateFormattingHelper.formatDatePoint(sampleSizeMeta.currentDate, average)}` : "";
    let description = isNaN(sampleSizeMeta.sampleSize.unweighted) ? startOfDescription : `${startOfDescription} for ${date}: n = ${addCommaSeparator(sampleSizeMeta.sampleSize.unweighted)}`;
    const sampleSizeForEntity = sampleSizeMeta.sampleSizeByEntity[sampleSizeFor];

    if (sampleSizeForEntity) {
        description = `${sampleSizeFor} sample size for ${date}: n = ${addCommaSeparator(sampleSizeForEntity.unweighted)}`;
    } else {
        const metricKeys = Object.keys(sampleSizeMeta.sampleSizeByMetric);
        const entityKeys = Object.keys(sampleSizeMeta.sampleSizeByEntity);
        const sampleName = sampleSizeMeta.sampleSizeEntityInstanceName ? `${sampleSizeMeta.sampleSizeEntityInstanceName} sample` : 'Sample';

        if (metricKeys.length > 0) {
            description = `${sampleName} sizes for ${date}: ${metricKeys.map(k => `${getMetricNameWithProperCasing(k, metrics)} n = ${addCommaSeparator(sampleSizeMeta.sampleSizeByMetric[k].unweighted)}`).join('; ')}`;
        } else if (entityKeys.length > 0) {
            description = `${sampleName} sizes for ${date}: ${getSampleSizeByEntityDescription(sampleSizeMeta)}`;
        }
    }

    return description;
}

export function getAllVueSampleSizeDescription(sampleSizeMeta: SampleSizeMetadata): string {
    let description = `Sample size: n = ${addCommaSeparator(sampleSizeMeta.sampleSize.unweighted)}`;
    if (sampleSizeMeta.sampleSize.hasDifferentWeightedSample) {
        description += ` (weighted n = ${addCommaSeparator(sampleSizeMeta.sampleSize.weighted)})`;
    }

    const entityKeys = Object.keys(sampleSizeMeta.sampleSizeByEntity);
    if (entityKeys.length > 0) {
        const values = Object.values(sampleSizeMeta.sampleSizeByEntity);
        const unweightedValues = values.map(v => v.unweighted);
        const weightedValues = values.map(v => v.weighted);
        const allHasSameSample = values.every(v => !v.hasDifferentWeightedSample);
        if (new Set(unweightedValues).size === 1 && (allHasSameSample || new Set(weightedValues).size === 1)) {
            description = `Sample size: n = ${addCommaSeparator(unweightedValues[0])}`;
            if (!allHasSameSample) {
                description += ` (weighted n = ${addCommaSeparator(weightedValues[0])})`;
            }
        } else {
            description = `Sample sizes: ${getSampleSizeByEntityDescription(sampleSizeMeta)}`;
        }
    }

    return description;
}

export function getSimpleSampleSizeDescription(sampleSizeMeta: SampleSizeMetadata): string {
    let description = `n = ${addCommaSeparator(sampleSizeMeta.sampleSize.unweighted)}`;

    const entityKeys = Object.keys(sampleSizeMeta.sampleSizeByEntity);
    if (entityKeys.length > 0) {
        const values = Object.values(sampleSizeMeta.sampleSizeByEntity);
        const unweightedValues = values.map(v => v.unweighted);
        if (new Set(unweightedValues).size === 1) {
            description = `n = ${addCommaSeparator(unweightedValues[0])}`;
        } else {
            description = `Sample sizes: ${getSampleSizeByEntityDescription(sampleSizeMeta)}`;
        }
    }

    return description;
}

function getSampleSizeByEntityDescription(sampleSizeMeta: SampleSizeMetadata) {
    const entitiesByName = Object.keys(sampleSizeMeta.sampleSizeByEntity);

    var mapSampleDescriptionToEntityName = new Map<string, string[]>();

    entitiesByName.map(entityName => {
        const entitySample = sampleSizeMeta.sampleSizeByEntity[entityName];
        let description = `n = ${addCommaSeparator(entitySample.unweighted)}`;
        if (entitySample.hasDifferentWeightedSample) {
            description += `; weighted n = ${addCommaSeparator(entitySample.weighted)}`;
        }

        var arrayOfEntityNames = mapSampleDescriptionToEntityName.get(description);
        if (arrayOfEntityNames == undefined) {
            arrayOfEntityNames = [entityName];
        }
        else {
            arrayOfEntityNames.push(entityName);
        }
        mapSampleDescriptionToEntityName.set(description, arrayOfEntityNames);
    });
    var groupedItems: string[] = [];

    mapSampleDescriptionToEntityName.forEach((instanceNames, sampleDescription) => {
        const formatted = `${instanceNames.join(', ')} (${sampleDescription})`;
        groupedItems.push(formatted);
    });
    return groupedItems.join("; ");
}

function addCommaSeparator(input: number): string {
    return new Intl.NumberFormat(undefined, { maximumFractionDigits: 0 }).format(input);
}

function getMetricNameWithProperCasing(input: string, metrics: Metric[]): string {
    const metric = metrics.find(m => m.name.toLowerCase() === input);
    return metric ? metric.name : input;
}

export function parseSampleSizeDescription(sampleSizeDescription: string): string {
    let sampleSize: number = 0;
    let nCount: number = 0;

    if (sampleSizeDescription.indexOf("n =") === -1) {
        return sampleSizeDescription;
    }

    sampleSizeDescription = sampleSizeDescription.replaceAll(",", "");

    let position: number = sampleSizeDescription.indexOf("n =");
    while (position !== -1) {
        sampleSize += parseInt(sampleSizeDescription.substr(position + 3));
        nCount += 1;

        position = sampleSizeDescription.indexOf("n =", position + 3);
    }

    switch (nCount) {
    case 1:
        return `n = ${addCommaSeparator(sampleSize)}`;
    default:
        return `n ~= ${addCommaSeparator(Math.round(sampleSize / nCount))}`;
    }
}

export const getSampleSizeMetaSlice = (sampleSizeMetadata: SampleSizeMetadata, sliceSize: number, startIndex: number): SampleSizeMetadata => {
    const sampleSizeByEntityEntries = Object.entries(sampleSizeMetadata.sampleSizeByEntity);
    const filteredEntries = sampleSizeByEntityEntries.slice(startIndex * sliceSize, (startIndex + 1) * sliceSize);
    const sampleDataForChart = { ...sampleSizeMetadata, sampleSizeByEntity: Object.fromEntries(filteredEntries) } as SampleSizeMetadata;

    return sampleDataForChart;
}