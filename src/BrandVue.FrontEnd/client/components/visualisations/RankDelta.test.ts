import { getDeltaPresentation } from "./RankDelta";

describe("getDeltaPresentation", () => {

    it.each`
        downIsGood
        ${true}
        ${false}
    `("should return neutral, no arrow when delta is zero and downIsGood is $downIsGood",
        ({downIsGood}) => {
                const deltaPresentation = getDeltaPresentation(0, downIsGood);
                expect(deltaPresentation.cssClass).toBe("rank-neutral");
                expect(deltaPresentation.icon).toBe("remove");
            });

    it.each`
        delta
        ${0.1}
        ${0.0001}
        ${100}
        ${5}
    `("should return red arrow down when rank increases and downIsGood is false",
        ({delta}) => {
            const deltaPresentation = getDeltaPresentation(delta, false);
            expect(deltaPresentation.cssClass).toBe("rank-negative");
            expect(deltaPresentation.icon).toBe("arrow_downward");
        });

    it.each`
        delta
        ${0.1}
        ${0.0001}
        ${100}
        ${5}
    `("should return green arrow down when rank increases and downIsGood is true",
        ({delta}) => {
            const deltaPresentation = getDeltaPresentation(delta, true);
            expect(deltaPresentation.cssClass).toBe("rank-positive");
            expect(deltaPresentation.icon).toBe("arrow_downward");
        });

    it.each`
        delta
        ${-0.1}
        ${-0.0001}
        ${-100}
        ${-5}
    `("should return green arrow up when rank decreases and downIsGood is false",
        ({delta}) => {
            const deltaPresentation = getDeltaPresentation(delta, false);
            expect(deltaPresentation.cssClass).toBe("rank-positive");
            expect(deltaPresentation.icon).toBe("arrow_upward");
        });

    it.each`
        delta
        ${-0.1}
        ${-0.0001}
        ${-100}
        ${-5}
    `("should return red arrow up when rank decreases and downIsGood is true",
        ({delta}) => {
            const deltaPresentation = getDeltaPresentation(delta, true);
            expect(deltaPresentation.cssClass).toBe("rank-negative");
            expect(deltaPresentation.icon).toBe("arrow_upward");
        });
});