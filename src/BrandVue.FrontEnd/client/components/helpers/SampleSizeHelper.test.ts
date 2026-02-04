import { parseSampleSizeDescription } from "./SampleSizeHelper"

describe("parseSampleSizeDescription", () => {
    it("should return the original string when there is no n = in the input", () => {
        let inputString = "Test sampleSize";

        let result = parseSampleSizeDescription(inputString);

        expect(result).toBe(inputString);
    })

    it("should return the basic n = when there is only one n value", () => {
        let inputString = "Test n = 1";
        let expectedResult = "n = 1";

        let result = parseSampleSizeDescription(inputString);

        expect(result).toBe(expectedResult);
    });

    it("should return a comma-formatted value when n > 1000", () => {
        let inputString = "Test n = 1000";
        let expectedResult = "n = 1,000";

        let result = parseSampleSizeDescription(inputString);

        expect(result).toBe(expectedResult);
    });

    it("should return an average when there are multiple n values", () => {
        let inputString = "Test n = 1; Test2 n = 2; Test 3 n = 3";
        let expectedResult = "n ~= 2";

        let result = parseSampleSizeDescription(inputString);

        expect(result).toBe(expectedResult);
    });

    it("should return a formatted average when there are multiple n values", () => {
        let inputString = "Test n = 1000; Test2 n = 2000; Test 3 n = 3000";
        let expectedResult = "n ~= 2,000";

        let result = parseSampleSizeDescription(inputString);

        expect(result).toBe(expectedResult);
    });

    it("should return a value rounded to an integer", () => {
        let inputString = "Test n = 10; Test2 n = 10; Test 3 n = 5";
        // actual average is 8.333
        let expectedResult = "n ~= 8";

        let result = parseSampleSizeDescription(inputString);

        expect(result).toBe(expectedResult);
    })

    it("should cope with a pre-formatted n number", () => {
        let inputString = "Test n = 1,000 followed by more text";
        let expectedResult = "n = 1,000";

        let result = parseSampleSizeDescription(inputString);

        expect(result).toBe(expectedResult);
    })

    it("should cope with multiple pre-formatted n numbers", () => {
        let inputString = "Test n = 1,000 Test 2 n = 2,000 Test 3 n = 3,000";
        let expectedResult = "n ~= 2,000";

        let result = parseSampleSizeDescription(inputString);

        expect(result).toBe(expectedResult);
    })
});