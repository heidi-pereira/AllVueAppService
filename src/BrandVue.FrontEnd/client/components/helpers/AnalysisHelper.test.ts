import { ScoreCurrentVsPrevious, orderMetricScoreResults } from "./AnalysisHelper";

const testDataIncreases = (): ScoreCurrentVsPrevious[] => {
    return [
        { instanceName: "testMetricOne", currentScore: 73, previousScore: 67, diff: 6 },
        { instanceName: "testMetricTwo", currentScore: 100, previousScore: 80, diff: 20 },
        { instanceName: "testMetricThree", currentScore: 92, previousScore: 78, diff: 14 }
    ]
}

const testDataDecreases = (): ScoreCurrentVsPrevious[] => {
    return [
        { instanceName: "testMetricOne", currentScore: 67, previousScore: 73, diff: -6 },
        { instanceName: "testMetricTwo", currentScore: 80, previousScore: 100, diff: -20 },
        { instanceName: "testMetricThree", currentScore: 78, previousScore: 92, diff: -14 }
    ]
}

const testDataMixture = (): ScoreCurrentVsPrevious[] => {
    return [
        { instanceName:"testMetricOne", currentScore: 73, previousScore: 67, diff: 6 },
        { instanceName:"testMetricTwo", currentScore: 80, previousScore: 100, diff: -20 },
        { instanceName:"testMetricThree", currentScore: 92, previousScore: 78, diff: 14 },
        { instanceName:"testMetricFour", currentScore: 82, previousScore: 91, diff: -9 }
    ]
}

describe("orderMetricScoreResults", () => {
    it("should return an array sorted in descending order by diff, given an array of good increases", () => {
        const sortedResults = orderMetricScoreResults(testDataIncreases());

        expect(sortedResults?.length).toBe(3);
        expect(sortedResults[0].instanceName).toBe("testMetricTwo");
        expect(sortedResults[1].instanceName).toBe("testMetricThree");
        expect(sortedResults[2].instanceName).toBe("testMetricOne");
    });
    

    it("should return an array sorted in descending order by diff, given an array of bad decreases", () => {
        const sortedResults = orderMetricScoreResults(testDataDecreases());

        expect(sortedResults?.length).toBe(3);
        expect(sortedResults[0].instanceName).toBe("testMetricOne");
        expect(sortedResults[1].instanceName).toBe("testMetricThree");
        expect(sortedResults[2].instanceName).toBe("testMetricTwo");
    });
})