import { EntitySetAverage } from "./EntitySetAverage";

describe('EntitySetAverage', () => {
    it('appends (competitive average) to the name if it does not contain it', () => {
        var result = EntitySetAverage.getChartDisplayName("test");

        expect(result).toBe("test (competitive average)");
    });

    it('does not append (competitive average) to the name if it already contains it', () => {
        var result = EntitySetAverage.getChartDisplayName("test (competitive average)");

        expect(result).toBe("test (competitive average)");
    });
});