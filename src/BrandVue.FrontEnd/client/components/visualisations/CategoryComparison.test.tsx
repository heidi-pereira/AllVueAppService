import { transformResultsForDisplay } from "./CategoryComparison";
import { CategoryResult } from "../../BrandVueApi";

describe("Check category results are displayed correctly", () => {

    const activeBrandName = "McDonalds";
    const getCategoryResult = (measureName: string, entityInstanceName: string, result: number, averageValue: number | undefined, baseVariableConfigurationId: number | undefined) => {
        return new CategoryResult({
            measureName: measureName,
            entityInstanceName: entityInstanceName,
            result: result,
            averageValue: averageValue,
            baseVariableConfigurationId: baseVariableConfigurationId
        });
    };

    it("Audience results with no overridden bases are parsed correctly", async () => {
        // No basename or baseid defined
        const baseName1 = undefined;
        const baseName2 = undefined;
        const baseVariableId1 = undefined;
        const categoryResults = [
            getCategoryResult("Measure", "Entity 1", 20, 30, undefined),
            getCategoryResult("Measure", "Entity 2", 50, 70, undefined),
            getCategoryResult("Measure2", "Another Entity", 1, 2, undefined),
            getCategoryResult("Measure3", "", 80, 12, undefined),
        ]

        const results = transformResultsForDisplay(categoryResults, baseName1, baseName2, baseVariableId1, activeBrandName);

        expect(results.length).toBe(4);

        expect(results[0].displayName).toBe("Measure: Entity 1");
        expect(results[0].firstBase).toBe(activeBrandName);
        expect(results[0].firstBaseValue).toBe(20);
        expect(results[0].secondBase).toBe("Average");
        expect(results[0].secondBaseValue).toBe(30);

        expect(results[1].displayName).toBe("Measure: Entity 2");
        expect(results[1].firstBase).toBe(activeBrandName);
        expect(results[1].firstBaseValue).toBe(50);
        expect(results[1].secondBase).toBe("Average");
        expect(results[1].secondBaseValue).toBe(70);

        expect(results[2].displayName).toBe("Measure2: Another Entity");
        expect(results[2].firstBase).toBe(activeBrandName);
        expect(results[2].firstBaseValue).toBe(1);
        expect(results[2].secondBase).toBe("Average");
        expect(results[2].secondBaseValue).toBe(2);

        expect(results[3].displayName).toBe("Measure3");
        expect(results[3].firstBase).toBe(activeBrandName);
        expect(results[3].firstBaseValue).toBe(80);
        expect(results[3].secondBase).toBe("Average");
        expect(results[3].secondBaseValue).toBe(12);
    });

    it("Brand vs average results are parsed correctly", async () => {
        // One basename and baseid defined
        const baseName1 = "Base1";
        const baseName2 = undefined;
        const baseVariableId1 = 123;
        const categoryResults = [
            getCategoryResult("Measure", "Entity 1", 20, 30, 123),
            getCategoryResult("Measure", "Entity 2", 50, 70, 123),
            getCategoryResult("Measure2", "Another Entity", 1, 2, 123),
            getCategoryResult("Measure3", "", 80, 12, 123),
        ]

        const results = transformResultsForDisplay(categoryResults, baseName1, baseName2, baseVariableId1, activeBrandName);

        expect(results.length).toBe(4);

        expect(results[0].displayName).toBe("Measure: Entity 1");
        expect(results[0].firstBase).toBe(baseName1);
        expect(results[0].firstBaseValue).toBe(20);
        expect(results[0].secondBase).toBe("Average");
        expect(results[0].secondBaseValue).toBe(30);

        expect(results[1].displayName).toBe("Measure: Entity 2");
        expect(results[1].firstBase).toBe(baseName1);
        expect(results[1].firstBaseValue).toBe(50);
        expect(results[1].secondBase).toBe("Average");
        expect(results[1].secondBaseValue).toBe(70);

        expect(results[2].displayName).toBe("Measure2: Another Entity");
        expect(results[2].firstBase).toBe(baseName1);
        expect(results[2].firstBaseValue).toBe(1);
        expect(results[2].secondBase).toBe("Average");
        expect(results[2].secondBaseValue).toBe(2);

        expect(results[3].displayName).toBe("Measure3");
        expect(results[3].firstBase).toBe(baseName1);
        expect(results[3].firstBaseValue).toBe(80);
        expect(results[3].secondBase).toBe("Average");
        expect(results[3].secondBaseValue).toBe(12);
    });

    it("Brand vs brand results are parsed correctly", async () => {
        // Two basenames and baseids defined, averages ignored
        const baseName1 = "Base1";
        const baseName2 = "Base2";
        const baseVariableId1 = 123;
        const categoryResults = [
            getCategoryResult("Measure", "Entity 1", 20, 30, 123),
            getCategoryResult("Measure", "Entity 1", 90, 16, 567),
            getCategoryResult("Measure", "Entity 2", 50, 70, 123),
            getCategoryResult("Measure", "Entity 2", 1, 2, 567),
            getCategoryResult("Measure2", "Another Entity", 1, 2, 123),
            getCategoryResult("Measure2", "Another Entity", 50, 33, 567),
            getCategoryResult("Measure3", "", 80, 12, 123),
            getCategoryResult("Measure3", "", 66, 4, 567),
        ]

        const results = transformResultsForDisplay(categoryResults, baseName1, baseName2, baseVariableId1, activeBrandName);

        expect(results.length).toBe(4);

        expect(results[0].displayName).toBe("Measure: Entity 1");
        expect(results[0].firstBase).toBe(baseName1);
        expect(results[0].firstBaseValue).toBe(20);
        expect(results[0].secondBase).toBe(baseName2);
        expect(results[0].secondBaseValue).toBe(90);

        expect(results[1].displayName).toBe("Measure: Entity 2");
        expect(results[1].firstBase).toBe(baseName1);
        expect(results[1].firstBaseValue).toBe(50);
        expect(results[1].secondBase).toBe(baseName2);
        expect(results[1].secondBaseValue).toBe(1);

        expect(results[2].displayName).toBe("Measure2: Another Entity");
        expect(results[2].firstBase).toBe(baseName1);
        expect(results[2].firstBaseValue).toBe(1);
        expect(results[2].secondBase).toBe(baseName2);
        expect(results[2].secondBaseValue).toBe(50);

        expect(results[3].displayName).toBe("Measure3");
        expect(results[3].firstBase).toBe(baseName1);
        expect(results[3].firstBaseValue).toBe(80);
        expect(results[3].secondBase).toBe(baseName2);
        expect(results[3].secondBaseValue).toBe(66);
    });
});