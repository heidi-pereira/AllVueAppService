import { MetricFilterState } from "./metricFilterState";

const splitCommaSeparatedStringToNumberArray = (input: string): number[] => {
    return new MetricFilterState().withValues(input, false).values;
}

describe('splitCommaSeparatedStringToNumberArray', () => {
    it('should return an array of numbers when given a comma-separated string of numbers', () => {
        const input = '1,2,3,4,5';
        const expectedOutput = [1, 2, 3, 4, 5];
        expect(splitCommaSeparatedStringToNumberArray(input)).toEqual(expectedOutput);
    });

    it('should return inverted when forcing invert', () => {
        const input = '1,2,3,4,5';
        expect(new MetricFilterState().withValues(input, true).invert).toBeTruthy();
    });
    
    it('should return an empty array when given an empty string', () => {
        const input = '';
        const expectedOutput = [];
        expect(splitCommaSeparatedStringToNumberArray(input)).toEqual(expectedOutput);
    });

    it('should ignore non-numeric values and return an array of valid numbers', () => {
        const input = '1,2,foo,4,bar';
        const expectedOutput = [1, 2, 4];
        expect(splitCommaSeparatedStringToNumberArray(input)).toEqual(expectedOutput);
    });

    it('should return an empty array when given a string with only non-numeric values', () => {
        const input = 'foo,bar,baz';
        const expectedOutput = [];
        expect(splitCommaSeparatedStringToNumberArray(input)).toEqual(expectedOutput);
    });

    it('should handle negative numbers correctly', () => {
        const input = '-1,-2,-3';
        const expectedOutput = [-1, -2, -3];
        expect(splitCommaSeparatedStringToNumberArray(input)).toEqual(expectedOutput);
    });

    it('should handle decimal numbers correctly', () => {
        const input = '1.1,2.2,3.3';
        const expectedOutput = [1.1, 2.2, 3.3];
        expect(splitCommaSeparatedStringToNumberArray(input)).toEqual(expectedOutput);
    });

    it('should handle a mix of integers and decimals', () => {
        const input = '1,2.2,3';
        const expectedOutput = [1, 2.2, 3];
        expect(splitCommaSeparatedStringToNumberArray(input)).toEqual(expectedOutput);
    });

    it('should handle leading and trailing spaces', () => {
        const input = ' 1 , 2 , 3 ';
        const expectedOutput = [1, 2, 3];
        expect(splitCommaSeparatedStringToNumberArray(input)).toEqual(expectedOutput);
    });

    it('should handle multiple commas between numbers', () => {
        const input = '1,,2,,,3';
        const expectedOutput = [1, 2, 3];
        expect(splitCommaSeparatedStringToNumberArray(input)).toEqual(expectedOutput);
    });

    it('should handle a string with only commas', () => {
        const input = ',,,,';
        const expectedOutput = [];
        expect(splitCommaSeparatedStringToNumberArray(input)).toEqual(expectedOutput);
    });
});
