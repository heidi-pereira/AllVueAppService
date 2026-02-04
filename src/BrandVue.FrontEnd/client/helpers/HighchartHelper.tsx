import { EntityInstance } from "client/BrandVueApi";
import { Metric } from "client/metrics/metric";

const getTwoLineWrappedLabel = (input: string, fontWeight: string, minLength: number) => {
    if (input.length > minLength && input.indexOf(" ") >= 0) {
        const words = input.split(' ');
        let firstLine = words[0];
        let secondLine = words.slice(1).join(' ');

        for (let i = 1; i < words.length - 1; i++) {
            const potentialFirstLine = words.slice(0, i + 1).join(' ');
            const potentialSecondLine = words.slice(i + 1).join(' ');

            if (Math.abs(potentialFirstLine.length - potentialSecondLine.length) <
                Math.abs(firstLine.length - secondLine.length)) {
                firstLine = potentialFirstLine;
                secondLine = potentialSecondLine;
            }
        }
        return `<span style="font-weight:${fontWeight};font-size:0.9em;display:inline-block;line-height:0.9em;text-align:right;">${firstLine}<br/>${secondLine}</span>`;
    }

    return `<span style="font-weight:${fontWeight};font-size:0.9em;">${input}</span>`;
};

/*
 * This will two-line wrap the label text, depending on the
 * number of labels and character length to avoid label overlap
 */
export const getFormattedLabel = (axis: any, labelText: string, fontWeight: string) => {
    const labelCountLimitForTwoLineWrap = 26;
    const multiLineLengthRatio = 2.6;

    const useCategories = axis.categories.length > 0;
    const labels = useCategories ? axis.categories : axis.names;
    const maxLabelLength = Math.max(...(useCategories ? axis.categories.map(c => c.name.length) : axis.names.map(c => c.length)));
    const minLength = maxLabelLength / multiLineLengthRatio;

    return labels.length > labelCountLimitForTwoLineWrap ? getTwoLineWrappedLabel(labelText, fontWeight, minLength) : labelText;
}

export const getMeanCalculationValue = (entityInstance: EntityInstance, metric: Metric) => {
    let meanValue = entityInstance.id.toString();
    if (metric.entityInstanceIdMeanCalculationValueMapping) {
        const mapping = metric.entityInstanceIdMeanCalculationValueMapping.mapping.find(m => m.entityId == entityInstance.id);

        if (mapping) {
            if (!mapping.includeInCalculation) {
                meanValue = "-";
            } else {
                meanValue = mapping.meanCalculationValue.toString();
            }
        }
    }
    return meanValue;
}